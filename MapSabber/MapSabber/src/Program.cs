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
      }
   }
}
