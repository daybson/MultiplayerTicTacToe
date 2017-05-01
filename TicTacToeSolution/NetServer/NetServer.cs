using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using Protocol;

namespace NetServer
{
    public class NetServer
    {
        private TcpListener tcpListener;

        public const int DefaultPort = 2929;

        private Dictionary<NetMessage, TcpClient> Clients;

        private byte[] bufferIn;

        private byte[] bufferOut;

        public bool IsListening;

        private Thread ioThread;
        private int napTime = 10;

        public NetServer()
        {
            this.Clients = new Dictionary<NetMessage, TcpClient>();
            this.tcpListener = new TcpListener(GetIpAddress(), DefaultPort);
            this.tcpListener?.Start();
            this.IsListening = true;
            //Console.WriteLine("SERVER ONLINE");
        }


        public void AcceptNewClient()
        {
            while (true)
            {
                //Console.WriteLine("Esperando conexão do cliente...")
                TcpClient clientTcp = tcpListener.AcceptTcpClient();
                NetMessage message = new NetMessage();

                lock (this.Clients)
                {
                    this.Clients.Add(message, clientTcp);
                    //Console.WriteLine("Novo cliente conectado!");

                    this.ioThread = new Thread(
                        () =>
                        {
                            Handshake(ref message);
                        });
                    this.ioThread.Start();
                }

                Thread.Sleep(this.napTime);
            }
        }

        private void Handshake(ref NetMessage message)
        {
            var stream = this.Clients[message].GetStream();

            message.Id = Guid.NewGuid().ToString();
            message.Avatar = true;
            message.Type = NetMessageType.Handhsake;

            this.bufferOut = NetMessage.Serialize(message);

            if (stream.CanWrite)
                stream.BeginWrite(this.bufferOut, 0, this.bufferOut.Length, WriteCallback, this.Clients[message]);

            this.bufferIn = new byte[this.Clients[message].ReceiveBufferSize];

            if (stream.CanRead)
                stream.BeginRead(this.bufferIn, 0, this.bufferIn.Length, ReadCallback, this.Clients[message]);
        }

        private void WriteCallback(IAsyncResult ar)
        {
            TcpClient tcp = (TcpClient)ar.AsyncState;
            try
            {
                NetworkStream outStream = tcp.GetStream();
                outStream.EndWrite(ar);
            }
            catch (Exception e)
            {
                //Console.WriteLine("Client disconnected!");
                lock (this.Clients)
                {
                    this.Clients.Remove(this.Clients.FirstOrDefault(c => c.Value.Equals(tcp)).Key);
                }
            }
        }


        private void ReadCallback(IAsyncResult ar)
        {
            TcpClient tcp = (TcpClient)ar.AsyncState;
            try
            {
                NetMessage remote = this.Clients.First(c => c.Value.Equals(tcp)).Key;
                NetworkStream inStream = tcp.GetStream();
                int count = inStream.EndRead(ar);

                if (count <= 0)
                    return;

                //converte os bytes para o tipo RemoteClient
                var tempBuffer = new byte[count];
                Buffer.BlockCopy(this.bufferIn, 0, tempBuffer, 0, count);
                remote = NetMessage.Deserialize(tempBuffer);

                this.Clients.First(c => c.Value.Equals(tcp)).Key.Type = remote.Type;

                switch (remote.Type)
                {
                    case NetMessageType.Update:
                        Broadcast(remote);
                        break;

                    case NetMessageType.ClientReady:
                        break;

                    case NetMessageType.Disconnect:
                        this.Clients.Remove(remote);
                        break;
                }

                //Console.WriteLine("Recebido: {0}", remote.ToString());

                inStream.BeginRead(this.bufferIn, 0, tcp.ReceiveBufferSize, ReadCallback, tcp);
            }
            catch (Exception)
            {
                //Console.WriteLine("Cliente desconectado!");
                lock (this.Clients)
                {
                    this.Clients.Remove(this.Clients.FirstOrDefault(c => c.Value.Equals(tcp)).Key);
                }
            }
        }
        

        private void Broadcast(NetMessage netMessage)
        {
            foreach (var clientTarget in this.Clients)
            {
                if (clientTarget.Key.Id.Equals(netMessage.Id))
                    continue;

                var stream = clientTarget.Value.GetStream();
                this.bufferOut = NetMessage.Serialize(netMessage);
                if (stream.CanWrite)
                    stream.BeginWrite(this.bufferOut, 0, this.bufferOut.Length, WriteCallback, clientTarget.Value);
            }
        }

        public void StopListen()
        {
            this.IsListening = false;
            this.Clients.Clear();
            this.tcpListener.Stop();

            //Console.WriteLine("Server is offline.");
            Thread.CurrentThread.Abort();
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
