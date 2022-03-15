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
    internal class ConversationClient
    {
        bool isHost = false;
        private static SpinLock ImagingBlock = new SpinLock();
        private static bool ImagingBlockDisposed = false;
        public static Socket GetSocket(TcpClient cln)
        {
            Socket s = cln.Client;
            return s;
        }
        public static string GetRemoteIP(TcpClient cln)
        {
            string ip = GetSocket(cln).RemoteEndPoint.ToString().Split(':')[0];
            return ip;
        }
        public static int GetRemotePort(TcpClient cln)
        {
            string temp = GetSocket(cln).RemoteEndPoint.ToString().Split(':')[1];
            int port = Convert.ToInt32(temp);
            return port;
        }
        protected ManualResetEvent MsgRecved = new ManualResetEvent(false);
        public string Name { get; set; } = "";
        readonly byte[] OKReply = new byte[2] { 122, 122 };
        public enum ConversationType
        {
            ServerCommand = 0,
            ServerData = 1,
            ClientCommand = 2,
            ClientData = 3
        }
        public enum CommandType
        {
            SetName = 0,
            BeginData = 1,
            EndData = 2,
            Test = 3
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct DataHead
        {
            public uint Rev;
            public byte ct;
            public byte cmt;
            public uint Length;
        }
        readonly TcpClient Client;
        readonly NetworkStream StreamL;
        public bool Connected { get { return Client.Connected; } }
        byte[] RecvInsertRange(NetworkStream Stream, int Size)
        {
            var RecvedData = new byte[Size];
            int RecvedSize = 0;

            do
            {
                RecvedSize += Stream.Read(RecvedData, RecvedSize, (int)(Size - RecvedSize));
            } while (RecvedSize < Size);
            return RecvedData;
        }
        public bool hostflag = false;
        public ConversationClient(TcpClient Client)
        {
            this.Client = Client;
            StreamL = this.Client.GetStream();
            MsgRecved.Reset();
            new Thread(new ParameterizedThreadStart(ClientHandler)) { IsBackground = true }.Start();
            Task.Run(() =>
            {
                while (true)
                    if (!isHost)
                    {
                        Task.Delay(500);
                        byte[] Buf2 = new byte[1];
                        MutifidImage(false, ref Buf2);

                        TransHead(StreamL, ConversationType.ServerData, CommandType.BeginData, (uint)Buf2.Length);
                        StreamL.Write(Buf2);
                        if (hostflag) { 
                            SetAsMain();
                            hostflag = false;
                            continue;
                        }
                    }
            });
        }
        public void Disconnect()
        {
            Client.Close();
            Client.Dispose();
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

                    if (ct == ConversationType.ClientCommand)
                    {
                        if (cm == CommandType.Test)
                        {
                            byte[] Buf = RecvInsertRange(Stream, (int)Head.Length);

                            Console.WriteLine(Encoding.ASCII.GetString(Buf) + " From:" + GetRemoteIP(Client));

                        }
                        else if (cm == CommandType.SetName)
                        {
                            byte[] Buf = RecvInsertRange(Stream, (int)Head.Length);
                            Name = Encoding.ASCII.GetString(Buf);
                            Console.WriteLine("Name of " + GetRemoteIP(Client) + " was set as " + Name + ".");

                        }
                        else if (cm == CommandType.EndData)
                        {
                            var RecvedData = RecvInsertRange(Stream, (int)Head.Length);

                            MutifidImage(true, ref RecvedData);
                        }
                    }
                    else if (ct == ConversationType.ClientData)
                    {
                        if (cm == CommandType.Test)
                        {
                            byte[] Buf = RecvInsertRange(Stream, (int)Head.Length);

                            Console.WriteLine(Encoding.ASCII.GetString(Buf) + " From:" + GetRemoteIP(Client));
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    break;
                }
                MsgRecved.Set();
            }

        }
        public void Test(string VV)
        {
            byte[] DataC = Encoding.UTF8.GetBytes(VV);

            TransHead(StreamL, ConversationType.ServerCommand, CommandType.Test, (uint)DataC.Length);
            StreamL.Write(DataC);
        }
        public void SetAsMain()
        {
            TransHead(StreamL, ConversationType.ServerCommand, CommandType.SetName, 1U);
            StreamL.Write(new byte[] { 1 });

            isHost = true;
        }

        public void UnMain()
        {
            TransHead(StreamL, ConversationType.ServerCommand, CommandType.SetName, 1U);
            StreamL.Write(new byte[] { 0 });

            isHost = false;
        }
        void WaitForMsg()
        {
            MsgRecved.WaitOne();
            MsgRecved.Reset();
        }

        void MutifidImage(bool io,ref byte [] Buf)
        {
            ImagingBlockDisposed = false;
            ImagingBlock.Enter(ref ImagingBlockDisposed);
            if (io)
            {
                try
                {
                    var newbmp = ImagingMedu.GetBitmap(Buf);

                    Conversation.BMP.Dispose();
                    Conversation.BMP = newbmp;
                }
                catch (Exception)
                {

                }

            }
            else
            {
                Buf = Conversation.BMP.CompressionImage(35);
            }
            ImagingBlock.Exit();
        }
        #region MMN
        public void TransHead(NetworkStream Stream, ConversationType ct, CommandType cm, uint Length)
        {
            DataHead Dt = new DataHead() { ct = (byte)ct, cmt = (byte)cm, Rev = 0xff, Length = Length };
            var Buffer = StructToBytes(Dt);

            Stream.Write(Buffer, 0, Buffer.Length);
        }
        //// <summary>
        /// 结构体转byte数组
        /// </summary>
        /// <param name="structObj">要转换的结构体</param>
        /// <returns>转换后的byte数组</returns>
        public static byte[] StructToBytes(object structObj)
        {
            //得到结构体的大小
            int size = Marshal.SizeOf(structObj);
            //创建byte数组
            byte[] bytes = new byte[size];
            //分配结构体大小的内存空间
            IntPtr structPtr = Marshal.AllocHGlobal(size);
            //将结构体拷到分配好的内存空间
            Marshal.StructureToPtr(structObj, structPtr, false);
            //从内存空间拷到byte数组
            Marshal.Copy(structPtr, bytes, 0, size);
            //释放内存空间
            Marshal.FreeHGlobal(structPtr);
            //返回byte数组
            return bytes;
        }
        /// <summary>
        /// byte数组转结构体
        /// </summary>
        /// <param name="bytes">byte数组</param>
        /// <param name="type">结构体类型</param>
        /// <returns>转换后的结构体</returns>
        public static object BytesToStuct(byte[] bytes, Type type)
        {
            //得到结构体的大小
            int size = Marshal.SizeOf(type);
            //byte数组长度小于结构体的大小
            if (size > bytes.Length)
            {
                //返回空
                return null;
            }
            //分配结构体大小的内存空间
            IntPtr structPtr = Marshal.AllocHGlobal(size);
            //将byte数组拷到分配好的内存空间
            Marshal.Copy(bytes, 0, structPtr, size);
            //将内存空间转换为目标结构体
            object obj = Marshal.PtrToStructure(structPtr, type);
            //释放内存空间
            Marshal.FreeHGlobal(structPtr);
            //返回结构体
            return obj;
        }
        #endregion
    }
}
