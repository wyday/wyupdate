using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace wyUpdate
{
    /// <summary>
    /// Allow pipe communication between a server and a client
    /// </summary>
    public class PipeServer
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern SafeFileHandle CreateNamedPipe(
           String pipeName,
           uint dwOpenMode,
           uint dwPipeMode,
           uint nMaxInstances,
           uint nOutBufferSize,
           uint nInBufferSize,
           uint nDefaultTimeOut,
           IntPtr lpSecurityAttributes);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern int ConnectNamedPipe(
           SafeFileHandle hNamedPipe,
           IntPtr lpOverlapped);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool DisconnectNamedPipe(
            SafeFileHandle hHandle);


        public class Client
        {
            public SafeFileHandle handle;
            public FileStream stream;
        }

        /// <summary>
        /// Handles messages received from a client pipe
        /// </summary>
        /// <param name="message">The byte message received</param>
        /// <param name="client">The client that sent the message</param>
        public delegate void MessageReceivedHandler(byte[] message, Client client);

        /// <summary>
        /// Event is called whenever a message is received from a client pipe
        /// </summary>
        public event MessageReceivedHandler MessageReceived;


        /// <summary>
        /// Handles client disconnected messages
        /// </summary>
        public delegate void ClientDisconnectedHandler(Client client);

        /// <summary>
        /// Event is called when a client pipe is severed.
        /// </summary>
        public event ClientDisconnectedHandler ClientDisconnected;

        const int BUFFER_SIZE = 4096;

        Thread listenThread;
        readonly List<Client> clients = new List<Client>();

        /// <summary>
        /// The total number of PipeClients connected to this server
        /// </summary>
        public int TotalConnectedClients
        {
            get
            {
                lock (clients)
                    return clients.Count;
            }
        }

        /// <summary>
        /// The name of the pipe this server is connected to
        /// </summary>
        public string PipeName { get; private set; }

        /// <summary>
        /// Is the server currently running
        /// </summary>
        public bool Running { get; private set; }


        /// <summary>
        /// Starts the pipe server on a particular name.
        /// </summary>
        /// <param name="pipename">The name of the pipe.</param>
        public void Start(string pipename)
        {
            PipeName = pipename;

            //start the listening thread
            listenThread = new Thread(ListenForClients)
                               {
                                   IsBackground = true
                               };

            listenThread.Start();

            Running = true;
        }

        private void ListenForClients()
        {
            while (true)
            {
                SafeFileHandle clientHandle =
                    CreateNamedPipe(
                         PipeName,

                         // DUPLEX | FILE_FLAG_OVERLAPPED = 0x00000003 | 0x40000000;
                         0x40000003,
                         0,
                         255,
                         BUFFER_SIZE,
                         BUFFER_SIZE,
                         0,
                         IntPtr.Zero);

                //could not create named pipe
                if (clientHandle.IsInvalid)
                    continue;

                int success = ConnectNamedPipe(clientHandle, IntPtr.Zero);

                //could not connect client
                if (success == 0)
                {
                    // close the handle, and wait for the next client
                    clientHandle.Close();
                    continue;
                }

                Client client = new Client {handle = clientHandle};

                lock (clients)
                    clients.Add(client);

                Thread readThread = new Thread(Read)
                                        {
                                            IsBackground = true
                                        };
                readThread.Start(client);
            }
        }

        private void Read(object clientObj)
        {
            Client client = (Client)clientObj;
            client.stream = new FileStream(client.handle, FileAccess.ReadWrite, BUFFER_SIZE, true);
            byte[] buffer = new byte[BUFFER_SIZE];

            while (true)
            {
                int bytesRead = 0;

                using (MemoryStream ms = new MemoryStream())
                {
                    try
                    {
                        // read the total stream length
                        int totalSize = client.stream.Read(buffer, 0, 4);

                        // client has disconnected
                        if (totalSize == 0)
                            break;

                        totalSize = BitConverter.ToInt32(buffer, 0);

                        do
                        {
                            int numBytes = client.stream.Read(buffer, 0, Math.Min(totalSize - bytesRead, BUFFER_SIZE));

                            ms.Write(buffer, 0, numBytes);

                            bytesRead += numBytes;

                        } while (bytesRead < totalSize);

                    }
                    catch
                    {
                        //read error has occurred
                        break;
                    }

                    //client has disconnected
                    if (bytesRead == 0)
                        break;

                    //fire message received event
                    if (MessageReceived != null)
                        MessageReceived(ms.ToArray(), client);
                }
            }

            //clean up resources
            DisconnectNamedPipe(client.handle);
            client.stream.Close();
            client.handle.Close();
            lock (clients)
                clients.Remove(client);

            // invoke the event, a client disconnected
            if (ClientDisconnected != null)
                ClientDisconnected(client);
        }

        /// <summary>
        /// Sends a message to all connected clients.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public void SendMessage(byte[] message)
        {
            lock (clients)
            {
                //get the entire stream length
                byte[] messageLength = BitConverter.GetBytes(message.Length);

                foreach (Client client in clients)
                {
                    // length
                    client.stream.Write(messageLength, 0, 4);

                    // data
                    client.stream.Write(message, 0, message.Length);
                    client.stream.Flush();
                }
            }
        }

        public void SendMessageExclude(byte[] message, Client clientToExclude)
        {
            lock (clients)
            {
                //get the entire stream length
                byte[] messageLength = BitConverter.GetBytes(message.Length);

                foreach (Client client in clients)
                {
                    if (client == clientToExclude)
                        continue;

                    // length
                    client.stream.Write(messageLength, 0, 4);

                    // data
                    client.stream.Write(message, 0, message.Length);
                    client.stream.Flush();
                }
            }
        }

        /// <summary>
        /// Sends a message to a single connected client.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="client">The client to send the message to.</param>
        public void SendMessage(byte[] message, Client client)
        {
            // length
            client.stream.Write(BitConverter.GetBytes(message.Length), 0, 4);

            // data
            client.stream.Write(message, 0, message.Length);
            client.stream.Flush();
        }
    }
}