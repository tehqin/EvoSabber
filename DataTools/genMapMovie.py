import matplotlib
matplotlib.use("agg")
import matplotlib.pyplot as plt

import numpy as np
import os
import cv2
import csv
import glob
import seaborn as sns

feature1_index = 7
feature1_label = 'None'
feature2_index = 4
feature2_label = 'None'


logFilename = "elite_map_log.csv"
imageDims = (640,480)
MIN_VALUE = -10 ** 18

def createImage(rowData, filename):
   
    mapDims = tuple(map(int, rowData[0].split('x')))
    mapData = rowData[1:]

    fitnessValues = set()
    fitnessMap = np.full(mapDims[::-1], MIN_VALUE)
    for cellData in mapData:
        data = cellData.split(":")
        cellRow = int(data[0])
        cellCol = int(data[1])
        winCount = int(data[3])
        fitness = int(data[4])
        cellCol = mapDims[1] - cellCol - 1;        

        fitnessMap[cellCol][cellRow] = fitness

    # Write the map for the cell fitness
    with sns.axes_style("white"):
        g = sns.heatmap(fitnessMap, xticklabels=False, yticklabels=False)
        fig = g.get_figure()
        fig.savefig(filename)
    plt.close('all')

def createImages(stepSize, rows, filenameTemplate):
    for endInterval in range(stepSize, len(rows)+1, stepSize):
        print('Generating : {}'.format(endInterval))
        filename = filenameTemplate.format(endInterval)
        createImage(rows[endInterval], filename)

def createMovie(folderName, filename):
    globStr = os.path.join(folderName, '*.png')
    imageFiles = sorted(glob.glob(globStr))

    # Create a video
    fourcc = cv2.VideoWriter_fourcc(*'XVID')
    frameRate = 30
    video = cv2.VideoWriter(filename, fourcc, frameRate, imageDims)

    for imgFilename in imageFiles:
        img = cv2.imread(imgFilename)
        video.write(img)

    video.release()


import csv
with open(logFilename, 'r') as csvfile:
    allRows = list(csv.reader(csvfile, delimiter=',', quotechar='|'))

    template = 'images/fitness/grid_{:05d}.png'
    createImage(allRows[-1], 'fitness_map.png')
    createImages(10, allRows[1:], template)

    createMovie('images/fitness', 'fitness.avi') 
