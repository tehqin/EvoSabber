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
      public PlayerParams Player { get; set; }
   }

   class OpponentParams
   {
      public string DeckFile { get; set; }
      public string Strategy { get; set; }
   }

   class PlayerParams
   {
      public string Strategy { get; set; }
   }
}
