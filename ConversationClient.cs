using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DrawAndGuessServer
{
    internal class ConversationClient: ConversationBase
    {
        bool isHost = false;
        public string Name { get; set; } = "";
        public bool hostflag = false;
        public ConversationClient(TcpClient Client):base (Client)
        {
            new Thread(new ParameterizedThreadStart(ClientHandler)) { IsBackground = true }.Start();
            Task.Run(() =>
            {
                while (true)
                    if (!isHost)
                    {
                        byte[] Buf2 = new byte[1];
                        MutifidImage(false, ref Buf2);

                        TransHead(ConversationType.ServerData, CommandType.BeginData, (uint)Buf2.Length);
                        StreamL.Write(Buf2);
                        if (hostflag) { 
                            SetAsMain();
                            hostflag = false;
                            continue;
                        }

                    }
            });
        }
        void ClientHandler(object ?Param)
        {
            NetworkStream Stream = Client.GetStream();
            while (true)
            {
                try
                {
                    byte[] HeadData = RecvInsertRange(Stream, Marshal.SizeOf(typeof(DataHead))); 

                    DataHead Head = (DataHead)BytesToStuct(HeadData, typeof(DataHead));
                    if (Head.Rev != 255) continue;
                    ConversationType ct = (ConversationType)Head.ct;
                    CommandType cm = (CommandType)Head.cmt;

                    switch (ct)
                    {
                        case ConversationType.ClientCommand:
                            switch (cm)
                            {
                                case CommandType.SetName:
                                    {
                                        byte[] Buf = RecvInsertRange(Stream, (int)Head.Length);
                                        Name = Encoding.ASCII.GetString(Buf);
                                        Console.WriteLine("Name of " + GetRemoteIP(Client) + " was set as " + Name + ".");
                                        break;
                                    }
                                case CommandType.Test:
                                    {
                                        byte[] Buf = RecvInsertRange(Stream, (int)Head.Length);
                                        Console.WriteLine(Encoding.ASCII.GetString(Buf) + " From:" + GetRemoteIP(Client));
                                        break;
                                    }
                            }
                            break;
                        case ConversationType.ClientData:
                            switch(cm)
                            {
                                case CommandType.Test:
                                    {
                                        byte[] Buf = RecvInsertRange(Stream, (int)Head.Length);
                                        Console.WriteLine(Encoding.ASCII.GetString(Buf) + " From:" + GetRemoteIP(Client));
                                        break;
                                    }
                                case CommandType.EndData:
                                    {
                                        var RecvedData = RecvInsertRange(Stream, (int)Head.Length);
                                        MutifidImage(true, ref RecvedData);
                                        break;
                                    }
                            }
                            break;
                        default:
                            break;
                    }

                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    break;
                }
            }

        }
        public void Test(string VV)
        {
            byte[] DataC = Encoding.UTF8.GetBytes(VV);

            TransHead(ConversationType.ServerCommand, CommandType.Test, (uint)DataC.Length);
            StreamL.Write(DataC);
        }
        public void SetAsMain()
        {
            TransHead(ConversationType.ServerCommand, CommandType.SetName, 1U);
            StreamL.Write(new byte[] { 1 });

            isHost = true;
        }
        public void UnMain()
        {
            TransHead(ConversationType.ServerCommand, CommandType.SetName, 1U);
            StreamL.Write(new byte[] { 0 });

            isHost = false;
        }

        void MutifidImage(bool io, ref byte[] Buf)
        {
            //    ImagingBlockDisposed = false;
            //    ImagingBlock.Enter(ref ImagingBlockDisposed);
            if (io)
            {
                Conversation.BMP = Buf;
            }
            else
            {
                Buf = (byte[])Conversation.BMP.Clone();
            }
            //   ImagingBlock.Exit();
        }
       
    }
}
