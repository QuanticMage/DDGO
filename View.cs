namespace DDUP
{
	public sealed class ItemViewRow
	{
		public int Rating { get; set; }
		public int Sides { get; set; }

		public string Name { get; set; }
		public string Location { get; set; }
		public string Quality { get; set; }
		public string Type { get; set; }
		public string SubType { get; set; }

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

		public int BestAvailable { get; set; }

		public ItemViewRow(
				int Rating,
				int Sides,
				string Name,
				string Location,
				string Quality,
				string Type,
				string SubType,
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
				int BestAvailable)
		{
			this.Rating = Rating;
			this.Sides = Sides;

			this.Name = Name;
			this.Location = Location;
			this.Quality = Quality;
			this.Type = Type;
			this.SubType = SubType;

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
			this.BestAvailable = BestAvailable;
		}
	}
}

