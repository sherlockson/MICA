import socket
import struct
import sys

import client_functions
import ATAKMain

host = '52.222.45.68'
port = 8089

async def SSLCoTSender(bmsg):
    print("connecting to Server")
    reader, writer = await client_functions.protocol_factory(host, port)
    msg = ATAKMain.dataParser(bmsg)
    if msg is not None:
        print("Preparing Message")
        evt_str = "{}".format(msg)
        print(evt_str)
        writer.write(evt_str.encode())
        print('Message sent')


async def MCASTCoTSender(DATA, MCAST_GRP, MCAST_PORT, IS_ALL_GROUPS, sock=None):
    MULTICAST_TTL = 2
    if sock is None:
        sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM, socket.IPPROTO_UDP)
        # sock.setsockopt(socket.IPPROTO_IP, socket.IP_MULTICAST_TTL, MULTICAST_TTL)
        sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)

        if IS_ALL_GROUPS:
            # on this port, receives ALL multicast groups
            sock.bind(('', MCAST_PORT))
        else:
            # on this port, listen ONLY to MCAST_GRP
            sock.bind((MCAST_GRP, MCAST_PORT))

        mreq = struct.pack("4sl", socket.inet_aton(MCAST_GRP), socket.INADDR_ANY)

        sock.setsockopt(socket.IPPROTO_IP, socket.IP_ADD_MEMBERSHIP, mreq)

    try:
        # Creates Cot Message to be sent as Byte stream
        evt = "{}".format(ATAKMain.cot_builder(DATA))
        b_evt = evt.encode('utf-8')

        print(sys.stderr, 'sending "%s"' % b_evt)
        # Send event through Mulitcast stream
        sock.sendto(b_evt, (MCAST_GRP, MCAST_PORT))
    finally:
        sock.close()