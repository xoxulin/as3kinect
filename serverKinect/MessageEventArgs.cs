using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace KinectServer
{
    class MessageEventArgs : EventArgs, IDisposable
    {
        public MessageEventArgs(Dictionary<string, object> arguments, byte command, IPAddress ipAddress, int port)
        {

            _arguments = arguments;
            _command = command;
            _ipAddress = ipAddress;
            _port = port;
        }

        private Dictionary<string, object> _arguments;
        private byte _command;
        
        private IPAddress _ipAddress;
        private int _port;

        public Dictionary<string, object> Arguments { get { return _arguments; } }
        public int Command { get { return _command; } }
        
        public IPAddress IpAddress { get { return _ipAddress; } }
        public int Port { get { return _port; } }
        
        public void Dispose() {

            _arguments = null;
            _command = 0;
            
            _ipAddress = null;
            _port = 0;            
        
        }

    }
}
