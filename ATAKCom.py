#!/usr/bin/env python3.9

import asyncio
import os
import socket
import ssl
import struct
import sys
import urllib
import datetime
import pytak
from pytak import pycot

# Server info for CEDER marathon
os.environ['client_cert'] = r"C:\Users\pixel\RiderProjects\ConsoleAFMApi\ceder.pem"
os.environ['client_key'] = r"C:\Users\pixel\RiderProjects\ConsoleAFMApi\ceder.key"

cot_multicast_url: urllib.parse.ParseResult = urllib.parse.urlparse("udp:239.2.3.1:6969")
cot_server_url: urllib.parse.ParseResult = urllib.parse.urlparse("ssl:52.222.45.68:8089")

host = '52.222.45.68'
port = 8089
key = r"C:\Users\pixel\RiderProjects\ConsoleAFMApi\ceder.key"
pem = r"C:\Users\pixel\RiderProjects\ConsoleAFMApi\ceder.pem"

# Message to be sent if no Data is passed for cot_builder
DATA = "this is a filler constant"

testString = b'lat: 39.825297 lon: -84.027601 uid: point 2 how: h-g-i-g-o link_type: a-f-G-I-B remarks: 200.00% argb: -65536 iconsetpath: COT_MAPPING_SPOTMAP/b-m-p-s-m/-256'


def connect_LocalH():
    HOST = '127.0.0.1'
    PORT = 8080
    ADDR = (HOST, PORT)

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    sock.bind(ADDR)

    return sock
    # client_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    # client_socket.connect(('127.0.0.1', 8080))
    # return client_socket


def connect_MCAST(MCAST_GRP, MCAST_PORT, IS_ALL_GROUPS):
    MULTICAST_TTL = 2
    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM, socket.IPPROTO_UDP)
    # sock.setsockopt(socket.IPPROTO_IP, socket.IP_MULTICAST_TTL, MULTICAST_TTL)
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)

    if IS_ALL_GROUPS:
        # on this port, receives ALL multicast groups
        sock.bind(('', MCAST_PORT))
    else:
        # on this port, listen ONLY to MCAST_GRP
        sock.bind((MCAST_GRP, MCAST_PORT))

    print("Connected to Multicast")
    mreq = struct.pack("4sl", socket.inet_aton(MCAST_GRP), socket.INADDR_ANY)

    sock.setsockopt(socket.IPPROTO_IP, socket.IP_ADD_MEMBERSHIP, mreq)

    return sock


# Builds a Cot Message from variables passed, will use defaults if any are not passed.
def cot_builder(lat=None, lon=None, how=None, event_type=None, uid=None, link_type=None, msg_remarks=None,
                contact_callsign=None, iconsetpath=None, argb=None):
    time = datetime.datetime.now()

    _keys = [
        'version', 'event_type', 'access', 'qos', 'opex', 'uid', 'time',
        'start', 'stale', 'how', 'point', 'detail', 'remarks']

    my_point = pycot.Point()

    # GPS Location Values Default can be changed.
    if lat is None:
        my_point.lat = '39.76'
    else:
        my_point.lat = lat

    if lon is None:
        my_point.lon = '-84.4975'
    else:
        my_point.lon = lon

    my_point.ce = '45.3'
    my_point.le = '99.5'
    my_point.hae = '-42.6'

    if iconsetpath is not None:
        usericon = pycot.Usericon()
        usericon.iconsetpath = iconsetpath
    if argb is not None:
        color = pycot.Color()
        color.argb = argb

    if link_type is not None:
        link = pycot.Link()
        link.type = link_type

    contact = pycot.Contact()
    if contact_callsign is not None:
        contact.callsign = contact_callsign

    # Where Data is placed unformated as a str
    remarks = pycot.Remarks()
    if remarks is None:
        remarks.value = "{}".format(DATA)
    else:
        remarks.value = msg_remarks

    detail = pycot.Detail()
    if contact_callsign is not None:
        detail.contact = contact
    if link_type is not None:
        detail.link_type = link
    if iconsetpath is not None:
        detail.usericon = usericon
    if argb is not None:
        detail.color = color
    detail.remarks = remarks

    evt = pycot.Event()
    evt.version = '2.0'
    if uid is None:
        evt.uid = "ceder"
    else:
        evt.uid = uid

    if event_type is None:
        evt.event_type = "a-h-A-M-F-U-M"
    else:
        evt.event_type = event_type

    evt.time = time
    evt.start = time
    evt.stale = time + datetime.timedelta(hours=1)
    if how is None:
        evt.how = 'm-g'
    else:
        evt.how = how
    evt.point = my_point
    evt.detail = detail

    # Builds COT message as an XML
    evt.render(standalone=True, pretty=True)

    return evt


# Will parse byte message to Str using spaces and generic titles for COT data
def dataParser(data):
    try:
        # if data is bytes:
        decodeD = data.decode('utf-8')
        msg = decodeD.split(" ")
        print(msg)

        try:
            index = msg.index("lat:")
            lat = msg[index + 1]
        except ValueError:
            lat = None
        try:
            index = msg.index("lon:")
            lon = msg[index + 1]
        except ValueError:
            lon = None
        try:
            index = msg.index("how:")
            how = msg[index + 1]
        except ValueError:
            how = None
        try:
            index = msg.index("uid:")
            uid = msg[index + 1]
        except ValueError:
            uid = None
        try:
            index = msg.index("link_type:")
            event_type = msg[index + 1]
        except ValueError:
            event_type = None
        try:
            index = msg.index("remarks:")
            remarks = msg[index + 1]
        except ValueError:
            remarks = None
        try:
            index = msg.index("link_type:")
            link_type = msg[index + 1]
        except ValueError:
            link_type = None
        try:
            index = msg.index("callsign:")
            callsign = msg[index + 1]
        except ValueError:
            callsign = None
        try:
            index = msg.index("iconsetpath:")
            iconsetpath = msg[index + 1]
        except ValueError:
            iconsetpath = None
        try:
            index = msg.index("argb:")
            argb = msg[index + 1]
        except ValueError:
            argb = None

        evt = cot_builder(lat, lon, how, event_type, uid, link_type, remarks, callsign, iconsetpath, argb)
        return evt
    # except ValueError:
    #     print("Not enough information for COT Message")
    #     pass
    except AttributeError:
        pass
    except UnicodeDecodeError:
        pass


async def protocol_factory(host, port):
    """
    Given a CoT Destination URL, create a Connection Class Instance for the given protocol.

    :param cot_url: CoT Destination URL
    :param fts_token:
    :return:
    """
    reader = None
    writer = None

    os.environ['client_cert'] = '/Users/Cruzethebear/Dropbox/My Mac (MacBook-Pro.lan)/Desktop/TAK/CERTs/peote.p12'
    os.environ[
        'client_key'] = '/Users/Cruzethebear/Dropbox/My Mac (MacBook-Pro.lan)/Desktop/TAK/CERTs/trustore-root.p12'

    client_cafile = os.getenv("PYTAK_TLS_CLIENT_CAFILE")
    client_ciphers = os.getenv(
        "PYTAK_TLS_CLIENT_CIPHERS", pytak.DEFAULT_FIPS_CIPHERS)

    # SSL Context setup:
    ssl_ctx = ssl.SSLContext(ssl.PROTOCOL_TLS_CLIENT)
    ssl_ctx.options |= ssl.OP_NO_TLSv1
    ssl_ctx.options |= ssl.OP_NO_TLSv1_1
    ssl_ctx.set_ciphers(client_ciphers)
    ssl_ctx.check_hostname = False
    ssl_ctx.verify_mode = ssl.CERT_NONE

    ssl_ctx.load_cert_chain(pem, keyfile=key, password="atakatak")

    if client_cafile:
        ssl_ctx.load_verify_locations(cafile=client_cafile)

    ssl_ctx.check_hostname = False
    ssl_ctx.verify_mode = ssl.CERT_NONE

    ssl_ctx.check_hostname = False

    reader, writer = await asyncio.open_connection(host, port, ssl=ssl_ctx)

    return reader, writer


async def SSLCoTSender(bmsg):
    print("connecting to Server")
    reader, writer = await protocol_factory(host, port)
    msg = dataParser(bmsg)
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
        evt = "{}".format(cot_builder(DATA))
        b_evt = evt.encode('utf-8')

        print(sys.stderr, 'sending "%s"' % b_evt)
        # Send event through Mulitcast stream
        sock.sendto(b_evt, (MCAST_GRP, MCAST_PORT))
    finally:
        sock.close()


def MailMan(dataList, SEND, MCAST_GRP, MCAST_PORT, IS_ALL_GROUPS, sock):
    while True:
        if len(dataList) > 0:
            if SEND == "SSL":
                for msg in dataList:
                    print("Sending on SSL")
                    asyncio.run(SSLCoTSender(msg))
                    dataList.remove(msg)
            elif SEND == "MCAST":
                for msg in dataList:
                    print("Sending on Multicast")
                    asyncio.run(MCASTCoTSender(msg, MCAST_GRP, MCAST_PORT, IS_ALL_GROUPS, sock))
                    dataList.remove(msg)
        else:
            pass


if __name__ == "__main__":
    MCAST_GRP = '239.2.3.1'
    MCAST_PORT = 6969
    IS_ALL_GROUPS = True
    RECV = "LHOST"
    SEND = "SSL"
    dataList = []

    if RECV == "MCAST":
        sock = connect_MCAST(MCAST_GRP, MCAST_PORT, IS_ALL_GROUPS)
    elif RECV == "LHOST":
        print("Connecting Local Host")
        s = connect_LocalH()

    # testmsg = dataParser(testString)
    # test_str = "{}".format(testmsg)
    # print(testmsg)
    # b_test_evt = test_str.encode('utf-8')
    # asyncio.run(SSLCoTSender(b))

    # sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM, socket.IPPROTO_UDP)
    # # sock.setsockopt(socket.IPPROTO_IP, socket.IP_MULTICAST_TTL, MULTICAST_TTL)
    # sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    #
    # if IS_ALL_GROUPS:
    #     # on this port, receives ALL multicast groups
    #     sock.bind(('', MCAST_PORT))
    # else:
    #     # on this port, listen ONLY to MCAST_GRP
    #     sock.bind((MCAST_GRP, MCAST_PORT))
    #
    # print("Connected to Multicast")
    # mreq = struct.pack("4sl", socket.inet_aton(MCAST_GRP), socket.INADDR_ANY)
    #
    # sock.setsockopt(socket.IPPROTO_IP, socket.IP_ADD_MEMBERSHIP, mreq)

    while True:
        print('waiting to receive')
        try:
            if RECV == "MCAST":
                data, server = sock.recvfrom(10240)
            elif RECV == "LHOST":
                data, addr = s.recvfrom(10240)

            print(data)
            # dataList.append(data)
            # thread = threading.Thread(target=MailMan(dataList, SEND, MCAST_GRP, MCAST_PORT, IS_ALL_GROUPS, sock),
            #                           args=(1,), daemon=True)
            # thread.start()
            try:
                if SEND == "SSL":
                    print("Sending on SSL")
                    asyncio.run(SSLCoTSender(data))
                elif SEND == "MCAST":
                    print("Sending on Multicast")
                    asyncio.run(MCASTCoTSender(data, MCAST_GRP, MCAST_PORT, IS_ALL_GROUPS, sock))
            except TypeError:
                pass
            finally:
                pass
        except socket.timeout:
            print(sys.stderr, 'timed out, no more responses')
            sock.close()
            break
        except KeyboardInterrupt:
            print('Interrupted')
            sock.close()
            sys.exit(0)
        else:
            if SEND == "MCAST":
                print('received {} from {}'.format(data, server))
            if SEND == "SSL":
                print('received {}'.format(data))
