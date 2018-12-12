using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using SabberStoneCore.Enums;
using SabberStoneCore.Model;

using SabberStoneCoreAi.Score;

using DeckEvaluator.Evaluation;

namespace DeckEvaluator.Evaluation
{
   class GameDispatcher
   {
      private int _numGames;
      private int _numActive;
      private CardClass _opponentClass;
      private List<Card> _opponentDeck;
      private Score _opponentStrategy;
      private CardClass _playerClass;
		private List<Card> _playerDeck;
      private Score _playerStrategy;

      // Total stats for all the games played.
		private readonly object _statsLock = new object();
      private int _winCount;
      private Dictionary<string,int> _usageCount;
      private int _totalHealthDifference;
      private int _totalDamage;
      private int _totalTurns;
      private int _totalCardsDrawn;
      private int _totalHandSize;
      private int _totalManaSpent;
      private int _totalManaWasted;
      private int _totalStrategyAlignment;

		public GameDispatcher(int numGames, 
                            CardClass playerClass,
                            List<Card> playerDeck, 
                            Score playerStrategy,
                            CardClass opponentClass,
                            List<Card> opponentDeck,
                            Score opponentStrategy)
		{
         // Save the configuration information.
         _numGames = numGames;
         _playerClass = playerClass;
         _playerDeck = playerDeck;
         _playerStrategy = playerStrategy;
         _opponentClass = opponentClass;
         _opponentDeck = opponentDeck;
         _opponentStrategy = opponentStrategy;
         _numActive = numGames;
      
         // Setup the statistics keeping.
         _winCount = 0;
         _usageCount = new Dictionary<string,int>();
         _totalDamage = 0;
         _totalHealthDifference = 0;
         _totalTurns = 0;
         _totalCardsDrawn = 0;
         _totalHandSize = 0;
         _totalManaSpent = 0;
         _totalManaWasted = 0;
         _totalStrategyAlignment = 0;
         foreach (Card curCard in playerDeck)
         {
				if (!_usageCount.ContainsKey(curCard.Name))
				{
					_usageCount.Add(curCard.Name, 0);
				}
         }
      }

      private void runGame(int gameId, GameEvaluator ev)
      {
         Console.WriteLine("Starting game: "+gameId);

         // Run a game
         GameEvaluator.GameResult result = ev.PlayGame();
         
         // Record stats
         lock (_statsLock)
         {
            if (result._didWin)
            {
               _winCount++;
            }
            
	         foreach (string cardName in result._cardUsage.Keys)
            {
               if (_usageCount.ContainsKey(cardName))
               {
                  _usageCount[cardName] += result._cardUsage[cardName];
               }
            }
  
            _totalHealthDifference += result._healthDifference;
            _totalDamage += result._damageDone;
            _totalTurns += result._numTurns;
            _totalCardsDrawn += result._cardsDrawn;
            _totalHandSize += result._handSize;
            _totalManaSpent += result._manaSpent;
            _totalManaWasted += result._manaWasted;
            _totalStrategyAlignment += result._strategyAlignment;
            _numActive--;
         }

         Console.WriteLine("Finished game: "+gameId);
      }

      private void queueGame(int gameId)
      {
         var playerDeck = new List<Card>(_playerDeck);
         var opponentDeck = new List<Card>(_opponentDeck);

      	var ev = new GameEvaluator(_playerClass, playerDeck, 
               _playerStrategy, _opponentClass, opponentDeck,
               _opponentStrategy);
         runGame(gameId, ev);
      }

      private void WriteText(Stream fs, string s)
      {
         s += "\n";
         byte[] info = new UTF8Encoding(true).GetBytes(s);
         fs.Write(info, 0, info.Length);
      }

      public void Run(string resultsFilename)
      {
			// Queue up the games
         _numActive = _numGames;
         //Parallel.For(0, _numGames, i => {queueGame(i);});
         for (int i=0; i<_numGames; i++)
         {
            queueGame(i);
            
            // Rest every game.
            Thread.Sleep(1000);
         }

         // Calculate turn averages from the totals
         long avgDamage = _totalDamage * 1000000L / _totalTurns;
         long avgCardsDrawn = _totalCardsDrawn * 1000000L / _totalTurns;
         long avgHandSize = _totalHandSize * 1000000L / _totalTurns;
         long avgManaSpent = _totalManaSpent * 1000000L / _totalTurns;
         long avgManaWasted = _totalManaWasted * 1000000L / _totalTurns;
         long avgStrategyAlignment = _totalStrategyAlignment * 100L / _totalTurns;
         long turnsPerGame = _totalTurns * 1000000L / _numGames;

         // Calculate the dust cost of the deck
         int dust = 0;
         foreach (Card c in _playerDeck)
         {
            if (c.Rarity == Rarity.COMMON)
               dust += 40;
            else if (c.Rarity == Rarity.RARE)
               dust += 100;
            else if (c.Rarity == Rarity.EPIC)
               dust += 400;
            else if (c.Rarity == Rarity.LEGENDARY)
               dust += 1600;
         }

         // Calculate the sum of mana costs
         int deckManaSum = 0;
         foreach (Card c in _playerDeck)
            deckManaSum += c.Cost;
         
         // Calculate the variance of mana costs
         double avgDeckMana = deckManaSum * 1.0 / _playerDeck.Count;
         double runningVariance = 0;
         foreach (Card c in _playerDeck)
         {
            double diff = c.Cost - avgDeckMana;
            runningVariance += diff * diff;
         }
         int deckManaVariance = (int)(runningVariance * 1000000 / _playerDeck.Count);

         // Calculate the number of minion and spell cards
         int numMinionCards = 0;
         int numSpellCards = 0;
         foreach (Card c in _playerDeck)
         {
            if (c.Type == CardType.MINION) 
               numMinionCards++;
            else if (c.Type == CardType.SPELL)
               numSpellCards++;
         }

         // Output the results to the output file.
			using (FileStream ow = File.Open(resultsFilename, 
                   FileMode.Create, FileAccess.Write, FileShare.None))
         {
            List<string> outputDeck =
               _playerDeck.ConvertAll<string>(a => a.Name);
            List<int> usageCounts =
               outputDeck.ConvertAll<int>(a => _usageCount[a]);
            WriteText(ow, string.Join("*", outputDeck));
            WriteText(ow, string.Join("*", usageCounts));
            WriteText(ow, _winCount.ToString());
            WriteText(ow, _totalHealthDifference.ToString());
            WriteText(ow, avgDamage.ToString());
            WriteText(ow, turnsPerGame.ToString());
            WriteText(ow, avgCardsDrawn.ToString());
            WriteText(ow, avgHandSize.ToString());
            WriteText(ow, avgManaSpent.ToString());
            WriteText(ow, avgManaWasted.ToString());
            WriteText(ow, avgStrategyAlignment.ToString());
            WriteText(ow, dust.ToString());
            WriteText(ow, deckManaSum.ToString());
            WriteText(ow, deckManaVariance.ToString());
            WriteText(ow, numMinionCards.ToString());
            WriteText(ow, numSpellCards.ToString());
            ow.Close();
         }
      }
   }
}
