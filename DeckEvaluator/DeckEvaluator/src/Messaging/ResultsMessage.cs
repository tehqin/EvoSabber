using DeckEvaluator.Config;

namespace DeckEvaluator.Messaging
{
   class ResultsMessage
   {
      public DeckParams PlayerDeck { get; set; }
		public OverallStatistics OverallStats { get; set; }
		public StrategyStatistics[] StrategyStats { get; set; }
   }

   class OverallStatistics
   {
      public int[] UsageCounts { get; set; }
      public int WinCount { get; set; }
      public int TotalHealthDifference { get; set; }
      public int DamageDone { get; set; }
      public int NumTurns { get; set; }
      public int CardsDrawn { get; set; }
      public int HandSize { get; set; }
      public int ManaSpent { get; set; }
      public int ManaWasted { get; set; }
      public int StrategyAlignment { get; set; }
      public int Dust { get; set; }
      public int DeckManaSum { get; set; }
      public int DeckManaVariance { get; set; }
      public int NumMinionCards { get; set; }
      public int NumSpellCards { get; set; }
   
      public void Accumulate(OverallStatistics rhs)
      {
         for (int i=0; i<UsageCounts.Length; i++)
            UsageCounts[i] += rhs.UsageCounts[i];
         WinCount += rhs.WinCount;
         TotalHealthDifference += rhs.TotalHealthDifference;
         DamageDone += rhs.DamageDone;
         NumTurns += rhs.NumTurns;
         CardsDrawn += rhs.CardsDrawn;
         HandSize += rhs.HandSize;
         ManaSpent += rhs.ManaSpent;
         ManaWasted += rhs.ManaWasted;
         StrategyAlignment += rhs.StrategyAlignment;
      }

      public void ScaleByNumStrategies(int numStrats)
      {
         DamageDone /= numStrats;
         NumTurns /= numStrats;
         CardsDrawn /= numStrats;
         HandSize /= numStrats;
         ManaSpent /= numStrats;
         ManaWasted /= numStrats;
         StrategyAlignment /= numStrats;
      }
   }

   class StrategyStatistics
   {
      public int WinCount { get; set; }
      public int Alignment { get; set; }
   }
}
