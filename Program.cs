// See https://aka.ms/new-console-template for more information
using DrawAndGuessServer;
using System.Drawing;

Conversation Server = new Conversation();
Server.Start();


Conversation.BMP = new Bitmap("koev09e.bmp");

while(true)
{
    string Data = Console.ReadLine();
    if(Data=="Host")
    {
        Server.Conversations[0].hostflag = true;
        continue;
    }
    if (Data == "UnHost")
    {
        Server.Conversations[0].UnMain();
        continue;
    }
    if(Data=="Draw")
    {
        var g = Graphics.FromImage(Conversation.BMP);
        g.DrawLine(new Pen(new SolidBrush(Color.Blue), 3f), 0, 0, new Random().Next(0, 1280), new Random().Next(0, 720));
        g.Dispose();
        continue;
    }
    Server.Conversations[0].Test(Data);
}

