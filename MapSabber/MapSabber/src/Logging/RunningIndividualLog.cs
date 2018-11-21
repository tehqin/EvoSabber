using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using MapSabber.Search;

namespace MapSabber.Logging
{
   class RunningIndividualLog
   {
      private string _logPath;

      public RunningIndividualLog(string logPath)
      {
         _logPath = logPath; 
      
         // Create a log for individuals
         using (FileStream ow = File.Open(_logPath,
                   FileMode.Create, FileAccess.Write, FileShare.None))
         {
				// The data to maintain for individuals evaluated.
				string[] dataLabels = {
                  "Individual",
                  "Parent",
                  "Win Count",
                  "Health Difference",
                  "Damage Done",
                  "Num Turns",
                  "Cards Drawn",
                  "Mana Spent",
                  "Strategy Alignment",
                  "Dust",
                  "Deck",
               };

            WriteText(ow, string.Join(",", dataLabels));
            ow.Close();
         }
      }

      private static void WriteText(Stream fs, string s)
      {
         s += "\n";
         byte[] info = new UTF8Encoding(true).GetBytes(s);
         fs.Write(info, 0, info.Length);
      }

      public void LogIndividual(Individual cur)
      {
			using (StreamWriter sw = File.AppendText(_logPath))
         {
				List<string> deck = cur.GetCards();

            string[] data = {
                  cur.ID.ToString(),
                  cur.ParentID.ToString(),
                  cur.WinCount.ToString(),
						cur.TotalHealthDifference.ToString(),
						cur.DamageDone.ToString(),
						cur.NumTurns.ToString(),
						cur.CardsDrawn.ToString(),
						cur.ManaSpent.ToString(),
						cur.StrategyAlignment.ToString(),
						cur.Dust.ToString(),
						string.Join("*", deck),
               };
            
            sw.WriteLine(string.Join(",", data));
            sw.Close();
         }
      }
   }
}
