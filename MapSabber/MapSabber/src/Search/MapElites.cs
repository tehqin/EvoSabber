using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

using SabberStoneCore.Enums;
using SabberStoneCore.Model;

using MapSabber.Logging;
using MapSabber.Mapping;

namespace MapSabber.Search
{
   class MapElites
   {
      readonly private CardClass _heroClass;
      readonly private List<Card> _cardSet;
      private Queue<int> _runningWorkers;
      private Queue<int> _idleWorkers;

      private int _individualsEvaluated;
      private int _individualsDispatched;
      
      // MapElites Parameters
      private const int INITIAL_POPULATION = 100;
      private const int NUM_TO_EVALUATE = 10000;
      private const int REMAP_FREQUENCY = 100;
      private const int NUM_GROUPS_PER_FEATURE = 20;
      private const int NUM_FEATURES = 2;

      // Logging 
      private const string LOG_DIRECTORY = "logs/";
      private const string INDIVIDUAL_LOG_FILENAME = 
         LOG_DIRECTORY + "individual_log.csv";
      private const string CHAMPION_LOG_FILENAME = 
         LOG_DIRECTORY + "champion_log.csv";
      private const string FITTEST_LOG_FILENAME = 
         LOG_DIRECTORY + "fittest_log.csv";
      private const string ELITES_FILENAME = 
         LOG_DIRECTORY + "elites_log.csv";
      private const string ELITE_MAP_FILENAME = 
         LOG_DIRECTORY + "elite_map_log.csv";
      private FrequentMapLog _map_log;
      private RunningIndividualLog _individualLog;
      private RunningIndividualLog _championLog;
      private RunningIndividualLog _fittestLog;

      public MapElites(CardClass heroClass, List<Card> cardSet)
      {
         _heroClass = heroClass;
         _cardSet = cardSet;
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
         cur.ManaSpent = Int32.Parse(textLines[7]);
         cur.StrategyAlignment = Int32.Parse(textLines[8]);
         
         // Save which elements are relevant to the search
         cur.Features = new []{cur.DamageDone, cur.NumTurns};
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
         Console.WriteLine("Mana Spent: "+cur.ManaSpent);
         Console.WriteLine("Strategy Alignment: "+cur.StrategyAlignment);
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

      private void InitLogs()
      {
         _individualLog =
            new RunningIndividualLog(INDIVIDUAL_LOG_FILENAME);
         _championLog =
            new RunningIndividualLog(CHAMPION_LOG_FILENAME);
         _fittestLog =
            new RunningIndividualLog(FITTEST_LOG_FILENAME);
      }

      private void LogIndividual(string filepath, string[] data)
      {
         using (StreamWriter sw = File.AppendText(filepath))
         {
            sw.WriteLine(string.Join(",", data));
            sw.Close();
         }
      }

      public void Run()
      {
         _individualsEvaluated = 0;
         _maxWins = 0;
         _maxFitness = Int32.MinValue;
         _runningWorkers = new Queue<int>();
         _idleWorkers = new Queue<int>();
         
         string boxesDirectory = "boxes/";
         string inboxTemplate = boxesDirectory
            + "deck-{0,4:D4}-inbox.txt";
         string outboxTemplate = boxesDirectory
            + "deck-{0,4:D4}-outbox.txt";

         // Let the workers know we are here.
         string activeDirectory = "active/";
         string activeWorkerTemplate = activeDirectory
            + "worker-{0,4:D4}.txt";
         string activeSearchPath = activeDirectory
            + "search.txt";
			using (FileStream ow = File.Open(activeSearchPath,
						FileMode.Create, FileAccess.Write, FileShare.None))
			{
				WriteText(ow, "MAP Elites");
				ow.Close();
			}

         /*
         var featureMap = new FixedFeatureMap(NUM_FEATURES,
               NUM_GROUPS_PER_FEATURE, new int[]{4382,2280},
               new int[]{6352,2889});
         */
         var featureMap = new SlidingFeatureMap(NUM_FEATURES,
               REMAP_FREQUENCY, NUM_GROUPS_PER_FEATURE);
         var individualStable = new Dictionary<int,Individual>();
         
         // Setup the logs to record the data on individuals
         InitLogs();
         _map_log = new FrequentMapLog(ELITE_MAP_FILENAME, featureMap);
         
         Console.WriteLine("Begin search...");
         while (_individualsEvaluated < NUM_TO_EVALUATE)
         {
            // Look for new workers.
            string[] hailingFiles = Directory.GetFiles(activeDirectory);
            foreach (string activeFile in hailingFiles)
            {
               string prefix = activeDirectory + "worker-";
               if (activeFile.StartsWith(prefix))
               {
                  string suffix = ".txt";
                  int start = prefix.Length;
                  int end = activeFile.Length - suffix.Length;
                  string label = activeFile.Substring(start, end-start);
                  int workerId = Int32.Parse(label);
                  _idleWorkers.Enqueue(workerId);
                  individualStable.Add(workerId, null);
                  File.Delete(activeFile);
                  Console.WriteLine("Found worker " + workerId);
               }
            }
            
            // Dispatch jobs to the available workers.
            while (_idleWorkers.Count > 0)
            {
               if (_individualsDispatched >= INITIAL_POPULATION &&
                   _individualsEvaluated == 0)
               {
                  break;
               }

               int workerId = _idleWorkers.Dequeue();
               _runningWorkers.Enqueue(workerId);
					Console.WriteLine("Starting worker: "+workerId);
               
               Individual choiceIndividual =
                  _individualsDispatched < INITIAL_POPULATION ? 
                     Individual.GenerateRandomIndividual(_cardSet) :
                     featureMap.GetRandomElite().Mutate();

               string inboxPath = string.Format(inboxTemplate, workerId);
               SendWork(inboxPath, choiceIndividual);
               individualStable[workerId] = choiceIndividual;
               _individualsDispatched++;
            }

				// Look for individuals that are done.
				int numActiveWorkers = _runningWorkers.Count;
				for (int i=0; i<numActiveWorkers; i++)
				{
					int workerId = _runningWorkers.Dequeue();
					string outboxPath = string.Format(outboxTemplate, workerId);

					// Test if this worker is done.
					if (File.Exists(outboxPath))
					{
						// Wait for the file to finish being written.
						Console.WriteLine("Worker done: " + workerId);
						Thread.Sleep(1000);

						ReceiveResults(outboxPath, individualStable[workerId]);
						featureMap.Add(individualStable[workerId]);
						_idleWorkers.Enqueue(workerId);
						_individualsEvaluated++;
                  _map_log.UpdateLog();
					}
					else
					{
						_runningWorkers.Enqueue(workerId);
					}
				}


            Thread.Sleep(5000);
         }
      
         // Let the workers know that we are done.
         File.Delete(activeSearchPath);

         // Create a log that has all of the elites.
         featureMap.LogMap(ELITES_FILENAME); 
      }
   }
}
