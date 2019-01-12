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
