using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using MyData;
using System.Threading;

namespace Chitchat
{

    class Server
    {
        public static string GetIp4Address()
        {
            IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());
            foreach (IPAddress i in ips)
            {
                if (i.AddressFamily == AddressFamily.InterNetwork)
                    return i.ToString();

            }
            return "127.0.0.1";
        }
        static Socket Listener;
        static List<ClientData> Clients;
        static void Main(string[] args)
        {
            Console.WriteLine("Initializing on " + GetIp4Address() + " (ip required for clients to connect) ");
            Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Clients = new List<ClientData>();
            IPEndPoint Ipend = new IPEndPoint(IPAddress.Parse(GetIp4Address()), 111); 
            Listener.Bind(Ipend);//bind soketa
            Thread KeepListening = new Thread(listen);
            KeepListening.Start();
            Console.WriteLine("i'm listening for clients");
            Thread Logger = new Thread(overview);
            Logger.Start();
        }
        static void overview()
        {
            while (true)
            {
                for (int i = 0; i < Clients.Count; i++)
                {
                    if (Clients[i].ClientSocket.Connected == true && Clients[i].Logged == true)
                    {
                        Console.WriteLine(Clients[i].Login + " is on");
                        Clients[i].Logged = true;
                    }
                    else
                    {
                        Console.WriteLine(Clients[i].Login + " is off");
                        Clients[i].Logged = false;
                        if (Clients[i].ClientSocket.Connected == false)
                        {
                            Clients[i].ClientSocket.Close();
                            Clients[i].CThread.Abort();
                        }
                    }
                }
                Thread.Sleep(1000);
            }
        }
        static void listen()
        {
            while (true)
            {
                Listener.Listen(0);
                Clients.Add(new ClientData(Listener.Accept()));

            }
        }

        public static void Input(object cSocket)
        {
            Socket ClientSocket = (Socket)cSocket;
            byte[] Buffer;
            int Read;
            while (true)
            {
                try
                {
                    Buffer = new byte[ClientSocket.SendBufferSize];
                    Read = ClientSocket.Receive(Buffer);
                    if (Read > 0)
                    {
                        Data d = new Data(Buffer);
                        ManageData(d);
                    }
                }
                catch (SocketException ex)
                {
                    break;
                }
            }


        }
        public static void ManageData(Data ClientMessage)
        {
            switch (ClientMessage.Type)
            {
                case "regular":
                    var WordsArray = ClientMessage.data[0].Split();
                    string Command = WordsArray[0];
                    string SenderLogin = "";
                    string Time = DateTime.Now.ToString("h:mm:ss tt");
                    string MsgText = "";

                    switch (Command)
                    {
                        case "/all":


                            foreach (ClientData Client in Clients)
                            {
                                if (Client.Id == ClientMessage.SenderID)
                                {
                                    SenderLogin = Client.Login;
                                }

                            }
                            for (int i = 1; i < WordsArray.Count(); i++)
                            {
                                MsgText += WordsArray[i] + " ";
                            }

                            foreach (ClientData Client in Clients)
                            {
                                if (Client.Id != ClientMessage.SenderID)
                                {
                                    Client.ClientSocket.Send(ClientMessage.ToBytes());
                                    MsgText = "[" + Time + SenderLogin + "]:" + MsgText;
                                    Data Broadcast = new Data("", "regular");
                                    Broadcast.data.Add(MsgText);
                                    Client.ClientSocket.Send(Broadcast.ToBytes());
                                }
                            }
                            break;
                        case "/showall":
                            string PeopleOnline = "";
                            foreach (ClientData Client in Clients)
                            {
                                if (Client.Logged == true)
                                    PeopleOnline += Client.Login + " ";
                            }

                            foreach (ClientData Client in Clients)
                            {
                                if (Client.Id == ClientMessage.SenderID)
                                {
                                    Data ServerResponse = new Data("", "regular");
                                    ServerResponse.data.Add(PeopleOnline);
                                    Client.ClientSocket.Send(ServerResponse.ToBytes());
                                }

                            }
                            break;
                        case "/w":
                            bool Found = false;
                            string TargetPerson = WordsArray[1];
                            foreach (ClientData Client in Clients)
                            {
                                if (Client.Id == ClientMessage.SenderID)
                                {
                                    SenderLogin = Client.Login;
                                }

                            }
                            for (int i = 2; i < WordsArray.Count(); i++)
                            {
                                MsgText += WordsArray[i] + " ";
                            }
                            foreach (ClientData Client in Clients)
                            {
                                if (Client.Login == TargetPerson)
                                {
                                    Found = true;
                                    MsgText = "[" + Time + SenderLogin + "]:" + MsgText;

                                    Data Whisper = new Data("", "regular");
                                    Whisper.data.Add(MsgText);
                                    Client.ClientSocket.Send(Whisper.ToBytes());
                                }

                            }
                            if (Found == false)
                            {
                                Data NotAvailServerResponse = new Data("", "regular");
                                NotAvailServerResponse.data.Add(TargetPerson + " is not avail");
                                foreach (ClientData Client in Clients)
                                {
                                    if (Client.Id == ClientMessage.SenderID)
                                    {
                                        Client.ClientSocket.Send(NotAvailServerResponse.ToBytes());
                                    }

                                }
                            }
                            break;
                    }

                    break;
                case "login":
                    bool Success = false;
                    for (int i = 0; i < Clients.Count; i++)
                    {
                        if (Clients[i].Login == ClientMessage.Login)
                        {
                            if (Clients[i].Pass == ClientMessage.Pass)
                            {


                                for (int k = 0; k < Clients.Count; k++)
                                {
                                    if (Clients[k].Id == ClientMessage.SenderID)
                                    {
                                        Data ServerConfirmation = new Data("", "confirmlogin");
                                        ServerConfirmation.data.Add("u r registered and logged in. Hello! /showall - shows every1 whos online, /all <your msg here> - sends broadcast msg, /w <username> <msg> - sends msg to certain user");
                                        Clients[i].ClientSocket = Clients[k].ClientSocket;
                                        Clients[i].ClientSocket.Send(ServerConfirmation.ToBytes());
                                        Clients[i].Id = ClientMessage.SenderID;
                                        Clients.RemoveAt(k);
                                        Success = true;
                                        Clients[i].Logged = true;
                                    }
                                }
                            }
                        }
                    }
                    if (Success == false)
                    {
                        for (int i = 0; i < Clients.Count; i++)
                        {
                            if (Clients[i].Id == ClientMessage.SenderID)
                            {



                                Data ServerRefusal = new Data("", "refused");
                                ServerRefusal.data.Add("refused");
                                Clients[i].ClientSocket.Send(ServerRefusal.ToBytes());
                            }
                        }
                    }

                    break;
                case "register":
                    bool Taken = false;
                    for (int i = 0; i < Clients.Count; i++)
                    {
                        if (Clients[i].Login == ClientMessage.Login)
                        {
                            Taken = true;

                        }
                    }
                    if (Taken == false)
                    {
                        for (int i = 0; i < Clients.Count; i++)
                        {
                            if (Clients[i].Id == ClientMessage.SenderID)
                            {
                                Clients[i].Logged = true;
                                Clients[i].Registered = true;
                                Clients[i].Login = ClientMessage.Login;
                                Clients[i].Pass = ClientMessage.Pass;
                                Data ServerConfirmation = new Data("", "confirmreg");
                                ServerConfirmation.data.Add("u r registered and logged in. Hello! /showall - shows every1 whos online, /all <your msg here> - sends broadcast msg, /w <username> <msg> - sends msg to certain user");
                                Clients[i].ClientSocket.Send(ServerConfirmation.ToBytes());
                            }
                        }
                    }
                    else
                    {
                        for (int i = 0; i < Clients.Count; i++)
                        {
                            if (Clients[i].Id == ClientMessage.SenderID)
                            {



                                Data ServerRefusal = new Data("", "refused");
                                ServerRefusal.data.Add("refused");
                                Clients[i].ClientSocket.Send(ServerRefusal.ToBytes());
                            }
                        }
                    }
                    break;

            }
        }



    }
}
