using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

using SabberStoneCore.Enums;
using SabberStoneCore.Model;

using FracturingSabber.Logging;
using FracturingSabber.Population;

namespace FracturingSabber.Search
{
   class FracturingSearch
   {
      readonly private CardClass _heroClass;
      readonly private List<Card> _cardSet;
      private Queue<Shard> _pendingEval;
      private Queue<int> _runningWorkers;
      private Queue<int> _idleWorkers;
     
      private int _individualsEvaluated;
      private int _individualsDispatched;
      
      // FracturingSearch Parameters
      private const int INITIAL_POPULATION = 1;
      private const int NUM_TO_EVALUATE = 10000;
      private const int MAX_POPULATION = 1000000;

      // Logging
      private static readonly string _logDirectory = "logs/";
      private static readonly string INDIVIDUAL_LOG_FILENAME = 
         _logDirectory + "individual_log.csv";
      private RunningIndividualLog _individualLog;

      public FracturingSearch(CardClass heroClass, List<Card> cardSet)
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

      private void SendWork(string workerInboxPath, Shard curShard)
      {
         var deck = curShard.Representative.GetCards();
         
         using (FileStream ow = File.Open(workerInboxPath,
                   FileMode.Create, FileAccess.Write, FileShare.None))
         {
            WriteText(ow, _heroClass.ToString().ToLower());
            WriteText(ow, string.Join("*", deck));
            ow.Close();
         }
      }

      private void ReceiveResults(string workerOutboxPath, Individual cur)
      {
         // Read the file and calculate a true fitness
         string[] textLines = File.ReadAllLines(workerOutboxPath);
         
         // Delete the file!
         File.Delete(workerOutboxPath);
			
         // Pull out the data from the text of the file.
         char[] delimeters = {'*'};
         string[] cardNames = textLines[0].Split(delimeters);
         string[] countText = textLines[1].Split(delimeters);
         cur.ID = _individualsEvaluated;
			cur.WinCount = Int32.Parse(textLines[2]);
		   cur.TotalHealthDifference = Int32.Parse(textLines[3]);
			cur.DamageDone = Int32.Parse(textLines[4]);
			cur.NumTurns = Int32.Parse(textLines[5]);
			cur.CardsDrawn = Int32.Parse(textLines[6]);
			cur.ManaSpent = Int32.Parse(textLines[7]);
			cur.StrategyAlignment = Int32.Parse(textLines[8]);

			Console.WriteLine("------------------");
         Console.WriteLine(string.Format("Eval ({0}): {1}",
                           _individualsEvaluated, cur));
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

         // Build a build a frequency table based on card usage
         // for each name.
         var cardUsage = new int[_cardSet.Count];
         for (int cardId=0; cardId<_cardSet.Count; cardId++)
         {
            bool seenCard = false;
            for (int i=0; i<cardNames.Length; i++)
            {
               if (_cardSet[cardId].Name.Equals(cardNames[i]))
               {
                  if (seenCard)
                     cardUsage[cardId] /= 2;
                  else
                     cardUsage[cardId] = Int32.Parse(countText[i]);
                  seenCard = true;
               }
            }
         }

         // Put the cards in the order of usage.
         cur.GenerateCardRanking(cardUsage);

         // Log the individual
         _individualLog.LogIndividual(cur);
      }

      private void InitLogs()
      {
         _individualLog =
            new RunningIndividualLog(INDIVIDUAL_LOG_FILENAME);
      }

      public void Run()
      {
         _individualsEvaluated = 0;
         _individualsDispatched = 0;
         _pendingEval = new Queue<Shard>();
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
            WriteText(ow, "Fracturing Search");
            ow.Close();
         }
         
         int numWorkers = 0;
         var shardStable = new Dictionary<int,Shard>();
         var population = new MapPopulation(MAX_POPULATION);
         //var population = new FittestPopulation(MAX_POPULATION);

         // Setup the logs to record the data on individuals
         InitLogs();

         // Generate the initial population.
         for (int curInd=0; curInd<INITIAL_POPULATION; curInd++)
         {
            Shard curShard = Shard.GenerateInitialShard(_cardSet);
            _pendingEval.Enqueue(curShard);
         }

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
                  shardStable.Add(workerId, null);
                  File.Delete(activeFile);
                  Console.WriteLine("Found worker " + workerId);
                  numWorkers++;
               }
            }

            // Dispatch jobs to the available workers
            while (_idleWorkers.Count > 0 && _pendingEval.Count > 0)
            {
               int workerId = _idleWorkers.Dequeue();
               Shard curShard = _pendingEval.Dequeue();
               shardStable[workerId] = curShard;
               _runningWorkers.Enqueue(workerId);
               Console.WriteLine("Starting worker: "+workerId);

               string inboxPath = string.Format(inboxTemplate, workerId);
               SendWork(inboxPath, curShard);
               _individualsDispatched++;
            }
            
            // If there is a need for more pending decks to evaluate,
            // create them by fracuturing some shards.
            while (_pendingEval.Count < numWorkers 
                   && !population.IsEmpty())
            {
               Shard curShard = population.PollFittest();
               Console.WriteLine(string.Format("Fracturing on (fitness:{0}, raw:{1})", 
                        curShard.Representative.Fitness,
                        curShard.Representative.RawFitness));
               List<Shard> subspaces = 
                  curShard.FractureOnRepresentative();
               foreach (Shard nxtShard in subspaces)
                  _pendingEval.Enqueue(nxtShard);
            }

            // Look for workers that are done.
            // Add them to the working queue.
            //
            // Take statistical information from the worker queue.
            // Calculate the fitness from the statistical info.
            // Add the evaluated shard to the fractQueue.
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

                  ReceiveResults(outboxPath, 
                     shardStable[workerId].Representative);
                  population.Add(shardStable[workerId]);
                  _idleWorkers.Enqueue(workerId);
                  _individualsEvaluated++; 
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
      }
   }
}
