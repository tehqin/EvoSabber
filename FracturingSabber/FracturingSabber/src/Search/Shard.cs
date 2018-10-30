using System;
using System.Collections.Generic;

using SabberStoneCore.Model;

namespace FracturingSabber.Search
{
   class Shard
   {
      private static Random rnd = new Random();
      
      public static Shard GenerateInitialShard(List<Card> cardSet)
      {
         var newShard = new Shard(cardSet);
         for (int i=0; i<cardSet.Count; i++)
         {
            newShard._high[i] = cardSet[i].MaxAllowedInDeck; 
         }
         newShard.GenerateRandomRepresentative();
         return newShard;
      }

      // Search space
      private List<Card> _cardSet;
		private int[] _high;
	   private int[] _low;
  
      // Representative individual
		public Individual Representative { get; private set; }

      public Shard(List<Card> cardSet)
      {
         _cardSet = cardSet;
      	_high = new int[cardSet.Count];
      	_low = new int[cardSet.Count];

         // No individual yet.
      	Representative = null;
      }

      public bool GenerateRandomRepresentative()
      {
			int cardsLeft = 30;
	      int[] cardCounts = new int[_high.Length];

			// First add all the cards that must be in the deck
			for (int i=0; i<_low.Length; i++)
			{
				if (_low[i] > _high[i])
					return false;
				cardCounts[i] = _low[i];
				cardsLeft -= _low[i];
			}

			if (cardsLeft < 0)
				return false;

			// Grab all cards that can still be in the deck
			var available = new List<int>();
			for (int i=0; i<_low.Length; i++)
			{
				int cnt = _high[i]-_low[i];
				for (int j=0; j<cnt; j++)
					available.Add(i);
			}

			if (available.Count < cardsLeft)
				return false;

			// Would use available.Shuffle() but doesn't work with the
			// version of mono installed on the cluster.
			for (int i=1; i<available.Count; i++)
			{
				int j = (int)(rnd.NextDouble() * i);
				int tmp = available[i];
				available[i] = available[j];
				available[j] = tmp;
			}
			for (int i=0; i<cardsLeft; i++)
				cardCounts[available[i]]++;

         Representative = new Individual(cardCounts, _cardSet);
			return true;
      }
   
      public List<Shard> FractureOnRepresentative()
      {
         var subspaces = new List<Shard>();
         List<int> cardRanking = Representative.CardRanking;
  
			// Loop through all possible split points
			for (int splitPoint=0; splitPoint<cardRanking.Count; splitPoint++)
			{
            int cardId = cardRanking[splitPoint];
            if (_low[cardId] < Representative.CardCounts[cardId])
            {
               var nxtShard = new Shard(_cardSet);
               for (int i=0; i<_cardSet.Count; i++)
               {
                  nxtShard._high[i] = _high[i];
                  nxtShard._low[i] = _low[i];
               }
   
               // Prevent from taking this card in the future.
               nxtShard._high[cardId] = 
                  Representative.CardCounts[cardId]-1;
   
               // Add cards indexed less than the SplitPoint must take 
               // at least this many of this card.
               for (int i=0; i<splitPoint; i++)
               {
                  cardId = cardRanking[i];
                  nxtShard._low[cardId] = 
                     Representative.CardCounts[cardId];
               }
  
               // Only add the Shard if it contains at least one individual.
               if (nxtShard.GenerateRandomRepresentative())
                  subspaces.Add(nxtShard);
            }
         }
  
         return subspaces;
      }
   }

   class ShardComparer : IComparer<Shard>
   {
      public int Compare(Shard a, Shard b)
      {
         if (a.Representative.Fitness != b.Representative.Fitness)
            return a.Representative.Fitness <= b.Representative.Fitness ? -1 : 1;
         return a.Representative.RawFitness <= b.Representative.RawFitness ? -1 : 1;
      }
   }
}
