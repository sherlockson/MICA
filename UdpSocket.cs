using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

//Credit to: https://gist.github.com/darkguy2008/413a6fea3a5b4e67e5e0d96f750088a9

namespace CT_API_GUI
{
    public class UdpSocket
    {
        private Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private const int bufSize = 16 * 1024;
        private State state = new State();
        private EndPoint epFrom = new IPEndPoint(IPAddress.Any, 0);
        private AsyncCallback recv = null;

        public class State
        {
            public byte[] buffer = new byte[bufSize];
        }

        public void Server(string address, int port)
        {
            socket.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            socket.Bind(new IPEndPoint(IPAddress.Parse(address), port));
            //Receive();
        }

        public void Client(string address, int port)
        {
            socket.Connect(IPAddress.Parse(address), port);
            //Receive();
        }

        public void Send(string text)
        {
            var data = Encoding.ASCII.GetBytes(text);
            socket.BeginSend(data, 0, data.Length, SocketFlags.None, (ar) =>
            {
                var so = (State) ar.AsyncState;
                var bytes = socket.EndSend(ar);
                Console.WriteLine("SEND: {0}, {1}", bytes, text);
            }, state);
        }

        private void Receive()
        {
            socket.BeginReceiveFrom(state.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv = (ar) =>
            {
                var so = (State) ar.AsyncState;
                var bytes = socket.EndReceiveFrom(ar, ref epFrom);
                socket.BeginReceiveFrom(so.buffer, 0, bufSize, SocketFlags.None, ref epFrom, recv, so);
                Console.WriteLine("RECV: {0}: {1}, {2}", epFrom.ToString(), bytes,
                    Encoding.ASCII.GetString(so.buffer, 0, bytes));
            }, state);
        }
    }
}