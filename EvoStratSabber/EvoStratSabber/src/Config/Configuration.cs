namespace EvoStratSabber.Config
{
   class Configuration
   {
      public DeckspaceParams Deckspace { get; set; }
      public SearchParams Search { get; set; }
   }

   class DeckspaceParams
   {
      public string HeroClass { get; set; }
   }

   class SearchParams
   {
      public int InitialPopulation { get; set; }
      public int NumToEvaluate { get; set; }
      public int NumElites { get; set; }
   }
}
