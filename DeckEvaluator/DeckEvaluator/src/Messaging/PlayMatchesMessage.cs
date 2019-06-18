using DeckEvaluator.Config;

namespace DeckEvaluator.Messaging
{
   class PlayMatchesMessage
   {
      public DeckParams Deck { get; set; }
      public CustomStratWeights Strategy { get; set; }
   }
}
