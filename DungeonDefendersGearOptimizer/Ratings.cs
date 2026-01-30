using Microsoft.AspNetCore.Components.Web.Virtualization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using static System.Net.WebRequestMethods;

namespace DDUP
{
	using System.Data.Common;
	using System.Text.Json;

	public sealed class PriceEntry
	{
		public int estimatedPrice { get; set; }
	}
	public class Ratings
	{
		public struct RatingModeInfo
		{			
			public string Name;
			public string Icon;
			public string UpgradePriority;
			public List<DDStat> RatingStatsPriority;
			public List<DDStat> SidesStatsPriority;
			public bool RequireResists;
			public string APIRoles;			
			public bool CanBeBestFor;
		};


		public static readonly List<RatingModeInfo> RatingModes = new()
		{
			new RatingModeInfo
			{
				Name = "Builder Stats",
				Icon = "png/Tower Damage.png",
				UpgradePriority = "",
				RatingStatsPriority = new List<DDStat>() { DDStat.TowerDamage, DDStat.TowerRate, DDStat.TowerRange, DDStat.TowerHealth },
				SidesStatsPriority = new List<DDStat>() { },
				RequireResists = false,
				APIRoles = "",
				CanBeBestFor = false
			},

			new RatingModeInfo
			{
				Name = "Hero Stats",
				Icon = "png/Hero Damage.png",
				UpgradePriority = "",
				RatingStatsPriority = new List<DDStat>() { DDStat.HeroDamage, DDStat.HeroHealth, DDStat.HeroCastRate, DDStat.HeroSpeed },
				SidesStatsPriority = new List<DDStat>() { },
				RequireResists = true,
				APIRoles = "",
				CanBeBestFor = false
			},

			new RatingModeInfo
			{
				Name = "Builder App",
				Icon = "png/Apprentice_TinyIcon.png",
				UpgradePriority = "",
				RatingStatsPriority = new List<DDStat>() { DDStat.TowerDamage, DDStat.TowerRate, DDStat.TowerRange },
				SidesStatsPriority = new List<DDStat>() { DDStat.TowerHealth },
				RequireResists = false,
				APIRoles = "app",
				CanBeBestFor = true
			},

			new RatingModeInfo
			{
				Name = "Builder Hermit",
				Icon = "png/Hermit_Icon_Tiny.png",
				UpgradePriority = "",
				RatingStatsPriority = new List<DDStat>() { DDStat.TowerDamage, DDStat.TowerRange, DDStat.TowerHealth },
				SidesStatsPriority = new List<DDStat>() { DDStat.TowerRate },
				RequireResists = false,
				APIRoles = "hermit",
				CanBeBestFor = true
			},

			new RatingModeInfo
			{
				Name = "Builder TRange",
				Icon = "png/Tower Range.png",
				UpgradePriority = "",
				RatingStatsPriority = new List<DDStat>() { DDStat.TowerRange },
				SidesStatsPriority = new List<DDStat>() { DDStat.TowerHealth, DDStat.TowerDamage, DDStat.TowerRate },
				RequireResists = false,
				APIRoles = "trange",
				CanBeBestFor = true
			},

			new RatingModeInfo
			{
				Name = "Builder EV",
				Icon = "png/Tower Damage.png",
				UpgradePriority = "",
				RatingStatsPriority = new List<DDStat>() { DDStat.TowerDamage },
				SidesStatsPriority = new List<DDStat>() { DDStat.TowerHealth, DDStat.TowerRate, DDStat.TowerRange },
				RequireResists = false,
				APIRoles = "builder ev",
				CanBeBestFor = true
			},

			new RatingModeInfo
			{
				Name = "Builder Summoner",
				Icon = "png/Tower Health.png",
				UpgradePriority = "",
				RatingStatsPriority = new List<DDStat>() { DDStat.TowerHealth },
				SidesStatsPriority = new List<DDStat>() { DDStat.TowerDamage, DDStat.TowerRate, DDStat.TowerRange },
				RequireResists = false,
				APIRoles = "summoner",
				CanBeBestFor = true
			},

			new RatingModeInfo
			{
				Name = "AB1 Only",
				Icon = "png/AB1/Monk AB1.png",
				UpgradePriority = "",
				RatingStatsPriority = new List<DDStat>() { DDStat.HeroAbility1 },
				SidesStatsPriority = new List<DDStat>() { DDStat.HeroHealth, DDStat.HeroDamage, DDStat.HeroCastRate },
				RequireResists = true,
				APIRoles = "tb",
				CanBeBestFor = true
			},

			new RatingModeInfo
			{
				Name = "DPS AB1",
				Icon = "png/AB1/Countess AB1.png",
				UpgradePriority = "",
				RatingStatsPriority = new List<DDStat>() { DDStat.HeroDamage, DDStat.HeroAbility1 },
				SidesStatsPriority = new List<DDStat>() { DDStat.HeroHealth, DDStat.HeroCastRate },
				RequireResists = true,
				APIRoles = "dps ab1",
				CanBeBestFor = true
			},

			new RatingModeInfo
			{
				Name = "DPS AB2",
				Icon = "png/AB2/Hero Boost Monk AB2.png",
				UpgradePriority = "",
				RatingStatsPriority = new List<DDStat>() { DDStat.HeroDamage, DDStat.HeroAbility2 },
				SidesStatsPriority = new List<DDStat>() { DDStat.HeroHealth, DDStat.HeroCastRate },
				RequireResists = true,
				APIRoles = "dps ab2",
				CanBeBestFor = true
			},

			new RatingModeInfo
			{
				Name = "Pure DPS",
				Icon = "png/Hero Damage.png",
				UpgradePriority = "",
				RatingStatsPriority = new List<DDStat>() { DDStat.HeroDamage },
				SidesStatsPriority = new List<DDStat>() { DDStat.HeroHealth, DDStat.HeroAbility1, DDStat.HeroAbility2, DDStat.HeroCastRate },
				RequireResists = true,
				APIRoles = "",
				CanBeBestFor = false
			},

			new RatingModeInfo
			{
				Name = "Gunwitch",
				Icon = "png/Gunwitch_TinyIcon.png",
				UpgradePriority = "",
				RatingStatsPriority = new List<DDStat>() { DDStat.HeroDamage, DDStat.TowerDamage },
				SidesStatsPriority = new List<DDStat>() { DDStat.TowerRate, DDStat.HeroHealth },
				RequireResists = true,
				APIRoles = "gunwitch",
				CanBeBestFor = true
			},

			new RatingModeInfo
			{
				Name = "Needle Gunwitch",
				Icon = "png/Tower Range Gunwitch.png",
				UpgradePriority = "",
				RatingStatsPriority = new List<DDStat>() { DDStat.TowerRange },
				SidesStatsPriority = new List<DDStat>() { DDStat.HeroDamage, DDStat.TowerDamage, DDStat.TowerRate, DDStat.HeroHealth },
				RequireResists = true,
				APIRoles = "",
				CanBeBestFor = false
			},

			new RatingModeInfo
			{
				Name = "Guardian Summoner",
				Icon = "png/Summoner_TinyIcon.png",
				UpgradePriority = "",
				RatingStatsPriority = new List<DDStat>() { DDStat.HeroHealth, DDStat.HeroAbility2 },
				SidesStatsPriority = new List<DDStat>() { },
				RequireResists = true,
				APIRoles = "",
				CanBeBestFor = true
			}
		};



		static Dictionary<int, ItemViewRow> JsonQueries = [];
		static List<string> ValuableItemList = new();
		static int JsonQueryIndex = 0;

		static Dictionary<string, int[]> CV_Values = new() {
			["Builder App"]= new int[] { 2000, 25, 2050, 35, 2100, 50, 2150, 80, 2200, 150, 2250, 220, 2300, 500, 2350, 1000, 2400, 4000, 2450, 8000, 2500, 20000 }, // app builder
			["DPS AB2"]= new int[] { 1400, 20, 1450, 35, 1500, 50, 1550, 75, 1580, 150, 1600, 250, 1650, 500, 1700, 1000, 1750, 2000, 1800, 8000, 1850, 15000 }, // dps ab2
			["DPS AB1"]= new int[] { 1400, 20, 1500, 40, 1550, 60, 1600, 100, 1650, 300, 1700, 500, 1750, 1000, 1800, 2500  }, // dps ab1
			["Builder Summoner"]= new int[]	 { 1000, 10, 1050, 12, 1100, 15, 1150, 25, 1200, 30, 1250, 100, 1300, 500, 1350, 1500 }, // summoner
			["Builder Hermit"]= new int[]	 { 2000, 13, 2050, 17, 2100, 24, 2150, 40, 2200, 75, 2250, 110, 2300, 250, 2350, 500, 2400, 1000 }, // hermit
			["Gunwitch"]= new int[]	 { 1400, 20, 1500, 40, 1550, 60, 1600, 250, 1700, 600, 1750, 1200, 1800, 2000 }, // gunwitch
			["Builder TRange"]= new int[]	 { 1000, 5, 1050, 15, 1100, 25, 1150, 50, 1200, 100, 1250, 200, 1300, 800, 1350, 1500 }, //trange
			["Builder EV"]= new int[]	 { 1000, 5, 1050, 15, 1100, 25, 1150, 50, 1200, 100, 1250, 200, 1300, 500, 1350, 1500 }, // builder ev
			["AB1 Only"]= new int[]	 { 1000, 20, 1050, 40, 1100, 100, 1150, 250, 1200, 1500, 1250, 3000 }, // ab1 only		
		};


		// how best to evaluate... ? 
		static Dictionary<string, float>[] BestRatings = new Dictionary<string, float>[RatingModes.Count];
		public static void ClearBestRatings()
		{
			ValuableItemList.Clear();
			JsonQueryIndex = 0;
			JsonQueries.Clear();

			for (int i = 0; i < RatingModes.Count; i++) { BestRatings[i] = new Dictionary<string, float>(); }
		}

		private static readonly HttpClient _http = new HttpClient();

		public static async Task AsyncShiroPriceAPICall(DDUP.Pages.Index index)
		{
			// skip this for now

			if (ValuableItemList.Count == 0)
				return;
			string json = JsonSerializer.Serialize(ValuableItemList);
			string encoded = Uri.EscapeDataString(json);

			var url = $"https://est.overflow.fun/estimate?mass={encoded}";
			Console.WriteLine("Sending " + url);

			// Force a timeout so "hang" becomes an error you can see
			using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

			try
			{
				using var req = new HttpRequestMessage(HttpMethod.Get, url);
				req.Headers.UserAgent.ParseAdd("DDGO/1.0"); // sometimes helps with picky servers

				using var resp = await _http.SendAsync(
					req,
					HttpCompletionOption.ResponseHeadersRead,
					cts.Token);

				Console.WriteLine($"Status: {(int)resp.StatusCode} {resp.ReasonPhrase}");

				var body = await resp.Content.ReadAsStringAsync(cts.Token);
				Console.WriteLine(body);
				var prices = JsonSerializer
					.Deserialize<List<PriceEntry>>(body)!
					.Select(x => x.estimatedPrice)
					.ToList();

				if (prices.Count == JsonQueries.Count)
				{
					for (int i = 0; i < prices.Count; i++)
					{
						Console.WriteLine(JsonQueries[i].Name + ": " + JsonQueries[i].Value + "=>" + prices[i]);
						// update price estimates
						JsonQueries[i].Value = prices[i];
					}
				}
				else
				{
					Console.WriteLine("Query Count Mismatch!");
				}

			}
			catch (TaskCanceledException ex)
			{
				Console.WriteLine("Timed out (or cancelled): " + ex.Message);
			}
			catch (HttpRequestException ex)
			{
				Console.WriteLine("HttpRequestException: " + ex.Message);
				if (ex.InnerException != null)
					Console.WriteLine("Inner: " + ex.InnerException.Message);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Other exception: " + ex);
			}

			index.CalculateVanityTotals();
			index.SetPriceStatus("Armor prices live using Shiro's API", false);

		}

		// always measure CV first to also get the BestRatings populated
		public static (int, string) GetBestValue(bool measureCV, ItemViewRow vr)
		{
			int[] ratings = new int[RatingModes.Count];
			
			for (int i = 0; i < Ratings.RatingModes.Count; i++ )
			{
				if (Ratings.RatingModes[i].CanBeBestFor )
				{
					(ratings[i], _) = EvalRating(vr, RatingModes[i].RatingStatsPriority, RatingModes[i].SidesStatsPriority, RatingModes[i].RequireResists);					
				}
			}
			
			string category = vr.Type;
			if (vr.IsArmor && !vr.IsEvent)
			{
				category = vr.Type + "_" + vr.Set;
			}

			if (measureCV)
			{
				int bestValue = 0;
				int bestIndex = 0;
				int bestIndexPriced = 0;
				int bestValuePriced = 0;
				for (int i = 0; i < RatingModes.Count; i++)
				{
					if (!RatingModes[i].CanBeBestFor)
						continue;
					bool isPriced = (RatingModes[i].APIRoles != "");

					int value = 0;
					if (isPriced)
					{
						if (CV_Values.ContainsKey(RatingModes[i].Name))
						{
							value = (int)(EvaluateExponential(ratings[i], CV_Values[RatingModes[i].Name]));
							if (value > bestValue)
							{
								bestValue = value;
								bestIndex = i;
							}

							if ((value > bestValuePriced) && isPriced)
							{
								bestValuePriced = value;
								bestIndexPriced = i;
							}
						}
						else
						{
							Console.WriteLine("**ERROR**: Rating mode inconsistency");
						}
					}
					
					// best overall ratings per category
					if ((!BestRatings[i].ContainsKey(category)) || (ratings[i] > BestRatings[i][category]))
					{
						BestRatings[i][category] = ratings[i];
					}
				}

				if ((bestValuePriced > 0) && (vr.IsArmor) && (!vr.IsEvent))
				{
					ValuableItemList.Add(ratings[bestIndexPriced] + " " + RatingModes[bestIndexPriced].APIRoles);
					JsonQueries[JsonQueryIndex++] = vr;
				}

				// return CV value
				if (bestValue != 0)
				{
					if (vr.IsArmor)
					{
						// round to nearest 5cv to not imply precision
						return ((int)((float)bestValue / 5.0f + 0.5f) * 5, RatingModes[bestIndex].Name);
					}
					return (0, "");
				}
				return (0, "");
			}
			else
			{
				// 2nd pass for substandard stuff only looks at Rating relative to top in category
				int bestValue = 0;
				int bestIndex = 0;
				for (int i = 0; i < RatingModes.Count; i++)
				{
					if (BestRatings[i].ContainsKey(category))
					{
						int value = (int)(ratings[i] * 1000 / BestRatings[i][category]);
						if (value > bestValue)
						{
							bestValue = value;
							bestIndex = i;
						}
					}
				}
				// return RelativeRatingIndex - goes negative for parsing in the front end later
				return (bestValue - 1000, RatingModes[bestIndex].Name);
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


		public static (int, int) EvalRating(ItemViewRow vr, List<DDStat> RatingStatsPriority, List<DDStat> SidesStatsPriority, bool bRequireResists)
		{
			int rating = 0;
			int sides = 0;
			float fracLeftOver = 0;
			int maxGrowthLevels = 0;

			for (int i = 1; i < 11; i++) // loop through stats
			{
				int ratingMode = (RatingStatsPriority.Contains((DDStat)i) ? 1 : 0);
				if (SidesStatsPriority.Contains((DDStat)i))
					ratingMode = 2;

				maxGrowthLevels += AddRating(ref rating, ref sides, ref fracLeftOver, ratingMode, vr.Stats[i], vr.MaxStat, vr.SetBonus);
			}
			
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

			int allowedMisplaced = (vr.Quality == "Supreme") ? -2 : 0;
			bool cantHitResistCap = (levelsLeft - remainingUpgrades < allowedMisplaced);

			int levelsForStatsLeft = Math.Max(0, levelsLeft - remainingUpgrades);

			if (!bRequireResists || (vr.ResistanceTarget == 0)) levelsForStatsLeft = levelsLeft;


			// apply fractional rounding to leveling up, if there's anything to level up
			// this isn't perfect; it doesn't know what order you might upgrade an item with two primary stats
			// but at least it's mostly accurate
			// This keeps cap at 840 for Ult90s, and 0 for things with no range stat (for instance), while not messing with fully upgraded items
			rating += (int)Math.Ceiling((Math.Max(0, Math.Min(levelsForStatsLeft, maxGrowthLevels) - fracLeftOver)) * vr.SetBonus);

			if (bRequireResists && cantHitResistCap)
			{
				rating = 0;
			}
			// actual rating and sides can be higher because each stat gets ceilinged first, but we wont' worry about that.

			return (rating, sides);
		}

		private static int AddRating(ref int rating, ref int sides, ref float fracLeftOver, int idx, int value, int maxStat, float mult)
		{
			// find the ceiling when we get the set bonus
			int bonus = (int)Math.Ceiling(value * mult);

			int ratingAdd = 0;
			int sidesAdd = 0;

			if (idx == 1) ratingAdd = (value > 0) ? bonus : value;
			else if (idx == 2) sidesAdd = (value > 0) ? bonus : value;

			rating += ratingAdd;
			sides += sidesAdd;

			int levelsLeft = (maxStat - value);
			if (levelsLeft < 0) levelsLeft = 0;
			if (idx != 1) levelsLeft = 0;

			// save off the fraction that is left over to the next int just in case we're doing an upgrade at all here
			if (levelsLeft > 0)
				fracLeftOver = Math.Max(fracLeftOver, bonus - (value * mult));

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
