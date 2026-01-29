using DDUP;
using Microsoft.AspNetCore.Components;

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

		public int HHP { get; set; }
		public int HDmg { get; set; }
		public int HSpd { get; set; }
		public int HRate { get; set; }
		public int Ab1 { get; set; }
		public int Ab2 { get; set; }

		public int THP { get; set; }
		public int TDmg { get; set; }
		public int TRange { get; set; }
		public int TRate { get; set; }

		public int RG { get; set; }
		public int RP { get; set; }
		public int RF { get; set; }
		public int RL { get; set; }

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

			this.HHP = HHP;
			this.HDmg = HDmg;
			this.HSpd = HSpd;
			this.HRate = HRate;
			this.Ab1 = Ab1;
			this.Ab2 = Ab2;

			this.THP = THP;
			this.TDmg = TDmg;
			this.TRange = TRange;
			this.TRate = TRate;

			this.RG = RG;
			this.RP = RP;
			this.RF = RF;
			this.RL = RL;

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
		}
	}
}

