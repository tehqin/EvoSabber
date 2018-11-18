using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using SabberStoneCore.Enums;
using SabberStoneCore.Model;

namespace EvoStratSabber.Config
{
   class CardReader
   {
      public static CardClass GetClassFromName(string className)
      {
         className = className.ToUpper();
         foreach (CardClass curClass in Cards.HeroClasses)
            if (curClass.ToString().Equals(className))
               return curClass;

         Console.WriteLine("Card class "+className
                           +" not a valid hero class.");
         return CardClass.NEUTRAL;
      }

      public static List<Card> GetCards(CardClass heroClass)
      {
         var hs = new HashSet<Card>();
         var cards = new List<Card>();
         List<Card> allCards = GetAllCards();
         foreach (Card c in allCards)
         {
            if (c != Cards.FromName("Default")
                && c.Implemented
                && c.Set == CardSet.CORE
                && c.Collectible
                && c.Type != CardType.HERO
                && c.Type != CardType.ENCHANTMENT
                && c.Type != CardType.INVALID
                && c.Type != CardType.HERO_POWER
                && c.Type != CardType.TOKEN
                && (c.Class == CardClass.NEUTRAL || c.Class == heroClass)
                && !hs.Contains(c))
            {
               cards.Add(c);
               hs.Add(c);
            }
         }

         return cards;
      }

      public static List<Card> GetAllCards()
      {
         string fileName = "resources/CardDefs.xml";
         XDocument doc = XDocument.Load(fileName);
         var authors = doc.Descendants("Entity")
                        .Descendants("Tag")
                        .Where(x => x.Attribute("name").Value == "CARDNAME")
                        .Elements("enUS");

         var allCards = new List<Card>();
         foreach(var c in authors)
         {
            if (Cards.FromName(c.Value) != null)
            {
               allCards.Add(Cards.FromName(c.Value));
            }
         }
         return allCards;
      }
   }
}
