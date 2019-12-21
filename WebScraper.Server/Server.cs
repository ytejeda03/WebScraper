using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using WebScraper.Packets;
using System.Diagnostics;

namespace WebScraper.Server
{
    class Server
    {
        static Socket taskListener;
        static Socket clientListener;
        static List<ClientData> connectedServers = new List<ClientData>();
        static List<ClientData> connectedClients;
        static bool clientConnected = false;
        static Socket listenerSocketServer;
        static bool iAmTheBoot = true;
        static int portUDP = 8001;
        static int portTCP = 8002;
        static int clientsPort = 8000;
        static int taskPort = 10001;
        static int MAXServers = 100;
        static int myID;
        static Tuple<Socket, string, int>[] serversList = new Tuple<Socket, string, int>[MAXServers];
        static int currentTasks = 0;

        static void Main(string[] args)
        {
            JoinToChord();

            Thread serverListenerT = new Thread(listenUdp);
            serverListenerT.Start();

            Console.WriteLine("Comenzando servidor en " + Packet.GetIp4Address() + ":" + clientsPort.ToString() + "Con ID: " + "{0}",myID.ToString());

            taskListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ipT = new IPEndPoint(IPAddress.Parse(Packet.GetIp4Address()), taskPort);
            taskListener.Bind(ipT);

            Thread newT = new Thread(ListenTask);
            newT.Start();

            clientListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            connectedClients = new List<ClientData>();

            IPEndPoint ip = new IPEndPoint(IPAddress.Parse(Packet.GetIp4Address()), clientsPort);

            clientListener.Bind(ip);

            Thread listenThread = new Thread(ListenThread);

            listenThread.Start();

        }

        private static void listenUdp()
        {
            UdpClient serverListener = new UdpClient();
            serverListener.Client.Bind(new IPEndPoint(IPAddress.Any, portUDP));

            var from = new IPEndPoint(0, 0);
            while (true)
            {
                byte[] recvBuffer = serverListener.Receive(ref from);
                Packet p = new Packet(recvBuffer);
                if(p.packetType == PacketType.Join)
                {
                    Console.WriteLine(p.packetData[0] + " quiere unirse a la red de servidores");
                    Thread.Sleep(1000);
                    Thread t = new Thread(connectToNewServer);
                    t.Start(p.packetData[0]);
                    Thread.Sleep(2000);
                    if (!clientConnected)
                    {
                        try
                        {
                            t.Suspend();
                        }
                        catch { }
                        
                    }
                }
            }
        }

        private static void connectToNewServer(object cIP)
        {
            string ip = (string)cIP;

            Socket newServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ipE = new IPEndPoint(IPAddress.Parse(ip), portTCP);

            try
            {
                newServerSocket.Connect(ipE);
                clientConnected = true;

                int id = -1;

                for(int i = 0; i < MAXServers; ++i)
                {
                    if(serversList[i] == null)
                    {
                        id = i;
                        break;
                    }
                }

                if(id == -1)
                {
                    Console.WriteLine(ip + " no se ha podido unir a la red de servidores porque ya se ha alcanzado el maximo de servidores");
                }
                else
                {
                    string myid = myID.ToString();
                    Packet p = new Packet(PacketType.JoinResponse, myid);
                    for(int i = 0; i < MAXServers; ++i)
                    {
                        if(serversList[i] == null)
                        {
                            p.serversList[i] = null;
                        }
                        else
                        {
                            p.serversList[i] = new Tuple<string, int>(serversList[i].Item2, serversList[i].Item3);
                        }
                    }

                    p.packetData.Add(id.ToString());
                    byte[] buffer = p.ToBytes();
                    newServerSocket.Send(buffer);

                    newServerSocket.Close();
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("No se ha podido agregar a " + ip + " a la red de servidores");
            }
        }
      
        private static void JoinToChord()
        {
            Console.WriteLine("Uniendo Servidor a la Red");
            UdpClient udpClient = new UdpClient();
            udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, portUDP));
            var data = new List<string> { Packet.GetIp4Address(), portTCP.ToString() };
            Packet p = new Packet(PacketType.Join, String.Empty, data);
            udpClient.Send(p.ToBytes(), p.ToBytes().Length, "255.255.255.255", portUDP);
            udpClient.Close();

            listenerSocketServer = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ip = new IPEndPoint(IPAddress.Parse(Packet.GetIp4Address()), portTCP);
            listenerSocketServer.Bind(ip);
            Thread listenThreadServer = new Thread(ListenThreadServer);
            listenThreadServer.Start();
            Thread.Sleep(4000);
            if (iAmTheBoot)
            {
                listenThreadServer.Suspend();
                for (int i = 0; i < MAXServers; ++i)
                {
                    serversList[i] = null;
                }
                myID = 0;
                serversList[0] = new Tuple<Socket, string, int>(null, Packet.GetIp4Address(), currentTasks);
                
            }
        }

        private static void ListenThread()
        {
            while (true)
            {
                clientListener.Listen(0);
                connectedClients.Add(new ClientData(clientListener.Accept()));
            }
        }

        private static void ListenTask()
        {
            while (true)
            {
                taskListener.Listen(0);
                connectedClients.Add(new ClientData(taskListener.Accept()));
            }
        }
        private static void ListenThreadServer()
        {
                listenerSocketServer.Listen(0);
                Socket receiver = listenerSocketServer.Accept();

                iAmTheBoot = false;

                byte[] buffer = new byte[receiver.ReceiveBufferSize];
                int bifferSize = receiver.Receive(buffer);
                receiver.Close();
                Packet p = new Packet(buffer);
                myID = int.Parse(p.packetData[0]);
                serversList[myID] = new Tuple<Socket, string, int>(null, Packet.GetIp4Address(), 0);
                for (int i = 0; i < MAXServers; ++i)
                {
                    if (i == myID)
                        continue;
                    if (p.serversList[i] == null)
                    {
                        serversList[i] = null;
                    }
                    else
                    {
                        serversList[i] = new Tuple<Socket, string, int>(new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp),
                                                                        p.serversList[i].Item1,
                                                                        p.serversList[i].Item2);

                        IPEndPoint ipE = new IPEndPoint(IPAddress.Parse(p.serversList[i].Item1), 8000);
                        serversList[i].Item1.Connect(ipE);
                        Packet pSend = new Packet(PacketType.ServerJoined, myID.ToString());
                        pSend.packetData.Add(Packet.GetIp4Address());
                        pSend.packetData.Add(0.ToString());
                        pSend.packetData.Add(myID.ToString());
                        serversList[i].Item1.Send(pSend.ToBytes());
                        connectedServers.Add(new ClientData(serversList[i].Item1, i, true));
                    }
                }
                Console.WriteLine("Me he unido a la Red de Servidores con ID: {0}", myID);
            
            
        }

        class ClientData
        {
            public Socket clientSocket;
            public Thread clientThread;
            public string id;
            public bool isServer;

            public ClientData(Socket clientSocket)
            {
                this.isServer = false;
                this.clientSocket = clientSocket;
                id = Guid.NewGuid().ToString();
                clientThread = new Thread(Server.ClientDataIN);
                clientThread.Start(this);
            }
            public ClientData(Socket clientSocket, int id, bool isServer)
            {
                this.clientSocket = clientSocket;
                this.id = id.ToString();
                this.isServer = isServer;
                clientThread = new Thread(Server.ClientDataIN);
                clientThread.Start(this);
                
            }
        }

        private static void ClientDataIN(object arguments)
        {
            Socket clientSocket = ((ClientData)arguments).clientSocket;
            string clientID = ((ClientData)arguments).id;
            bool isServer = ((ClientData)arguments).isServer;

            byte[] buffer;

            int readBytes;

            int counter = 0;

            while (true)
            {
                try
                {
                    buffer = new byte[clientSocket.SendBufferSize];

                    readBytes = clientSocket.Receive(buffer);

                    if (readBytes > 0)
                    {
                        Packet requestPacket = new Packet(buffer);
                        requestPacket.packetData.Add(counter.ToString());
                        if(requestPacket.packetType == PacketType.Download)
                        {
                            currentTasks += 1;
                            serversList[myID] = new Tuple<Socket, string, int>(null, Packet.GetIp4Address(), currentTasks);
                            for(int i = 0; i < MAXServers; ++i)
                            {
                                if(serversList[i] != null && i != myID)
                                {
                                    Packet p = new Packet(PacketType.TaskStatus, myID.ToString());
                                    p.packetData.Add(currentTasks.ToString());
                                    serversList[i].Item1.Send(p.ToBytes());
                                }
                            }

                            Tuple<Packet, Socket> argument = new Tuple<Packet, Socket>(requestPacket, clientSocket);
                            Thread downloadThread = new Thread(Server.HandleRequest);
                            downloadThread.Start(argument);
                            counter = (counter + 1) % 1000000;
                        }
                        else
                        {
                            if(requestPacket.packetType == PacketType.ServerJoined)
                            {
                                isServer = true;
                                clientID = requestPacket.packetData[2];
                                serversList[int.Parse(requestPacket.senderID)] = new Tuple<Socket, string, int>(clientSocket, requestPacket.packetData[0], int.Parse(requestPacket.packetData[1]));
                                Console.WriteLine(requestPacket.packetData[0] + " se ha unido a la red de servidores");
                            }
                            else
                            {
                                if(requestPacket.packetType == PacketType.ServerDisconnected)
                                {
                                    int index = int.Parse(requestPacket.senderID);
                                    serversList[index] = null;
                                }
                                else
                                {
                                    if (requestPacket.packetType == PacketType.Request)
                                    {
                                        findCandidates:
                                        int ca = 1000000, cb = 1000000, cc = 1000000;
                                        int a = -1, b = -1, c = -1;
                                        string ai = "", bi = "", ci = "";

                                        for(int i = 0; i < MAXServers; ++i)
                                        {
                                            if(serversList[i] != null)
                                            {
                                                if(serversList[i].Item3 < ca)
                                                {
                                                    c = b;
                                                    b = a;
                                                    a = i;
                                                    cc = cb;
                                                    cb = ca;
                                                    ca = serversList[i].Item3;
                                                }
                                                else
                                                {
                                                    if(serversList[i].Item3 < cb)
                                                    {
                                                        c = b;
                                                        b = i;
                                                        cc = cb;
                                                        cb = serversList[i].Item3;
                                                    }
                                                    else
                                                    {
                                                        c = i;
                                                        cc = serversList[i].Item3;
                                                    }
                                                }
                                            }
                                        }

                                        bool iHaveToDownload = (a == myID || b == myID || c == myID);
                                        bool someWhereDisconnected = false;

                                        if(a != -1 && a != myID)
                                        {
                                            Packet p = new Packet(requestPacket.ToBytes());
                                            p.packetType = PacketType.ConnectionTest;
                                            try
                                            {
                                                serversList[a].Item1.Send(p.ToBytes());
                                                ai = serversList[a].Item2;
                                            }
                                            catch
                                            {
                                                serversList[a] = null;
                                                someWhereDisconnected = true;
                                            }
                                            
                                        }

                                        if(b != -1 && b != myID)
                                        {
                                            Packet p = new Packet(requestPacket.ToBytes());
                                            p.packetType = PacketType.ConnectionTest;
                                            try
                                            {
                                                serversList[b].Item1.Send(p.ToBytes());
                                                bi = serversList[b].Item2;
                                            }
                                            catch
                                            {
                                                serversList[b] = null;
                                                someWhereDisconnected = true;
                                            }
                                        }

                                        if (c != -1 && c != myID)
                                        {
                                            Packet p = new Packet(requestPacket.ToBytes());
                                            p.packetType = PacketType.ConnectionTest;
                                            try
                                            {
                                                serversList[c].Item1.Send(p.ToBytes());
                                                ci = serversList[c].Item2;
                                            }
                                            catch
                                            {
                                                serversList[c] = null;
                                                someWhereDisconnected = true;
                                            }
                                        }

                                        if (someWhereDisconnected)
                                        {
                                            goto findCandidates;
                                        }

                                        Console.WriteLine(a + " " + b + " " + c);

                                        if(a == myID)
                                        {
                                            ai = Packet.GetIp4Address();
                                        }
                                        if(b == myID)
                                        {
                                            bi = Packet.GetIp4Address();
                                        }
                                        if(c == myID)
                                        {
                                            ci = Packet.GetIp4Address();
                                        }

                                        Packet oa = new Packet(PacketType.IpSending, myID.ToString());
                                        if (ai != "")
                                            oa.packetData.Add(ai);
                                        if (bi != "")
                                            oa.packetData.Add(bi);
                                        if (ci != "")
                                            oa.packetData.Add(ci);

                                        clientSocket.Send(oa.ToBytes());

                                    }
                                    else
                                    {
                                        if(requestPacket.packetType == PacketType.TaskStatus)
                                        {
                                            int index = int.Parse(requestPacket.senderID);
                                            int amount = int.Parse(requestPacket.packetData[0]);
                                            serversList[index] = new Tuple<Socket, string, int>(serversList[index].Item1, serversList[index].Item2, amount);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (SocketException ex)
                {
                    if (isServer)
                    {
                        Console.WriteLine("Perdida de conexion con el servidor " + clientID);
                        int index = int.Parse(clientID);
                        serversList[index] = null;
                        for(int i = 0; i < MAXServers; ++i)
                        {
                            if (i == myID || serversList[i] == null)
                                continue;
                            Packet p = new Packet(PacketType.ServerDisconnected, index.ToString());
                            serversList[i].Item1.Send(p.ToBytes());
                        }
                    }
                    else
                    {
                        Console.WriteLine("Perdida de conexion con el cliente " + clientID);
                    }
                    break;
                }
            }
        }

        private static void HandleRequest(object argument)
        {
            Packet requestPacket = ((Tuple<Packet, Socket>)argument).Item1;
            Socket clientSocket = ((Tuple<Packet, Socket>)argument).Item2;

            string url = requestPacket.packetData[0];
            string clientID = requestPacket.senderID;

            Console.WriteLine(clientID + ": Descargando " + url);

            try
            {
                WebClient myWebclient = new WebClient();
                Uri myUri = new Uri(url);
                myWebclient.DownloadFile(myUri, clientID + requestPacket.packetData[1]);
                Console.WriteLine(clientID + ": Descargado " + url);
                Packet download = new Packet(PacketType.Response, url);
                download.packetData.Add(File.ReadAllText(clientID + requestPacket.packetData[1]));
                download.packetData.Add(requestPacket.packetData[1]);
                try
                {
                    clientSocket.Send(download.ToBytes());
                    Console.WriteLine(url + " enviado a " + clientID);
                    Thread.Sleep(3000);
                    clientSocket.Close();
                }
                catch
                {
                    Console.WriteLine("No se ha podido enviar " + url + " a " + clientID);
                }
            }
            catch
            {
                Console.WriteLine(clientID + ": Error descargando " + url);
                Packet p = new Packet(PacketType.Error, url);
                try
                {
                    clientSocket.Send(p.ToBytes());
                }
                catch { }        
            }

            currentTasks -= 1;
            serversList[myID] = new Tuple<Socket, string, int>(null, Packet.GetIp4Address(), currentTasks);
            for (int i = 0; i < MAXServers; ++i)
            {
                if (serversList[i] != null && i != myID)
                {
                    Packet p = new Packet(PacketType.TaskStatus, myID.ToString());
                    p.packetData.Add(currentTasks.ToString());
                    serversList[i].Item1.Send(p.ToBytes());
                }
            }
        }
    }
}
