import os
import cv2
import csv
import png
import glob

logFilename = "elite_map_log.csv"
imageDims = (256,256)
MIN_VALUE = -10 ** 18

def getColor(intensity, maxValue):
    maxValue = max(1, maxValue)
    redComp = intensity * 255 / maxValue
    greenComp = 0
    blueComp = 255-redComp
    return (redComp, greenComp, blueComp)

def getColorMap(possibleValues):
    possibleValues = sorted(list(possibleValues))
    minValue = possibleValues[0]
    maxValue = possibleValues[-1]
    d = {x:getColor(x-minValue, maxValue-minValue) for x in possibleValues}
    d[MIN_VALUE] = (255,255,255)
    return d


def writePng(filename, mapDims, valueMap, colorMap):
    imageData = []
    for curY in range(imageDims[0]):
        row = []
        for curX in range(imageDims[1]):

            # Make the x axis the first feature and the
            # y axis the second feature
            curRow = curX
            curCol = imageDims[0]-curY-1

            cellRow = mapDims[0] * curRow / imageDims[0]
            cellCol = mapDims[1] * curCol / imageDims[1]
            
            value = valueMap[cellRow][cellCol]
            color = colorMap[value]
            row += list(color)
       
        imageData.append(row)

    
    f = open(filename, 'w')
    w = png.Writer(imageDims[0], imageDims[1])
    w.write(f, imageData)
    f.close()

def createImages(individualId, rowData):
   
    mapDims = tuple(map(int, rowData[0].split('x')))
    mapData = rowData[1:]
    print 'Generating images for:', individualId

    sizeValues = set()
    fitnessValues = set()
    sizeMap = [[MIN_VALUE] * mapDims[1] for i in range(mapDims[0])]
    fitnessMap = [[MIN_VALUE] * mapDims[1] for i in range(mapDims[0])]
    for cellData in mapData:
        data = cellData.split(":")
        cellRow = int(data[0])
        cellCol = int(data[1])
        size = int(data[2])
        fitness = int(data[4])
        
        sizeValues.add(size)
        sizeMap[cellRow][cellCol] = size
        
        fitnessValues.add(fitness)
        fitnessMap[cellRow][cellCol] = fitness

    sizeColorMap = getColorMap(sizeValues)
    fitnessColorMap = getColorMap(fitnessValues)

    # Write the map for the cell sizes
    filename = 'images/size/grid_{:05d}.png'.format(individualId)
    writePng(filename, mapDims, sizeMap, sizeColorMap)
    
    # Write the map for the cell fitness
    filename = 'images/fitness/grid_{:05d}.png'.format(individualId)
    writePng(filename, mapDims, fitnessMap, fitnessColorMap)

def createMovie(folderName, filename):
    globStr = os.path.join(folderName, '*.png')
    imageFiles = sorted(glob.glob(globStr))
    
    # Create a video
    fourcc = cv2.VideoWriter_fourcc(*'XVID')
    frameRate = 60
    video = cv2.VideoWriter(filename, fourcc, frameRate, imageDims)
    
    for imgFilename in imageFiles:
        img = cv2.imread(imgFilename)
        video.write(img)
    
    video.release()

import csv
with open(logFilename, 'r') as csvfile:
    allRows = list(csv.reader(csvfile, delimiter=',', quotechar='|'))

    for i, rowData in enumerate(allRows[1:]):
       createImages(i, rowData) 

    createMovie('images/fitness', 'grid_fitness.avi') 
    createMovie('images/size', 'grid_size.avi') 
