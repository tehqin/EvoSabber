namespace StratSearchSabber
{
   class Configuration
   {
   	public EvaluationParams Evaluation { get; set; }
      public PlayerParams Player { get; set; }
      public SearchParams Search { get; set; }
   }

   class EvaluationParams
   {
      public string[] DeckPools { get; set; }
   }

   class SearchParams
   {
      public int InitialPopulation { get; set; }
      public int NumToEvaluate { get; set; }
      public int NumElites { get; set; }
      public double MutationScalar { get; set; }
   }

	class PlayerParams
	{
      public string DeckPool { get; set; }
      public string DeckName { get; set; }
	}
}
