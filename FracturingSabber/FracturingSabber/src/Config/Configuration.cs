namespace FracturingSabber.Config
{
   class Configuration
   {
      public DeckspaceParams Deckspace { get; set; }
      public SearchParams Search { get; set; }
   }

   class DeckspaceParams
   {
      public string HeroClass { get; set; }
      public string[] CardSets { get; set; }
   }

   class SearchParams
   {
      public int InitialPopulation { get; set; }
      public int NumToEvaluate { get; set; }
      public int MaxPopulation { get; set; }
   }
}
