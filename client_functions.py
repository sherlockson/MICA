#!/usr/bin/env python
# -*- coding: utf-8 -*-

import asyncio
import os
import socket
import ssl
import struct

key = os.path.abspath(os.path.expanduser('~/Downloads/tak.key'))
pem = os.path.abspath(os.path.expanduser('~/Downloads/tak.cer'))

async def protocol_factory(SSL_PORT, SSL_GRP):
    """
    Given a CoT Destination port and Host address, create a Connection Class Instance.

    :param host: server address
    :param port: server port
    :return: reader, writer
    """
    reader = None
    writer = None


    # SSL Context setup:
    ssl_ctx = ssl.SSLContext(ssl.PROTOCOL_TLS_CLIENT)
    ssl_ctx.options |= ssl.OP_NO_TLSv1
    ssl_ctx.options |= ssl.OP_NO_TLSv1_1
    ssl_ctx.check_hostname = False
    ssl_ctx.verify_mode = ssl.CERT_NONE
    ssl_ctx.load_cert_chain(pem, keyfile=key, password="atakatak")


    ssl_ctx.check_hostname = False
    ssl_ctx.verify_mode = ssl.CERT_NONE

    ssl_ctx.check_hostname = False

    reader, writer = await asyncio.open_connection(SSL_GRP, SSL_PORT, ssl=ssl_ctx)

    return reader, writer


def connect_LocalH():
    """
    Create a UDP connection using Local host
    :return: Local host socket
    """
    HOST = '127.0.0.1'
    PORT = 8080
    ADDR = (HOST, PORT)

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    sock.bind(ADDR)


    return sock


def connect_MCAST(MCAST_GRP, MCAST_PORT, IS_ALL_GROUPS):
    """
    Given a CoT Mulitcast Destination port and Host address, create a Connection Class Instance.

    :param MCAST_GRP: server address
    :param MCAST_PORT: server port
    :param IS_ALL_GROUPS: create socket connection to all address on port, or just a specific address and port.
    :return: multicast socket
    """

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