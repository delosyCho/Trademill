import numpy
import socket
import os
import sys
import urllib.parse
import urllib.request
import threading

import numpy as np
import RegressionModel
import tensorflow as tf

class DataHolder:

    def test(self, name):
        while True:
            print(name)

    def __init__(self):
        self.Regression_Model = 0

        self.PriceData = 0

        self.IP = ''
        self.Port = ''
        self.Msg_Buffer = ''

        self.conn = 0

        self.Prepare_Network(self.Port)

    def Prepare_Network(self, port):
        HOST = ''  # 호스트를 지정하지 않으면 가능한 모든 인터페이스를 의미한다.
        PORT = 15231  # 포트지정

        s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        s.bind((HOST, PORT))
        s.listen(1)  # 접속이 있을때까지 기다림
        self.conn, addr = s.accept()  # 접속 승인
        print("Joined!")

        def Get_Data():
            while True:
                data = self.conn.recv(1024)

                if not data: break

                msg = str(data)

                temp = msg.replace('b\'', '')
                temp = temp.replace('\'', '')

                check = temp.split('&')
                if len(check) > 1:
                    print(msg)

                    if check[0] == '@exp_data':
                        Expectation_Propagate(check[1], check[2])

                    elif check[0] == "@Train":
                        print('msg check', msg)
                        Process_Buffer_Training(check[1], check[2], check[3])

                else:

                    self.Msg_Buffer += temp
                    print(msg)
                    print()
                    print()
                    print(self.Msg_Buffer)
            self.conn.close()
            print("Closed")

        def RequestData():
            while True:
                Code = str(input())
                print(Code)
                TK = Code.split('#')
                if(TK[0] == '@rqdata'):
                    self.Msg_Buffer = ""
                    self.conn.send("".join(Code).encode(encoding='utf-8'))  # 받은 데이터를 그대로 클라이언트에 전송
                    print(Code)
                elif(TK[0] == '@setData'):
                    Process_Buffer_Training(path=TK[1])
                elif(TK[0] == '@Propagate'):
                    Process_Buffer_Propagate(path=TK[1])
                else:
                    self.conn.send("".join(Code).encode(encoding='utf-8'))

        def temp_Data(path, reset_graph=False):
            Data_TKs = self.Msg_Buffer.split('@')

            self.PriceData = np.zeros(dtype=np.float32, shape=[100])

            for i in range(len(Data_TKs) - 1):
                self.PriceData[i] = float(Data_TKs[i])

            print('Price Data Shape', self.PriceData.shape)

            tf.reset_default_graph()

            Regression_Model = RegressionModel.simple_RNN_Regression(20, 10, path='15')
            Regression_Model.set_path(path)
            Regression_Model.setBatch(self.PriceData, 99)
            #Regression_Model.Training(1)
            Regression_Model.Propagate()

        def Process_Buffer_Training(path, code='', epoch_=''):
            Data_TKs = self.Msg_Buffer.split('@')

            epoch = 0
            try:
                epoch = int(epoch_)
            except:
                epoch = 1000

            self.PriceData = np.zeros(dtype=np.float32, shape=[len(Data_TKs) - 1])

            for i in range(len(Data_TKs) - 1):
                self.PriceData[i] = float(Data_TKs[i])

            print('Price Data Shape', self.PriceData.shape)

            tf.reset_default_graph()

            Regression_Model = RegressionModel.simple_RNN_Regression(20, 10, path=path + code)
            Regression_Model.setBatch(self.PriceData, len(Data_TKs) - 1)
            Regression_Model.Training(epoch=epoch)

            msg = '@Trained#' + path + '#' + code
            self.conn.send("".join(msg).encode(encoding='utf-8'))

            self.Msg_Buffer = ""

        def Process_Buffer_Propagate(path, code=''):
            Data_TKs = self.Msg_Buffer.split('@')

            self.PriceData = np.zeros(dtype=np.float32, shape=[len(Data_TKs) - 1])

            for i in range(len(Data_TKs) - 1):
                self.PriceData[i] = float(Data_TKs[i])

            print('Price Data Shape', self.PriceData.shape)

            tf.reset_default_graph()

            Regression_Model = RegressionModel.simple_RNN_Regression(20, 10, path=path + code)
            Regression_Model.setBatch(self.PriceData, len(Data_TKs) - 1)
            result, batch_size = Regression_Model.Propagate()

            msg = ''

            for i in range(batch_size):
                line = ''

                for j in range(20):
                    line += str(result[i, j]) + '!'
                msg += line + '%'

                self.conn.send("".join(msg).encode(encoding='utf-8'))

            self.Msg_Buffer = ""

        def Expectation_Propagate(path, code=''):
            Data_TKs = self.Msg_Buffer.split('@')

            self.PriceData = np.zeros(dtype=np.float32, shape=[len(Data_TKs) - 1])

            for i in range(len(Data_TKs) - 1):
                self.PriceData[i] = float(Data_TKs[i])

            print('Price Data Shape', self.PriceData.shape)

            tf.reset_default_graph()

            Regression_Model = RegressionModel.simple_RNN_Regression(20, 10, path=path + code)
            Regression_Model.setPropagateBatch(self.PriceData)
            result, batch_size, Max_Value = Regression_Model.Propagate()

            #print(result)
            #print(batch_size)

            if batch_size == -1:
                msg = '@notTrain#' + path
                self.conn.send("".join(msg).encode(encoding='utf-8'))
                self.Msg_Buffer = ''
            else:
                print(result[0])
                print(result.shape)
                msg = ''

                line = '@Exp_data#path#code#'

                for j in range(20):
                    line += str(int((result[0, j] * Max_Value))) + '!'

                self.conn.send("".join(msg).encode(encoding='utf-8'))
                self.Msg_Buffer = ''
                print('Expectation Done!', msg)

        #temp_Data('17')
        tf.reset_default_graph()
        temp_Data('0', True)

        threading._start_new_thread(Get_Data, ())
        print("Thread Setting!")
        threading._start_new_thread(RequestData, ())
        print("Sent!")



