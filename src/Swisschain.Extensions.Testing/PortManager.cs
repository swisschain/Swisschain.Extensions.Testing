using System.Net;
using System.Net.Sockets;

namespace Swisschain.Extensions.Testing
{
    public static class PortManager
    {
        private static readonly object PortLock = new object();

        public static int GetNextPort()
        {
            int port;
            lock (PortLock)
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                port = ((IPEndPoint)socket.LocalEndPoint).Port;
                socket.Close();
            }

            return port;
        }
    }
}
