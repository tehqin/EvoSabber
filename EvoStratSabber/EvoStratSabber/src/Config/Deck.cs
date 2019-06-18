using System.Collections.Generic;

using SabberStoneCore.Enums;
using SabberStoneCore.Model;

namespace EvoStratSabber.Config
{
   class DeckParams
   {
      public string DeckName { get; set; }
      public string ClassName { get; set; }
      public string[] CardList { get; set; }

      public Deck ContructDeck()
      {
         return new Deck(ClassName, CardList);
      }
   }

   class Deck
   {
      public CardClass DeckClass { get; private set; }
      public List<Card> CardList { get; private set; }
 
      public Deck(CardClass deckClass, List<Card> cardList)
      {
         DeckClass = deckClass;
         CardList = cardList;
      }

      public Deck(string className, string[] cardNames)
      {
         // Find the class for this deck
         className = className.ToUpper();
         foreach (CardClass curClass in Cards.HeroClasses)
            if (curClass.ToString().Equals(className))
               DeckClass = curClass;

         // Construct the cards from the list of card names
         CardList = new List<Card>();
         foreach (string cardName in cardNames)
            CardList.Add(Cards.FromName(cardName));
      }

      public string[] GetCardNames()
      {
         var names = new string[CardList.Count]; 
         for (int i=0; i<CardList.Count; i++)
            names[i] = CardList[i].Name;
         return names;
      }
   }
}
