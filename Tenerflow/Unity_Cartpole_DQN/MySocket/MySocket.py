import socket
import threading
import marshal
import numpy as np
from struct import *

class MySocket:
    def __init__(self):
        self.HOST = "127.0.0.1"
        self.PORT = 8080
        self.s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.s.connect((self.HOST, self.PORT))
        self.s.setblocking(True)

    def sendingMsg(self, action):
        data = pack("f", action)
        self.s.send(data)

    def getStep(self):
       data = self.s.recv(100)
       pktFormat = 'fffff'
       pktSize = calcsize(pktFormat)
       a, b, c, d, e = unpack(pktFormat,data[:pktSize])
       #print("RecvData : ", a , " ", b, " ", c, " " , d, " ", e)
       data = 0

       return np.array([a,b,c,d]),1,e

    