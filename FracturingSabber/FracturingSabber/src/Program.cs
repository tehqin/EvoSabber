using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

using SabberStoneCore.Enums;
using SabberStoneCore.Model;

using FracturingSabber.Search;

namespace FracturingSabber
{
   class Program
   {
      static void Main(string[] args)
      {
         var search = new FracturingSearch(args[0]);
         search.Run();
      }
   }
}
