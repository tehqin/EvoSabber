using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using Nett;

using SabberStoneCore.Enums;
using SabberStoneCore.Model;

using MapSabber.Config;
using MapSabber.Mapping.Sizers;
using MapSabber.Search;


namespace MapSabber
{
   class Program
   {
      static void Main(string[] args)
      {
			string configFilename = args[0];
         var search = new MapElites(configFilename); 
         search.Run();
         
         // Temporary code for extracting data from the card set
         /*
         Console.WriteLine("TEST");
         var setNames = new string[]{"CORE", "EXPERT1"};
         CardSet[] cardSets = CardReader.GetSetsFromNames(setNames);
         
         var classNames = new string[]{"hunter", "paladin", "warlock"};
         foreach (var className in classNames)
         {
            CardClass heroClass = CardReader.GetClassFromName(className);
            List<Card> cards = CardReader.GetCards(heroClass, cardSets);
         
            Console.WriteLine("Class: "+className);
            var costStrings = new List<string>();
            foreach (var curCard in cards)
               costStrings.Add(curCard.Cost.ToString());
            Console.WriteLine("Cost: "+string.Join(",", costStrings));
 
            var maxStrings = new List<string>();
            foreach (var curCard in cards)
               maxStrings.Add(curCard.MaxAllowedInDeck.ToString());
            Console.WriteLine("MaxInDeck: "+string.Join(",", maxStrings));
         }
         */

         /*  Temporary code for filtering cards ****************
         XElement xelement = XElement.Load("resources/CardDefs.xml");
         var filteredCards = new List<XElement>();
         IEnumerable<XElement> cards = xelement.Elements();
         foreach (var curCard in cards)
         {
            IEnumerable<XElement> tags = curCard.Elements();

            bool isOk = false;
            foreach (var curTag in tags)
            {
               var name = curTag.Attribute("name");
               if (name != null && name.Value.Equals("CARD_SET"))
               {
                  var val = curTag.Attribute("value").Value;
                  int v = Int32.Parse(val);
                  if (v <= 3)
                  {
                     Console.WriteLine(v);
                     isOk = true; 
                  }
               }
            }

            if (isOk)
            {
               filteredCards.Add(curCard);
            }
         }

         xelement.RemoveNodes();
         foreach (var curCard in filteredCards)
            xelement.Add(curCard);
      
         xelement.Save("resources/CardDefsFiltered.xml");
         */
      }
   }
}
