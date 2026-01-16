namespace DDUP
{
	public class Ratings
	{
		const int NRoles = 10;
		static string[] Roles = { "Builder App", "Dps AB2", "Dps AB1", "Builder Summoner", "Builder Hermit", "Gunwitch", "Builder TRange", "Builder EV", "Tower Boost AB1", "Guardian Summoner" };
		
				
		static int[][] CV_Values =
		{
			new [] { 2000, 25, 2050, 35, 2100, 50, 2150, 80, 2200, 150, 2250, 220, 2300, 500, 2350, 1000, 2400, 2000 }, // app builder
			new [] { 1400, 20, 1450, 35, 1500, 50, 1550, 75, 1580, 150, 1600, 250, 1650, 500, 1700, 1000, 1750, 2000, 1800, 10000 }, // dps ab2
			new [] { 1400, 20, 1500, 40, 1550, 60, 1600, 100, 1667, 300, 1770, 1500 }, // dps ab1
			new [] { 1000, 10, 1050, 12, 1100, 15, 1150, 25, 1200, 30, 1250, 100, 1300, 500 }, // summoner
			new [] { 2000, 13, 2050, 17, 2100, 24, 2150, 40, 2200, 75, 2250, 110, 2300, 250, 2350, 500, 2400, 1000 }, // hermit
			new [] { 1400, 20, 1500, 40, 1550, 60, 1600, 250, 1700, 500 }, // gunwitch
			new [] { 1000, 5, 1050, 15, 1100, 25, 1150, 50, 1200, 100, 1250, 200, 1300, 300 }, //trange
			new [] { 1000, 5, 1050, 15, 1100, 25, 1150, 50, 1200, 100, 1250, 200, 1300, 300 }, // builder ev
			new [] { 1400, 20, 1500, 40, 1550, 60, 1600, 100, 1667, 300, 170, 1500 }, // tb
			new [] { 1400, 0, 1500, 0}, // worth nothing for now
		};

		// how best to evaluate... ? 
		static Dictionary<string, float>[] BestRatings = new Dictionary<string, float>[NRoles];
		public static void ClearBestRatings()
		{
			for (int i = 0; i < NRoles; i++) { BestRatings[i] = new Dictionary<string, float>();}
		}

		// always measure CV first to also get the BestRatings populated
		public static (int, string) GetBestValue(bool measureCV, ItemViewRow vr)
		{
			int[] ratings = new int[NRoles];
			
			(ratings[0], _) = EvalRating(vr, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, false); // app builder
			(ratings[1], _) = EvalRating(vr, 0, 1, 0, 0, 0, 1, 0, 0, 0, 0, true); // dps ab2
			(ratings[2], _) = EvalRating(vr, 0, 1, 0, 0, 1, 0, 0, 0, 0, 0, true); // dps ab1
			(ratings[3], _) = EvalRating(vr, 0, 0, 0, 0, 0, 0, 1, 0, 0, 0, false); // summoner
			(ratings[4], _) = EvalRating(vr, 0, 0, 0, 0, 0, 0, 1, 1, 1, 0, false); // hermit
			(ratings[5], _) = EvalRating(vr, 0, 1, 0, 0, 0, 0, 0, 1, 0, 0, true); // gunwitch
			(ratings[6], _) = EvalRating(vr, 0, 0, 0, 0, 0, 0, 0, 0, 1, 0, false); // trange
			(ratings[7], _) = EvalRating(vr, 0, 0, 0, 0, 0, 0, 0, 1, 0, 0, false); // builder ev
			(ratings[8], _) = EvalRating(vr, 0, 0, 0, 0, 1, 0, 0, 0, 0, 0, true); // tb			
			(ratings[9], _) = EvalRating(vr, 1, 0, 0, 0, 0, 1, 0, 0, 0, 0, true); // guardian summoner	

			if (measureCV)
			{
				int bestValue = 0;
				int bestIndex = 0;
				for (int i = 0; i < NRoles; i++)
				{
					int value = (int)(EvaluateExponential(ratings[i], CV_Values[i]));
					if (value > bestValue)
					{
						bestValue = value;
						bestIndex = i;
					}

					// best overall ratings per category
					if ((!BestRatings[i].ContainsKey(vr.Type)) || (ratings[i] > BestRatings[i][vr.Type]))
					{
						BestRatings[i][vr.Type] = ratings[i];
					}
				}

				// return CV value
				if (bestValue != 0)
				{
					return (bestValue, Roles[bestIndex]);
				}
				return (0,"");
			}
			else
			{	
				// 2nd pass for substandard stuff only looks at Rating relative to top in category
				int bestValue = 0;
				int bestIndex = 0;
				for (int i = 0; i < NRoles; i++)
				{
					if (BestRatings[i].ContainsKey(vr.Type))
					{
						int value = (int)(ratings[i] * 1000 / BestRatings[i][vr.Type]);
						if (value > bestValue)
						{
							bestValue = value;
							bestIndex = i;
						}
					}
				}
				// return RelativeRatingIndex - goes negative for parsing in the front end later
				return (bestValue - 1000, Roles[bestIndex]);
			}								
		}

		// exponential interoplation
		public static double EvaluateExponential(double x, int[] points)
		{
			int count = points.Length / 2;

			double minX = points[0];
			double maxX = points[points.Length - 2];
			double maxY = points[points.Length - 1];

			if (x < minX)
				return 0;

			if (x >= maxX)
				return maxY;

			// Find the segment [x0, x1] that contains x
			for (int i = 0; i < points.Length - 2; i += 2)
			{
				double x0 = points[i];
				double y0 = points[i + 1];
				double x1 = points[i + 2];
				double y1 = points[i + 3];

				if (x >= x0 && x <= x1)
				{
					if (y0 <= 0 || y1 <= 0)
						return y0;

					double t = (x - x0) / (x1 - x0);

					// Exponential interpolation via log-space
					double logY =
						Math.Log(y0) + t * (Math.Log(y1) - Math.Log(y0));

					return Math.Exp(logY);
				}
			}

			return maxY; 
		}


		public static (int, int) EvalRating(ItemViewRow vr, int hhp, int hdmg, int hspd, int hrate, int ab1, int ab2, int thp, int tdmg, int trange, int trate, bool factorInResists)
		{
			int rating = 0;
			int sides = 0;
			int maxGrowthLevels = 0;
			maxGrowthLevels += AddRating(ref rating, ref sides, hhp, vr.HHP, vr.MaxStat, vr.SetBonus);
			maxGrowthLevels += AddRating(ref rating, ref sides, hdmg, vr.HDmg, vr.MaxStat, vr.SetBonus);
			maxGrowthLevels += AddRating(ref rating, ref sides, hspd, vr.HSpd, vr.MaxStat, vr.SetBonus);
			maxGrowthLevels += AddRating(ref rating, ref sides, hrate, vr.HRate, vr.MaxStat, vr.SetBonus);
			maxGrowthLevels += AddRating(ref rating, ref sides, ab1, vr.Ab1, vr.MaxStat, vr.SetBonus);
			maxGrowthLevels += AddRating(ref rating, ref sides, ab2, vr.Ab2, vr.MaxStat, vr.SetBonus);
			maxGrowthLevels += AddRating(ref rating, ref sides, thp, vr.THP, vr.MaxStat, vr.SetBonus);
			maxGrowthLevels += AddRating(ref rating, ref sides, tdmg, vr.TDmg, vr.MaxStat, vr.SetBonus);
			maxGrowthLevels += AddRating(ref rating, ref sides, trange, vr.TRange, vr.MaxStat, vr.SetBonus);
			maxGrowthLevels += AddRating(ref rating, ref sides, trate, vr.TRate, vr.MaxStat, vr.SetBonus);

			int levelsLeft = vr.MaxLevel - vr.Level;
			int resG = vr.RG;
			int resP = vr.RP;
			int resF = vr.RF;
			int resL = vr.RL;

			int upgradesRequiredForResists = GetUpgradesRequired(vr.ResistanceTarget, resG, resP, resF, resL);
			int overcappedUpgrades = levelsLeft / 10;
			int overcappedUpgradesNeeded =
				(vr.ResistanceTarget - ((resG < 23) ? 23 : resG)) +
				(vr.ResistanceTarget - ((resP < 23) ? 23 : resP)) +
				(vr.ResistanceTarget - ((resF < 23) ? 23 : resF)) +
				(vr.ResistanceTarget - ((resL < 23) ? 23 : resL));

			int remainingOvercappedUpgrades = Math.Min(overcappedUpgrades, overcappedUpgradesNeeded);
			int remainingUpgrades = upgradesRequiredForResists - overcappedUpgradesNeeded + remainingOvercappedUpgrades;
			int levelsForStatsLeft = Math.Max(0, levelsLeft - remainingUpgrades);

			if (!factorInResists || (vr.ResistanceTarget == 0)) levelsForStatsLeft = levelsLeft;

			rating += (int)Math.Ceiling(Math.Min(levelsForStatsLeft, maxGrowthLevels) * vr.SetBonus);

			// actual rating and sides can be higher because each stat gets ceilinged first, but we wont' worry about that.

			return (rating, sides);
		}

		private static int AddRating(ref int rating, ref int sides, int idx, int value, int maxStat, float mult)
		{
			if (idx == 1) rating += (value > 0) ? (int)Math.Ceiling(value * mult) : value;
			else if (idx == 2) sides += (value > 0) ? (int)Math.Ceiling(value * mult) : value;

			int levelsLeft = (maxStat - value);
			if (levelsLeft < 0) levelsLeft = 0;
			if (idx != 1) levelsLeft = 0;
			return levelsLeft;
		}

		static int[] ResistRequirements =
	{
			42,42,42,41,41,41,40,40,40,              // -29 to -21
            39, 39, 38, 38, 37, 37, 36, 36, 35, 34,  // -20 to -11
			33, 32, 31, 30, 29, 28, 27, 26, 25, 24,  // -10 to -1
			23, 23, 22, 21, 20, 19, 18, 17, 16, 15,  // 0 to 9
			14, 13, 12, 11, 10, 9, 9, 8, 8, 7, // 10 to 19
			7, 6, 5, 6, 5, 4, 3, 2, 1, 0, -1, -2}; // 20 to 31

		public static int GetUpgradesRequired(int tgt, int g, int p, int f, int l)
		{
			int delta = tgt - 29;

			int greq = ((g >= -29) && (g < 31)) ? ResistRequirements[g + 29] + delta : 0;
			int preq = ((p >= -29) && (p < 31)) ? ResistRequirements[p + 29] + delta : 0;
			int freq = ((f >= -29) && (f < 31)) ? ResistRequirements[f + 29] + delta : 0;
			int lreq = ((l >= -29) && (l < 31)) ? ResistRequirements[l + 29] + delta : 0;

			if (greq < 0) greq = 0;
			if (preq < 0) preq = 0;
			if (freq < 0) freq = 0;
			if (lreq < 0) lreq = 0;

			return greq + preq + freq + lreq;
		}


	}
}
