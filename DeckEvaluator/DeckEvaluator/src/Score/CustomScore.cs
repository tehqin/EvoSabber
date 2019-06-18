using System;
using System.Collections.Generic;
using System.Linq;
using SabberStoneCore.Model.Zones;
using SabberStoneCore.Model.Entities;

using DeckEvaluator.Messaging;

namespace SabberStoneCoreAi.Score
{
	public class CustomScore : SabberStoneCoreAi.Score.Score
	{
      public CustomStratWeights Weights { get; set; }

      public CustomScore(CustomStratWeights weights)
      {
         Weights = weights;
      }

		public override int Rate()
		{
         // Hard guard the win conditions
			if (OpHeroHp < 1)
				return Int32.MaxValue;
			if (HeroHp < 1)
				return Int32.MinValue;

         double result = 0;
			
         result += Weights.OpHeroHp * OpHeroHp;
         
         result *= 1000;
         return (int)result;
		}

		public override Func<List<IPlayable>, List<int>> MulliganRule()
		{
			return p => p.Where(t => t.Cost > 3).Select(t => t.Id).ToList();
		}
	}
}
