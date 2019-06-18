using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using Nett;

using SabberStoneCore.Enums;
using SabberStoneCore.Model;

using StratSearchSabber.Config;
using StratSearchSabber.Decks;
using StratSearchSabber.Logging;
using StratSearchSabber.Messaging;

namespace StratSearchSabber.Search
{
   class EvolutionaryStrategies
   {
      private Queue<int> _runningWorkers;
      private Queue<int> _idleWorkers;
		private Dictionary<int,Individual> _individualStable;
      private Random rnd = new Random();

      private int _individualsEvaluated;
      private int _individualsDispatched;

      // ES Parameters
      private string _configFilename;
      private SearchParams _params;

      // Deck info
      readonly private Deck _playerDeck; 

      // Logging 
      private const string LOG_DIRECTORY = "logs/";
      private const string INDIVIDUAL_LOG_FILENAME = 
         LOG_DIRECTORY + "individual_log.csv";
      private const string CHAMPION_LOG_FILENAME = 
         LOG_DIRECTORY + "champion_log.csv";
      private const string FITTEST_LOG_FILENAME = 
         LOG_DIRECTORY + "fittest_log.csv";
      private RunningIndividualLog _individualLog;
      private RunningIndividualLog _championLog;
      private RunningIndividualLog _fittestLog;

      // Node communication
      private const string _boxesDirectory = "boxes/";
      private const string _inboxTemplate = _boxesDirectory
         + "deck-{0,4:D4}-inbox.tml";
      private const string _outboxTemplate = _boxesDirectory
         + "deck-{0,4:D4}-outbox.tml";
      private const string _activeDirectory = "active/";
      private const string _activeWorkerTemplate = _activeDirectory
         + "worker-{0,4:D4}.txt";
      private const string _activeSearchPath = _activeDirectory
         + "search.txt";
         
      public EvolutionaryStrategies(string configFilename)
      {
         // Grab the config info
         _configFilename = configFilename;
         var config = Toml.ReadFile<Configuration>(_configFilename);
         _params = config.Search;

         // Setup the deck pool and grab our deck and class
         var deckPoolManager = new DeckPoolManager(); 
         deckPoolManager.AddDeckPools(config.Evaluation.DeckPools);
         string poolName = config.Player.DeckPool;
         string deckName = config.Player.DeckName;
         Console.WriteLine(string.Format("names {0} {1}", poolName, deckName)); 
         _playerDeck = deckPoolManager.GetDeck(poolName, deckName);


         // Setup the logs to record the data on individuals
         InitLogs();
      }

      private void InitLogs()
      {
         _individualLog =
            new RunningIndividualLog(INDIVIDUAL_LOG_FILENAME);
         _championLog =
            new RunningIndividualLog(CHAMPION_LOG_FILENAME);
         _fittestLog =
            new RunningIndividualLog(FITTEST_LOG_FILENAME);
      }
      
      private static void WriteText(Stream fs, string s)
      {
         s += "\n";
         byte[] info = new UTF8Encoding(true).GetBytes(s);
         fs.Write(info, 0, info.Length);
      }

      private void SendWork(string workerInboxPath, Individual cur)
      {
         var deckParams = new DeckParams();
         deckParams.ClassName = _playerDeck.DeckClass.ToString();
         deckParams.CardList = _playerDeck.GetCardNames();
         CustomStratWeights weights = cur.GetWeights(); 

         var msg = new PlayMatchesMessage();
         msg.Deck = deckParams;
         msg.Strategy = weights;

         Toml.WriteFile<PlayMatchesMessage>(msg, workerInboxPath);
      }

      private int _maxWins;
      private int _maxFitness;
      private void ReceiveResults(string workerOutboxPath, Individual cur)
      {
         // Read the message and then delete the file.
         var results = Toml.ReadFile<ResultsMessage>(workerOutboxPath);
         File.Delete(workerOutboxPath);

			// Save the statistics for this individual.
         cur.ID = _individualsEvaluated;
         cur.OverallData = results.OverallStats;
         cur.StrategyData = results.StrategyStats;
 
         // Save which elements are relevant to the search
         cur.Fitness = cur.OverallData.TotalHealthDifference;

         var os = results.OverallStats;
         Console.WriteLine("------------------");
         Console.WriteLine(string.Format("Eval ({0}):", _individualsEvaluated));
         Console.WriteLine("Win Count: "+os.WinCount);
         Console.WriteLine("Total Health Difference: "
                           +os.TotalHealthDifference);
         Console.WriteLine("Damage Done: "+os.DamageDone);
         Console.WriteLine("Num Turns: "+os.NumTurns);
         Console.WriteLine("Cards Drawn: "+os.CardsDrawn);
         Console.WriteLine("Hand Size: "+os.HandSize);
         Console.WriteLine("Mana Spent: "+os.ManaSpent);
         Console.WriteLine("Mana Wasted: "+os.ManaWasted);
         Console.WriteLine("Strategy Alignment: "+os.StrategyAlignment);
         Console.WriteLine("Dust: "+os.Dust);
         Console.WriteLine("Deck Mana Sum: "+os.DeckManaSum);
         Console.WriteLine("Deck Mana Variance: "+os.DeckManaVariance);
         Console.WriteLine("Num Minion Cards: "+os.NumMinionCards);
         Console.WriteLine("Num Spell Cards: "+os.NumSpellCards);
         Console.WriteLine("------------------");
         foreach (var fs in results.StrategyStats)
         {
            Console.WriteLine("WinCount: "+fs.WinCount);
            Console.WriteLine("Alignment: "+fs.Alignment);
            Console.WriteLine("------------------");
         }

         // Save stats
         bool didHitMaxWins = cur.OverallData.WinCount > _maxWins;
         bool didHitMaxFitness = cur.Fitness > _maxFitness;
         _maxWins = Math.Max(_maxWins, cur.OverallData.WinCount);
         _maxFitness = Math.Max(_maxFitness, cur.Fitness);

         // Log the individuals
         _individualLog.LogIndividual(cur);
         if (didHitMaxWins)
            _championLog.LogIndividual(cur);
         if (didHitMaxFitness)
            _fittestLog.LogIndividual(cur);
      }

      private void FindNewWorkers()
      {
         string[] hailingFiles = Directory.GetFiles(_activeDirectory);
         foreach (string activeFile in hailingFiles)
         {
            string prefix = _activeDirectory + "worker-";
   			if (activeFile.StartsWith(prefix))
				{
					string suffix = ".txt";
					int start = prefix.Length;
					int end = activeFile.Length - suffix.Length;
					string label = activeFile.Substring(start, end-start);
					int workerId = Int32.Parse(label);
					_idleWorkers.Enqueue(workerId);
					_individualStable.Add(workerId, null);
					File.Delete(activeFile);
					Console.WriteLine("Found worker: " + workerId);
				}
         }
      }

	   private Individual ChooseElite(List<Individual> elites)
      {
         int pos = rnd.Next(elites.Count);
         return elites[pos];
      }

      public void Run()
      {
         _individualsEvaluated = 0;
         _maxWins = 0;
         _maxFitness = Int32.MinValue;
         _runningWorkers = new Queue<int>();
         _idleWorkers = new Queue<int>();
         _individualStable = new Dictionary<int,Individual>();
         var population = new List<Individual>();
         
         // Let the workers know we are here.
			using (FileStream ow = File.Open(_activeSearchPath,
						FileMode.Create, FileAccess.Write, FileShare.None))
			{
				WriteText(ow, "Strategy Search");
				WriteText(ow, _configFilename);
				ow.Close();
			}

         Console.WriteLine("Begin search...");
         while (_individualsEvaluated < _params.NumToEvaluate)
         {
				FindNewWorkers();
            
            // Grab the elites.
            var elites = population.OrderBy(o => o.Fitness)
               .Reverse().Take(_params.NumElites).ToList();

            // Disbatch jobs to the available workers.
            while (_idleWorkers.Count > 0)
            {
               if (_individualsDispatched >= _params.InitialPopulation &&
                   _individualsEvaluated == 0)
               {
                  break;
               }

               int workerId = _idleWorkers.Dequeue();
               _runningWorkers.Enqueue(workerId);
               Console.WriteLine("Starting worker: "+workerId);

               Individual choiceIndividual =
                  _individualsDispatched < _params.InitialPopulation ?
                     Individual.GenerateRandomIndividual() :
                     ChooseElite(elites).Mutate(_params.MutationScalar);

               string inboxPath = string.Format(_inboxTemplate, workerId);
               SendWork(inboxPath, choiceIndividual);
               _individualStable[workerId] = choiceIndividual;
               _individualsDispatched++;
            }

            // Look for individuals that are done.
				population.Clear();
            int numActiveWorkers = _runningWorkers.Count;
            for (int i=0; i<numActiveWorkers; i++)
            {
               int workerId = _runningWorkers.Dequeue();
               string inboxPath = string.Format(_inboxTemplate, workerId);
               string outboxPath = string.Format(_outboxTemplate, workerId);

               // Test if this worker is done.
               if (File.Exists(outboxPath) && !File.Exists(inboxPath))
               {
                  // Wait for the file to finish being written.
                  Console.WriteLine("Worker done: " + workerId);

                  ReceiveResults(outboxPath, _individualStable[workerId]);
                  population.Add(_individualStable[workerId]);
                  _idleWorkers.Enqueue(workerId);
                  _individualsEvaluated++;
               }
               else
               {
                  _runningWorkers.Enqueue(workerId);
               }
            }

            // Add the elites back in.
            foreach (var curElite in elites)
            {
               population.Add(curElite);
            }

            Thread.Sleep(1000);
         }

         // Let the workers know that we are done.
         File.Delete(_activeSearchPath);
      }
   }
}
