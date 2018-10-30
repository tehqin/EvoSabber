using System;

using C5;

using FracturingSabber.Logging;
using FracturingSabber.Search;

namespace FracturingSabber.Population
{
   class FittestPopulation
   {
      private int _maxPopulation;
      private IntervalHeap<Shard> _populationDeque;

		// Stats
      private int _maxWins;
      private int _maxFitness;

		// Logging
      private static readonly string _logDirectory = "logs/";
      private static readonly string CHAMPION_LOG_FILENAME =
			_logDirectory + "champion_log.csv";
		private static readonly string FITTEST_LOG_FILENAME =
			_logDirectory + "fittest_log.csv";
		private RunningIndividualLog _champion_log;
		private RunningIndividualLog _fittest_log;

      public FittestPopulation(int maxPopulation)
      {
         _maxPopulation = maxPopulation;
         _populationDeque = new IntervalHeap<Shard>(new ShardComparer());

         _maxWins = 0;
         _maxFitness = Int32.MinValue;

			_champion_log =
				new RunningIndividualLog(CHAMPION_LOG_FILENAME);
			_fittest_log =
				new RunningIndividualLog(FITTEST_LOG_FILENAME);
      }

      private void CalculateFitness(Individual cur)
      {
         cur.Fitness = cur.TotalHealthDifference;
         Console.WriteLine("Fitness: "+cur.Fitness);

			// Save stats
         bool didHitMaxWins =
            cur.WinCount > _maxWins;
         bool didHitMaxFitness =
            cur.Fitness > _maxFitness;
         _maxWins = Math.Max(_maxWins, cur.WinCount);
         _maxFitness = Math.Max(_maxFitness, cur.Fitness);

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
      }

      public Shard PollFittest()
      {
         return _populationDeque.DeleteMax();
      }
   }
}
