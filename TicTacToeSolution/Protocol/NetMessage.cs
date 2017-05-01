using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Protocol
{
    [Serializable]
    public enum NetMessageType
    {
        Handhsake,
        ClientReady,
        StartParty,
        InstantiatePlayers,
        Disconnect,
        Update
    }

    [Serializable]
    public class NetMessage
    {
        public string Id;
        public bool Avatar;
        public int[,] Cell;
        public NetMessageType Type;


        public static byte[] Serialize(NetMessage remote)
        {
            using (var memoryStream = new MemoryStream())
            {
                new BinaryFormatter().Serialize(memoryStream, remote);
                return memoryStream.ToArray();
            }
        }

        public static NetMessage Deserialize(byte[] buffer)
        {
            try
            {
                using (var memoryStream = new MemoryStream(buffer))
                {
                    var remote = new BinaryFormatter().Deserialize(memoryStream);
                    return (NetMessage)remote;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Deserialize error! {0}", e.Message);
                return null;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj is NetMessage)
                return this.Id.Equals(((NetMessage)obj).Id);

            return false;
        }

        public string ToString()
        {
            return String.Format("Id: {0}\nAvatar: {1}\nCell: {2}\nMessageType: {3}", 
                                 this.Id, this.Avatar, this.Cell, this.Type.ToString());
        }
    }
}
