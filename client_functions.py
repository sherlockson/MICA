#!/usr/bin/env python
# -*- coding: utf-8 -*-

"""PyTAK Functions."""

import asyncio
import os
import socket
import ssl
import struct


import pytak
import pytak.asyncio_dgram

host = '52.222.45.68'
port = 8089
key = os.path.abspath(os.path.expanduser('~/Downloads/tak.key'))
pem = os.path.abspath(os.path.expanduser('~/Downloads/tak.pem'))

print(key, pem)

async def protocol_factory(host, port):
    """
    Given a CoT Destination URL, create a Connection Class Instance for the given protocol.

    :param cot_url: CoT Destination URL
    :param fts_token:
    :return:
    """
    reader = None
    writer = None

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


def connect_LocalH():
    HOST = '127.0.0.1'
    PORT = 8080
    ADDR = (HOST, PORT)

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    sock.bind(ADDR)

    return sock


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