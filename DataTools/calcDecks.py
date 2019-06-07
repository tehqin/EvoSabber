# This script calculates the number of decks that are possible for each class.
from functools import lru_cache
from collections import Counter

decks = {
        'hunter' : list(zip(
            [1,5,1,3,3,4,2,2,3,3,3,3,4,3,2,1,4,5,5,6,7,6,1,1,1,2,2,4,3,4,7,5,1,1,3,4,6,7,3,6,5,7,5,5,0,5,5,4,1,5,4,1,1,7,1,6,1,5,3,3,1,1,1,1,2,3,2,3,3,3,3,4,4,5,1,6,6,4,3,2,4,4,2,2,4,2,2,2,6,2,1,3,3,3,4,6,2,5,2,3,3,12,6,5,2,3,7,6,3,2,4,5,1,3,2,3,1,1,2,2,6,3,5,3,3,9,2,1,2,3,2,5,9,9,9,9,9,6,3,6,4,10,3,5,4,3,2,2,2,6,2,3,1,2,2,2,2,4,2,5,1,4,3,2,10,3,2,8,6,5,3],
            [2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,2,2,2,2,2,2,2,2,1,1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,2,2,2,2,2,2,1,2,2,2,1,1,2,2,1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,2,2,2,2,1,1,1,1,1,1,1,1,2,2,2,2,2,2,2,2,2,2,2,1,2,2,2,2,2,2,2,2,2,1,2,2,2,1,1,2,2,1,1,2,2],
            )),
        'paladin' : list(zip(
            [1,5,1,7,2,1,4,4,4,4,3,3,4,2,2,3,3,3,3,4,3,2,1,4,5,5,6,7,6,1,1,1,2,2,4,3,4,7,5,1,1,3,4,6,7,3,6,5,7,5,5,0,5,1,6,1,5,3,3,1,1,1,1,2,3,2,3,3,3,3,4,4,5,1,6,6,4,3,2,4,4,2,2,4,2,2,2,6,2,1,3,3,3,4,6,2,5,2,3,3,12,6,5,1,1,1,2,3,7,6,3,8,5,1,2,1,5,3,1,1,3,8,6,3,2,4,5,1,3,2,3,1,1,3,2,5,9,9,9,9,9,6,3,6,4,10,3,5,4,3,6,2,2,1,2,2,2,2,4,2,5,1,4,3,2,10,2,8,6,5,3],
            [2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,2,2,2,2,2,2,2,2,1,1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,2,2,2,2,2,2,1,2,2,2,1,1,2,2,2,2,2,1,2,2,2,2,2,2,2,2,2,2,2,2,1,2,2,2,2,2,2,2,2,2,2,2,2,1,1,1,1,1,1,1,1,2,2,2,2,2,2,2,2,1,2,2,2,2,2,2,2,2,2,1,2,2,2,1,1,2,1,1,2,2],
            )),
        'warlock' : list(zip(
            [1,5,3,1,3,4,1,6,1,3,3,4,2,2,3,3,3,3,4,3,2,1,4,5,5,6,7,6,1,1,1,2,2,4,3,4,7,5,1,1,3,4,6,7,3,6,5,7,5,5,0,5,1,6,1,5,3,3,1,1,1,1,2,3,2,3,3,3,3,4,4,5,1,6,6,4,3,2,4,4,2,2,4,2,2,2,6,2,1,3,3,3,4,6,2,5,2,3,3,12,6,5,2,3,7,6,3,1,4,3,2,1,6,5,8,4,3,1,5,9,3,2,4,5,1,3,2,3,1,1,3,2,5,9,9,9,9,9,6,3,6,4,10,3,5,4,2,3,6,2,0,1,2,2,2,2,4,2,5,1,4,3,2,10,2,8,6,5,3],
            [2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,2,2,2,2,2,2,2,2,1,1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,2,2,2,2,2,2,1,2,2,2,1,1,2,2,1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,2,2,2,2,2,2,2,2,2,2,2,1,1,1,1,1,1,1,1,2,2,2,2,2,2,2,2,2,1,2,2,2,2,2,2,2,2,2,1,2,2,2,1,1,2,1,1,2,2],
            )),
        }

@lru_cache(maxsize=None)
def go(numLeft, numReg, numLegend):
    if numLeft == 0:
        return 1
    if numLeft < 0:
        return 0

    # Go through regular cards first
    result = 0
    if numReg > 0:
        # Number of times you can take [0, 2]
        result = go(numLeft, numReg-1, numLegend) 
        result += go(numLeft-1, numReg-1, numLegend)
        result += go(numLeft-2, numReg-1, numLegend)
    elif numLegend > 0:
        # Number of times you can take [0, 2]
        result = go(numLeft, numReg, numLegend-1)
        result += go(numLeft-1, numReg, numLegend-1)
    return result


# DP state (curCard) -- (numLeft, sumCost, sumCostSquares)
def calcDistribution(className):
    deckList = decks[className]

    curTable = Counter()
    curTable[(30, 0, 0)] = 1

    for i, (curCost, maxInclusion) in enumerate(deckList):
        nxtTable = Counter()
        curCost2 = curCost ** 2

        for curState in curTable:
            numLeft, sumCost, sumCostSquares = curState
            for x in range(maxInclusion+1):
                if numLeft-x >= 0:
                    nxtState = (
                            numLeft-x, 
                            sumCost+x*curCost, 
                            sumCostSquares+x*curCost2
                        )
                    nxtTable[nxtState] += curTable[curState]

        curTable = nxtTable
        print('{}/{}: {}'.format(i, len(deckList), len(curTable)))

    fp = open(className+'.csv', "w")
    fp.write('Average Mana, Mana Variance, Frequency\n')

    for curState in curTable:
        numLeft, sumCost, sumCostSquares = curState
        avgCost = sumCost / 30.0
        variance = sumCostSquares / 30.0 - avgCost * avgCost
        if numLeft == 0:
            deckCount = curTable[curState]
            fp.write('{},{},{}\n'.format(avgCost, variance, deckCount))

for className in decks:
    calcDistribution(className)
