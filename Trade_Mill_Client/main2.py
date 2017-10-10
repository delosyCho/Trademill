import RegressionModel
import numpy as np

model = RegressionModel.simple_RNN_Regression(length=20, exp_Index=10)
inp = np.zeros(dtype=np.float32, shape=[300])
model.setBatch(inp, 300)
model.Training()