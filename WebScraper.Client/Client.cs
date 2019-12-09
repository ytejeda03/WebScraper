using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using WebScraper.Packets;
using System.IO;

namespace WebScraper.Client
{
    
    class Client
    {
        public static object o = new object();
        public static Socket serverListener;
        public static Socket masterSocket;
        public static string url;
        public static string ip;
        private static Dictionary<string, bool> taskCompletion;
        static void Main(string[] args)
        {
            serverListener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ipServer = new IPEndPoint(IPAddress.Parse(Packet.GetIp4Address()), 8010);
            serverListener.Bind(ipServer);

            Thread serverL = new Thread(ListenThread);
            serverL.Start();

            taskCompletion = new Dictionary<string, bool>();

            Init: Console.Clear();
            Console.WriteLine("Introduzca la dirección ip del servidor");
            ip = Console.ReadLine();

            masterSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint ipE = new IPEndPoint(IPAddress.Parse(ip), 8000);
            try
            {
                masterSocket.Connect(ipE);
            }
            catch
            {
                Console.WriteLine("No se pudo establecer conexión con el servidor");
                Thread.Sleep(1000);
                goto Init;
            }
            URL: Console.WriteLine("Introduzca la dirección url del sitio web que desea descargar");
            url = Console.ReadLine();
            taskCompletion[url] = false;
            Console.WriteLine("Introduzca el nombre que desea que tenga el archivo");
            var name = Console.ReadLine();
            var data = new List<string> { url };
            Packet p = new Packet(PacketType.Request, Packet.GetIp4Address(), data);
            p.packetData.Add(name);
            masterSocket.Send(p.ToBytes());
            Thread.Sleep(1000);
            goto URL;
        }

        public static void ListenThread()
        {
            while (true)
            {
                serverListener.Listen(0);
                Socket s = serverListener.Accept();
                Thread t = new Thread(Download);
                t.Start(s);
            }
        }

        static void Download(object sender)
        {
            Socket senderServer = (Socket)sender;

            byte[] buffer;
            int readBytes;

            while (true)
            {
                try
                {
                    buffer = new byte[senderServer.SendBufferSize + 1024];
                    readBytes = senderServer.Receive(buffer);

                    lock (o)
                    {
                        if (readBytes > 0)
                        {
                            Packet p = new Packet(buffer);
                            if (taskCompletion[p.senderID])
                                break;
                            switch (p.packetType)
                            {
                                case PacketType.Error:
                                    Console.WriteLine("No se pudo completar su descarga");
                                    break;

                                case PacketType.Response:
                                    if (!taskCompletion[p.senderID])
                                    {
                                        File.WriteAllText(p.packetData[1], p.packetData[0]);
                                        Console.WriteLine("Descarga finalizada");
                                    }
                                    break;
                            }
                            taskCompletion[p.senderID] = true;
                            break;
                        }
                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine("Uno de los servidores de descarga se ha desconectado");
                    break;
                }
            }

        }

        static void DataIN(object name)
        {
            byte[] buffer;
            int readBytes;

            while (true)
            {
                try
                {
                    buffer = new byte[masterSocket.SendBufferSize];
                    readBytes = masterSocket.Receive(buffer);

                    if (readBytes > 0)
                    {
                        DataManager(new Packet(buffer), (string)name);
                        break;
                    }
                }
                catch (SocketException ex)
                {
                    Console.WriteLine("El servidor se ha desconectado");
                    Console.ReadLine();
                    Environment.Exit(0);
                }
            }
        }

        static void DataManager(Packet p, string name)
        {
            switch (p.packetType)
            {
                case PacketType.Error:
                    Console.WriteLine("No se pudo completar su descarga, inténtelo de nuevo");
                    break;

                case PacketType.Response:
                    if (!taskCompletion[p.senderID])
                    {
                        File.WriteAllText(name, p.packetData[0]);
                        Console.WriteLine("Descarga finalizada");
                    }
                    break;
            }
            taskCompletion[p.senderID] = true;
        }
    }
}
