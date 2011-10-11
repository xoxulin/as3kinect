using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace KinectServer
{
    class ClientConnection : IDisposable
    {
        private static Dictionary<IPEndPoint, ClientConnection> Connections = new Dictionary<IPEndPoint, ClientConnection>();

        private IPEndPoint ipEp;
        private Socket Sock;
        private SocketAsyncEventArgs SockAsyncEventArgs;
        private static string crossdomain = "<?xml version='1.0'?><cross-domain-policy><site-control permitted-cross-domain-policies='all'/><allow-access-from domain='*' to-ports='*'/><allow-access-from domain='localhost' to-ports='*'/><allow-access-from domain='127.0.0.1' to-ports='*'/></cross-domain-policy>";

        private MemoryStream buffer;
        private BinaryWriter bufferWriter;
        private BinaryReader bufferReader;
        private byte firstByte = 0x00;
        private byte secondByte = 0x00;
        private uint packetLength = 0;
        
        private byte[] buff;

        public ClientConnection(Socket AcceptedSocket)
        {
            buff = new byte[1024];
            Sock = AcceptedSocket;

            ipEp = (IPEndPoint)Sock.RemoteEndPoint;

            Connections.Add(ipEp, this);

            // - - -

            buffer = new MemoryStream();
            bufferWriter = new BinaryWriter(buffer);
            bufferReader = new BinaryReader(buffer);

            // - - - 

            SockAsyncEventArgs = new SocketAsyncEventArgs();
            SockAsyncEventArgs.Completed += SockAsyncEventArgs_Completed;
            SockAsyncEventArgs.SetBuffer(buff, 0, buff.Length);

            ReceiveAsync(SockAsyncEventArgs);
        }

        public void Dispose()
        {
            bufferReader.Close();
            bufferReader.Dispose();

            bufferWriter.Close();
            bufferWriter.Dispose();
            
            buffer.Close();
            buffer.Dispose();
        }

        private void SockAsyncEventArgs_Completed(object sender, SocketAsyncEventArgs e)
        {
            if(!IsConnected())
            {
                ProcessDisconnect(e);
                return;
            }

            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
            }
        }

        private Boolean IsConnected()
        {
            try
            {
                return !(Sock.Poll(1, SelectMode.SelectRead) && Sock.Available == 0);
            }
            catch (SocketException)
            {
                return false;
            }
        }

        private void ProcessDisconnect(SocketAsyncEventArgs e)
        {
            if (this.Disconnected != null)
                this.Disconnected(this, new ConnectionEventArgs(ipEp.Address, ipEp.Port));

            Connections.Remove(ipEp);
            Sock.Close();
            Sock = null;
            ipEp = null;
            SockAsyncEventArgs = null;
        }

        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                // - - - write to end
                long oldPosition = buffer.Position;

                buffer.Position = buffer.Length;
                bufferWriter.Write(e.Buffer, 0, e.BytesTransferred);
                buffer.Position = oldPosition;

                // - - - we need calculate actual bytesAvailable
                long bytesAvailable = buffer.Length - buffer.Position;

                while (bytesAvailable > 0)
                {
                    // - - - if firstByte reset then we read it again
                    if (firstByte == 0 && bytesAvailable > 6)
                    {
                        firstByte = bufferReader.ReadByte();
                        secondByte = bufferReader.ReadByte();
                        packetLength = bufferReader.ReadUInt32();
                        bytesAvailable = buffer.Length - buffer.Position;

                        //Console.WriteLine("Incoming msg from {0}:{1}, 0x {2:X}|{3:X}|{4:X8}...", ipEp.Address, ipEp.Port, firstByte, secondByte, packetLength);
                    }

                    // - - - - xml request

                    if (firstByte == 60)
                    {
                        buffer.Position = oldPosition + e.BytesTransferred;

                        firstByte = 0;
                        bytesAvailable = buffer.Length - buffer.Position;
                        
                        SendCrossdomain();
                        return;
                    }

                    // - - - if bytesAvailable enough then read next bytes
                    if (bytesAvailable >= packetLength)
                    {
                        // - - - but if first byte is not MS_DATA then skip
                        if (firstByte != KinectConst.MS_DATA)
                        {
                            buffer.Position += packetLength;
                            bytesAvailable = buffer.Length - buffer.Position;
                        }
                        else
                        {
                            // - - - create argumens
                            Dictionary<string, object> arguments = new Dictionary<string, object>();
                            // - - - mark position for reading packetLength
                            oldPosition = buffer.Position;

                            // - - - switch by secondByte
                            switch (secondByte)
                            {

                                case KinectConst.IN_ADD_WORD:

                                    // - - - create array of strings
                                    List<string> words = new List<string>();
                                    
                                    // - - - read strings: 2 bytes (length) + word (length)
                                    while (buffer.Position - oldPosition < packetLength)
                                    {

                                        Int16 wordLength = bufferReader.ReadInt16();
                                        string word = new string(bufferReader.ReadChars((int)wordLength));
                                        words.Add(word);
                                        word = null;
                                    }

                                    // - - - add words to arguments
                                    arguments["words"] = words;
                                    break;
                            }


                            
                            if (this.Recieved != null)
                                this.Recieved(this, new MessageEventArgs(arguments, secondByte, ipEp.Address, ipEp.Port));

                            arguments.Clear();
                            arguments = null;

                            firstByte = 0;
                            bytesAvailable = buffer.Length - buffer.Position;
                        }
                    }
                    else
                    {
                        ReceiveAsync(SockAsyncEventArgs);
                        return;
                    }
                }

                ReceiveAsync(SockAsyncEventArgs);
            }
        }

        private void SendCrossdomain()
        {
            using (MemoryStream crossdomainStream = new MemoryStream())
            {
                using (BinaryWriter crossdomainWriter = new BinaryWriter(crossdomainStream))
                {

                    crossdomainWriter.Write(crossdomain.ToCharArray());
                    crossdomainWriter.Write(0x00);

                    Send(crossdomainStream.ToArray());
                }
            }
        }

        private void ProcessSend(SocketAsyncEventArgs e)
        {
            //Console.WriteLine("Outgoing msg to {0}:{1}, dataLength {2}", ipEp.Address, ipEp.Port, e.Buffer.Length);
        }

        // async ---------------------------------

        private void ReceiveAsync(SocketAsyncEventArgs e)
        {
            bool willRaiseEvent = Sock.ReceiveAsync(e);
            if (!willRaiseEvent)
            {
                ProcessReceive(e);
            }
        }

        private void SendAsync(SocketAsyncEventArgs e)
        {
            bool willRaiseEvent = Sock.SendAsync(e);
            if (!willRaiseEvent)
            {
                ProcessSend(e);
            }
        }

        // static --------------------------------

        public static void Send(byte[] data)
        {
            foreach (var connection in Connections)
	        {
	            connection.Value.SendAsync(data);		        
	        }
        }

        private void SendAsync(byte[] data)
        {
            SocketAsyncEventArgs SendAsyncEventArgs = new SocketAsyncEventArgs();
            SendAsyncEventArgs.Completed += SockAsyncEventArgs_Completed;
            SendAsyncEventArgs.SetBuffer(data, 0, data.Length);

            SendAsync(SendAsyncEventArgs);
        }

        public static void DisposeAll()
        {
            foreach (var connection in Connections)
            {
                connection.Value.Dispose();
            }
        }

        public static int Count()
        {
            return Connections.Count();
        }

        public event EventHandler<ConnectionEventArgs> Disconnected;
        public event EventHandler<MessageEventArgs> Recieved;
    }
}
