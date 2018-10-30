using System;
using System.Collections.Generic;

using C5;

using FracturingSabber.Logging;
using FracturingSabber.Mapping;
using FracturingSabber.Search;

namespace FracturingSabber.Population
{
   class MapPopulation
   {
      private int _maxPopulation;
      private IntervalHeap<Shard> _populationDeque;
      private FeatureMap _featureMap;

      // Mapping Paraneters
      private const int REMAP_FREQUENCY = 100;
      private const int NUM_GROUPS_PER_FEATURE = 20;
      private const int NUM_FEATURES = 2;

		// Stats
      private int _maxWins;
      private int _maxFitness;

		// Logging
      private static readonly string _logDirectory = "logs/";
      private static readonly string CHAMPION_LOG_FILENAME =
			_logDirectory + "champion_log.csv";
		private static readonly string FITTEST_LOG_FILENAME =
			_logDirectory + "fittest_log.csv";
      private static readonly string ELITE_MAP_FILENAME =
         _logDirectory + "elite_map_log.csv";
		private RunningIndividualLog _champion_log;
		private RunningIndividualLog _fittest_log;
      private FrequentMapLog _map_log;

      public MapPopulation(int maxPopulation)
      {
         _maxPopulation = maxPopulation;
         _populationDeque = new IntervalHeap<Shard>(new ShardComparer());
         _featureMap = new FeatureMap(NUM_FEATURES, REMAP_FREQUENCY, 
               NUM_GROUPS_PER_FEATURE);

         _maxWins = 0;
         _maxFitness = Int32.MinValue;

			_champion_log =
				new RunningIndividualLog(CHAMPION_LOG_FILENAME);
			_fittest_log =
				new RunningIndividualLog(FITTEST_LOG_FILENAME);
         _map_log =
            new FrequentMapLog(ELITE_MAP_FILENAME, _featureMap);
      }

      private void CalculateFitness(Individual cur)
      {
         cur.RawFitness = cur.TotalHealthDifference;
         cur.Features = new []{cur.DamageDone, cur.NumTurns};
         Console.WriteLine("Raw Fitness: "+cur.RawFitness);

         if (_featureMap.Add(cur))
         {
            // Reassess the fitness of all elements
            var queue = new Queue<Shard>();
            while (!_populationDeque.IsEmpty)
            {
               Shard curShard = _populationDeque.DeleteMax();
               curShard.Representative.Fitness = 
                  -_featureMap.GetRank(curShard.Representative);
               queue.Enqueue(curShard);
            }

            while (queue.Count > 0)
            {
               Shard curShard = queue.Dequeue();
               _populationDeque.Add(curShard);
            }
         }
         cur.Fitness = -_featureMap.GetRank(cur);
         Console.WriteLine("Fitness: "+cur.Fitness);

			// Save stats
         bool didHitMaxWins =
            cur.WinCount > _maxWins;
         bool didHitMaxFitness =
            cur.RawFitness > _maxFitness;
         _maxWins = Math.Max(_maxWins, cur.WinCount);
         _maxFitness = Math.Max(_maxFitness, cur.RawFitness);

         // Log individual
         if (didHitMaxWins)
            _champion_log.LogIndividual(cur);
         if (didHitMaxFitness)
            _fittest_log.LogIndividual(cur);
      }

      public bool IsEmpty()
      {
         return _populationDeque.IsEmpty;
      }

      public void Add(Shard cur)
      {
         CalculateFitness(cur.Representative);
         _populationDeque.Add(cur);

         if (_populationDeque.Count > _maxPopulation)
         {
            _populationDeque.DeleteMin();
         }

         _map_log.UpdateLog();
      }

      public Shard PollFittest()
      {
         return _populationDeque.DeleteMax();
      }
   }
}
