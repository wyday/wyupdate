using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using wyDay.Controls;

namespace wyUpdate.Common
{
    class UpdateHelper
    {
        readonly PipeServer pipeServer = new PipeServer();

        public UpdateStep UpdateStep;


        public bool PreInstallInfoSent;

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
            string pipeName = System.Reflection.Assembly.GetExecutingAssembly().Location.Replace("\\", "").ToLower();
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

            if (UpdateStep == UpdateStep.PreInstallInfo)
            {
                PreInstallInfoSent = true;

                // load the pre-install info
                if (data.ExtraData.Count >= 1)
                    FileToExecuteAfterUpdate = data.ExtraData[0];

                if (data.ExtraData.Count >= 2)
                    UpdateSuccessArgs = data.ExtraData[1];

                if (data.ExtraData.Count >= 3)
                    UpdateFailArgs = data.ExtraData[2];
            }
            else if (UpdateStep == UpdateStep.GetwyUpdateProcessID)
            {
                // send ProcessID
                pipeServer.SendMessage(new UpdateHelperData(Response.Succeeded, UpdateStep){ProcessID = Process.GetCurrentProcess().Id}.GetByteArray());
                return;
            }

            if (RequestReceived != null)
                RequestReceived(this, UpdateStep);
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


    internal class UpdateHelperData
    {
        public Action Action;
        public UpdateStep UpdateStep;


        public List<string> ExtraData = new List<string>();
        public List<bool> ExtraDataIsRTF = new List<bool>();



        public List<RichTextBoxLink> LinksData;


        public Response ResponseType = Response.Nothing;
        public int Progress = -1;


        public int ProcessID;


        public UpdateHelperData() { }

        public UpdateHelperData(UpdateStep step)
        {
            UpdateStep = step;
        }

        public UpdateHelperData(UpdateStep step, string extraData)
        {
            UpdateStep = step;

            ExtraData.Add(extraData);
            ExtraDataIsRTF.Add(false);
        }

        public UpdateHelperData(Response responseType, UpdateStep step)
        {
            ResponseType = responseType;
            UpdateStep = step;
        }

        public UpdateHelperData(Response responseType, UpdateStep step, int progress)
            : this(responseType, step)
        {
            Progress = progress;
        }

        public UpdateHelperData(Response responseType, UpdateStep step, string messageTitle, string messageBody)
            : this(responseType, step)
        {
            ExtraData.Add(messageTitle);
            ExtraData.Add(messageBody);

            ExtraDataIsRTF.Add(false);
            ExtraDataIsRTF.Add(false);
        }


        public byte[] GetByteArray()
        {
            MemoryStream ms = new MemoryStream();

            WriteFiles.WriteInt(ms, 0x01, (int)Action);

            // what update step are we on?
            WriteFiles.WriteInt(ms, 0x02, (int)UpdateStep);

            // write extra string data
            for (int i = 0; i < ExtraData.Count; i++)
            {
                if (!string.IsNullOrEmpty(ExtraData[i]))
                {
                    if (ExtraDataIsRTF[i])
                        ms.WriteByte(0x80);

                    WriteFiles.WriteString(ms, 0x03, ExtraData[i]);
                }
            }


            if (LinksData != null)
            {
                foreach (RichTextBoxLink link in LinksData)
                {
                    WriteFiles.WriteInt(ms, 0x07, link.StartIndex);
                    WriteFiles.WriteInt(ms, 0x08, link.Length);
                    WriteFiles.WriteString(ms, 0x09, link.LinkTarget);
                }
            }

            if (ProcessID != 0)
                WriteFiles.WriteInt(ms, 0x04, ProcessID);

            if (Progress > -1 && Progress <= 100)
                WriteFiles.WriteInt(ms, 0x05, Progress);

            if (ResponseType != Response.Nothing)
                WriteFiles.WriteInt(ms, 0x06, (int)ResponseType);

            ms.WriteByte(0xFF);

            byte[] arr = ms.ToArray();

            ms.Close();

            return arr;
        }

        public static UpdateHelperData FromByteArray(byte[] data)
        {
            UpdateHelperData uhData = new UpdateHelperData();

            MemoryStream ms = new MemoryStream(data);

            byte bType = (byte)ms.ReadByte();

            //read until the end byte is detected
            while (!ReadFiles.ReachedEndByte(ms, bType, 0xFF))
            {
                switch (bType)
                {
                    case 0x01:
                        uhData.Action = (Action)ReadFiles.ReadInt(ms);
                        break;
                    case 0x02: // update step we're on
                        uhData.UpdateStep = (UpdateStep)ReadFiles.ReadInt(ms);
                        break;
                    case 0x80:
                        uhData.ExtraDataIsRTF.Add(true);
                        break;
                    case 0x03: // extra data

                        uhData.ExtraData.Add(ReadFiles.ReadString(ms));

                        // keep the 'ExtraDataIsRTF' same length as ExtraData
                        if (uhData.ExtraDataIsRTF.Count != uhData.ExtraData.Count)
                            uhData.ExtraDataIsRTF.Add(false);

                        break;
                    case 0x04:
                        uhData.ProcessID = ReadFiles.ReadInt(ms);
                        break;
                    case 0x05:
                        uhData.Progress = ReadFiles.ReadInt(ms);
                        break;
                    case 0x06:
                        uhData.ResponseType = (Response)ReadFiles.ReadInt(ms);
                        break;

                    case 0x07:

                        if (uhData.LinksData == null)
                            uhData.LinksData = new List<RichTextBoxLink>();

                        uhData.LinksData.Add(new RichTextBoxLink(ReadFiles.ReadInt(ms)));

                        break;
                    case 0x08:
                        uhData.LinksData[uhData.LinksData.Count - 1].Length = ReadFiles.ReadInt(ms);
                        break;
                    case 0x09:
                        uhData.LinksData[uhData.LinksData.Count - 1].LinkTarget = ReadFiles.ReadString(ms);
                        break;

                    default:
                        ReadFiles.SkipField(ms, bType);
                        break;
                }

                bType = (byte)ms.ReadByte();
            }


            ms.Close();

            return uhData;
        }
    }

    internal enum Action { UpdateStep = 0, GetwyUpdateProcessID = 1, Cancel = 2 }
    internal enum UpdateStep { CheckForUpdate = 0, DownloadUpdate = 1, BeginExtraction = 2, PreInstallInfo = 3, Install = 4, GetwyUpdateProcessID = 5 }
    internal enum Response { Failed = -1, Nothing = 0, Succeeded = 1, Progress = 2 }
    internal delegate void RequestHandler(object sender, UpdateStep e);
}
