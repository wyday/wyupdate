using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using wyDay.Controls;

namespace wyUpdate.Common
{
    class UpdateHelper
    {
        readonly PipeServer pipeServer = new PipeServer();

        public UpdateStep UpdateStep;

        public bool Installing;

        public string FileToExecuteAfterUpdate;
        public string UpdateSuccessArgs;
        public string UpdateFailArgs;


        public event EventHandler SenderProcessClosed;
        public event RequestHandler RequestReceived;


        private Control owner;

        public UpdateHelper(Form OwnerHandle)
        {
            owner = OwnerHandle;

            pipeServer.MessageReceived += pipeServer_MessageReceived;
            pipeServer.ClientDisconnected += pipeServer_ClientDisconnected;

            // get the unique pipe name (the last 246 chars of the complete path)
            string pipeName = Application.ExecutablePath.Replace("\\", "").ToLower();
            int pipeNameL = pipeName.Length;

            pipeServer.Start("\\\\.\\pipe\\" + pipeName.Substring(Math.Max(0, pipeNameL - 246), Math.Min(246, pipeNameL)));
        }

        void pipeServer_ClientDisconnected()
        {
            try
            {
                owner.Invoke(new PipeServer.ClientDisconnectedHandler(ClientDisconnected));
            }
            catch { }
        }

        void ClientDisconnected()
        {
            if (SenderProcessClosed != null && pipeServer.TotalConnectedClients == 0)
                SenderProcessClosed(this, EventArgs.Empty);
        }

        void pipeServer_MessageReceived(byte[] message)
        {
            try
            {
                owner.Invoke(new PipeServer.MessageReceivedHandler(ProcessReceivedData),
                             new object[] {message});
            }
            catch { }
        }

        void ProcessReceivedData(byte[] message)
        {
            // get the data
            UpdateHelperData data = UpdateHelperData.FromByteArray(message);

            UpdateStep = data.UpdateStep;
            
            if (data.Action == Action.GetwyUpdateProcessID)
            {
                // send ProcessID
                pipeServer.SendMessage(new UpdateHelperData(Action.GetwyUpdateProcessID){ProcessID = Process.GetCurrentProcess().Id}.GetByteArray());
                return;
            }

            if (UpdateStep == UpdateStep.RestartInfo)
            {
                // load the pre-install info
                if (data.ExtraData.Count >= 1)
                    FileToExecuteAfterUpdate = data.ExtraData[0];

                if (data.ExtraData.Count >= 2)
                    UpdateSuccessArgs = data.ExtraData[1];

                if (data.ExtraData.Count >= 3)
                    UpdateFailArgs = data.ExtraData[2];
            }
            else if (UpdateStep == UpdateStep.Install)
                Installing = true;

            if (RequestReceived != null)
                RequestReceived(this, data.Action, UpdateStep);
        }

        public void SendProgress(int progress)
        {
            pipeServer.SendMessage(new UpdateHelperData(Response.Progress, UpdateStep, progress).GetByteArray());
        }

        public void SendSuccess(string extraData1, string extraData2, bool ed2IsRtf, List<RichTextBoxLink> links)
        {
            UpdateHelperData uh = new UpdateHelperData(Response.Succeeded, UpdateStep, extraData1, extraData2);

            uh.ExtraDataIsRTF[1] = ed2IsRtf;
            
            uh.LinksData = links;

            pipeServer.SendMessage(uh.GetByteArray());
        }

        public void SendSuccess()
        {
            UpdateHelperData uh = new UpdateHelperData(Response.Succeeded, UpdateStep);

            pipeServer.SendMessage(uh.GetByteArray());
        }

        public void SendFailed(string messageTitle, string messageBody)
        {
            pipeServer.SendMessage(new UpdateHelperData(Response.Failed, UpdateStep, messageTitle, messageBody).GetByteArray());
        }
    }

    internal delegate void RequestHandler(object sender, Action a, UpdateStep s);
}
