namespace MapSabber.Config
{
   class Configuration
   {
      public DeckspaceParams Deckspace { get; set; }
      public SearchParams Search { get; set; }
      public MapParams Map { get; set; }
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
   }

   class MapParams
   {
      public int RemapFrequency { get; set; }
      public int NumFeatures { get; set; }
   }
}
