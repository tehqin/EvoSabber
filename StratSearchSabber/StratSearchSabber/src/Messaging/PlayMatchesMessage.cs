using StratSearchSabber.Config;

namespace StratSearchSabber.Messaging
{
   class PlayMatchesMessage
   {
      public DeckParams Deck { get; set; }
      public CustomStratWeights Strategy { get; set; }
   }
}
