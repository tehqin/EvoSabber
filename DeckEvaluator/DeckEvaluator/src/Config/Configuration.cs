namespace DeckEvaluator.Config
{
   class Configuration
   {
      public EvaluationParams Evaluation { get; set; }
   }

   class EvaluationParams
   {
      public int NumGames { get; set; }
      public OpponentParams Opponent { get; set; }
   }

   class OpponentParams
   {
      public string DeckFile { get; set; }
   }
}
