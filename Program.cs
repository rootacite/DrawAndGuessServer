// See https://aka.ms/new-console-template for more information
using DrawAndGuessServer;

Conversation Server = new Conversation();
Server.Start();

while(true)
{
    string Data = Console.ReadLine();
    Server.Conversations[0].Test(Data);
}

