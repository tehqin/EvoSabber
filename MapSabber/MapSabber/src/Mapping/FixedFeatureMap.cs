using System;
using System.Collections.Generic;
using System.IO;

using MapSabber.Search;

/* This is a FeatureMap that has fixed boundaries at even intervals.
 * It is exactly as described in the original MAP-Elites paper.
 */

namespace MapSabber.Mapping
{
   class FixedFeatureMap : FeatureMap
   {
      private static Random rnd = new Random();

      public int NumGroups { get; private set; }
      public int NumFeatures { get; private set; }
      public Dictionary<string, Individual> EliteMap { get; private set; }
      public Dictionary<string, int> CellCount { get; private set; }

      private List<string> _eliteIndices;
      private int[] _lowGroupBound;
      private int[] _highGroupBound;

      public FixedFeatureMap(int numFeatures, int numGroups, 
            int[] lowGroupBound, int[] highGroupBound)
      {
         NumFeatures = numFeatures;
         NumGroups = numGroups;
         _lowGroupBound = new int[numFeatures];
         _highGroupBound = new int[numFeatures];
         Array.Copy(lowGroupBound, _lowGroupBound, numFeatures);
         Array.Copy(highGroupBound, _highGroupBound, numFeatures);
      
         // Populate the feature map using the new boundaries.
         _eliteIndices = new List<string>();
         EliteMap = new Dictionary<string,Individual>();
         CellCount = new Dictionary<string,int>();
      }

      private int getFeatureIndex(int featureId, int feature)
      {
         if (feature <= _lowGroupBound[featureId])
            return 0;
         if (_highGroupBound[featureId] <= feature)
            return NumGroups-1;

         int gap = _highGroupBound[featureId] - _lowGroupBound[featureId] + 1;
         int pos = feature - _lowGroupBound[featureId];
         int index = NumGroups * pos / gap;
         return index;
      }

      public void Add(Individual toAdd)
      {
         var features = new int[NumFeatures];
         for (int i=0; i<NumFeatures; i++)
            features[i] = getFeatureIndex(i, toAdd.Features[i]);
         string index = string.Join(":", features);
     
         if (!EliteMap.ContainsKey(index))
         {
            _eliteIndices.Add(index);
            EliteMap.Add(index, toAdd);
            CellCount.Add(index, 0);
         }
         else if (EliteMap[index].Fitness < toAdd.Fitness)
         {
            EliteMap[index] = toAdd;
         }

         CellCount[index] += 1;
      }

      public Individual GetRandomElite()
      {
         int pos = rnd.Next(_eliteIndices.Count);
         string index = _eliteIndices[pos];
         return EliteMap[index];
      }

      public void LogMap(string logFilename)
      {
         using (var sw = new StreamWriter(logFilename)) 
         {
            // The data to maintain for individuals evaluated.
            string[] dataLabels = {
                  "Map Key",
                  "Win Count",
                  "Health Difference",
                  "Damage Done",
                  "Num Turns",
                  "Cards Drawn",
                  "Mana Spent",
                  "Strategy Alignment",
                  "Deck",
               };

            sw.WriteLine(string.Join(",", dataLabels));
            foreach (string index in _eliteIndices)
            {
               Individual cur = EliteMap[index];
					string[] data = {
							index,
							cur.WinCount.ToString(),
							cur.TotalHealthDifference.ToString(),
							cur.DamageDone.ToString(),
							cur.NumTurns.ToString(),
							cur.CardsDrawn.ToString(),
							cur.ManaSpent.ToString(),
							cur.StrategyAlignment.ToString(),
							string.Join("*", cur.GetCards())
						};
               sw.WriteLine(string.Join(",", data));
            }
         }
      }
   }
}
