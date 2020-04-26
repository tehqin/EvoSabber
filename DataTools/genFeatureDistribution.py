import matplotlib
matplotlib.use("agg")
import matplotlib.pyplot as plt

import os
import cv2
import csv
import glob
import seaborn as sns

feature1_index = 4
feature1_label = 'Num Turns'
feature1_scalar = 1 / 1.0
feature2_index = 6
feature2_label = 'Hand Size'
feature2_scalar = 1 / 1.0

logFilename = "individual_log.csv"

def createImage(x, y, filepath):
    with sns.axes_style("white"):
        #g = sns.jointplot(x=x, y=y, kind="hex", color="k");
        #g = sns.jointplot(x=x, y=y, kind="kde")
        g = sns.jointplot(x=x, y=y, kind="hex").set_axis_labels()
        g = g.set_axis_labels(feature1_label, feature2_label)
        g.savefig(filepath)
    plt.close('all')

def createImages(stepSize, x, y, filenameTemplate):
    for endInterval in range(stepSize, len(x)+1, stepSize):
        cur_x = x[:endInterval]
        cur_y = y[:endInterval]

        print('Creating image: {}'.format(endInterval))
        filepath = filenameTemplate.format(endInterval)
        createImage(cur_x, cur_y, filepath)

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

print("STARTING")
with open(logFilename, "r") as csvfile:
    allRows = list(csv.reader(csvfile, delimiter=',', quotechar='|'))

    label1 = allRows[0][feature1_index]
    label2 = allRows[0][feature2_index]
    print('Feature 1: {}'.format(label1))
    print('Feature 2: {}'.format(label2))

    rowsTranspose = list(zip(*allRows[1:]))
    x = [int(v) * feature1_scalar for v in rowsTranspose[feature1_index]]
    y = [int(v) * feature2_scalar for v in rowsTranspose[feature2_index]]

    createImage(x, y, 'feature_dist.png')
    #createImages(60, x, y, 'images/dist/bi_{:05d}.png', )
    #createMovie('images/dist', 'dist_video.avi')
