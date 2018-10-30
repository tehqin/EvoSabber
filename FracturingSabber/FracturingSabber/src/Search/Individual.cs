using System;
using System.Collections.Generic;

using SabberStoneCore.Model;

namespace FracturingSabber.Search
{
   class Individual
   {
		private List<Card> _cardSet;
         
      public int[] CardCounts { get; private set; }
   
      // Run stats
      public int ID { get; set; }
      public int WinCount { get; set; }
      public int TotalHealthDifference { get; set; }
      public int DamageDone { get; set; }
      public int NumTurns { get; set; }
      public int CardsDrawn { get; set; }
      public int ManaSpent { get; set; }
      public int StrategyAlignment { get; set; }

      public int RawFitness { get; set; }
      public int Fitness { get; set; }
      public int[] Features { get; set; }
  
      // Order this individual should be fractured
      public List<int> CardRanking { get; private set; }
      public int[] CardUsageCounts { get; private set; }

      public Individual(int[] cardCounts, List<Card> cardSet)
      {
         CardCounts = cardCounts;
         _cardSet = cardSet;
      }

      public void GenerateCardRanking(int[] cardUsage)
      {
         CardUsageCounts = new int[cardUsage.Length];
         cardUsage.CopyTo(CardUsageCounts, 0);

			// Put the cards in the order of usage.
			// Cards are only elligible for fracture if it is
			// possible to take less cards.
			CardRanking = new List<int>();
			int foundIndex = 0;
			while (foundIndex >= 0)
			{
				foundIndex = -1;
				int bestUsage = -1;
				for (int i=0; i<_cardSet.Count; i++)
				{
					if (cardUsage[i] > bestUsage)
					{
						foundIndex = i;
						bestUsage = cardUsage[i];
					}
				}

				if (foundIndex >= 0)
				{
					CardRanking.Add(foundIndex);
					cardUsage[foundIndex] = -1;
				}
			}
		}

      public List<string> GetCards()
      {
         var cards = new List<string>();
         for (int i=0; i<CardCounts.Length; i++)
            for (int cnt=0; cnt<CardCounts[i]; cnt++)
               cards.Add(_cardSet[i].Name);
         return cards;
      }

      public override string ToString()
      {
         return string.Join("", CardCounts);
      }
   }
}
