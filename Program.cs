// See https://aka.ms/new-console-template for more information
using DrawAndGuessServer;
using System.Drawing;

Conversation Server = new Conversation();
Server.Start();


Conversation.BMP = new Bitmap("koev09e.bmp").CompressionImage(50);

while(true)
{
    string Data = Console.ReadLine();
    if(Data=="Host1")
    {
        Server.Conversations[0].hostflag = true;
        continue;
    }
    if (Data == "UnHost1")
    {
        Server.Conversations[0].UnMain();
        continue;
    }

    if (Data == "Host2")
    {
        Server.Conversations[1].hostflag = true;
        continue;
    }
    if (Data == "UnHost2")
    {
        Server.Conversations[1].UnMain();
        continue;
    }
    if(Data=="clean")
    {
        List<ConversationClient> Conversations = new List<ConversationClient>();
        for(int i=0;i< Server.Conversations.Count;i++)
        {
            if (!Server.Conversations[i].Connected) Conversations.Add(Server.Conversations[i]);
        }

        foreach(var i in Conversations)
        {
            Server.Conversations.Remove(i);
        }
    }
}

