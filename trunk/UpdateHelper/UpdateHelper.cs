using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using wyDay.Controls;

namespace wyUpdate.Common
{
    class UpdateHelper
    {
        PipeServer pipeServer;

        public bool RestartInfoSent;
        public bool Installing;

        public string FileOrServiceToExecuteAfterUpdate;
        public bool IsAService;
        public string ExecutionArguments;
        public string AutoUpdateID;

        public event EventHandler SenderProcessClosed;
        public event RequestHandler RequestReceived;

        Control owner;

        public bool RunningServer
        {
            get
            {
                // is the pipeserver running
                return pipeServer != null && pipeServer.Running;
            }
        }

        public int TotalConnectedClients
        {
            get
            {
                return pipeServer == null ? 0 : pipeServer.TotalConnectedClients;
            }
        }

        public void StartPipeServer(Control OwnerHandle)
        {
            //Note: this function can only be called once. Explosions otherwise.

            owner = OwnerHandle;

            pipeServer = new PipeServer();

            pipeServer.MessageReceived += pipeServer_MessageReceived;
            pipeServer.ClientDisconnected += pipeServer_ClientDisconnected;

            pipeServer.Start(UpdateHelperData.PipenameFromFilename(Application.ExecutablePath));
        }

        void pipeServer_ClientDisconnected()
        {
            try
            {
                // eat any messages after the owner closes (aka IsDisposed)
                if (owner.IsDisposed)
                    return;

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
                // eat any messages after the owner closes (aka IsDisposed)
                if (owner.IsDisposed)
                    return;

                owner.Invoke(new PipeServer.MessageReceivedHandler(ServerReceivedData),
                             new object[] {message});
            }
            catch { }
        }

        void ServerReceivedData(byte[] message)
        {
            UpdateHelperData data = UpdateHelperData.FromByteArray(message);

            if (data.Action == UpdateAction.GetwyUpdateProcessID)
            {
                // send ProcessID
                pipeServer.SendMessage(new UpdateHelperData(UpdateAction.GetwyUpdateProcessID) { ProcessID = Process.GetCurrentProcess().Id }.GetByteArray());
                return;
            }

            UpdateStep step = data.UpdateStep;

            if (step == UpdateStep.RestartInfo)
            {
                RestartInfoSent = true;

                // load the pre-install info
                if (data.ExtraData.Count > 0)
                {
                    FileOrServiceToExecuteAfterUpdate = data.ExtraData[0];
                    IsAService = data.ExtraDataIsRTF[0];
                }

                // load the AutoUpdateID (for writing to file whether the update failed or Succeeded)
                if (data.ExtraData.Count > 1)
                    AutoUpdateID = data.ExtraData[1];

                if (data.ExtraData.Count > 2)
                    ExecutionArguments = data.ExtraData[2];
            }
            else if (step == UpdateStep.Install)
            {
                // if we're already installing, don't bother to process the message again
                if (Installing)
                    return;

                Installing = true;
            }

            if (RequestReceived != null)
                RequestReceived(this, data.Action, step);
        }

        public void SendProgress(int progress, UpdateStep step)
        {
            pipeServer.SendMessage(new UpdateHelperData(Response.Progress, step, progress).GetByteArray());
        }

        public void SendSuccess(string extraData1, string extraData2, bool ed2IsRtf)
        {
            UpdateHelperData uh = new UpdateHelperData(Response.Succeeded, UpdateStep.CheckForUpdate, extraData1, extraData2);

            uh.ExtraDataIsRTF[1] = ed2IsRtf;

            pipeServer.SendMessage(uh.GetByteArray());
        }

        public void SendSuccess(UpdateStep step)
        {
            pipeServer.SendMessage(new UpdateHelperData(Response.Succeeded, step).GetByteArray());
        }

        public void SendSuccess(UpdateStep step, int windowHandle)
        {
            pipeServer.SendMessage(new UpdateHelperData(Response.Succeeded, step) { ProcessID = windowHandle }.GetByteArray());
        }

        public void SendFailed(string messageTitle, string messageBody, UpdateStep step)
        {
            pipeServer.SendMessage(new UpdateHelperData(Response.Failed, step, messageTitle, messageBody).GetByteArray());
        }

        public void SendNewWyUpdate(string pipeName, int processID)
        {
            UpdateHelperData uh = new UpdateHelperData(UpdateAction.NewWyUpdateProcess) { ProcessID = processID };

            uh.ExtraData.Add(pipeName);
            uh.ExtraDataIsRTF.Add(false);

            pipeServer.SendMessage(uh.GetByteArray());
        }
    }

    internal delegate void RequestHandler(object sender, UpdateAction a, UpdateStep s);
}
