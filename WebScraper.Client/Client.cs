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


            p.packetType = PacketType.Download;

            byte[] buffer = new byte[masterSocket.SendBufferSize];
            int readBytes = masterSocket.Receive(buffer);
            Packet servP = new Packet(buffer);

            for (int i = 0; i < servP.packetData.Count; ++i)
            {
                Console.WriteLine(servP.packetData[i]);
                Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipD = new IPEndPoint(IPAddress.Parse(servP.packetData[i]), 10001);
                try
                {
                    s.Connect(ipD);
                    Console.WriteLine("Conectado al servidor de descarga " + i);
                    s.Send(p.ToBytes());
                    Thread t = new Thread(Download);
                    t.Start(new Tuple<Socket, int>(s, i));
                }
                catch
                {
                    Console.WriteLine("No se pudo establecer conexión con el servidor de descarga # " + i);
                }
            }

            Thread.Sleep(1000);
            goto URL;
        }

        static void Download(object sender)
        {
            Tuple<Socket, int> aux = (Tuple<Socket, int>)sender;
            Socket senderServer = aux.Item1;
            int id = aux.Item2;

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
                    Console.WriteLine("El servidor de descarga " + id + " se ha desconectado");
                    break;
                }
            }

        }
    }
}
