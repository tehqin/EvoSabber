using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

using Nett;

using SabberStoneCore.Enums;
using SabberStoneCore.Model;

using SabberStoneCoreAi.Score;

using DeckEvaluator.Config;
using DeckEvaluator.Evaluation;

namespace DeckEvaluator
{
   class Program
   {
      static void Main(string[] args)
      {
         string nodeName = args[0];
         int nodeId = Int32.Parse(args[0]);
         Console.WriteLine("Node Id: "+nodeId);
			
         // These files are for asyncronous communication between this
         // worker and it's scheduler. 
         //
         // Decks to evaluate come in the inbox and are dished out of the
         // outbox.
         string boxesDirectory = "boxes/";
         string inboxPath = boxesDirectory + 
            string.Format("deck-{0,4:D4}-inbox.txt", nodeId);
         string outboxPath = boxesDirectory +
            string.Format("deck-{0,4:D4}-outbox.txt", nodeId);
			
         // Hailing
         string activeDirectory = "active/";
         string activeWorkerPath = activeDirectory + 
            string.Format("worker-{0,4:D4}.txt", nodeId);
         string activeSearchPath = activeDirectory + "search.txt";
         if (!File.Exists(activeSearchPath))
         {
            Console.WriteLine("No search has been found.");
            return;
         }

         // The opponent deck doesn't change so we can load it here.
         string[] textLines = File.ReadAllLines(activeSearchPath);
         var config = Toml.ReadFile<Configuration>(textLines[1]);
         string opponentDeckPath = config.Evaluation.Opponent.DeckFile;
         List<Card> opponentDeck = GetDeckFromFile(opponentDeckPath);
         CardClass opponentClass = GetClassFromFile(opponentDeckPath);
         Score opponentStrategy = GetStrategyFromName(config.Evaluation.Opponent.Strategy);
         Score playerStrategy = GetStrategyFromName(config.Evaluation.Player.Strategy);
         int numGames = config.Evaluation.NumGames;
         Console.WriteLine("Config File: " + textLines[1]);
         Console.WriteLine("Deck File: " + opponentDeckPath);
         Console.WriteLine("Opponent Hero Class: " + opponentClass);
         Console.WriteLine("Opponent Strategy: " + opponentStrategy);
         Console.WriteLine("Player Strategy: " + playerStrategy);
         Console.WriteLine("Num games: "+numGames);
        
         // Setup this worker to use all 8 cores on the node.
         // If this fails, don't go any further.
         if (!ConfigureThreadPool(8))
            return;

         // Let the scheduler know we are here.
			using (FileStream ow = File.Open(activeWorkerPath, 
                FileMode.Create, FileAccess.Write, FileShare.None))
			{
				WriteText(ow, "Hail!");
				ow.Close();
			}

         // Loop while the guiding search is running.
         while (File.Exists(activeSearchPath))
         {
            // Wait until we have some work.
            while (!File.Exists(inboxPath) && File.Exists(activeSearchPath))
            {
               Console.WriteLine("Waiting... ("+nodeId+")");
               Thread.Sleep(5000);
            }

            if (!File.Exists(activeSearchPath))
               break;
 
            // Wait for the file to be finish being written
            Thread.Sleep(1000);

            // Run games, evaluate the deck, and then save the results.
            List<Card> playerDeck = GetDeckFromFile(inboxPath);
            CardClass playerClass = GetClassFromFile(inboxPath);
				File.Delete(inboxPath);
            var launcher = new GameDispatcher(numGames, playerClass,
                  playerDeck, playerStrategy, opponentClass, 
                  opponentDeck, opponentStrategy);
            launcher.Run(outboxPath);
         
            // Cleanup.
            GC.Collect();

            // Look at all the files in the current directory.
            // Eliminate anythings that matches our log file.
            string[] oFiles = Directory.GetFiles(".", "DeckEvaluator.o*");
            foreach (string curFile in oFiles)
            {
               if (curFile.EndsWith(nodeName))
               {
                  File.Delete(curFile); 
               }
            }
         }
      }

		private static void WriteText(Stream fs, string s)
      {
         s += "\n";
         byte[] info = new UTF8Encoding(true).GetBytes(s);
         fs.Write(info, 0, info.Length);
      }

      private static bool ConfigureThreadPool(int maxThreads)
      {
         int maxWorker, maxIOC;
         ThreadPool.GetMaxThreads(out maxWorker, out maxIOC);
         if (ThreadPool.SetMaxThreads(maxThreads, maxIOC))
         {
				return true;
         }
         
         Console.WriteLine("ERROR: Failed to change MaxThreads");
			return false;
      }

      private static List<Card> GetDeckFromFile(string filepath)
      {
         var currDeck = new List<Card>();
         string[] textLines = File.ReadAllLines(filepath);
         char[] delimeters = {'*'};
         string[] cards = textLines[1].Split(delimeters);
         for (int i = 0; i < 30; i++)
         {
            currDeck.Add(Cards.FromName(cards[i]));
         }
         return currDeck;
      }

		private static CardClass GetClassFromFile(string path)
		{
			string[] textLines = File.ReadAllLines(path);
			string className = textLines[0].Trim().ToUpper();
         foreach (CardClass curClass in Cards.HeroClasses)
            if (curClass.ToString().Equals(className))
               return curClass;

         Console.WriteLine("Card class "+className+" not a valid hero class.");
			return CardClass.NEUTRAL;
		}

      private static Score GetStrategyFromName(string name)
      {
         if (name == "Aggro")
         {
            return new AggroScore();
         }
         else if (name == "Control")
         {
            return new ControlScore();
         }
         else if (name == "Fatigue")
         {
            return new FatigueScore();
         }
         else if (name == "MidRange")
         {
            return new MidRangeScore();
         }
         else if (name == "Ramp")
         {
            return new RampScore();
         }
         else
         {
            Console.WriteLine("Strategy "+name+" not a valid strategy.");
            return null;
         }
      }
   }
}
