using System;
using System.Collections.Generic;
using System.IO;

using FracturingSabber.Search;

namespace FracturingSabber.Mapping
{
   class FeatureMap
   {
      private static Random rnd = new Random();
      private List<Individual> _allIndividuals;

      private int _remapFrequency;

      private List<int>[] _groupBoundaries;
      private List<string> _eliteIndices;

      public int NumGroups { get; private set; }
      public int NumFeatures { get; private set; }
      public Dictionary<string, List<Individual>>
         EliteMap { get; private set; }

      public FeatureMap(int numFeatures, int remapFrequency,
                        int numGroups)
      {
         _allIndividuals = new List<Individual>();
         _remapFrequency = remapFrequency;
         NumFeatures = numFeatures;
         NumGroups = numGroups;
      
         _groupBoundaries = new List<int>[NumFeatures];
      }

      private int GetFeatureIndex(int featureId, int feature)
      {
         // Find the bucket index we belong on this dimension
         int index = 0;
         while (index < NumGroups && 
                _groupBoundaries[featureId][index] < feature)
         {
            index++;
         }

         return Math.Max(0, index-1);
      }

      private string GetIndex(Individual toAdd)
      {
         var features = new int[NumFeatures];
         for (int i=0; i<NumFeatures; i++)
            features[i] = GetFeatureIndex(i, toAdd.Features[i]);
         return string.Join(":", features);
      }

      private void AddToMap(Individual toAdd)
      {
         string index = GetIndex(toAdd);
         if (!EliteMap.ContainsKey(index))
         {
            _eliteIndices.Add(index);
            EliteMap.Add(index, new List<Individual>());
         }
         
         // Insert this individual to this position
         List<Individual> cellmates = EliteMap[index];
         int insertionPoint = 0;
         while (insertionPoint < cellmates.Count &&
                toAdd.RawFitness < cellmates[insertionPoint].RawFitness)
         {
            insertionPoint++;
         }
         cellmates.Insert(insertionPoint, toAdd);
      }

      // Update the boundaries of each feature.
      // Add all the individuals available to the map.
      private void Remap()
      {
         var features = new List<int>[NumFeatures];
         for (int i=0; i<NumFeatures; i++)
            features[i] = new List<int>();
      
         foreach (Individual cur in _allIndividuals)
            for (int i=0; i<NumFeatures; i++)
               features[i].Add(cur.Features[i]);
      
         for (int i=0; i<NumFeatures; i++)
            features[i].Sort();
        
         for (int i=0; i<NumFeatures; i++)
         {
            _groupBoundaries[i] = new List<int>();
         
            for (int x=0; x<NumGroups; x++)
            {
               int sampleIndex = x * _allIndividuals.Count / NumGroups;
               _groupBoundaries[i].Add(features[i][sampleIndex]);
            }
         }

         // Populate the feature map using the new boundaries.
         _eliteIndices = new List<string>();
         EliteMap = new Dictionary<string,List<Individual>>();
         foreach (Individual cur in _allIndividuals)
            AddToMap(cur);
      }

      public bool Add(Individual toAdd)
      {
         _allIndividuals.Add(toAdd);

         if (_allIndividuals.Count % _remapFrequency == 1)
         {
            Remap();
            return true;
         }
         
         AddToMap(toAdd);
         return false;
      }

      public int GetRank(Individual cur)
      {
         string index = GetIndex(cur);
         if (!EliteMap.ContainsKey(index))
            return 0;

         List<Individual> cellmates = EliteMap[index];
         int rank = 0;
         while (rank < cellmates.Count &&
                cur.RawFitness < cellmates[rank].RawFitness)
         {
            rank++; 
         }   
         return rank;
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
               Individual cur = EliteMap[index][0];
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
