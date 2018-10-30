#!/bin/sh
#
# Usage: gputest.sh
# Change job name and email address as needed 
#        

# -- our name ---
#$ -N MapElites
#$ -S /bin/sh
# Make sure that the .e and .o file arrive in the
#working directory
#$ -cwd
#Merge the standard out and standard error to one file
#$ -j y
# Send mail at submission and completion of script
# Specify GPU queue
#$ -q medium
#$ -l mem_free=14.0G
/bin/echo Running on host: `hostname`.
/bin/echo In directory: `pwd`
/bin/echo Starting on: `date`

# Load CUDA module
module load mono
#Full path to executable

mono ~/$USER/EvoSabber/MapSabber/MapSabber/bin/Release/MapSabber.exe warlock
