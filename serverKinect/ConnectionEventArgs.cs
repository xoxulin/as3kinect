using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace KinectServer
{
    class ConnectionEventArgs : EventArgs, IDisposable
    {
        public ConnectionEventArgs(IPAddress ipAddress, int port) {

            _ipAddress = ipAddress;
            _port = port;

        }

        private IPAddress _ipAddress;
        private int _port;
        
        public IPAddress IpAddress { get { return _ipAddress; } }
        public int Port { get { return _port; } }
        
        public void Dispose() {

            _ipAddress = null;
            _port = 0;
        }
    }
}
