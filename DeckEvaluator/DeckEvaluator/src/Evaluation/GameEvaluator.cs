using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using SabberStoneCore.Config;
using SabberStoneCore.Enums;
using SabberStoneCore.Model;
using SabberStoneCore.Model.Entities;
using SabberStoneCore.Tasks;
using SabberStoneCore.Tasks.PlayerTasks;

using SabberStoneCoreAi.Nodes;
using SabberStoneCoreAi.Score;

namespace DeckEvaluator.Evaluation
{
   class GameEvaluator
   {
      private static Random rnd = new Random();
		public struct PlayerSetup
      {
         public List<Card> _deck;
         public CardClass _hero;
         public Score _strategy;

         public PlayerSetup(List<Card> deck,
                            CardClass hero,
                            Score strategy)
         {
				// Shuffle the given deck.
            for (int i=1; i<deck.Count; i++)
            {
               int j = (int)(rnd.NextDouble() * i);
               Card tmp = deck[i];
               deck[i] = deck[j];
               deck[j] = tmp;
            }
            
            _deck = deck;
            _hero = hero;
            _strategy = strategy;
         }
      }

      public struct GameResult
      {
         public bool _didWin;
         public Dictionary<string, int> _cardUsage;
         public int _healthDifference;
         public int _damageDone;
         public int _numTurns;
         public int _cardsDrawn;
         public int _manaSpent;
         public int _strategyAlignment;

         public GameResult(bool didWin, Dictionary<string, int> cardUsage,
                           int healthDifference, int damageDone, 
                           int numTurns, int cardsDrawn, int manaSpent,
                           int strategyAlignment)
         {
            _didWin = didWin;
            _cardUsage = cardUsage;
            _healthDifference = healthDifference;
            _damageDone = damageDone;
            _numTurns = numTurns;
            _cardsDrawn = cardsDrawn;
            _manaSpent = manaSpent;
            _strategyAlignment = strategyAlignment;
         }
      }

      private CardClass _opponentClass;
      private List<Card> _opponentDeck;
      private CardClass _playerClass;
		private List<Card> _playerDeck;
      private Dictionary<string, int> _cardUsage; 

		public GameEvaluator(CardClass playerClass, List<Card> playerDeck,
            CardClass opponentClass, List<Card> opponentDeck)
		{
         _playerClass = playerClass;
			_playerDeck = playerDeck;
         _opponentClass = opponentClass;
         _opponentDeck = opponentDeck;

         _cardUsage = new Dictionary<string, int>();
      }

      public void updateUsage(Card playedCard)
      {
         string cardName = playedCard.Name.ToString();
         if (_cardUsage.ContainsKey(cardName))
         {
            _cardUsage[cardName] += 1;
         }
         else
         {
            _cardUsage.Add(cardName, 1);
         }
      }

      public GameResult PlayGame()
      {
         PlayerSetup player1 = new PlayerSetup(
                  _playerDeck, 
                  _playerClass, 
                  new AggroScore()
               );
         PlayerSetup player2 = new PlayerSetup(
                  _opponentDeck,
                  _playerClass, 
                  new AggroScore()
               );

         var game = new Game(
            new GameConfig()
               {
                  StartPlayer = 1,
                  Player1Name = "Player1",
                  Player1HeroClass = player1._hero,
                  Player1Deck = player1._deck,
                  Player2Name = "Player2",
                  Player2HeroClass = player2._hero,
                  Player2Deck = player2._deck,
                  FillDecks = false,
                  Shuffle = true,
                  SkipMulligan = false
               });

         int maxDepth = 13;
         int maxWidth = 4;

         game.StartGame();

         var aiPlayer1 = player1._strategy;
         var aiPlayer2 = player2._strategy;

         List<int> mulligan1 = aiPlayer1.MulliganRule().Invoke(game.Player1.Choice.Choices.Select(p => game.IdEntityDic[p]).ToList());
         List<int> mulligan2 = aiPlayer2.MulliganRule().Invoke(game.Player2.Choice.Choices.Select(p => game.IdEntityDic[p]).ToList());

         //Console.WriteLine($"Player1: Mulligan {string.Join(",", mulligan1)}");
         //Console.WriteLine($"Player2: Mulligan {string.Join(",", mulligan2)}");

         game.Process(ChooseTask.Mulligan(game.Player1, mulligan1));
         game.Process(ChooseTask.Mulligan(game.Player2, mulligan2));

         game.MainReady();
         int totalDamage = 0;
         int totalCardsDrawn = 0;
         int totalManaSpent = 0;
         int totalOptionScore = 0;

         while (game.State != State.COMPLETE)
         {
            /*
            Console.WriteLine($"Player1: {game.Player1.PlayState} / Player2: {game.Player2.PlayState} - " +
                              "ROUND {(game.Turn + 1) / 2} - {game.CurrentPlayer.Name}");
            Console.WriteLine($"Hero[P1]: {game.Player1.Hero.Health} / Hero[P2]: {game.Player2.Hero.Health}");
            Console.WriteLine("");
            */

            int numCardsDrawn = 0;
            while (game.State == State.RUNNING && game.CurrentPlayer == game.Player1)
            {
               //Console.WriteLine("* Calculating solutions *** Player 1 ***");
               List<OptionNode> solutions = OptionNode.GetSolutions(game, game.Player1.Id, aiPlayer1, maxDepth, maxWidth);


               var solution = new List<PlayerTask>();
               OptionNode bestOption = solutions.OrderByDescending(p => p.Score).First();
               bestOption.PlayerTasks(ref solution);

               if (bestOption.Score != Int32.MaxValue)
                  totalOptionScore += bestOption.Score;

               //Console.WriteLine($"- Player 1 - <{game.CurrentPlayer.Name}> ---------------------------");
               foreach (PlayerTask task in solution)
               {
                  //Console.WriteLine(task.FullPrint());
                  game.Process(task);

                  // Record some stats
                  if (task.PlayerTaskType == PlayerTaskType.PLAY_CARD)
                     updateUsage(task.Source.Card);
                  if (task.PlayerTaskType == PlayerTaskType.MINION_ATTACK)
                  {
                     int damageTaken = ((ICharacter)task.Source).AttackDamage;
                     totalDamage += damageTaken;
                  }
                  numCardsDrawn = Math.Max(numCardsDrawn,
                        task.Controller.NumCardsDrawnThisTurn);
                  totalManaSpent = Math.Max(totalManaSpent,
                        task.Controller.TotalManaSpentThisGame);

                  if (game.CurrentPlayer.Choice != null)
                  {
                     //Console.WriteLine("* Recalculating due to a final solution ...");
                     break;
                  }
               }
            }

            totalCardsDrawn = numCardsDrawn;

            // Random mode for Player 2
            //Console.WriteLine($"- Player 2 - <{game.CurrentPlayer.Name}> ---------------------------");
            while (game.State == State.RUNNING && game.CurrentPlayer == game.Player2)
            {
               //Console.WriteLine("* Calculating solutions *** Player 2 ***");
               List<OptionNode> solutions = OptionNode.GetSolutions(game, game.Player2.Id, aiPlayer2, maxDepth, maxWidth);
               var solution = new List<PlayerTask>();
               solutions.OrderByDescending(p => p.Score).First().PlayerTasks(ref solution);
               //Console.WriteLine($"- Player 2 - <{game.CurrentPlayer.Name}> ---------------------------");
               foreach (PlayerTask task in solution)
               {
                  //Console.WriteLine(task.FullPrint());
                  game.Process(task);
                  if (game.CurrentPlayer.Choice != null)
                  {
                     //Console.WriteLine("* Recalculating due to a final solution ...");
                     break;
                  }
               }
            }
         }

         Console.WriteLine($"\nGame: {game.State}, Player1: {game.Player1.PlayState} / Player2: {game.Player2.PlayState}\n");

         bool didWin = game.Player1.PlayState == PlayState.WON;
         return new GameResult(didWin, _cardUsage,
                       game.Player1.Hero.Health-game.Player2.Hero.Health,
                       totalDamage, game.Turn, totalCardsDrawn, 
                       totalManaSpent, totalOptionScore);
      }
   }
}
