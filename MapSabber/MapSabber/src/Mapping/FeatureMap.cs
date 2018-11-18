using System.Collections.Generic;

using MapSabber.Search;

namespace MapSabber.Mapping
{
   interface FeatureMap
   {
      int NumGroups { get; }
      int NumFeatures { get; }
      Dictionary<string, Individual> EliteMap { get; }
      Dictionary<string, int> CellCount { get; }

      void Add(Individual toAdd);
      Individual GetRandomElite();

      void LogMap(string logFilename);
   }
}
