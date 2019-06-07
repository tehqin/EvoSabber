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
import itertools

feature1Label = 'Average Mana'
feature1Scalar = 1 / 30.0
feature1Precision = 2
feature2Label = 'Mana Variance'
feature2Scalar = 1 / 1000000.0
feature2Precision = 2

logPaths = [
    # Experiment 1
    '/home/tehqin/Projects/HearthStone/Experiments/MapElites/Orig/Aggro/Hunter',
    '/home/tehqin/Projects/HearthStone/Experiments/MapElites/Orig/Aggro/Paladin',
    '/home/tehqin/Projects/HearthStone/Experiments/MapElites/Orig/Aggro/Warlock',
    '/home/tehqin/Projects/HearthStone/Experiments/MapElites/Orig/Control/Hunter',
    '/home/tehqin/Projects/HearthStone/Experiments/MapElites/Orig/Control/Paladin',
    '/home/tehqin/Projects/HearthStone/Experiments/MapElites/Orig/Control/Warlock',
        ]

mapLogFilename = "elite_map_log.csv"
individualLogFilename = "elite_map_log.csv"

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

def getFinalMap(rowData, strategyType, className):
    mapDims = tuple(map(int, rowData[0].split('x')))
    mapData = rowData[1:]

    dataLabels = [
            'CellRow',
            'CellCol',
            'CellSize',
            'IndividualId',
            'Win Count',
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
    dataLabels += [feature1Label, feature2Label, 'Strategy', 'Class']
    newRecordList = []
    for recordDatum in recordList:
        feat1 = recordDatum[6]
        feat2 = recordDatum[7]
        f1value = feat1 * feature1Scalar
        f2value = feat2 * feature2Scalar 
        featureTup = [f1value, f2value, strategyType, className]
        newRecordList.append(recordDatum+featureTup)
    dataDict = createRecordMap(dataLabels, newRecordList)
    

    return dataDict

def buildMultiplePlots():

    sns.set(font_scale=1.7)
    for folderPath in logPaths:
        print('Generating: ', folderPath)
        logPath = os.path.join(folderPath, mapLogFilename)
        headPath, className = os.path.split(folderPath)
        headPath, strategyType = os.path.split(headPath)

        with open(logPath, 'r') as csvfile:
            allRows = list(csv.reader(csvfile, delimiter=',', quotechar='|'))
            allRows = allRows[1:]

            dataDict = getFinalMap(allRows[9999], strategyType, className)
            recordFrame = pd.DataFrame(dataDict)

            cmap = sns.cubehelix_palette(rot=-.2, as_cmap=True)
            g = sns.scatterplot(x=feature1Label, y=feature2Label,
                                size="Fitness", hue="Fitness", palette=cmap,
                                data=recordFrame, sizes=(10,100))

            #plt.setp(g.get_legend().get_texts(), fontsize='2')
            locStr = 'best'
            if className == 'Warlock' and strategyType == 'Control':
                locStr = 'lower right'
            plt.legend(borderpad=0.1, fontsize = 'xx-small', loc=locStr)

            className = className.lower()
            strategyType = strategyType.lower()
            imageFilename = 'elites_{}_{}.png'.format(className, strategyType)
            print(imageFilename)
            fig = g.get_figure()
            fig.tight_layout()
            fig.savefig(imageFilename, dpi=200)
            plt.close('all')


def buildViaSubplots():
    fig, axes = plt.subplots(2, 3, figsize=(16,8), sharex=True, sharey=True)
    axes = list(itertools.chain.from_iterable(axes))

    for curAxis, folderPath in zip(axes, logPaths):
        print('Generating: ', folderPath)
        logPath = os.path.join(folderPath, mapLogFilename)
        headPath, className = os.path.split(folderPath)
        headPath, strategyType = os.path.split(headPath)
        
        print(className)
        print(strategyType)

        with open(logPath, 'r') as csvfile:
            
            # Read all the data from the csv file
            allRows = list(csv.reader(csvfile, delimiter=',', quotechar='|'))
            allRows = allRows[1:]
            
            dataDict = getFinalMap(allRows[9999], strategyType, className)
            recordFrame = pd.DataFrame(dataDict)
            
            cmap = sns.cubehelix_palette(rot=-.2, as_cmap=True)
            g = sns.scatterplot(x=feature1Label, y=feature2Label,
                    size="Fitness", hue="Fitness", palette=cmap, 
                    data=recordFrame, sizes=(10,100), ax=curAxis)


    imageFilename = 'elitesMap.png'
    fig.savefig(imageFilename)
    plt.close('all')

def buildViaRelPlot():

    allDataDict = {}
    for folderPath in logPaths:
        print('Generating: ', folderPath)
        logPath = os.path.join(folderPath, mapLogFilename)
        headPath, className = os.path.split(folderPath)
        headPath, strategyType = os.path.split(headPath)
        
        with open(logPath, 'r') as csvfile:
            
            # Read all the data from the csv file
            allRows = list(csv.reader(csvfile, delimiter=',', quotechar='|'))
            allRows = allRows[1:]
            
            dataDict = getFinalMap(allRows[9999], strategyType, className)
            for x in dataDict:
                if x in allDataDict:
                    allDataDict[x] += dataDict[x]
                else:
                    allDataDict[x] = dataDict[x]
            
    recordFrame = pd.DataFrame(allDataDict)
    fig = sns.relplot(x=feature1Label, y=feature2Label, hue="Win Count", size="Win Count",
                      row="Strategy", col="Class", sizes=(10, 100), alpha=.7, height=4, 
                      hue_norm = (0, 179), size_norm=(0, 179),
                      data=recordFrame, palette="Purples")


    imageFilename = 'elitesMap.png'
    fig.savefig(imageFilename)
    plt.close('all')

buildMultiplePlots()
#buildViaRelPlot()
