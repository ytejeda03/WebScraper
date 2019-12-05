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
        public static Socket masterSocket;
        public static string url;
        public static string ip;
        static void Main(string[] args)
        {
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
            Console.WriteLine("Introduzca el nombre que desea que tenga el archivo");
            var name = Console.ReadLine();
            var data = new List<string> { url};
            Packet p = new Packet(PacketType.Request, String.Empty, data);
            Thread t = new Thread(DataIN);
            masterSocket.Send(p.ToBytes());
            t.Start(name);
            Thread.Sleep(1000);
            goto URL;
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
                    File.WriteAllText(name, p.packetData[0]);
                    Console.WriteLine("Descarga finalizada");
                    break;
            }
        }
    }
}
