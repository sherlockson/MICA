using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace CT_API_GUI
{
    /**
   * @class MulticastSender - class containing all methods needed to create the multicast group and send multicast messages
   *
   * Author - Andrew Holmes
   */
    public static class MulticastSender
    {
        public static IPAddress McastAddress;
        public static int McastPort;
        private static Socket mcastSocket;
        private static MulticastOption mcastOption;

        /**
     * @function JoinMulticastGroup - function designed to create a multicast socket then proceed to bind to it
     */
        public static void JoinMulticastGroup()
        {
            try
            {
                // Create a multicast socket.
                mcastSocket = new Socket(AddressFamily.InterNetwork,
                    SocketType.Dgram,
                    ProtocolType.Udp);

                // Get the local IP address used by the listener and the sender to
                // exchange multicast messages.
                var name = Dns.GetHostName();
                var localIpAddr = Dns.GetHostEntry(name).AddressList.FirstOrDefault
                    (x => x.AddressFamily == AddressFamily.InterNetwork);

                Console.WriteLine("Local IP Address: " + localIpAddr);

                // Create an IPEndPoint object.
                var iplocal = new IPEndPoint(localIpAddr, 0);

                // Bind this endpoint to the multicast socket.
                mcastSocket.Bind(iplocal);

                // Define a MulticastOption object specifying the multicast group
                // address and the local IP address.
                // The multicast group address is the same as the address used by the listener.
                mcastOption = new MulticastOption(McastAddress, localIpAddr);

                mcastSocket.SetSocketOption(SocketOptionLevel.IP,
                    SocketOptionName.AddMembership,
                    mcastOption);
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + e);
            }
        }

        /**
     * @function BrodcastMessage - function to send a string out via multicast as a bytestream
     *
     * INPUT: string message - string to be sent out
     */
        public static void BroadcastMessage(string message)
        {
            try
            {
                //Send multicast packets to the listener.
                var endPoint = new IPEndPoint(McastAddress, McastPort);
                mcastSocket.SendTo(Encoding.ASCII.GetBytes(message), endPoint);
                Console.WriteLine("Multicast data sent.....");
            }
            catch (Exception e)
            {
                Console.WriteLine("\n" + e);
            }
        }
    }
}