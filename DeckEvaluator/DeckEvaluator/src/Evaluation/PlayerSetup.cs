using System;

using SabberStoneCoreAi.Score;

using DeckEvaluator.Config;
using DeckEvaluator.Messaging;

namespace DeckEvaluator.Evaluation
{
   class PlayerSetup
   {
      public Deck Deck { get; private set; }
      public Score Strategy { get; private set; }

      public PlayerSetup(Deck deck,
                         Score strategy)
      {
         Deck = deck;
         Strategy = strategy;
      }

      public static Score GetStrategy(string name, CustomStratWeights weights)
      {
         if (name.Equals("Aggro"))
            return new AggroScore();
         if (name.Equals("Control"))
            return new ControlScore();
         if (name.Equals("Fatigue"))
            return new FatigueScore();
         if (name.Equals("MidRange"))
            return new MidRangeScore();
         if (name.Equals("Ramp"))
            return new RampScore();
         if (name.Equals("Custom"))
            return new CustomScore(weights);

         Console.WriteLine("Strategy "+name+" not a valid strategy.");
         return null;
      }
   }
}
