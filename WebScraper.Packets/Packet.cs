using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Net;
using System.Net.Sockets;

namespace WebScraper.Packets
{
    [Serializable]
    public class Packet
    {
        public List<string> packetData;
        public string senderID;
        public PacketType packetType;

        public Packet(PacketType packetType, string senderID)
        {
            this.packetData = new List<string>();
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

    }
}
