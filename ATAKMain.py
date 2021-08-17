#!/usr/bin/env python3.9

import asyncio
import socket
import sys
import datetime
import ATAKXML
import client_functions
import ATAKSenders

# Message to be sent if no Data is passed for cot_builder
DATA = "this is a filler constant"

testString = b'lat: 39.825297 lon: -84.027601 uid: point 2 how: h-g-i-g-o link_type: a-f-G-I-B remarks: 200.00% argb: -65536 iconsetpath: COT_MAPPING_SPOTMAP/b-m-p-s-m/-256'
dataList = []


# Builds a Cot Message from variables passed, will use defaults if any are not passed.
def cot_builder(lat=None, lon=None, how=None, event_type=None, uid=None, link_type=None, msg_remarks=None,
                contact_callsign=None, iconsetpath=None, argb=None):
    time = datetime.datetime.now()

    _keys = [
        'version', 'event_type', 'access', 'qos', 'opex', 'uid', 'time',
        'start', 'stale', 'how', 'point', 'detail', 'remarks']

    my_point = ATAKXML.Point()

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
        usericon = ATAKXML.Usericon()
        usericon.iconsetpath = iconsetpath
    if argb is not None:
        color = ATAKXML.Color()
        color.argb = argb

    if link_type is not None:
        link = ATAKXML.Link()
        link.type = link_type

    contact = ATAKXML.Contact()
    if contact_callsign is not None:
        contact.callsign = contact_callsign

    # Where Data is placed unformated as a str
    remarks = ATAKXML.Remarks()
    if remarks is None:
        remarks.value = "{}".format(DATA)
    else:
        remarks.value = msg_remarks

    detail = ATAKXML.Detail()
    if contact_callsign is not None:
        detail.contact = contact
    if link_type is not None:
        detail.link_type = link
    if iconsetpath is not None:
        detail.usericon = usericon
    if argb is not None:
        detail.color = color
    detail.remarks = remarks

    evt = ATAKXML.Event()
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


def MailMan(dataList, SEND, MCAST_GRP, MCAST_PORT, IS_ALL_GROUPS, sock):
    while True:
        if len(dataList) > 0:
            if SEND == "SSL":
                for msg in dataList:
                    print("Sending on SSL")
                    asyncio.run(ATAKSenders.SSLCoTSender(msg))
                    dataList.remove(msg)
            elif SEND == "MCAST":
                for msg in dataList:
                    print("Sending on Multicast")
                    asyncio.run(ATAKSenders.MCASTCoTSender(msg, MCAST_GRP, MCAST_PORT, IS_ALL_GROUPS, sock))
                    dataList.remove(msg)
        else:
            pass


def main(MCAST_GRP, MCAST_PORT, IS_ALL_GROUPS, RECV, SEND):
    if RECV == "MCAST":
        sock = client_functions.connect_MCAST(MCAST_GRP, MCAST_PORT, IS_ALL_GROUPS)
    elif RECV == "LHOST":
        print("Connecting Local Host")
        s = client_functions.connect_LocalH()

    while True:
        print('waiting to receive')
        try:
            if RECV == "MCAST":
                data, server = sock.recvfrom(10240)
            elif RECV == "LHOST":
                data, addr = s.recvfrom(10240)

            print(data)
            try:
                if SEND == "SSL":
                    print("Sending on SSL")
                    asyncio.run(ATAKSenders.SSLCoTSender(data))
                elif SEND == "MCAST":
                    print("Sending on Multicast")
                    asyncio.run(ATAKSenders.MCASTCoTSender(data, MCAST_GRP, MCAST_PORT, IS_ALL_GROUPS, sock))
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


if __name__ == "__main__":
    MCAST_GRP = '239.2.3.1'
    MCAST_PORT = 6969
    IS_ALL_GROUPS = True
    RECV = "LHOST"
    SEND = "SSL"

    main(MCAST_GRP, MCAST_PORT, IS_ALL_GROUPS, RECV, SEND)
