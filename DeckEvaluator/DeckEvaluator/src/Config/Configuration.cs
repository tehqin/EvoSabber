namespace DeckEvaluator.Config
{
   class Configuration
   {
      public EvaluationParams Evaluation { get; set; }
   }

   class EvaluationParams
   {
      public string OpponentDeckSuite { get; set; }
      public string[] DeckPools { get; set; }
      public PlayerStrategyParams[] PlayerStrategies { get; set; }
   }

   class PlayerStrategyParams
   {
      public int NumGames { get; set; }
      public string Strategy { get; set; }
   }
}
