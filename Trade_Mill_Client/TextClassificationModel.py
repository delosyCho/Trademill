import tensorflow as tf
import numpy
import data_processor as dp
import POS_Embed

class Improved_AoA_Reader:

    def seq_length(self, sequence):
        used = tf.sign(tf.reduce_max(tf.abs(sequence), reduction_indices=2))
        length = tf.reduce_sum(used, reduction_indices=1)
        length = tf.cast(length, tf.int32)
        return length

    def weight_variable(self, shape):
        initial = tf.truncated_normal(shape, stddev=0.1)
        return tf.Variable(initial)

    def bias_variable(self, shape):
        initial = tf.constant(0.1, shape=shape)
        return tf.Variable(initial)

    def conv2d(self, x, W):
        return tf.nn.conv2d(x, W, strides=[1, 1, 1, 1], padding='VALID')

    def max_pool_k(self, x, k):
        return tf.nn.max_pool(x, ksize=[1, k, 1, 1],
                              strides=[1, 1, 1, 1], padding='VALID')

    def __init__(self, is_Para = True):
        self.dataset = dp.Data_holder()
        self.dataset.set_batch()
        #self.dataset.get_POS()

        self.question = 0
        self.paragraph = 0
        self.POS_Embeddings = 0
        self.POS_Q_Embeddings = 0
        self.start_index = 0
        self.stop_index = 0
        self.attention_Label = 0

        self.embedding_size = 50
        self.POS_Embedding_Size = 128
        self.batch = 500
        self.p_length = 125
        self.q_length = 30

        self.paragraph, self.question, self.start_index, self.stop_index, self.POS_Embeddings = self.dataset.get_next_batch()

        self.cell_Q_Enc_fw = tf.nn.rnn_cell.BasicLSTMCell(32)
        self.cell_Q_Enc_bw = tf.nn.rnn_cell.BasicLSTMCell(32)

        self.cell_P_Enc_fw = tf.nn.rnn_cell.BasicLSTMCell(32)
        self.cell_P_Enc_bw = tf.nn.rnn_cell.BasicLSTMCell(32)

        self.cell_POS_Enc_fw = tf.nn.rnn_cell.BasicLSTMCell(32)
        self.cell_POS_Enc_bw = tf.nn.rnn_cell.BasicLSTMCell(32)

        self.cell_output_Index_fw = tf.nn.rnn_cell.BasicLSTMCell(1)
        self.cell_output_Index_bw = tf.nn.rnn_cell.BasicLSTMCell(1)

        self.cell_modeling_fw = tf.nn.rnn_cell.BasicLSTMCell(64)
        self.cell_modeling_bw = tf.nn.rnn_cell.BasicLSTMCell(64)

        self.cell_modeling_fw_ = tf.nn.rnn_cell.BasicLSTMCell(32)
        self.cell_modeling_bw_ = tf.nn.rnn_cell.BasicLSTMCell(32)

        self.W_conv_2 = self.weight_variable([2, 1, 1, 4])
        self.W_conv_3 = self.weight_variable([3, 1, 1, 4])
        self.W_conv_4 = self.weight_variable([4, 1, 1, 4])
        self.W_conv_5 = self.weight_variable([5, 1, 1, 4])

        self.Weight_Paragraph = self.weight_variable(shape=[1, 64, 64])
        self.Weight_POS = self.weight_variable(shape=[1, 64, 64])

        self.Bias_Paragraph = self.bias_variable(shape=[64])
        self.Bias_POS = self.bias_variable(shape=[64])

        self.Weight_Dec_Input = self.weight_variable(shape=[1, 64 * 5, 128])
        self.Bias_Dec_Input = self.bias_variable(shape=[128])

        self.Weight_D_Out = self.weight_variable(shape=[1, 128, 1])
        self.Bias_D_Out = self.weight_variable(shape=[1])

        self.Weight_D_Out_Stop = self.weight_variable(shape=[1, 128, 1])
        self.Bias_D_Out_Stop = self.weight_variable(shape=[1])

        self.W_fc = self.weight_variable([1, 16, 1])
        self.b_fc = self.bias_variable([1])

        self.output_Start = None
        self.output_Stop = None

        self.x_q_holer = tf.placeholder(dtype=tf.float32, shape=[self.batch, self.q_length, self.embedding_size],
                                        name='x_q_holer')
        self.x_p_holer = tf.placeholder(dtype=tf.float32, shape=[self.batch, self.p_length, self.embedding_size],
                                        name='x_p_holer')

        self.x_pos_q_holder = tf.placeholder(dtype=tf.float32, shape=[self.batch, self.q_length, self.POS_Embedding_Size],
                                        name='x_POS_holer')

        self.x_pos_holder = tf.placeholder(dtype=tf.float32, shape=[self.batch, self.p_length, 85],
                                        name='x_POS_holer')

    def set_batch(self, q, p, sta, sto):
        self.question = q
        self.paragraph = p
        self.start_index = sta
        self.stop_index = sto

    def training_classification(self, training_epoch):
        with tf.Session() as sess:
            self.paragraph, self.question, self.start_index, self.stop_index, self.attention_Label = self.dataset.get_next_batch()

            Att_L = tf.placeholder(tf.float32, shape=[self.batch, 1], name='Attention_Label')

            S_Vector = self.model()

            with tf.variable_scope("classification") as scope:
                S_Vector_ = tf.expand_dims(S_Vector, axis=3)

                conv2 = self.conv2d(S_Vector_, self.W_conv_2)
                conv3 = self.conv2d(S_Vector_, self.W_conv_3)
                conv4 = self.conv2d(S_Vector_, self.W_conv_4)
                conv5 = self.conv2d(S_Vector_, self.W_conv_5)
                print("Conv", conv2, conv3, conv4, conv5)
                output2 = self.max_pool_k(conv2, self.P_Length - 1)
                output3 = self.max_pool_k(conv3, self.P_Length - 2)
                output4 = self.max_pool_k(conv4, self.P_Length - 3)
                output5 = self.max_pool_k(conv5, self.P_Length - 4)

                output2 = tf.reshape(output2, shape=[self.batch, -1])
                output3 = tf.reshape(output3, shape=[self.batch, -1])
                output4 = tf.reshape(output4, shape=[self.batch, -1])
                output5 = tf.reshape(output5, shape=[self.batch, -1])
                print("Shape Conv:", output2, output3, output4, output5)
                flat_output = tf.concat([output2, output3, output4, output5], axis=1)
                output = tf.nn.relu(tf.matmul(flat_output, self.W_fc) + self.b_fc)

                scope.reuse_variables()

            Probability_Attention = (output - Att_L) * (output - Att_L)
            loss = tf.reduce_sum(Probability_Attention)

            train_step = tf.train.AdamOptimizer(0.01).minimize(loss)

            sess.run(tf.initialize_all_variables())

            while self.dataset.whole_batch_index < training_epoch:
                self.paragraph, self.question, self.start_index, self.stop_index, self.attention_Label = self.dataset.get_next_batch()

                sess.run(train_step, feed_dict={self.x_q_holer: self.question, self.x_p_holer: self.paragraph,
                                                Att_L: self.attention_Label})

                if self.dataset.batch_index == self.batch:
                    print(self.dataset.whole_batch_index, sess.run(loss, feed_dict={self.x_q_holer: self.question,
                                                            self.x_p_holer: self.paragraph, Att_L: self.attention_Label}))
                    #print(self.attention_Label)
            saver = tf.train.Saver()
            save_path = saver.save(sess, 'C:/Users/Administrator/Desktop/PAIG_Model_Saver/aoa_Reader.ckpf')

        return 0

    def training_classification_continue(self, training_epoch):
        with tf.Session() as sess:
            saver = tf.train.Saver()
            save_path = saver.restore(sess, 'C:/Users/Administrator/Desktop/PAIG_Model_Saver/aoa_Reader.ckpf')

            self.paragraph, self.question, self.start_index, self.stop_index, self.attention_Label = self.dataset.get_next_batch()

            Att_L = tf.placeholder(tf.float32, shape=[self.batch, 1], name='Attention_Label')

            S_Vector = self.model()

            with tf.variable_scope("classification") as scope:
                S_Vector_ = tf.expand_dims(S_Vector, axis=3)

                conv2 = self.conv2d(S_Vector_, self.W_conv_2)
                conv3 = self.conv2d(S_Vector_, self.W_conv_3)
                conv4 = self.conv2d(S_Vector_, self.W_conv_4)
                conv5 = self.conv2d(S_Vector_, self.W_conv_5)
                print("Conv", conv2, conv3, conv4, conv5)
                output2 = self.max_pool_k(conv2, self.P_Length - 1)
                output3 = self.max_pool_k(conv3, self.P_Length - 2)
                output4 = self.max_pool_k(conv4, self.P_Length - 3)
                output5 = self.max_pool_k(conv5, self.P_Length - 4)

                output2 = tf.reshape(output2, shape=[self.batch, -1])
                output3 = tf.reshape(output3, shape=[self.batch, -1])
                output4 = tf.reshape(output4, shape=[self.batch, -1])
                output5 = tf.reshape(output5, shape=[self.batch, -1])
                print("Shape Conv:", output2, output3, output4, output5)
                flat_output = tf.concat([output2, output3, output4, output5], axis=1)
                output = tf.nn.relu(tf.matmul(flat_output, self.W_fc) + self.b_fc)

                scope.reuse_variables()

            Probability_Attention = (output - Att_L) * (output - Att_L)
            loss = tf.reduce_sum(Probability_Attention)

            train_step = tf.train.AdamOptimizer(0.01).minimize(loss)

            sess.run(tf.initialize_all_variables())

            saver = tf.train.Saver()
            save_path = saver.restore(sess, 'C:/Users/Administrator/Desktop/PAIG_Model_Saver/aoa_Reader.ckpf')

            while self.dataset.whole_batch_index < training_epoch:
                self.paragraph, self.question, self.start_index, self.stop_index, self.attention_Label = self.dataset.get_next_batch()

                sess.run(train_step, feed_dict={self.x_q_holer: self.question, self.x_p_holer: self.paragraph,
                                                Att_L: self.attention_Label})

                if self.dataset.batch_index == self.batch:
                    print(self.dataset.whole_batch_index, sess.run(loss, feed_dict={self.x_q_holer: self.question,
                                                            self.x_p_holer: self.paragraph, Att_L: self.attention_Label}))
                    #print(self.attention_Label)
            saver = tf.train.Saver()
            save_path = saver.save(sess, 'C:/Users/Administrator/Desktop/PAIG_Model_Saver/aoa_Reader.ckpf')

        return 0

    def test_classification(self):
        with tf.Session() as sess:
            saver = tf.train.Saver()
            save_path = saver.restore(sess, 'C:/Users/Administrator/Desktop/PAIG_Model_Saver/aoa_Reader.ckpf')

            self.paragraph, self.question, self.start_index, self.stop_index, self.attention_Label = self.dataset.get_next_batch()

            Att_L = tf.placeholder(tf.float32, shape=[self.batch, 1], name='Attention_Label')

            S_Vector = self.model()

            with tf.variable_scope("classification") as scope:
                S_Vector_ = tf.expand_dims(S_Vector, axis=3)

                conv2 = self.conv2d(S_Vector_, self.W_conv_2)
                conv3 = self.conv2d(S_Vector_, self.W_conv_3)
                conv4 = self.conv2d(S_Vector_, self.W_conv_4)
                conv5 = self.conv2d(S_Vector_, self.W_conv_5)
                print("Conv", conv2, conv3, conv4, conv5)
                output2 = self.max_pool_k(conv2, self.P_Length - 1)
                output3 = self.max_pool_k(conv3, self.P_Length - 2)
                output4 = self.max_pool_k(conv4, self.P_Length - 3)
                output5 = self.max_pool_k(conv5, self.P_Length - 4)

                output2 = tf.reshape(output2, shape=[self.batch, -1])
                output3 = tf.reshape(output3, shape=[self.batch, -1])
                output4 = tf.reshape(output4, shape=[self.batch, -1])
                output5 = tf.reshape(output5, shape=[self.batch, -1])
                print("Shape Conv:", output2, output3, output4, output5)
                flat_output = tf.concat([output2, output3, output4, output5], axis=1)
                output = tf.nn.relu(tf.matmul(flat_output, self.W_fc) + self.b_fc)

                scope.reuse_variables()

            Probability_Attention = (output - Att_L) * (output - Att_L)

            sess.run(tf.initialize_all_variables())

            saver = tf.train.Saver()
            save_path = saver.restore(sess, 'C:/Users/Administrator/Desktop/PAIG_Model_Saver/aoa_Reader.ckpf')

            self.paragraph, self.question, self.start_index, self.stop_index, self.attention_Label, self.POS_Embeddings\
                = self.dataset.get_test_batch()

            test_result = sess.run(Probability_Attention, feed_dict={self.x_q_holer: self.question, self.x_p_holer: self.paragraph,
                                            Att_L: self.attention_Label})

            output_result = sess.run(output,
                                   feed_dict={self.x_q_holer: self.question, self.x_p_holer: self.paragraph,
                                              Att_L: self.attention_Label})

            check = 0
            check_ = 0
            check2 = 0
            check2_ = 0

            for i in range(self.batch):
                if self.attention_Label[i] == 1:
                    if output_result[i] > 0.5:
                        #print("Right: ", self.dataset.pa,self.question)
                        check = check + 1
                    else:
                        check_ = check_ + 1
                        #print("No attention: ", self.paragraph, self.question)
                else:
                    if output_result[i] < 0.5:
                        check2 = check2 + 1
                        #print("Right: ", self.paragraph, self.question)
                    else:
                        check2_ = check2_ + 1
                        #print("Wrong Attention: ", self.paragraph, self.question)

            check3 = check + check2

            print(check3, "/", self.batch, " ", check, check2, check_, check2_)

        return 0

