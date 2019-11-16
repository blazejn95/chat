using System;
using System.Threading;
using System.Net.Sockets;
using MyData;

namespace Chitchat
{
    class ClientData
    {
        public Socket ClientSocket;
        public Thread CThread;
        public string Id;
        public string Pass;
        public string Login;
        public bool Logged;
        public bool Registered;

        public ClientData(Socket Soc)
        {
            ClientSocket = Soc;
            Id = Guid.NewGuid().ToString();
            CThread = new Thread(Server.Input);
            CThread.Start(ClientSocket);
            Login = "DefaultNewFriend";
            Pass = null;
            Logged = false;
            Registered = false;
            Data registr = new Data(Id, "registration");
            Soc.Send(registr.ToBytes());


        }
    }
}
