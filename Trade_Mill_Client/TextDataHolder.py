import tensorflow as tf
import json
import codecs
import numpy
import copy
import POS_Embed
import nltk

class Data_holder:

    def find_all(self, a_str, sub):
        start = 0
        while True:
            start = a_str.find(sub, start)
            if start == -1: return
            yield start
            start += len(sub)  # use start += 1 to find overlapping matches

    def __init__(self):
        self.tagged = 0

        self.p_length = 125
        self.q_length = 30
        self.s_length = 100

        self.embedding_size = 50
        self.batch_size = 500

        self.whole_batch_index = 0
        self.batch_index = 0
        self.numberOf_available_question = 0

        self.numberOf_available_sentence = 0

        self.batch = []
        self.paragraph_index = []
        self.question_batch = []
        self.paragraph_arr = []
        self.paragraph_arr_ = []
        self.attention_Label = []

        self.start_index_batch = []
        self.stop_index_batch = []

        self.SA_Label = []
        self.SA_question = []
        self.SA_paragraph = []
        self.SA_start = []
        self.SA_end = []

        self.Tagged_POS_Batch = []

        self.vocab_size = 85

        self.in_path = "C:\\Users\\Administrator\\Desktop\\qadataset\\train-v1.1.json"
        #path of your train data json file

        self.data = json.load(open(self.in_path, 'r'))

        in_path_glove = "C:\\Users\\Administrator\\Desktop\\qadataset\\glove.6B.50d.txt"
        glove_f = codecs.open(in_path_glove, 'r', 'utf-8')

        self.words = []
        self.vectors = []

        for line in glove_f:
            tokens = line.split(' ')
            self.words.append(tokens.pop(0))
            self.vectors.append(tokens)

        self.vectors = numpy.array((self.vectors), 'f').reshape((-1, self.embedding_size))

        self.dictionary = numpy.array(self.words)
        self.glove_arg_index = self.dictionary.argsort()
        self.dictionary.sort()

    def read_POS_From_Embed(self):
        result = numpy.zeros(shape=[13000, self.p_length], dtype='i')
        result2 = numpy.zeros(shape=[13000, self.p_length], dtype='i')

        f = open('C:\\Users\\Administrator\Desktop\\PAIG_Model_Saver\\POS_Tag_Result_Saver\\pos_tag_result')
        lines = f.readlines()

        for i in range(len(lines)):
            TK = lines[i].split('@')

            for j in range(self.p_length):
                result[i, j] = int(TK[j].split('#')[0])
                result2[i, j] = int(TK[j].split('#')[1])

        return result, result2

    def get_POS(self):
        pos_dict = []

        pos_filepath = "C:\\Users\\Administrator\\Desktop\\qadataset\\pos.txt"
        pos_f = open(pos_filepath, 'r')

        lines = pos_f.readlines()

        for i in range(len(lines)):
            temp = lines[i].split(' ')[0]
            pos_dict.append(temp)

        tagger = nltk.UnigramTagger(nltk.corpus.brown.tagged_sents())

        Prop_Result, _ = self.read_POS_From_Embed()

        self.Tagged_POS_Batch = numpy.zeros(shape=[self.numberOf_available_question, self.p_length], dtype='i')

        fileName = 'C:\\Users\\Administrator\Desktop\\PAIG_Model_Saver\\POS_Tag_Result_Saver\\pos_tag_result'
        f = open(fileName, 'r')

        lines = f.readlines()

        for p in range(self.numberOf_available_sentence):
            temp = self.paragraph_arr[p]
            self.tagged = tagger.tag(temp)

            for i in range(self.p_length):
                if temp[i] != '#@':
                    if self.tagged[i][1] != None:
                        TK = self.tagged[i][1].split('-')

                        if TK[0] != 'None':
                            index = pos_dict.index(TK[0]) if TK[0] in pos_dict else -1
                            if index != -1:
                                self.Tagged_POS_Batch[p, i] = index
                            else:
                                # Set POS from Training Model
                                self.Tagged_POS_Batch[p, i] = Prop_Result[p, i]
                        else:
                            # Set POS from Training Model
                            self.Tagged_POS_Batch[p, i] = Prop_Result[p, i]
                    else:
                        # Set POS from Training Model
                        self.Tagged_POS_Batch[p, i] = Prop_Result[p, i]

            for i in range(self.p_length):
                is_Wrong = False

                if temp[i] != '#@':
                    if self.tagged[i][1] != None:
                        TK = self.tagged[i][1].split('-')

                        if TK[0] != 'None':
                            index = pos_dict.index(TK[0]) if TK[0] in pos_dict else -1
                            if index != -1:
                                0
                            else:
                                # Set POS from Training Model
                                is_Wrong = True
                        else:
                            # Set POS from Training Model
                            is_Wrong = True
                    else:
                        # Set POS from Training Model
                        is_Wrong = True

                if is_Wrong:
                    index = pos_dict.index('NN') if 'NN' in pos_dict else -1
                    self.Tagged_POS_Batch[p, i] = index


    def get_json(self):
        return self.data

    def set_batch(self):
        p_length = self.p_length

        arr_s = numpy.array(["a", "b", "c", "a"])
        arr = numpy.array([[1, 2, 3], [3, 4, 5], [4, 5, 6], [7, 8, 9]])
        wrong_loc_count = 0
        wrong_count = 0
        wrong_count = 0
        loc_diffs = []

        index = 0
        a = 1
        while a == 1:
            index = numpy.where(arr_s == "a")

            if len(index[0]) > 0:
                arr = numpy.delete(arr, index[0][0], 0)
                arr_s = numpy.delete(arr_s, index[0][0], 0)
            else:
                a = 2

        print(arr_s, arr)

        quetion_maxlength = self.q_length
        paragraph_index = 0
        question_index = 0

        numberOfParagraph = 0
        numberOfQuestions = 0

        paragraph_str = []
        question_str = []
        start_str = []
        stop_str = []

        for article in self.data['data']:
            for para in article['paragraphs']:
                numberOfParagraph = numberOfParagraph + 1

                for qa in para['qas']:
                    for answer in qa['answers']:
                        start_index = int(answer['answer_start'])
                        answer_length = len(answer['text'])

                        original_str = "".join(para['context'])

                        para_str = list(para['context'])
                        para_str[start_index] = '#'
                        para_str[start_index + answer_length - 1] = '#'

                        #print(start_index, answer_length)
                        #print("".join(para_str))

                        temp_str = "".join(para_str)
                        temp_str = temp_str.replace('.', ' .')
                        temp_str = temp_str.replace(',', ' ,')
                        temp_str = temp_str.replace('?', ' ?')
                        temp_str = temp_str.replace('!', ' !')
                        temp_str = temp_str.replace('(', ' ')
                        temp_str = temp_str.replace(')', ' ')
                        temp_str = temp_str.replace(u'\u2013', ' - ')
                        temp_str = temp_str.replace(u'\u2014', ' - ')
                        temp_str = temp_str.replace('-', ' - ')
                        temp_str = temp_str.replace('\'', ' \' ')
                        temp_str = temp_str.replace('\"', '')

                        original_str = original_str.replace('.', ' .')
                        original_str = original_str.replace(',', ' ,')
                        original_str = original_str.replace('?', ' ?')
                        original_str = original_str.replace('!', ' !')
                        original_str = original_str.replace('(', ' ')
                        original_str = original_str.replace(')', ' ')
                        original_str = original_str.replace(u'\u2013', ' - ')
                        original_str = original_str.replace(u'\u2014', ' - ')
                        original_str = original_str.replace('-', ' - ')
                        original_str = original_str.replace('\'', ' \' ')
                        original_str = original_str.replace('\"', '')

                        #print(temp_str, len("".join(para_str).split(' ')))

                        Start_Index = 0
                        Stop_Index = 0

                        split1 = original_str.split(' ')

                        para_str = "".join(temp_str)
                        para_str = para_str.split(' ')

                        for i in range(len(para_str)):
                            temp_list = list(para_str[i])

                            if len(temp_list) > 0:
                                if temp_list[0] == '#':
                                    Start_Index = i
                                if temp_list[len(temp_list) - 1] == '#':
                                    Stop_Index = i

                        #print(para_str[Start_Index], para_str[Stop_Index])
                        #print(split1[Start_Index], split1[Stop_Index])

                        question_ = "".join(qa['question'])
                        question_ = question_.replace('?', '')
                        question_ = question_.split(' ')

                        if len(split1) < self.p_length:
                            if len(question_) < self.q_length:
                                paragraph_str.append("".join(original_str))

                                qq = "".join(qa['question']).replace('?', '')

                                question_str.append(qq)
                                start_str.append(Start_Index)
                                stop_str.append(Stop_Index)

                        #input()

                        numberOfQuestions = numberOfQuestions + 1

        numberOfQuestions = len(start_str)
        self.numberOf_available_question = numberOfQuestions

        self.paragraph_arr = numpy.zeros(shape=(numberOfQuestions, p_length), dtype="<U20")
        self.question_batch = numpy.zeros(shape=(numberOfQuestions, quetion_maxlength), dtype="<U20")
        self.start_index_batch = numpy.zeros(shape=(numberOfQuestions), dtype=numpy.int)
        self.stop_index_batch = numpy.zeros(shape=(numberOfQuestions), dtype=numpy.int)

        for k in range(numberOfQuestions):
            temp_str = paragraph_str[k].split(' ')

            for i in range(len(temp_str)):
                try:
                    int(temp_str[i])
                    self.paragraph_arr[k, i] = '10'
                except:
                    self.paragraph_arr[k, i] = temp_str[i]

            temp_str = question_str[k].split(' ')

            for i in range(len(temp_str)):
                self.question_batch[k, i] = temp_str[i]

            self.start_index_batch[k] = start_str[k]
            self.stop_index_batch[k] = stop_str[k]

        print(numberOfQuestions)
        """
        while True:
            a = int(input())
            print(self.question_batch[a])
            print(self.paragraph_arr[a])
            print(self.paragraph_arr[a, self.start_index_batch[a]], self.paragraph_arr[a, self.stop_index_batch[a]])
        """

    def set_batch_sentence(self):
        p_length = self.p_length

        arr_s = numpy.array(["a", "b", "c", "a"])
        arr = numpy.array([[1, 2, 3], [3, 4, 5], [4, 5, 6], [7, 8, 9]])
        wrong_loc_count = 0
        wrong_count = 0
        wrong_count = 0
        loc_diffs = []

        index = 0
        a = 1
        while a == 1:
            index = numpy.where(arr_s == "a")

            if len(index[0]) > 0:
                arr = numpy.delete(arr, index[0][0], 0)
                arr_s = numpy.delete(arr_s, index[0][0], 0)
            else:
                a = 2

        print(arr_s, arr)

        quetion_maxlength = self.q_length
        paragraph_index = 0
        question_index = 0

        numberOfParagraph = 0
        numberOfQuestions = 0

        for article in self.data['data']:
            for para in article['paragraphs']:
                numberOfParagraph = numberOfParagraph + 1

                for qa in para['qas']:
                    for answer in qa['answers']:
                        numberOfQuestions = numberOfQuestions + 1
        self.paragraph_index = numpy.zeros(shape=(numberOfQuestions), dtype=numpy.int)
        self.paragraph_arr = numpy.zeros(shape=(numberOfQuestions, p_length), dtype="<U20")
        self.question_batch = numpy.zeros(shape=(numberOfQuestions, quetion_maxlength), dtype="<U20")
        self.start_index_batch = numpy.zeros(shape=(numberOfQuestions), dtype=numpy.int)
        self.stop_index_batch = numpy.zeros(shape=(numberOfQuestions), dtype=numpy.int)

        for i in range(numberOfQuestions):
            self.start_index_batch[i] = -1

        for article in self.data['data']:
            for para in article['paragraphs']:
                para['context'] = para['context'].replace(u'\u000A', '')
                para['context'] = para['context'].replace(u'\u00A0', ' ')
                # para['context'] = para['context'].replace('\"', '')

                context = para['context']
                context = context.replace('.', ' .')
                context = context.replace(',', ' ,')
                context = context.replace('?', ' ?')
                context = context.replace('!', ' !')
                context = context.replace('(', ' ')
                context = context.replace(')', ' ')
                context = context.replace(u'\u2013', ' - ')
                context = context.replace(u'\u2014', ' - ')
                context = context.replace('-', ' - ')
                context = context.replace('\'', ' \' ')
                context = context.replace('\"', '')

                #print(context)
                paragraph = numpy.array(context.split(' '))
                #print("Max", max, "para:", len(paragraph))

                tempArr_p = numpy.zeros((p_length), dtype="<U20")
                for i in range(p_length):
                    if len(paragraph) > i:
                        tempArr_p[i] = paragraph[i]


                is_wrong = 0

                if len(paragraph) > self.p_length:
                    is_wrong = 1

                for qa in para['qas']:
                    for answer in qa['answers']:
                        answer['text'] = answer['text'].replace(u'\u00A0', ' ')
                        text = answer['text']
                        answer_start = answer['answer_start']
                        if context[answer_start:answer_start + len(text)] == text:
                            if text.lstrip() == text:
                                pass
                            else:
                                answer_start += len(text) - len(text.lstrip())
                                answer['answer_start'] = answer_start
                                text = text.lstrip()
                                answer['text'] = text
                        else:
                            wrong_loc_count += 1
                            text = text.lstrip()
                            answer['text'] = text
                            starts = list(self.find_all(context, text))
                            if len(starts) == 1:
                                answer_start = starts[0]
                            elif len(starts) > 1:
                                new_answer_start = min(starts, key=lambda s: abs(s - answer_start))
                                loc_diffs.append(abs(new_answer_start - answer_start))
                                answer_start = new_answer_start
                            else:
                                self.start_index_batch[question_index] = -1
                                self.stop_index_batch[question_index] = -1
                                print("Raise Exception")
                                #print(answer['text'], " : ", qa['question'])
                                # raise Exception()
                                is_wrong = 1
                                wrong_count = wrong_count + 1
                            answer['answer_start'] = answer_start

                        if is_wrong == 0:
                            answer_stop = answer_start + len(text)
                            answer['answer_stop'] = answer_stop

                            # print(answer_start, " : ", answer_stop, context[answer_start:answer_stop])
                            context2 = list(context)
                            context2[answer_start] = '#'
                            context2[answer_stop - 1] = '^'
                            context2 = "".join(context2)
                            context2 = numpy.array(context2.split(' '))

                            text = list(text)
                            text[0] = '#'
                            text[answer_stop - answer_start - 1] = '^'
                            text = "".join(text)
                            text = numpy.array(text.split(' '))

                            ans_index1 = numpy.where(context2 == text[0])
                            ans_index2 = numpy.where(context2 == text[text.size - 1])

                            q_length = len(qa['question'].split(' '))
                            question = "".join(qa['question']).replace('?', '')

                            question = question.split(' ')



                            if len(ans_index1[0]) > 0 and len(ans_index2[0]) > 0:
                                if len(question) < 30:
                                    tempArr_q = numpy.zeros((quetion_maxlength), dtype="<U20")

                                    for i in range(q_length):
                                        tempArr_q[i] = question[i]

                                    self.paragraph_index[question_index] = paragraph_index
                                    self.question_batch[question_index] = tempArr_q
                                    self.start_index_batch[question_index] = ans_index1[0][0]
                                    self.stop_index_batch[question_index] = ans_index2[0][0]

                                    print(ans_index1[0], ans_index2[0], text, question, "raise:", is_wrong)

                                    print("question:", question, " Length: ", len(question))
                                    print("@@@,", self.paragraph_arr[paragraph_index],
                                          self.paragraph_arr[paragraph_index][ans_index1[0][0]],
                                          self.paragraph_arr[paragraph_index][ans_index2[0][0]])

                                    question_index = question_index + 1
                                else:
                                    self.start_index_batch[question_index] = -1
                                    self.stop_index_batch[question_index] = -1
                                    wrong_count = wrong_count + 1

                            else:
                                self.start_index_batch[question_index] = -1
                                self.stop_index_batch[question_index] = -1
                                wrong_count = wrong_count + 1

                paragraph_index = paragraph_index + 1
        is_loop = 1
        index = 0
        while is_loop == 1:
            if self.start_index_batch[index] == -1:
                is_loop = 0
            else:
                index = index + 1

        print(index, self.start_index_batch[index - 1], self.stop_index_batch[index - 1])
        self.numberOf_available_question = index
        print("wrong_count", wrong_count)

    def get_glove_Test(self, word):
        word = "".join(word).lower()
        index = self.dictionary.searchsorted(word)

        try:
            int(word)
            index = self.dictionary.searchsorted('10')
            return 0
        except:
            if index == 400000:
                index = 0

            if word == self.dictionary[index]:
                # print("Success: ", word)
                return 0
            else:
                # if str != '':
                #    print("fail: ", word)
                return 1

    def get_glove(self, word):
        word = "".join(word).lower()
        index = self.dictionary.searchsorted(word)
        #print("index,", index)

        if index == 400000:
            index = 0

        if word == self.dictionary[index]:
            #print("Success: ", word)
            #input()
            return self.vectors[self.dictionary.searchsorted(word)]
        else:
            # if str != '':
            #    print("fail: ", word)
            none_result = numpy.zeros((self.embedding_size), dtype='f')
            for i in range(self.embedding_size):
                none_result[i] = 3.0 / self.embedding_size
            return none_result

    def get_glove_sequence(self, length, tokens):
        result = numpy.zeros((length, self.embedding_size), dtype='f')
        for i in range(length):
            result[i] = self.get_glove(tokens[i])

        return result

    def set_sentence_batch(self):
        sen_pa = numpy.zeros(shape=(self.numberOf_available_question * 5, self.s_length), dtype="<U20")
        sen_qa = numpy.zeros(shape=(self.numberOf_available_question * 5, self.q_length), dtype="<U20")

        sen_start = numpy.zeros(shape=(self.numberOf_available_question * 5, 1), dtype="f")
        sen_stop = numpy.zeros(shape=(self.numberOf_available_question * 5, 1), dtype="f")
        self.attention_Label = numpy.zeros(shape=(self.numberOf_available_question * 5, 1), dtype='f')

        self.paragraph_arr_ = copy.copy(self.paragraph_arr)

        for i in range(self.numberOf_available_question):
            para = self.paragraph_arr_[i]

            for j in range(len(para)):
                para[j] = para[j] + ' '

        for i in range(self.numberOf_available_question):
            para = self.paragraph_arr_[self.paragraph_index[i]]

            index_Start = -1
            index_Stop = 0
            index_sen = 0

            temp_start = "".join(para[self.start_index_batch[i]])
            temp_stop = "".join(para[self.stop_index_batch[i]])

            para[self.start_index_batch[i]] = 'Start_Index '
            para[self.stop_index_batch[i]] = 'Stop_Index '

            s_para = "".join(para)
            #print("S_PARA: ", s_para)
            sentences = s_para.split('.')

            for j in range(len(sentences)):
                temp_s = sentences[j].split(' ')
                #print("TEMP_S:", temp_s, '\n \n', s_para, '\n \n', self.paragraph_arr[self.paragraph_index[i]])
                for k in range(len(temp_s)):
                    if temp_s[k] == 'Start_Index':
                        index_Start = k
                    if temp_s[k] == 'Stop_Index':
                        index_Stop = k
                        index_sen = j

            senten = sentences[index_sen].split(' ')

            if len(senten) < self.s_length:
                self.attention_Label[self.numberOf_available_sentence] = 1

                if index_Start != -1:
                    sen_start[self.numberOf_available_sentence] = index_Start
                else:
                    sen_start[self.numberOf_available_sentence] = index_Stop
                    index_Start = index_Stop

                sen_stop[self.numberOf_available_sentence] = index_Stop

                senten[index_Start] = temp_start
                senten[index_Stop] = temp_stop

                para[self.start_index_batch[i]] = temp_start
                para[self.stop_index_batch[i]] = temp_stop

                for j in range(len(senten)):
                    #print('a:', len(senten),  '', j, temp_start, temp_stop, index_Start, index_Stop)
                    #print(senten)
                    sen_pa[self.numberOf_available_sentence, j] = senten[j]

                sen_qa[self.numberOf_available_sentence] = self.question_batch[self.numberOf_available_sentence]

                self.numberOf_available_sentence = self.numberOf_available_sentence + 1

            for j in range(len(sentences)):
                if j != index_sen:
                    self.attention_Label[self.numberOf_available_sentence] = 0

                    sen_start[self.numberOf_available_sentence] = -1
                    sen_stop[self.numberOf_available_sentence] = -1

                    senten = sentences[j].split(' ')

                    for k in range(len(senten)):
                        sen_pa[self.numberOf_available_sentence, k] = senten[k]

                    sen_qa[self.numberOf_available_sentence] = self.question_batch[self.numberOf_available_sentence]

                    self.numberOf_available_sentence = self.numberOf_available_sentence + 1

        self.SA_paragraph = sen_pa
        self.SA_question = sen_qa
        self.SA_start = sen_start
        self.SA_end = sen_stop

        return 10

    def set_sentence_batch_para(self):
        sen_pa = numpy.zeros(shape=(self.numberOf_available_question * 5, self.s_length), dtype="<U20")
        sen_qa = numpy.zeros(shape=(self.numberOf_available_question * 5, self.q_length), dtype="<U20")

        sen_start = numpy.zeros(shape=(self.numberOf_available_question * 5, 1), dtype="f")
        sen_stop = numpy.zeros(shape=(self.numberOf_available_question * 5, 1), dtype="f")

        self.attention_Label = numpy.zeros(shape=(self.numberOf_available_question * 5, 1), dtype='f')

        self.paragraph_arr_ = copy.copy(self.paragraph_arr)

        for i in range(self.numberOf_available_question):
            para = self.paragraph_arr_[i]

            for j in range(len(para)):
                para[j] = para[j] + ' '

        for i in range(self.numberOf_available_question):
            para = self.paragraph_arr_[self.paragraph_index[i]]

            index_Start = -1
            index_Stop = 0
            index_sen = 0

            temp_start = "".join(para[self.start_index_batch[i]])
            temp_stop = "".join(para[self.stop_index_batch[i]])

            para[self.start_index_batch[i]] = 'Start_Index '
            para[self.stop_index_batch[i]] = 'Stop_Index '

            s_para = "".join(para)
            # print("S_PARA: ", s_para)
            sentences = s_para.split('.')

            for j in range(len(sentences)):
                temp_s = sentences[j].split(' ')
                # print("TEMP_S:", temp_s, '\n \n', s_para, '\n \n', self.paragraph_arr[self.paragraph_index[i]])
                for k in range(len(temp_s)):
                    if temp_s[k] == 'Start_Index':
                        index_Start = k
                    if temp_s[k] == 'Stop_Index':
                        index_Stop = k
                        index_sen = j

            senten = sentences[index_sen].split(' ')

            if len(senten) < self.s_length:
                self.attention_Label[self.numberOf_available_sentence] = 1

                if index_Start != -1:
                    sen_start[self.numberOf_available_sentence] = index_Start
                else:
                    sen_start[self.numberOf_available_sentence] = index_Stop
                    index_Start = index_Stop

                sen_stop[self.numberOf_available_sentence] = index_Stop

                senten[index_Start] = temp_start
                senten[index_Stop] = temp_stop

                para[self.start_index_batch[i]] = temp_start
                para[self.stop_index_batch[i]] = temp_stop

                for j in range(len(senten)):
                    # print('a:', len(senten),  '', j, temp_start, temp_stop, index_Start, index_Stop)
                    # print(senten)
                    sen_pa[self.numberOf_available_sentence, j] = senten[j]

                sen_qa[self.numberOf_available_sentence] = self.question_batch[self.numberOf_available_sentence]

                self.numberOf_available_sentence = self.numberOf_available_sentence + 1

        self.SA_paragraph = sen_pa
        self.SA_question = sen_qa
        self.SA_start = sen_start
        self.SA_end = sen_stop

        return 10

    def re_init(self):
        self.batch_index = self.batch_size
        self.whole_batch_index = 0

    def get_test_batch(self):
        batch_paragraph = numpy.zeros((self.batch_size, self.p_length, self.embedding_size), dtype='f')
        batch_question = numpy.zeros((self.batch_size, self.q_length, self.embedding_size), dtype='f')
        batch_start_index = numpy.zeros((self.batch_size,1), dtype='f')
        batch_stop_index = numpy.zeros((self.batch_size, 1), dtype='f')
        batch_POS_Embeddings = numpy.zeros((self.batch_size, self.p_length, self.vocab_size), dtype='f')

        #print("Batch Check: ", self.paragraph_arr.shape, self.numberOf_available_question,
        #      self.numberOf_available_sentence, self.paragraph_index.shape)

        index = 0

        for i in range(self.batch_size):
            batch_paragraph[index] = self.get_glove_sequence(self.p_length, self.paragraph_arr[i])
            batch_question[index] = self.get_glove_sequence(self.q_length, self.question_batch[i])
            batch_start_index[index] = self.start_index_batch[i]
            batch_stop_index[index] = self.stop_index_batch[i]

            index = index + 1

        return batch_paragraph, batch_question, batch_start_index, batch_stop_index, batch_POS_Embeddings

    def get_propagate_batch(self, para, qu):
        ba = self.batch_size

        batch_paragraph = numpy.zeros((ba, self.p_length, self.embedding_size), dtype='f')
        batch_question = numpy.zeros((ba, self.q_length, self.embedding_size), dtype='f')
        batch_start_index = numpy.zeros((ba, 1), dtype='f')
        batch_stop_index = numpy.zeros((ba, 1), dtype='f')
        attention_L = numpy.zeros((self.batch_size, 1), dtype='f')

        pa = para.split(' ')
        str_pa = numpy.zeros((100, 1), dtype="<U20")
        for i in range(len(pa)):
            str_pa[i] = pa[i]

        qu = qu.split(' ')
        str_qu = numpy.zeros((30, 1), dtype="<U20")
        print(len(qu))
        for i in range(len(qu)):
            str_qu[i] = qu[i]

        batch_paragraph[0] = self.get_glove_sequence(self.p_length, str_pa)
        batch_question[0] = self.get_glove_sequence(self.q_length, str_qu)

        return batch_paragraph, batch_question, batch_start_index, batch_stop_index

    def get_next_batch(self):

        if self.batch_index + self.batch_size > self.numberOf_available_question:
            self.batch_index = self.batch_size
            self.whole_batch_index = self.whole_batch_index + 1


        #print(self.paragraph_arr.shape)
        #print(self.numberOf_available_question)
        #input()

        batch_paragraph = numpy.zeros((self.batch_size, self.p_length, self.embedding_size), dtype='f')
        batch_question = numpy.zeros((self.batch_size, self.q_length, self.embedding_size), dtype='f')
        batch_start_index = numpy.zeros((self.batch_size, 1), dtype='f')
        batch_stop_index = numpy.zeros((self.batch_size, 1), dtype='f')
        batch_POS_Embeddings = numpy.zeros((self.batch_size, self.p_length, self.vocab_size), dtype='f')

        index = 0
        #print(self.batch_index, '/', self.numberOf_available_question, ' ', self.batch_size)

        for i in range(self.batch_index, self.batch_index + self.batch_size):
            #print("Batch", i, self.SA_paragraph[i])
            batch_paragraph[index] = self.get_glove_sequence(self.p_length, self.paragraph_arr[i])
            batch_question[index] = self.get_glove_sequence(self.q_length, self.question_batch[i])
            batch_start_index[index] = self.start_index_batch[i]
            batch_stop_index[index] = self.stop_index_batch[i]

            index = index + 1

        self.batch_index = self.batch_index + self.batch_size

        return batch_paragraph, batch_question, batch_start_index, batch_stop_index, batch_POS_Embeddings


    def get_prop_batch(self):
        if self.batch_index + self.batch_size > self.numberOf_available_question:
            self.batch_index = self.batch_size
            self.whole_batch_index = self.whole_batch_index + 1
        else:
            self.batch_index = self.batch_index + self.batch_size


        batch_paragraph = numpy.zeros((self.batch_size, self.p_length, self.embedding_size), dtype='f')
        batch_question = numpy.zeros((self.batch_size, self.q_length, self.embedding_size), dtype='f')
        batch_start_index = numpy.zeros((self.batch_size, 1), dtype='f')
        batch_stop_index = numpy.zeros((self.batch_size, 1), dtype='f')
        batch_POS_Embeddings = numpy.zeros((self.batch_size, self.p_length, self.pos_Embedding.embedding_size), dtype='f')

        index = 0

        for i in range(self.batch_index, self.batch_index + self.batch_size):
            batch_paragraph[index] = self.get_glove_sequence(self.p_length, self.paragraph_arr[self.paragraph_index[i]])
            batch_question[index] = self.get_glove_sequence(self.q_length, self.question_batch[i])
            batch_POS_Embeddings[index] = self.pos_Embedding.pos_tagger(self.SA_paragraph[i], self.s_length)
            batch_start_index[index] = self.start_index_batch[i]
            batch_stop_index[index] = self.stop_index_batch[i]
            index = index + 1

        return batch_paragraph, batch_question, batch_start_index, batch_stop_index, batch_POS_Embeddings

    def get_para(self, index):
        return self.paragraph_arr[self.paragraph_index[index]]

    def get_qu(self, index):
        return self.question_batch[index]
