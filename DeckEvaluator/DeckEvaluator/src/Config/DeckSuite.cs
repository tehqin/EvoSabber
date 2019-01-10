namespace DeckEvaluator.Config
{
   class DeckSuite
   {
      public OpponentParams[] Opponents { get; set; }
   }
   
   class OpponentParams
   {
      public int Portion { get; set; }
      public string Strategy { get; set; }
      public string DeckPool { get; set; }
      public string DeckName { get; set; }
   }
}
