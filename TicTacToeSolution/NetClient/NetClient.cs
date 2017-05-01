using Protocol;
using System;
using System.Net;
using System.Net.Sockets;

namespace NetClient
{
    public class NetClient
    {
        public const int DefaultPort = 2929;
        public NetMessage NetMessage { get; private set; }
        private TcpClient tcpClient;
        private byte[] bufferIn;
        private byte[] bufferOut;
        private string id;

        public Action HandhsakeReceived;
        public Action ClientReadyReceived;
        public Action StartPartyReceived;
        public Action InstantiatePlayersReceived;
        public Action DisconnectReceived;
        public Action UpdateReceived;


        public NetClient()
        {
            try
            {
                this.NetMessage = new NetMessage();
                this.tcpClient = new TcpClient(GetIpAddress().ToString(), DefaultPort);
            }
            catch (Exception e) when (e is SocketException)
            {
                throw new Exception("Server is offline");
            }
        }



        public void ReceiveNetMessageFromServer()
        {
            try
            {
                NetworkStream inStream = this.tcpClient.GetStream();
                this.bufferIn = new byte[this.tcpClient.ReceiveBufferSize];
                if (inStream.CanRead)
                    inStream.BeginRead(bufferIn, 0, bufferIn.Length, ReadCallback, this.tcpClient);
            }
            catch (Exception e)
            {
                throw new Exception("Server is offline");
            }
        }

        private void ReadCallback(IAsyncResult ar)
        {
            TcpClient client = (TcpClient)ar.AsyncState;

            try
            {
                NetworkStream inStream = client.GetStream();
                int count = inStream.EndRead(ar);

                if (count <= 0)
                    return;

                var tmpBffer = new byte[count];
                Buffer.BlockCopy(this.bufferIn, 0, tmpBffer, 0, count);
                this.NetMessage = NetMessage.Deserialize(tmpBffer);

                switch (this.NetMessage.Type)
                {
                    case NetMessageType.Handhsake:
                        HandhsakeReceived();
                        break;
                    case NetMessageType.ClientReady:
                        ClientReadyReceived();
                        break;
                    case NetMessageType.StartParty:
                        StartPartyReceived();
                        break;
                    case NetMessageType.InstantiatePlayers:
                        InstantiatePlayersReceived();
                        break;
                    case NetMessageType.Update:
                        UpdateReceived();
                        break;
                    case NetMessageType.Disconnect:
                        DisconnectReceived();
                        break;
                }
            }
            catch (Exception e) when (e is SocketException || e is System.IO.IOException)
            {
                throw new Exception("Server is offline");
            }
        }



        public void SendMessageToServer(NetMessage netMessage)
        {
            netMessage.Id = this.id;
            this.bufferOut = NetMessage.Serialize(netMessage);

            try
            {
                NetworkStream outStream = this.tcpClient.GetStream();
                if (outStream.CanWrite)
                    outStream.BeginWrite(this.bufferOut, 0, this.bufferOut.Length, WriteCallback, outStream);
            }
            catch (Exception)
            {
                throw new Exception("Server is offline");
            }
        }

        private void WriteCallback(IAsyncResult ar)
        {
            try
            {
                NetworkStream outStream = (NetworkStream)ar.AsyncState;
                outStream.EndWrite(ar);
            }
            catch (Exception)
            {
                throw new Exception("Server is offline");
            }
        }




        private static IPAddress GetIpAddress()
        {
            IPAddress[] ips = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
            foreach (var ip in ips)
            {
                if (ip.AddressFamily.Equals(AddressFamily.InterNetwork))
                    return ip;
            }

            return IPAddress.Parse("127.0.0.1");
        }
    }
}
