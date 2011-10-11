using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace KinectServer
{
    class Server
    {
        private Socket Sock;
        private SocketAsyncEventArgs AcceptAsyncArgs;

        public Server()
        {
            Sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Sock.ReceiveTimeout = 1000;
            Sock.SendTimeout = 1000;
            
            AcceptAsyncArgs = new SocketAsyncEventArgs();
            AcceptAsyncArgs.Completed += AcceptCompleted;
        }

        private void AcceptCompleted(object sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                ClientConnection Client = new ClientConnection(e.AcceptSocket);

                IPEndPoint ipEp = (IPEndPoint)e.AcceptSocket.RemoteEndPoint;
                Client.Disconnected += client_Disconnected;
                Client.Recieved += client_Recieved;


                if (this.Connected != null)
                    this.Connected(this, new ConnectionEventArgs(ipEp.Address, ipEp.Port));                

            }
            e.AcceptSocket = null;
            AcceptAsync(AcceptAsyncArgs);
        }

        private void AcceptAsync(SocketAsyncEventArgs e)
        {
            bool willRaiseEvent = Sock.AcceptAsync(e);
            if (!willRaiseEvent)
                AcceptCompleted(Sock, e);
        }

        public void Start(int Port)
        {
            Sock.Bind(new IPEndPoint(IPAddress.Loopback, Port));
            Sock.Listen(50);
            //Console.WriteLine("TCP Server started on {0}:{1}", IPAddress.Loopback, Port);
            AcceptAsync(AcceptAsyncArgs);            
        }

        public void Stop()
        {
            Sock.Close();
            Sock.Dispose();

            ClientConnection.DisposeAll();
        }

        public void Send(byte[] data)
        {
            ClientConnection.Send(data);
        }

       private void client_Disconnected(object sender, ConnectionEventArgs e)
        {
            if (this.Disconnected != null)
                this.Disconnected(this, e);

            ClientConnection clientConnection = (ClientConnection)sender;
            
            clientConnection.Disconnected -= client_Disconnected;
            clientConnection.Recieved -= client_Recieved;
           
            clientConnection.Dispose();
        }

       private void client_Recieved(object sender, MessageEventArgs e)
        {
            if (this.Recieved != null)
                this.Recieved(this, e);
        }

        public event EventHandler<ConnectionEventArgs> Connected;
        public event EventHandler<ConnectionEventArgs> Disconnected;
        public event EventHandler<MessageEventArgs> Recieved;
    }
}
