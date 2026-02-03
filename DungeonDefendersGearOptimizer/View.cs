using DDUP;
using Microsoft.AspNetCore.Components;
using System.Text.RegularExpressions;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DDUP
{

	[Flags]
	public enum Filters
	{
		None = 0,

		// Armor types
		Pristine = 1 << 0,
		Chain = 1 << 1,
		Mail = 1 << 2,
		Plate = 1 << 3,
		Leather = 1 << 4,

		// Equipment slots
		Helmet = 1 << 5,
		Torso = 1 << 6,
		Gauntlet = 1 << 7,
		Boots = 1 << 8,
		Brooch = 1 << 9,
		Mask = 1 << 10,
		Bracers = 1 << 11,
		Shield = 1 << 12,

		// Categories
		Pets = 1 << 13,
		Weapons = 1 << 14,
		Currency = 1 << 15,
		Events = 1 << 16,
	}


	public sealed class ItemViewRow
	{
		public int Rating { get; set; }
		public int Sides { get; set; }

		public string Name { get; set; }
		public string Location { get; set; }
		public string Quality { get; set; }
		public string Type { get; set; }
		public string Set { get; set; }

		public int Level { get; set; }
		public int MaxLevel { get; set; }

		public int[] Stats = new int[11];
		public int[] UpgradedStats = new int[11];
		public int[] Resists = new int[4];
		public int[] UpgradedResists = new int[4];

		// IMPORTANT: keep this stable/unique for @key
		public int Idx { get; set; }

		public bool IsHidden { get; set; }

		public bool IsEvent{ get; set; }

		public bool IsMissingResists { get; set; }

		public bool IsEquipped { get; set; }
		public bool IsArmor { get; set; }

		public string Color1 { get; set; }
		public string Color2 { get; set; }

		public bool IsEligibleForBest { get; set; }
		public int Value { get; set; }
		public string BestFor { get; set; }

		public float SetBonus { get; set; }
		public int ResistanceTarget { get; set; }
		public int MaxStat { get; set; }
		public int IndexInFolder { get; set; }

		public int IconX { get; set; }
		public int IconY { get; set; }
		public int IconX1 { get; set; }
		public int IconY1 { get; set; }
		public int IconX2 { get; set; }
		public int IconY2 { get; set; }

		public string FunHashString { get; set; }

		public bool IsHiddenDueToSearch { get; set; }
		public string CurrentEquippedSlot { get; set; }

		public double CachedY { get; set; }
		public double CachedHeight { get; set; }

		public List<int> CachedRatings = new List<int>();
		public List<int> CachedSides = new List<int>();

		public string[] CachedStatValues = new string[11];
		public string CachedResistanceTooltip = "";
		public string CachedUpgradesToMaxResistText = "";
		public string CachedResistDisplayValue = "";
		public string CachedValueDisplayIcons = "";
		public string CachedValueDisplayText = "";
		public string CachedValueDisplayTooltip = "";
		public bool HasCustomColor = false;
		public string PlainName = "";

		public int UpgradesRequiredForResists { get; set; }
		public bool BrokenResists = false;

		string ResistLine(string img, string alt, int value)
		{
			return value != 0
				? $"<img class='resist-icon' src='{img}' alt='{alt}' />" +
				  $"<span class='resist-val'>{value}</span>"
				: "";
		}

		public ItemViewRow(
				int Rating,
				int Sides,
				string Name,
				string Location,
				string Quality,
				string Type,
				string Set,
				int Level,
				int MaxLevel,

				int HHP,
				int HDmg,
				int HSpd,
				int HRate,
				int Ab1,
				int Ab2,

				int THP,
				int TDmg,
				int TRange,
				int TRate,

				int RG,
				int RP,
				int RF,
				int RL,
				int Idx,
				int Value,
				string BestFor,
				string FunHashString,

				float SetBonus,
				int ResistanceTarget,
				int MaxStat,

				string Color1,
				string Color2,

				bool IsEvent,
				bool IsMissingResists,
				bool IsEquipped,
				bool IsArmor,
				bool IsEligibleForBest,
				int IndexInFolder,

				int IconX, int IconY, int IconX1, int IconY1, int IconX2, int IconY2)
		{
			this.Rating = Rating;
			this.Sides = Sides;

			this.Name = Name;
			this.Location = Location;
			this.Quality = Quality;
			this.Type = Type;
			this.Set = Set;

			this.Level = Level;
			this.MaxLevel = MaxLevel;


			this.Stats[(int)DDStat.HeroHealth] = HHP;
			this.Stats[(int)DDStat.HeroDamage] = HDmg;
			this.Stats[(int)DDStat.HeroSpeed] = HSpd;
			this.Stats[(int)DDStat.HeroCastRate] = HRate;
			this.Stats[(int)DDStat.HeroAbility1] = Ab1;
			this.Stats[(int)DDStat.HeroAbility2] = Ab2;
			this.Stats[(int)DDStat.TowerHealth] = THP;
			this.Stats[(int)DDStat.TowerDamage] = TDmg;
			this.Stats[(int)DDStat.TowerRate] = TRate;
			this.Stats[(int)DDStat.TowerRange] = TRange;

			this.Resists[0] = RG;
			this.Resists[1] = RP;
			this.Resists[2] = RF;
			this.Resists[3] = RL;

			this.Idx = Idx;

			this.IsEvent = IsEvent;
			this.IsMissingResists = IsMissingResists;
			this.IsEquipped = IsEquipped;
			this.IsArmor = IsArmor;
			this.IsEligibleForBest = IsEligibleForBest;
			this.Value = Value;
			this.BestFor = BestFor;

			this.SetBonus = SetBonus;
			this.MaxStat = MaxStat;
			this.ResistanceTarget = ResistanceTarget;
			this.FunHashString = FunHashString;
			this.Color1 = Color1;
			this.Color2 = Color2;
			this.IndexInFolder = IndexInFolder;
			this.IconX1 = IconX1;
			this.IconY1 = IconY1;
			this.IconX2 = IconX2;
			this.IconY2 = IconY2;
			this.IconX = IconX;
			this.IconY = IconY;
			this.IsHiddenDueToSearch = false;
			this.CurrentEquippedSlot = "";
			this.CachedUpgradesToMaxResistText = ShowUpgradesToMaxResist();
			this.CachedResistanceTooltip =
					$"<div class='resist-container'>" +
					ResistLine("png/Generic Resistance.png", "G", Resists[0]) +
					ResistLine("png/Poison Resistance.png", "P", Resists[1]) +
					ResistLine("png/Fire Resistance.png", "F", Resists[2]) +
					ResistLine("png/Lightning Resistance.png", "L", Resists[3]) +
					$"</div><br>{CachedUpgradesToMaxResistText}";
			this.HasCustomColor = !IsBlack(Color1) || !IsBlack(Color2);
			this.PlainName = RemoveParenthesizedName(this.Name);

			(CachedValueDisplayIcons, CachedValueDisplayText, CachedValueDisplayTooltip) = GetValueDisplay();	
		}

		public void UpdateValueDisplay()
		{
			(CachedValueDisplayIcons, CachedValueDisplayText, CachedValueDisplayTooltip) = GetValueDisplay();	
		}

		private (string emojiText, string text, string tooltip) GetValueDisplay()
		{
			if (IsEvent && (Value == 0))
			{
				return ("💎", "???", "");
			}

			if (Value > 0)
			{
				if (Value > 300)
					return ("💰", "Auction", "Estimated " + Value.ToString() + "cv");
				else
					return ("💎", Value.ToString() + "cv", "");
			}
			else
			{
				if ((Type != "Weapon") && (Type != "Pet") && (Type != "Currency"))
				{
					if (Value > -100) return ("⭐️⭐️⭐️", "", "");// "png/ValueHighest.png");
					else if (Value > -200) return ("⭐️⭐️", "", "");// "png/ValueHigh.png");
					else if (Value > -300) return ("⭐️", "", "");//"png/ValueMid.png");
					else return ("❌", "", "");// "png/ValueLow.png");
				}
				return ("", "", "");
			}
		}


		public string RemoveParenthesizedName(string s)
		{
			if (string.IsNullOrWhiteSpace(s))
				return s;

			// Remove anything inside parentheses including the parentheses
			var result = Regex.Replace(s, @"\s*\(.*?\)", "");

			return result.Trim();
		}


		// helper function
		bool IsBlack(string? c)
		{
			if (string.IsNullOrWhiteSpace(c)) return true; // treat empty as "no color"

			var s = c.Trim().ToLowerInvariant();
			return s == "#000" || s == "#000000" ||
			s == "rgb(0,0,0)" || s == "rgb(0, 0, 0)" ||
			s == "rgba(0,0,0,1)" || s == "rgba(0, 0, 0, 1)";
		}


		public string ShowUpgradesToMaxResist()
		{
			
			int upgradesRequiredForResists = Ratings.GetUpgradesRequired(ResistanceTarget, Resists[0], Resists[1], Resists[2], Resists[3]);
			int overcappedUpgradesLeft = (MaxLevel - Level) / 10;
			int overcappedUpgradesNeeded =
				(ResistanceTarget - ((Resists[0] < 23) ? 23 : Resists[0])) +
				(ResistanceTarget - ((Resists[1] < 23) ? 23 : Resists[1])) +
				(ResistanceTarget - ((Resists[2] < 23) ? 23 : Resists[2])) +
				(ResistanceTarget - ((Resists[3] < 23) ? 23 : Resists[3]));

			if ((Resists[0] == 0) && (Resists[1] == 0) && (Resists[2] == 0) && (Resists[3] == 0))
				return "No resists";
			else if ((Resists[0] == 0) || (Resists[1] == 0) || (Resists[2] == 0) || (Resists[3] == 0))
				return "Missing resist type";
			else if ((overcappedUpgradesLeft < overcappedUpgradesNeeded) || (upgradesRequiredForResists > (MaxLevel - Level)))
				return "Can't cap with remaining levels";
			else if (upgradesRequiredForResists == 0)
				return "";
			return $"{upgradesRequiredForResists} levels required for cap";
		}

		public string GetStringResistValue(bool _showUpgradedStats, bool _assumeSetBonuses)
		{
			int value = _showUpgradedStats ?
				(UpgradedResists[0] + UpgradedResists[1] + UpgradedResists[2] + UpgradedResists[3]) / 4 :
				(Resists[0] + Resists[1] + Resists[2] + Resists[3]) / 4;

			if (_assumeSetBonuses)
			{
				value = (value > 0) ? (int)Math.Ceiling(SetBonus * value) : value;
			}

			return value.ToString();
		}


		public string GetStringStatValue(DDStat stat,  bool showUpgradedStats, bool assumeSetBonuses, bool censor)
		{			
			int value = showUpgradedStats ? UpgradedStats[(int)stat] : Stats[(int)stat];

			if (assumeSetBonuses)
			{
				value = (value > 0) ? (int)Math.Ceiling(SetBonus * value) : value;
			}

			string s = value.ToString();
			if (s == "0")
				return "";

			if (((Quality == "Ult++") && censor) ||
				(((Quality == "Ult++") || (Quality == "Ult+") || (Quality == "Ult90") || (Quality == "Ult93") || (Quality == "Supreme") || (Quality == "Transcendent"))
				  && ((Name == "Unicorn") || (Name == "Rainbow Unicorn") || (Name == "Propeller Cat"))))
				return s.Substring(0, s.Length - 1) + "x";
			else
				return s;
		}
	}
}


