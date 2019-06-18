using StratSearchSabber.Search;

namespace StratSearchSabber
{
   class Program
   {
      static void Main(string[] args)
      {
         var search = new EvolutionaryStrategies(args[0]);
         search.Run();
      }
   }
}
