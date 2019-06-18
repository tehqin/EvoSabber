using System;
using System.Collections.Generic;

using Nett;

using StratSearchSabber.Config;

namespace StratSearchSabber.Decks
{
   class DeckPoolManager
   {
      private Dictionary<string, Dictionary<string, Deck>> _deckPools;

		public DeckPoolManager()
      {
         _deckPools = new Dictionary<string, Dictionary<string, Deck>>();
      }

      public void AddDeckPool(DeckPoolConfig config)
      {
         // For each entry in this deck pool, contruct a mapping from
         // the name of the deck to the class and card listing.
         var deckMap = new Dictionary<string, Deck>();
         foreach (DeckParams curDeckParams in config.Decks)
         {
            deckMap.Add(curDeckParams.DeckName,
                        curDeckParams.ContructDeck());
         }

         // Add this deck pool to the map of pools.
         _deckPools.Add(config.PoolName, deckMap);
      }

      public void AddDeckPools(string[] deckPoolFilenames)
      {
         foreach (string poolFilename in deckPoolFilenames)
         {
            var config = Toml.ReadFile<DeckPoolConfig>(poolFilename);
            AddDeckPool(config);
         }
      }

      public Deck GetDeck(string poolName, string deckName)
      {
         return _deckPools[poolName][deckName];
      }
   }
}
