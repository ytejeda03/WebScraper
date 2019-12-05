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

namespace WebScraper.Server
{
    class Server
    {
        static Socket listenerSocket;
        static List<ClientData> connectedClients;
        
        static void Main(string[] args)
        {
            Console.WriteLine("Comenzando servidor en " + Packet.GetIp4Address() + ":8000");

            listenerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            connectedClients = new List<ClientData>();

            IPEndPoint ip = new IPEndPoint(IPAddress.Parse(Packet.GetIp4Address()), 8000);

            listenerSocket.Bind(ip);

            Thread listenThread = new Thread(ListenThread);

            listenThread.Start();

        }

        private static void ListenThread()
        {
            while (true)
            {
                listenerSocket.Listen(0);
                connectedClients.Add(new ClientData(listenerSocket.Accept()));
            }
        }

        class ClientData
        {
            public Socket clientSocket;
            public Thread clientThread;
            public string id;

            public ClientData(Socket clientSocket)
            {
                this.clientSocket = clientSocket;
                id = Guid.NewGuid().ToString();
                clientThread = new Thread(Server.ClientDataIN);
                clientThread.Start(new Tuple<Socket, string>(clientSocket, id));
                
            }     
        }

        private static void ClientDataIN(object arguments)
        {
            Socket clientSocket = ((Tuple<Socket, string>)arguments).Item1;
            string clientID = ((Tuple<Socket, string>)arguments).Item2;

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
                        requestPacket.senderID = clientID;
                        requestPacket.packetData.Add(counter.ToString());
                        if(requestPacket.packetType == PacketType.Request)
                        {
                            Tuple<Packet, Socket> argument = new Tuple<Packet, Socket>(requestPacket, clientSocket);
                            Thread downloadThread = new Thread(Server.HandleRequest);
                            downloadThread.Start(argument);
                            counter = (counter + 1) % 1000000;
                        }
                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine("Perdida de conexion con el cliente " + clientID);
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
                Packet download = new Packet(PacketType.Response, Packet.GetIp4Address() + "8000");
                download.packetData.Add(File.ReadAllText(clientID + requestPacket.packetData[1]));
                try
                {
                    clientSocket.Send(download.ToBytes());
                    Console.WriteLine(url + " enviado a " + clientID);
                }
                catch
                {
                    Console.WriteLine("No se ha podido enviar " + url + " a " + clientID);
                }
            }
            catch
            {
                Console.WriteLine(clientID + ": Error descargando " + url);
                Packet p = new Packet(PacketType.Error, Packet.GetIp4Address() + "8000");
                clientSocket.Send(p.ToBytes());
            }
        }
    }
}
