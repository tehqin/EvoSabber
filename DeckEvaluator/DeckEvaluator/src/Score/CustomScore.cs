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
			
         result += Weights.HeroHp * HeroHp;
         result += Weights.OpHeroHp * OpHeroHp;
         result += Weights.HeroAtk * HeroAtk;
         result += Weights.OpHeroAtk * OpHeroAtk;
         result += Weights.HandTotCost * HandTotCost;
         result += Weights.HandCnt * HandCnt;
         result += Weights.OpHandCnt * OpHandCnt;
         result += Weights.DeckCnt * DeckCnt;
         result += Weights.OpDeckCnt * OpDeckCnt;

         result += Weights.MinionTotAtk * MinionTotAtk;
         result += Weights.OpMinionTotAtk * OpMinionTotAtk;
         result += Weights.MinionTotHealth * MinionTotHealth;
         result += Weights.OpMinionTotHealth * OpMinionTotHealth;
         result += Weights.MinionTotHealthTaunt * MinionTotHealthTaunt;
         result += Weights.OpMinionTotHealthTaunt * OpMinionTotHealthTaunt;
         
         result *= 1000;
         return (int)result;
		}

		public override Func<List<IPlayable>, List<int>> MulliganRule()
		{
			return p => p.Where(t => t.Cost > 3).Select(t => t.Id).ToList();
		}
	}
}
