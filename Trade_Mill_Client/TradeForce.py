#beta 1.0v
#Author Sanghyeon Cho
#delosycho@gmail.com

import numpy as np
import random

class Simulator:
    def __init__(self):
        self.Situation = 0

        self.Prices = 0
        self.Prices_day = 0
        self.Prices_week = 0
        self.BuyPrice = 0
        self.Amounts = 0

        self.Probability = np.array([0.33, 0.33, 0.33])

        self.Agent = ForceAgent(self.Probability)

    def Simulating(self):
        for t in range(self.Whole_Time_Length):
            self.Agent.setState(self.Prices[t], self.Prices_day[t], self.Prices_week[t], self.BuyPrice)
            self.Agent.Action(self.getAction())

    def get_SituationData(self, Prices, Amounts, WholeTime):
        #시세정보와 거래량 정보를 받는다
        Length = len(Prices)

        self.Prices = np.zeros(shape=[Length, 1])
        self.Prices_day = np.zeros(shape=[Length, 1])
        self.Prices_week = np.zeros(shape=[Length, 1])

        self.Amounts = np.zeros(shape=[Length, 1])

        self.Whole_Time_Length = WholeTime

        for i in range(Length):
            self.Prices[i, 0] = Prices[i]
            self.Amounts[i, 0] = Amounts[i]

    def getAction(self):
        rand = random.random()
        index = 0

        if rand < self.Probability[0]:
            index = 0
        elif rand < self.Probability[0] + rand < self.Probability[1]:
            index = 1
        elif rand < self.Probability[0] + rand < self.Probability[1] + rand < self.Probability[2]:
            index = 2

        return index

class ForceAgent:
    def __init__(self, Probs, commision=0.01, dayEnd_Factor=0, long_term_Factor=0):
        """
        Probs: Action's Probability
        :param commision: 증권사 거래 수수료 설정
        :param dayEnd_Factor: 장 마감 시간내에 거래를 끝내고 싶다면 설정권장
        :param long_term_Factor: 빠르게 사고 파는 경우-> 양수, 느리게 사고파는 경우->음수
        """

        self.Grid = 0
        self.State = np.zeros(shape=[4])
        self.Commision = commision
        self.DayEnd_Factor = dayEnd_Factor
        self.Long_term_Factor = long_term_Factor

        self.Probability = np.zeros(shape=[len(Probs, 1)])
        self.Policy_Network = np.zeros(shape=[4, 1000, 3])

        self.MaxPrice = 0

        for i in range(len(Probs)):
            self.Probability[i, 0] = Probs[i, 0]

    def setState(self, price, dayprice, weekprice, buyprice):
        self.State = np.zeros(shape=[4, 1000])

        index = int(price / self.MaxPrice)
        self.State[0] = index

        index = int(dayprice / self.MaxPrice)
        self.State[1] = index

        index = int(weekprice / self.MaxPrice)
        self.State[2] = index

        index = int(buyprice / self.MaxPrice)
        self.State[3] = index

    def Action(self, action_index):
        reward = 0

        if action_index == 1:
            reward = self.State[3]