import matplotlib
matplotlib.use("agg")
import matplotlib.pyplot as plt

import numpy as np
import math
import os
import cv2
import csv
import glob
import seaborn as sns
import pandas as pd

feature1Label = 'Average Mana'
feature1Scalar = 1 / 30.0
feature1Precision = 2
feature2Label = 'Mana Variance'
feature2Scalar = 1 / 1000000.0
feature2Precision = 2

logFilename = "elite_map_log.csv"

def createRecordList(mapData):
    recordList = []
    for cellData in mapData:
        data = [int(x) for x in cellData.split(":")]
        recordList.append(data)
    return recordList 

def createRecordMap(dataLabels, recordList):
    dataDict = {label:[] for label in dataLabels}
    for recordDatum in recordList:
        for i in range(len(dataLabels)):
            dataDict[dataLabels[i]].append(recordDatum[i])
    return dataDict

def createImage(rowData, filename):
    mapDims = tuple(map(int, rowData[0].split('x')))
    mapData = rowData[1:]

    dataLabels = [
            'CellRow',
            'CellCol',
            'CellSize',
            'IndividualId',
            'WinCount',
            'Fitness',
            'Feature1',
            'Feature2',
        ]
    recordList = createRecordList(mapData)
    dataDict = createRecordMap(dataLabels, recordList)
  
    recordFrame = pd.DataFrame(dataDict)
    rowFrame = recordFrame.pivot(index='CellCol', columns='CellRow', values='Feature1')
    rowFrame.dropna(how='all')
    rowLabels = [rowFrame[i].mean(skipna=True) if i in rowFrame else math.nan for i in range(mapDims[0])]
    colFrame = recordFrame.pivot(index='CellRow', columns='CellCol', values='Feature2')
    colFrame.dropna(how='all')
    colLabels = [colFrame[i].mean(skipna=True) if i in colFrame else math.nan for i in range(mapDims[1])]

    # Add the averages of the observed features
    dataLabels += [feature1Label, feature2Label] 
    newRecordList = []
    for recordDatum in recordList:
        cellRow = recordDatum[0]
        cellCol = recordDatum[1]
        f1value = round(rowLabels[cellRow] * feature1Scalar, feature1Precision) 
        f2value = round(colLabels[cellCol] * feature2Scalar, feature2Precision)
        featurePair = [f1value, f2value]
        newRecordList.append(recordDatum+featurePair)
    dataDict = createRecordMap(dataLabels, newRecordList)
    recordFrame = pd.DataFrame(dataDict)

    # Write the map for the cell fitness
    fitnessMap = recordFrame.pivot(index=feature2Label, columns=feature1Label, values='WinCount')
    fitnessMap.sort_index(level=1, ascending=False, inplace=True)
    #print(fitnessMap)
    with sns.axes_style("white"):
        numTicksX = mapDims[0] // 11 + 1
        numTicksY = mapDims[1] // 11 + 1
        plt.figure(figsize=(10,9))
        g = sns.heatmap(fitnessMap, annot=True, fmt=".0f",
                xticklabels=numTicksX, 
                yticklabels=numTicksY,
                vmin=65,
                vmax=170)
                #vmin=np.nanmin(fitnessMap),
                #vmax=np.nanmax(fitnessMap))
        fig = g.get_figure()
        fig.savefig(filename)
    plt.close('all')

def createImages(stepSize, rows, filenameTemplate):
    for endInterval in range(stepSize, len(rows), stepSize):
        print('Generating : {}'.format(endInterval))
        filename = filenameTemplate.format(endInterval)
        createImage(rows[endInterval], filename)

def createMovie(folderName, filename):
    globStr = os.path.join(folderName, '*.png')
    imageFiles = sorted(glob.glob(globStr))

    # Grab the dimensions of the image
    img = cv2.imread(imageFiles[0])
    imageDims = img.shape[:2][::-1]

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
    #createImage(allRows[-1], 'fitness_map.png')
    createImages(10, allRows[1:], template)
    createMovie('images/fitness', 'fitness.avi') 
