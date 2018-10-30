using System;
using System.Collections.Generic;
using System.IO;

using MapSabber.Search;

namespace MapSabber.Mapping
{
   interface FeatureMap
   {
      int NumGroups { get; }
      int NumFeatures { get; }
      Dictionary<string, Individual> EliteMap { get; }
      Dictionary<string, int> CellCount { get; }
   }
}
