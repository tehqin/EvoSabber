import matplotlib
matplotlib.use("agg")
import matplotlib.pyplot as plt

import os
import cv2
import csv
import glob
import seaborn as sns

logFilename = "hunter.csv"

feature1_index = 0
feature2_index = 1
freq_index = 2
resolution = 10 ** 7

def createImage(x, y, filepath):
    with sns.axes_style("white"):
        #g = sns.jointplot(x=x, y=y, kind="hex", color="k");
        #g = sns.jointplot(x=x, y=y, kind="kde")
        g = sns.jointplot(x=x, y=y, kind="hex").set_axis_labels()
        g = g.set_axis_labels(feature1_label, feature2_label)
        g.savefig(filepath)
    plt.close('all')


with open(logFilename, "r") as csvfile:
    allRows = list(csv.reader(csvfile, delimiter=',', quotechar='|'))

    feature1_label = allRows[0][feature1_index]
    feature2_label = allRows[0][feature2_index]
    print('Feature 1: {}'.format(feature1_label))
    print('Feature 2: {}'.format(feature2_label))

    rowsTranspose = list(zip(*allRows[1:]))
    x = [float(v) for v in rowsTranspose[feature1_index]]
    y = [float(v) for v in rowsTranspose[feature2_index]]
    z = [float(v) for v in rowsTranspose[freq_index]]

    nx = []
    ny = []

    totalSum = sum(z)
    for i in range(len(z)):
        z[i] = int(z[i] * resolution / totalSum)
        #print(z[i])
        for j in range(z[i]):
            nx.append(x[i])
            ny.append(y[i])

    createImage(nx, ny, 'feature_dist.png')
    #createImages(60, x, y, 'images/dist/bi_{:05d}.png', )
    #createMovie('images/dist', 'dist_video.avi')
