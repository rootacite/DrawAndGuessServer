using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DrawAndGuessServer
{
    internal class Conversation
    {
        static public byte [] BMP = null;
        public string DrawingHost { get; set; }
        readonly short Port = 25535;
        readonly TcpListener Server;
        public List<ConversationClient> Conversations = new List<ConversationClient>();

        public Conversation()
        {
            Server = new TcpListener(new IPEndPoint(IPAddress.Any, Port));
            
            Server.Start();
        }

        ~Conversation()
        {
            Server.Stop();
            foreach(var i in Conversations)
            {
                if(i.Connected)
                {
                    i.Disconnect();
                }
            }
        }
        async public Task Start()
        {
            await Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        var Client = Server.AcceptTcpClient();
                        Conversations.Add(new ConversationClient(Client));
                    }
                    catch (Exception)
                    {
                        break;
                    }

                }
            });

        }
     
    }
}
