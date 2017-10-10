import numpy as np
import tensorflow as tf
import os
from pathlib import Path

class simple_RNN_Regression:

    def seq_length(self, sequence):
        used = tf.sign(tf.reduce_max(tf.abs(sequence), reduction_indices=2))
        length = tf.reduce_sum(used, reduction_indices=1)
        length = tf.cast(length, tf.int32)
        return length

    def setInput(self, PriceData):
        self.Data = np.zeros(dtype=np.float32, shape=[1, self.sequence_Length, 1])
        self.Batch_Size = 1
        self.Max_Value = -1

        for i in range(self.sequence_Length):
            self.Data[0, i, 0] = PriceData[i]

    def set_path(self, path):
        self.save_Path = path

    def setPropagateBatch(self, PriceData):
        self.Data = np.zeros(dtype=np.float32, shape=[1, self.sequence_Length, 1])
        max_v = -999

        for i in range(20):
            if max_v < PriceData[i]:
                max_v = PriceData[i]

        for i in range(20):
            self.Data[0, i, 0] = PriceData[i] / max_v

        self.Max_Value = max_v

    def setBatch(self, PriceData, DataLength):
        #print(PriceData)

        max_v = -999

        for i in range(DataLength):
            if max_v < PriceData[i]:
                max_v = PriceData[i]

        self.Data = np.zeros(dtype=np.float32, shape=[DataLength - self.sequence_Length - self.Expectation_Time + 1,
                                                      self.sequence_Length, 1])
        self.forecast_Data = np.zeros(dtype=np.float32, shape=[DataLength - self.sequence_Length - self.Expectation_Time + 1,
                                                               self.sequence_Length, 1])
        self.Batch_Size = DataLength - self.sequence_Length - self.Expectation_Time + 1

        for i in range(DataLength - self.sequence_Length - self.Expectation_Time + 1):
            for j in range(self.sequence_Length):
                self.Data[i, j, 0] = PriceData[j + i] / max_v
                #print(self.Data[i, j, 0])
                #print(PriceData[j + i + self.Expectation_Time])
                #print(PriceData[j + i + self.Expectation_Time] / max_v)
                #print(max_v)
                self.forecast_Data[i, j, 0] = PriceData[j + i + self.Expectation_Time] / max_v

        self.Max_Value = max_v
        print('shape', self.Data)

    def __init__(self, length, exp_Index, path):
        mypath = 'C:/Users/Administrator/Documents/TradeMill_ModelSave/' + path
        print('path:', path)

        if not os.path.isdir(mypath):
            os.mkdir(mypath)

        self.save_Path = mypath

        self.rnn_Cell = tf.nn.rnn_cell.BasicLSTMCell(1)
        self.sequence_Length = length
        self.Expectation_Time = length

        self.input = tf.placeholder(dtype=tf.float32, shape=[None, self.sequence_Length, 1])
        self.Data = 0

        self.Label = tf.placeholder(dtype=tf.float32, shape=[None, self.sequence_Length, 1])
        self.forecast_Data = 0

        self.Batch_Size = 0

    def Model(self):
        with tf.variable_scope("Rnn_Layer") as scope:
            output, encoding = tf.nn.dynamic_rnn(cell=self.rnn_Cell,
                                                 inputs=self.input,
                                                 sequence_length=self.seq_length(self.input),
                                                 dtype=tf.float32)
        return tf.sigmoid(output)

    def Propagate(self):
        print('propagate Start')

        is_Exist = False

        my_file = Path(self.save_Path + '/checkpoint')
        if my_file.is_file():
            is_Exist = True

        if is_Exist == False:
            print('file doesn\'t exist!!')
            print(self.save_Path + '/regression_rnn.ckpf')
            return -1, -1, -1

        with tf.Session() as sess:
            output = self.Model()
            sess.run(tf.global_variables_initializer())

            print('input')
            print(self.Data)

            feed_dict = {self.input: self.Data}
            output_result = sess.run(output, feed_dict=feed_dict)

            result = np.zeros(shape=[self.Batch_Size, self.sequence_Length])

            saver = tf.train.Saver()
            save_path = saver.restore(sess, self.save_Path + '/regression_rnn.ckpf')

            print('result')
            print(output_result)

            for i in range(self.Batch_Size):
                for j in range(self.sequence_Length):
                    result[i, j] = output_result[i, j, 0]

            return output_result, 1, self.Max_Value

    def Training(self, epoch=10000, reset_graph=False):
        if reset_graph:
            tf.reset_default_graph()

        with tf.Session() as sess:
            output = self.Model()
            loss = tf.reduce_mean(tf.square(tf.subtract(output, self.Label)))
            train_step = tf.train.AdamOptimizer(learning_rate=0.0003).minimize(loss)

            sess.run(tf.global_variables_initializer())

            feed_dict = {self.input: self.Data, self.Label: self.forecast_Data}

            for k in range(epoch):
                sess.run(train_step, feed_dict=feed_dict)

                if k % 400 == 0:
                    saver = tf.train.Saver()
                    save_path = saver.save(sess, self.save_Path + '/regression_rnn.ckpf')

                    print(sess.run(loss, feed_dict=feed_dict))

            saver = tf.train.Saver()
            save_path = saver.save(sess, self.save_Path + '/regression_rnn.ckpf')

        print("Complete")