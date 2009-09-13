using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using wyDay.Controls;

namespace wyUpdate.Common
{
    class UpdateHelper
    {
        readonly PipeServer pipeServer;
        readonly PipeClient pipeClient;

        public bool Installing;

        public string FileToExecuteAfterUpdate;
        public string AutoUpdateID;

        public event EventHandler SenderProcessClosed;
        public event RequestHandler RequestReceived;


        Control owner;

        string m_PipeName;

        string PipeName
        {
            get
            {
                if(m_PipeName == null)
                {
                    // get the unique pipe name (the last 246 chars of the complete path)
                    string pipeName = Application.ExecutablePath.Replace("\\", "").ToLower();
                    int pipeNameL = pipeName.Length;

                    // store the pipename for communicating with Self-update
                    m_PipeName = "\\\\.\\pipe\\" + pipeName.Substring(Math.Max(0, pipeNameL - 246), Math.Min(246, pipeNameL));
                }

                return m_PipeName;
            }
        }

        PipeServer.Client SelfMaster;

        public UpdateHelper(Control OwnerHandle)
        {
            owner = OwnerHandle;

            pipeServer = new PipeServer();

            pipeServer.MessageReceived += pipeServer_MessageReceived;
            pipeServer.ClientDisconnected += pipeServer_ClientDisconnected;

            pipeServer.Start(PipeName);
        }

        public UpdateHelper(Control OwnerHandle, string pipeName)
        {
            owner = OwnerHandle;

            m_PipeName = pipeName;

            pipeClient = new PipeClient();

            pipeClient.MessageReceived += pipeClient_MessageReceived;
            pipeClient.ServerDisconnected += pipeClient_ServerDisconnected;

            pipeClient.Connect(PipeName);

            if (!pipeClient.Connected)
                ServerDisconnected();
        }

        void pipeClient_ServerDisconnected()
        {
            try
            {
                owner.Invoke(new PipeClient.ServerDisconnectedHandler(ServerDisconnected));
            }
            catch { }
        }

        void ServerDisconnected()
        {
            if (SenderProcessClosed != null)
                SenderProcessClosed(this, EventArgs.Empty);
        }

        void pipeServer_ClientDisconnected(PipeServer.Client client)
        {
            try
            {
                owner.Invoke(new PipeServer.ClientDisconnectedHandler(ClientDisconnected),
                             new object[] {client});
            }
            catch { }
        }

        void ClientDisconnected(PipeServer.Client client)
        {
            // we're no longer piping messages to/from the master
            if (client == SelfMaster)
                SelfMaster = null;

            //TODO: the SelfMaster needs to be notified if all the REAL clients are disconnected (i.e. SelfMaster != null && TotalConnectedClients == 1)
            // or better yet, just close this, and when the SelfMaster detects we've close, have it close.

            if (SenderProcessClosed != null && pipeServer.TotalConnectedClients == 0)
                SenderProcessClosed(this, EventArgs.Empty);
        }

        void pipeClient_MessageReceived(byte[] message)
        {
            try
            {
                owner.Invoke(new PipeClient.MessageReceivedHandler(ClientReceivedData),
                             new object[] { message });
            }
            catch { }
        }

        void ClientReceivedData(byte[] message)
        {
            ProcessMessage(UpdateHelperData.FromByteArray(message));
        }

        void pipeServer_MessageReceived(byte[] message, PipeServer.Client client)
        {
            try
            {
                owner.Invoke(new PipeServer.MessageReceivedHandler(ServerReceivedData),
                             new object[] {message, client});
            }
            catch { }
        }

        void ServerReceivedData(byte[] message, PipeServer.Client client)
        {
            if(SelfMaster != null)
            {
                if(client == SelfMaster)
                {
                    // relay the message to all other clients (exclude SelfMaster)
                    pipeServer.SendMessageExclude(message, client);
                }
                else
                {
                    // send the message to SelfMaster
                    pipeServer.SendMessage(message, client);
                }

                return;
            }

            // get the data
            UpdateHelperData data = UpdateHelperData.FromByteArray(message);

            if (data.Action == Action.wyUpdateSlave)
            {
                // We've been ordered to become a wyUpdate slave - save the Client class.
                // All further communications will be piped through this wyUpdate instance
                // to the "master"
                SelfMaster = client;
                return;
            }
            
            ProcessMessage(data);
        }

        void ProcessMessage(UpdateHelperData data)
        {
            if (data.Action == Action.GetwyUpdateProcessID)
            {
                // send ProcessID
                pipeServer.SendMessage(new UpdateHelperData(Action.GetwyUpdateProcessID) { ProcessID = Process.GetCurrentProcess().Id }.GetByteArray());
                return;
            }

            UpdateStep step = data.UpdateStep;

            if (step == UpdateStep.RestartInfo)
            {
                // load the pre-install info
                if (data.ExtraData.Count > 0)
                    FileToExecuteAfterUpdate = data.ExtraData[0];

                // load the AutoUpdateID (for writing to file whether the update failed or Succeeded)
                if (data.ExtraData.Count > 1)
                    AutoUpdateID = data.ExtraData[1];
            }
            else if (step == UpdateStep.Install)
                Installing = true;

            if (RequestReceived != null)
                RequestReceived(this, data.Action, step);
        }

        public void SendProgress(int progress, UpdateStep step)
        {
            Send(new UpdateHelperData(Response.Progress, step, progress).GetByteArray());
        }

        public void SendSuccess(string extraData1, string extraData2, bool ed2IsRtf, List<RichTextBoxLink> links)
        {
            UpdateHelperData uh = new UpdateHelperData(Response.Succeeded, UpdateStep.CheckForUpdate, extraData1, extraData2);

            uh.ExtraDataIsRTF[1] = ed2IsRtf;
            
            uh.LinksData = links;

            Send(uh.GetByteArray());
        }

        public void SendSuccess(UpdateStep step)
        {
            Send(new UpdateHelperData(Response.Succeeded, step).GetByteArray());
        }

        public void SendFailed(string messageTitle, string messageBody, UpdateStep step)
        {
            Send(new UpdateHelperData(Response.Failed, step, messageTitle, messageBody).GetByteArray());
        }

        void Send(byte[] message)
        {
            if (pipeServer != null)
                pipeServer.SendMessage(message);

            else
                pipeClient.SendMessage(message);
        }
    }

    internal delegate void RequestHandler(object sender, Action a, UpdateStep s);
}
