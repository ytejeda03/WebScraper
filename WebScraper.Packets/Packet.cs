using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace WebScraper.Packets
{
    [Serializable]
    public class Packet
    {
        public List<string> packetData;
        public string senderID;
        public PacketType packetType;
        public Tuple<string, int>[] serversList = new Tuple<string, int>[100];

        public Packet(PacketType packetType, string senderID)
        {
            this.packetData = new List<string>();
            this.senderID = senderID;
            this.packetType = packetType;

        }
            
        public Packet(PacketType packetType, string senderID, List<string> packetData)
        {
            this.packetData = packetData;
            this.senderID = senderID;
            this.packetType = packetType;
        }

        public Packet(byte[] packetBytes)
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream(packetBytes);

            Packet p = (Packet)bf.Deserialize(ms);
            ms.Close();

            this.packetData = p.packetData;
            this.senderID = p.senderID;
            this.packetType = p.packetType;
            this.serversList = p.serversList;
        }

        public byte[] ToBytes()
        {
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();

            bf.Serialize(ms, this);

            byte[] bytes = ms.ToArray();
            ms.Close();

            return bytes;
        }

        public static string GetIp4Address()
        {
            IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());

            foreach (IPAddress i in ips)
            {
                if (i.AddressFamily == AddressFamily.InterNetwork)
                {
                    return i.ToString();
                }
            }

            return "127.0.0.1";
        }
    }

    public enum PacketType
    {
        Request,
        Response,
        Error,
        Join,
        JoinResponse,
        ServerJoined,
        ServerDisconnected,
        Download,
        TaskStatus
    }
}
