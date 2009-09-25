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

        public UpdateHelper(Control OwnerHandle)
        {
            owner = OwnerHandle;

            pipeServer = new PipeServer();

            pipeServer.MessageReceived += pipeServer_MessageReceived;
            pipeServer.ClientDisconnected += pipeServer_ClientDisconnected;

            pipeServer.Start(PipeName);
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
            //TODO: the SelfMaster needs to be notified if all the REAL clients are disconnected (i.e. SelfMaster != null && TotalConnectedClients == 1)
            // or better yet, just close this, and when the SelfMaster detects we've close, have it close.

            if (SenderProcessClosed != null && pipeServer.TotalConnectedClients == 0)
                SenderProcessClosed(this, EventArgs.Empty);
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
            ProcessMessage(UpdateHelperData.FromByteArray(message));
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
            pipeServer.SendMessage(new UpdateHelperData(Response.Progress, step, progress).GetByteArray());
        }

        public void SendSuccess(string extraData1, string extraData2, bool ed2IsRtf, List<RichTextBoxLink> links)
        {
            UpdateHelperData uh = new UpdateHelperData(Response.Succeeded, UpdateStep.CheckForUpdate, extraData1, extraData2);

            uh.ExtraDataIsRTF[1] = ed2IsRtf;
            
            uh.LinksData = links;

            pipeServer.SendMessage(uh.GetByteArray());
        }

        public void SendSuccess(UpdateStep step)
        {
            pipeServer.SendMessage(new UpdateHelperData(Response.Succeeded, step).GetByteArray());
        }

        public void SendFailed(string messageTitle, string messageBody, UpdateStep step)
        {
            pipeServer.SendMessage(new UpdateHelperData(Response.Failed, step, messageTitle, messageBody).GetByteArray());
        }
    }

    internal delegate void RequestHandler(object sender, Action a, UpdateStep s);
}
