using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using Nett;

using SabberStoneCore.Enums;
using SabberStoneCore.Model;

using EvoStratSabber.Config;
using EvoStratSabber.Logging;

namespace EvoStratSabber.Search
{
   class EvolutionaryStrategies
   {
      readonly private CardClass _heroClass;
      readonly private List<Card> _cardSet;
      private Queue<int> _runningWorkers;
      private Queue<int> _idleWorkers;
		private Dictionary<int,Individual> _individualStable;
      private Random rnd = new Random();

      private int _individualsEvaluated;
      private int _individualsDispatched;

      // ES Parameters
      private string _configFilename;
      private SearchParams _params;

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
         + "deck-{0,4:D4}-inbox.txt";
      private const string _outboxTemplate = _boxesDirectory
         + "deck-{0,4:D4}-outbox.txt";
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

         // Configure the search space
         _heroClass = CardReader.GetClassFromName(config.Deckspace.HeroClass);
         CardSet[] sets = CardReader.GetSetsFromNames(config.Deckspace.CardSets);
         _cardSet = CardReader.GetCards(_heroClass, sets);
         
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
         List<string> deck = cur.GetCards();

			using (FileStream ow = File.Open(workerInboxPath,
                   FileMode.Create, FileAccess.Write, FileShare.None))
         {
            WriteText(ow, _heroClass.ToString().ToLower());
            WriteText(ow, string.Join("*", deck));
            ow.Close();
         }
      }

      private int _maxWins;
      private int _maxFitness;
      private void ReceiveResults(string workerOutboxPath, Individual cur)
      {
         // Read the file and calculate a true fitness
         string[] textLines = File.ReadAllLines(workerOutboxPath);

         // Delete the file!
         File.Delete(workerOutboxPath);

			// Pull out the data from the text of the file.
         char[] delimeters = {'*'};
         //string[] cardNames = textLines[0].Split(delimeters);
         string[] countText = textLines[1].Split(delimeters);
         cur.ID = _individualsEvaluated;
         cur.WinCount = Int32.Parse(textLines[2]);
         cur.TotalHealthDifference = Int32.Parse(textLines[3]);
         cur.DamageDone = Int32.Parse(textLines[4]);
         cur.NumTurns = Int32.Parse(textLines[5]);
         cur.CardsDrawn = Int32.Parse(textLines[6]);
         cur.HandSize = Int32.Parse(textLines[7]);
         cur.ManaSpent = Int32.Parse(textLines[8]);
         cur.ManaWasted = Int32.Parse(textLines[9]);
         cur.StrategyAlignment = Int32.Parse(textLines[10]);
         cur.Dust = Int32.Parse(textLines[11]);
         cur.DeckManaSum = Int32.Parse(textLines[12]);
         cur.DeckManaVariance = Int32.Parse(textLines[13]);
         cur.NumMinionCards = Int32.Parse(textLines[14]);
         cur.NumSpellCards = Int32.Parse(textLines[15]);
         
         // Save which elements are relevant to the search
         cur.Fitness = cur.TotalHealthDifference;

         Console.WriteLine("------------------");
         Console.WriteLine(string.Format("Eval ({0}): {1}",
               _individualsEvaluated,
               string.Join("", cur.ToString())));
         Console.WriteLine(String.Join(" ", countText));
         Console.WriteLine("Win Count: "+cur.WinCount);
         Console.WriteLine("Total Health Difference: "
                           +cur.TotalHealthDifference);
         Console.WriteLine("Damage Done: "+cur.DamageDone);
         Console.WriteLine("Num Turns: "+cur.NumTurns);
         Console.WriteLine("Cards Drawn: "+cur.CardsDrawn);
         Console.WriteLine("Hand Size: "+cur.HandSize);
         Console.WriteLine("Mana Spent: "+cur.ManaSpent);
         Console.WriteLine("Mana Wasted: "+cur.ManaWasted);
         Console.WriteLine("Strategy Alignment: "+cur.StrategyAlignment);
         Console.WriteLine("Dust: "+cur.Dust);
         Console.WriteLine("Deck Mana Sum: "+cur.DeckManaSum);
         Console.WriteLine("Deck Mana Variance: "+cur.DeckManaVariance);
         Console.WriteLine("Num Minion Cards: "+cur.NumMinionCards);
         Console.WriteLine("Num Spell Cards: "+cur.NumSpellCards);
         Console.WriteLine("------------------");

         // Save stats
         bool didHitMaxWins = 
            cur.WinCount > _maxWins;
         bool didHitMaxFitness = 
            cur.Fitness > _maxFitness;
         _maxWins = Math.Max(_maxWins, cur.WinCount);
         _maxFitness = 
            Math.Max(_maxFitness, cur.Fitness);

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
					Console.WriteLine("Found worker " + workerId);
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
				WriteText(ow, "Evolutionary Strategies");
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
                     Individual.GenerateRandomIndividual(_cardSet) :
                     ChooseElite(elites).Mutate();

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
               string outboxPath = string.Format(_outboxTemplate, workerId);

               // Test if this worker is done.
               if (File.Exists(outboxPath))
               {
                  // Wait for the file to finish being written.
                  Console.WriteLine("Worker done: " + workerId);
                  Thread.Sleep(3000);

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

            Thread.Sleep(5000);
         }

         // Let the workers know that we are done.
         File.Delete(_activeSearchPath);
      }
   }
}
