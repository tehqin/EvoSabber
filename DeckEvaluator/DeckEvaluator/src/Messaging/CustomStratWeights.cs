using DeckEvaluator.Config;

namespace DeckEvaluator.Messaging
{
   public class CustomStratWeights
   {
		public double HeroHp { get; set; } 
		public double OpHeroHp { get; set; }    
		public double HeroAtk { get; set; }   
		public double OpHeroAtk { get; set; }
		public double HandTotCost { get; set; }
		public double HandCnt { get; set; }
		public double OpHandCnt { get; set; }
		public double DeckCnt { get; set; }
		public double OpDeckCnt { get; set; }   
		
		public double MinionTotAtk { get; set; }
		public double OpMinionTotAtk { get; set; }
		public double MinionTotHealth { get; set; }
		public double OpMinionTotHealth { get; set; }
		public double MinionTotHealthTaunt { get; set; }
		public double OpMinionTotHealthTaunt { get; set; }
		
	   public static CustomStratWeights CreateFromVector(double[] vec)
		{  
			var weights = new CustomStratWeights();
			weights.HeroHp = vec[0]; 
			weights.OpHeroHp = vec[1]; 
			weights.HeroAtk = vec[2]; 
			weights.OpHeroAtk = vec[3];
			weights.HandTotCost = vec[4];
			weights.HandCnt = vec[5];
			weights.OpHandCnt = vec[6];
			weights.DeckCnt = vec[7];
			weights.OpDeckCnt = vec[8]; 
			
			weights.MinionTotAtk = vec[9]; 
			weights.OpMinionTotAtk = vec[10];
			weights.MinionTotHealth = vec[11];
			weights.OpMinionTotHealth = vec[12];
			weights.MinionTotHealthTaunt = vec[13];
			weights.OpMinionTotHealthTaunt = vec[14];
		
			return weights;
		}
	}
}
