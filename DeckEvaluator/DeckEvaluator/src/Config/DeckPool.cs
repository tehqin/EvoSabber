using SabberStoneCore.Model;

namespace DeckEvaluator.Config
{
   class DeckPoolConfig
   {
      public string PoolName { get; set; }
      public DeckParams[] Decks { get; set; }
   }
}
