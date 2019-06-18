using System;

using StratSearchSabber.Messaging;

namespace StratSearchSabber.Search
{
   class Individual
   {
      private static Random rnd = new Random();
      public static Individual GenerateRandomIndividual()
      {
         var ind = new Individual();
         for (int i=0; i<NUM_PARAMS; i++)
            ind._paramVector[i] = rnd.NextDouble() * 2 - 1;
         return ind;
      }

      private readonly static int NUM_PARAMS = 15;
      private double[] _paramVector;
   
      public int ID { get; set; }
      public int ParentID { get; set; }
      
      public OverallStatistics OverallData { get; set; }
      public StrategyStatistics[] StrategyData { get; set; }
      
      public int Fitness { get; set; }

      public Individual()
      {
         ParentID = -1;
         _paramVector = new double[NUM_PARAMS];
      }

      public int GetStatByName(string name)
      {
         return OverallData.GetStatByName(name);
      }

      // Generate a random individual via mutation
      public Individual Mutate(double scalar)
      {
         var child = new Individual();
         for (int i=0; i<NUM_PARAMS; i++)
            child._paramVector[i] = _clip(_gaussian(scalar) + _paramVector[i]);
         
         child.ParentID = ID;
         return child;
      }

      // Unpack the vector searched by the evolution strategy
      public CustomStratWeights GetWeights()
      {
         return CustomStratWeights.CreateFromVector(_paramVector);
      }

      private double _gaussian(double stdDev)
      {
         double u1 = 1.0-rnd.NextDouble();
         double u2 = 1.0-rnd.NextDouble();
         double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) *
                         Math.Sin(2.0 * Math.PI * u2);
         return stdDev * randStdNormal;
      }

      private double _clip(double v)
      {
         if (v < -1.0)
            return -1.0;
         if (v > 1.0)
            return 1.0;
         return v;
      }
   }
}
