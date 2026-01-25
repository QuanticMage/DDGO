using DDUP;
using Microsoft.AspNetCore.Components.Web.Virtualization;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Reflection.PortableExecutable;
using System.Runtime.Intrinsics.Arm;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

public enum DDStat : int
{
	// --- Hero Stats ---
	HeroHealth = 1,
	HeroSpeed = 2,
	HeroDamage = 3,
	HeroCastingRate = 4,

	// --- Hero Abilities ---
	HeroAbility1 = 5,
	HeroAbility2 = 6,

	// --- Tower Stats ---
	TowerHealth = 7,
	TowerRate = 8,
	TowerDamage = 9,
	TowerRange = 10,
}

public struct DDLinearColor
{
	public float R, G, B, A;

	public DDLinearColor( float r, float g, float b, float a )
	{
		R = r; G = g; B = b; A = a;
	}

	public void Scale( float amt )
	{
		R *= amt;
		G *= amt;
		B *= amt;
		A *= amt;
	}
	public void Add(DDLinearColor o)
	{
		R += o.R;
		G += o.G;
		B += o.B;
		A += o.A;
	}

	public string ToCString()
	{
		float r = R;
		float g = G;
		float b = B;
		float a = A;

		// --- Normalize RGB if any component > 1 ---
		float maxRgb = Math.Max(r, Math.Max(g, b));
		if (maxRgb > 1.0f)
		{
			float inv = 1.0f / maxRgb;
			r *= inv;
			g *= inv;
			b *= inv;
		}

		// --- Clamp all channels ---
		r = Clamp01(r);
		g = Clamp01(g);
		b = Clamp01(b);
		a = Clamp01(a); // A is clamped, not normalized


		r = (float)Math.Pow(r, 1 / 2.2);
		g = (float)Math.Pow(g, 1 / 2.2);
		b = (float)Math.Pow(b, 1 / 2.2);

		// --- Convert to 0–255 ---
		uint Ri = (uint)(r * 255.0f + 0.5f);
		uint Gi = (uint)(g * 255.0f + 0.5f);
		uint Bi = (uint)(b * 255.0f + 0.5f);
		uint Ai = (uint)(a * 255.0f + 0.5f);

		return $"#{Ri:X2}{Gi:X2}{Bi:X2}";
	}

	private static float Clamp01(float v)
	{
		if (v < 0f) return 0f;
		if (v > 1f) return 1f;
		return v;
	}
};

public struct DDFolder
{
	public int Index;
	public int ParentIndex;
	public string Name;
};

public class DDEquipmentInfo
{
	public int Idx;
	public bool IsInitialized;
	public int[] ResistIdx = new int[4];
	public int[] ResistAmt = new int[4];
	public int[] Stats = new int[24];
	public int WeaponDamageBonus;
	public int WeaponProjectiles;
	public int WeaponSpeedType;
	public float DrawScale, SwingSpeed;
	public int Level, MaxLevel;
	public int StoredMana;
	public byte[] WeaponBonuses = new byte[7];
	public byte NameVariantIdx, NameResistIdx, NameQualityIdx;
	public byte PrimaryColorSet, SecondaryColorSet;
	public int ID1, ID2;
	public int MinSell, MaxSell;
	public Int64 ManaValue;
	public int LocX, LocY, LocZ;
	public byte bUpgradable, bRenameAtMax, bNoSell, bNoDrop, bAutoLock, bOnceEffect, bLocked, ManualLR;
	public DDLinearColor Color1, Color2;
	public DDLinearColor IconColor1, IconColor2;
	public string GeneratedName = "";
	public string UserEquipName = "";
	public string ForgerName = "";
	public string Description = "";
	public string Template = "";
	public string Location = "";

	public string Quality = "";
	public string Type = "";
	public string Set = "";
	
	public int FolderID;
	public int IndexInFolder = -1;
	public bool bIsSecondary;
	public int UserSellPrice;
	public bool bIsArmor;
	public bool bIsEvent;
	public bool bIsEquipped;
	public bool bIsMissingResists;

	public int IconX;
	public int IconY;
	public int IconX1;
	public int IconY1;
	public int IconX2;
	public int IconY2;

	public int EventItemValue = 0;
	public string FunHashString = "";

	public ItemViewRow? cachedItemRow = null;

	static string FormatAmount(long xBillions, long remainder)
	{
		double value = xBillions * 1000000000.0 + remainder;
		return FormatCompact(value);
	}

	static string FormatCompact(double value)
	{
		if (value == 0) return "0";
		if (value < 0) return "-" + FormatCompact(-value);

		string suffix;
		double scaled;

		if (value >= 1e12) { suffix = "T"; scaled = value / 1e12; }
		else if (value >= 1e9) { suffix = "B"; scaled = value / 1e9; }
		else if (value >= 1e6) { suffix = "M"; scaled = value / 1e6; }
		else if (value >= 1e3) { suffix = "K"; scaled = value / 1e3; }
		else { suffix = ""; scaled = value; }

		// 4 significant digits
		int digits = (int)Math.Floor(Math.Log10(scaled)) + 1;
		int decimals = Math.Max(0, 4 - digits);

		return scaled
			.ToString("F" + decimals)
			.TrimEnd('0')
			.TrimEnd('.') + suffix;
	}

	public ItemViewRow ToItemViewRow()
	{
		if (cachedItemRow == null)
		{
			static int Stat(DDEquipmentInfo x, DDStat s)
			=> (x.Stats is { Length: > 0 } && (int)s >= 0 && (int)s < x.Stats.Length) ? x.Stats[(int)s] : 0;

			static int Resist(DDEquipmentInfo x, int i)
				=> (x.ResistAmt is { Length: > 0 } && i >= 0 && i < x.ResistAmt.Length) ? x.ResistAmt[i] : 0;


			// Name rule:
			// - If UserEquipName empty => GeneratedName
			// - Else => "UserEquipName (GeneratedName)"
			var gen = Regex.Replace(this.GeneratedName, "<[^>]*>", "").Trim();
		
			var user = this.UserEquipName ?? "";
			var name = string.IsNullOrWhiteSpace(user) ? gen : ((this.bIsArmor || (gen==user) || (bIsEvent) || (this.Type == "Currency")) ? user : $"{user} ({gen})");

			// avoid () for common pets
			if ((user != null) && ((gen == "GearCat") || (gen.Trim() == "") || (gen == "Griffin") || (gen == "Basic Familar") || (gen == "Aura"))) 
			{
				name = user;
			}

			if (( this.Type == "Currency") && (name == "Mana Token"))
			{				
				name += $" ({FormatAmount(this.StoredMana, this.MinSell)})";				
			}
			else if (this.Type == "Currency") 
			{
				if (Stats[0] > 0)
					name += $" ({1 + (this.Stats[0]) / 16})";
			}
			
			bool bIsEligibleForBest =
				(this.Type == "Boots") ||
				(this.Type == "Helmet") ||
				(this.Type == "Torso") ||
				(this.Type == "Gauntlet") ||
				(this.Type == "Bracers") ||
				(this.Type == "Brooch") ||
				(this.Type == "Mask");

			float mult = 1.0f;
			int maxR = 0;
			int maxStatLevel = 0;
			
			// fix this if we do more than armor
			if (this.Quality == "Ult++") { mult = 1.4f; maxR = 29; maxStatLevel = 999; }
			else if (this.Quality == "Ult+") { mult = 1.4f; maxR = 29; maxStatLevel = 700; }
			else if (this.Quality == "Ult93") { mult = 1.4f; maxR = 29; maxStatLevel = 600; }
			else if (this.Quality == "Ult90") { mult = 1.4f; maxR = 29; maxStatLevel = 600; }
			else if (this.Quality == "Supreme") { mult = 1.3666f; maxR = 30; maxStatLevel = 500; }
			else if (this.Quality == "Transcendent") { mult = 1.3333f; maxR = 31; maxStatLevel = 420; }
			else if (this.Quality == "Mythical") { mult = 1.3f; maxR = 31; maxStatLevel = 360; }
			else { mult = 1.25f; maxR = 31; maxStatLevel = 300; }

			if (!this.bIsArmor)
			{
				mult = 1.0f;
				maxR = 0;
			}


			cachedItemRow = new ItemViewRow(
				Rating: 0,
				Sides: 0,
				Name: name,
				Location: this.Location,//this.Description + $"{Color1.R},{Color1.G},{Color1.B},{Color1.A} -  {Color2.R},{Color2.G},{Color2.B},{Color2.A}" ?? "",
				Quality: this.Quality ?? "",
				Type: this.Type ?? "",
				Set: this.Set ?? "",
				Level: this.Level,
				MaxLevel: this.MaxLevel,

				HHP: Stat(this, DDStat.HeroHealth),
				HDmg: Stat(this, DDStat.HeroDamage),
				HSpd: Stat(this, DDStat.HeroSpeed),
				HRate: Stat(this, DDStat.HeroCastingRate),
				Ab1: Stat(this, DDStat.HeroAbility1),
				Ab2: Stat(this, DDStat.HeroAbility2),

				THP: Stat(this, DDStat.TowerHealth),
				TDmg: Stat(this, DDStat.TowerDamage),
				TRange: Stat(this, DDStat.TowerRange),
				TRate: Stat(this, DDStat.TowerRate),

				RG: Resist(this, 0),
				RP: Resist(this, 1),
				RF: Resist(this, 2),
				RL: Resist(this, 3),
				Idx: this.Idx,
				BestAvailable: Idx % 2,
				Value: EventItemValue,
				BestFor: "",

				IsEquipped: this.bIsEquipped,
				IsMissingResists: this.bIsMissingResists,
				IsEvent: this.bIsEvent,
				IsArmor: this.bIsArmor,
				IsEligibleForBest: bIsEligibleForBest,

				SetBonus: mult,
				MaxStat: maxStatLevel,
				ResistanceTarget: maxR,
				FunHashString: this.FunHashString,
				Color1: IconColor1.ToCString(),
				Color2: IconColor2.ToCString(),

				IndexInFolder: IndexInFolder,
				IconX: IconX,
				IconY: IconY,
				IconX1: IconX1,
				IconY1: IconY1, 
				IconX2: IconX2,
				IconY2: IconY2
			);
		}
		return cachedItemRow;
	}
};


public class DDHeroInfo
{
	public bool IsInitialized;

	public string Name = "";
	public int HeroLevel;
	public int HeroExperience;
	public int ManaPower;
	public int[] Stats = new int[10];
	public int CostumeIndex;
	public int GUID1, GUID2, GUID3, GUID4;
	public DDLinearColor C1, C2, C3;
	public byte bDidRespec, bGaveExpBonus, bAllowRename;

	public string Template = "";
	public int EquipmentCount;
};

public class DDDatabase
{
	// this should notify something when changed
	public string Status = "Unloaded";
	public bool IsReady = false;
	public List<DDHeroInfo> Heroes = [];
	public List<DDEquipmentInfo> Items = [];
	public Dictionary<int, DDFolder> ItemBoxFolders = [];
	public Dictionary<int, DDFolder> ShopFolders = [];
	public Dictionary<string, int> SubfolderCount = [];

	private const uint UE3_COMPRESSED_TAG = 0x9E2A83C1;
	public async Task LoadFromDun(byte[] byteArray)
	{
		IsReady = false;
		Status = "Unloaded";
		Heroes.Clear();
		Items.Clear();
		ItemBoxFolders.Clear();
		ShopFolders.Clear();
		SubfolderCount.Clear();

		MemoryStream? outputMemoryStream = UnpackUE3Archive(byteArray);

		if (outputMemoryStream == null)
			return;

		Status = "Unpacked UE3 Archive";
		await Task.Yield();

		var reader = new BinaryReader(outputMemoryStream);
		
		ReadOptions(reader);
		int heroCount = reader.ReadInt32();
		Status = $"Reading Heroes ({heroCount})";
		await Task.Yield();

		for (int i = 0; i < heroCount; i++)
		{
			DDHeroInfo hero = ReadHero(reader);
			if (hero == null) { Status = "Load: Failed to parse heroes"; return; }

			int eCount = hero.EquipmentCount;
			for (int j = 0; j < eCount; j++)
			{
				//Console.WriteLine(j);
				DDEquipmentInfo equipment = ReadEquipment(reader);
				equipment.Location = "Character > " + hero.Name;
				equipment.bIsEquipped = true;
				if (equipment == null) { Status = "Load: Failed to parse equipment"; return; }				
				Items.Add(equipment);
			}
			Heroes.Add(hero);
		}
		Status = $"Reading Items";
		await Task.Yield();
		// Discard achievements, core unlocks, core options

		reader.ReadBytes(500);
		reader.ReadBytes(40);
		reader.ReadInt32();
		for (int i = 0; i < 3; i++) 
			ReadLinearColor(reader);


		// Beaten/Unlocked levels
		ReadLevelProgressList(reader); // Beaten
		ReadLevelProgressList(reader); // Unlocked

		// ItemBoxInfo
		reader.ReadInt32(); // StoredMana

		// 12 extra bytes - not sure what these are
		reader.ReadInt32();
		reader.ReadInt32();
		reader.ReadInt32();

		// Equipment arrays
		int equipCount = reader.ReadInt32();
		Status = $"Reading Items {equipCount}";
		for (int i = 0; i < equipCount; i++)
		{
			var equipment = ReadEquipment(reader);
			if (equipment == null) { Status = "Load: Failed to parse equipment for item box"; return; }
			equipment.Location = "ItemBox";
			Items.Add(equipment);
			if (i % 100 == 0)
			{
				Status = $"Load: Loading ItemBox... {i} / {equipCount}";
				await Task.Yield();
			}
		}
		equipCount = reader.ReadInt32();
		for (int i = 0; i < equipCount; i++)
		{
			var equipment = ReadEquipment(reader);
			if (equipment == null) { Status = "Load: Failed to parse equipment list"; return; }
			equipment.Location = "HeroEquipment?";
			Items.Add(equipment);
		}
		equipCount = reader.ReadInt32();
		for (int i = 0; i < equipCount; i++)
		{
			var equipment = ReadEquipment(reader);
			if (equipment == null) { Status = "Load: Failed to parse equipment for lobby"; return; }
			equipment.Location = "Tavern";
			Items.Add(equipment);
			if ( i % 100 == 0)
			{
				Status = $"Load: Loading Lobby... {i} / {equipCount}";
				await Task.Yield();
			}
		}
		// Shopkeeper sales- we just discard
		int shopSetCount = reader.ReadInt32();
		for (int i = 0; i < shopSetCount; i++)
		{
			equipCount = reader.ReadInt32();
			for (int j = 0; j < equipCount; j++)
			{
				var equipment = ReadEquipment(reader);
				if (equipment == null) { Status = "Load: Failed to parse shopkeeper equipment"; return; }				
			}
		}

		// What player sells
		equipCount = reader.ReadInt32();
		for (int j = 0; j < equipCount; j++)
		{
			var equipment = ReadEquipment(reader);
			if (equipment == null) { Status = "Load: Failed to parse shop equipment"; return; }
			equipment.UserSellPrice = reader.ReadInt32();
			equipment.Location = "Shop";
			Items.Add(equipment);
		}

		ReadFolderArray(reader, ItemBoxFolders);
		ReadFolderArray(reader, ShopFolders);

		BuildItemLocations("ItemBox", ItemBoxFolders);
		BuildItemLocations("Shop", ShopFolders);

		SubfolderCount.Clear();

		CalculateSubFolderCounts("ItemBox", ItemBoxFolders);
		CalculateSubFolderCounts("Shop", ShopFolders);

		BuildStringsFromData();

		// Build Item Locations
		// Build Item Names
		int numItems = Items.Count;
		int numHeroes = Heroes.Count;
		Status = $"Successfully loaded {numHeroes} heroes and {numItems} items";
		IsReady = true;
	}

	// ------------------ DERIVED DATA ----------------------
	private void BuildStringsFromData()
	{
		List<string> AllEventHashes = new List<string>();

		for (int i = 0; i < Items.Count; i++)
		{
			var itemInfo = Items[i];
			string quality = "Unknown";
			if (itemInfo.NameQualityIdx == 19) quality = "Ult++";
			else if (itemInfo.NameQualityIdx == 18) quality = "Ult+";
			else if (itemInfo.NameQualityIdx == 17) quality = "Ult93";
			else if (itemInfo.NameQualityIdx == 16) quality = "Ult90";
			else if (itemInfo.NameQualityIdx == 15) quality = "Supreme";
			else if (itemInfo.NameQualityIdx == 14) quality = "Transcendent";
			else if (itemInfo.NameQualityIdx == 13) quality = "Mythical";
			else if (itemInfo.NameQualityIdx == 12) quality = "Cursed";
			else if (itemInfo.NameQualityIdx == 11) quality = "Torn";
			else if (itemInfo.NameQualityIdx == 10) quality = "Worn";
			else if (itemInfo.NameQualityIdx == 9) quality = "Stocky";
			else if (itemInfo.NameQualityIdx == 6) quality = "Polished";
			else if (itemInfo.NameQualityIdx == 1) quality = "Legendary";
			else if (itemInfo.NameQualityIdx == 0) quality = "Godly";

			string type = "Unknown";
			string set = "";
			bool isArmor = false;
			//Items[i].Description = "Unknown";
			Items[i].GeneratedName = (Items[i].ForgerName != "")? Items[i].ForgerName : Items[i].Template;

			Items[i].Quality = quality;
			Items[i].Idx = i;

			DDLinearColor IconColor1 = Items[i].Color1;
			DDLinearColor IconColor2 = Items[i].Color2;

			if (ItemTemplateInfo.Map.ContainsKey(itemInfo.Template))
			{
				ItemEntry entry = ItemTemplateInfo.Map[itemInfo.Template];
		
				if (entry.EquipmentType == EquipmentType.Weapon) { type = "Weapon"; }
				else if (entry.EquipmentType == EquipmentType.Helmet) { type = "Helmet"; isArmor = true; }
				else if (entry.EquipmentType == EquipmentType.Torso) { type = "Torso"; isArmor = true; }
				else if (entry.EquipmentType == EquipmentType.Gloves) { type = "Gauntlet"; isArmor = true; }
				else if (entry.EquipmentType == EquipmentType.Boots) { type = "Boots"; isArmor = true; }
				else if (entry.EquipmentType == EquipmentType.Bracers) { type = "Bracers"; }
				else if (entry.EquipmentType == EquipmentType.Brooch) { type = "Brooch"; }
				else if (entry.EquipmentType == EquipmentType.Mask) { type = "Mask"; }
				else if (entry.EquipmentType == EquipmentType.Shield) { type = "Shield"; }
				else if (entry.EquipmentType == EquipmentType.Familiar) { type = "Pet"; }
				else if (entry.EquipmentType == EquipmentType.Currency) { type = "Currency"; }

				if ((Items[i].Level == 1)  && (Items[i].MaxLevel == 1) &&
					(Items[i].Stats[1] == 0) && (Items[i].Stats[2] == 0) &&
					(Items[i].Stats[3] == 0) &&	(Items[i].Stats[4] == 0) &&
					(Items[i].Stats[5] == 0) && (Items[i].Stats[6] == 0) &&
					(Items[i].Stats[7] == 0) && (Items[i].Stats[8] == 0) &&
					(Items[i].Stats[9] == 0) && (Items[i].Stats[10] == 0))
				{
					type = "Currency";
				}
				Items[i].bIsArmor = isArmor;


				if (entry.EquipmentSet == EquipmentSet.None) { set = ""; }
				else if (entry.EquipmentSet == EquipmentSet.Leather) { set = "Leather"; }
				else if (entry.EquipmentSet == EquipmentSet.Pristine) { set = "Pristine"; }
				else if (entry.EquipmentSet == EquipmentSet.Plate) { set = "Plate"; }
				else if (entry.EquipmentSet == EquipmentSet.Mail) { set = "Mail"; }
				else if (entry.EquipmentSet == EquipmentSet.Chain) { set = "Chain"; }
				else if (entry.EquipmentSet == EquipmentSet.Zamira) { set = "Zamira"; }
				else if (entry.EquipmentSet == EquipmentSet.Any) { set = "Any"; }

				if (entry.WeaponType == WeaponType.Squire) { set = "Squire"; }
				else if (entry.WeaponType == WeaponType.Huntress) { set = "Huntress"; }
				else if (entry.WeaponType == WeaponType.Apprentice) { set = "Apprentice"; }
				else if (entry.WeaponType == WeaponType.Monk) { set = "Monk"; }


				if (Items[i].Description.Trim() == "")
					Items[i].Description = entry.Description;
				if ((entry.Names.Count > 0) && (Items[i].NameVariantIdx < entry.Names.Count))
				{
					Items[i].GeneratedName = entry.Names[Items[i].NameVariantIdx];
					if (Items[i].GeneratedName == "") Items[i].GeneratedName = Items[i].Template;
				}

				Items[i].IconX = entry.IconX;
				Items[i].IconY = entry.IconY;
				Items[i].IconX1 = entry.IconX1;
				Items[i].IconY1 = entry.IconY1;
				Items[i].IconX2 = entry.IconX2;
				Items[i].IconY2 = entry.IconY2;

				
				if ((Items[i].Color1.A == 1) && (Items[i].Color1.R == 0) && (Items[i].Color1.G == 0) && (Items[i].Color1.B == 0))
				{
					// use color set
					int colorSet = Items[i].SecondaryColorSet;
					if ((entry.SecondaryColorSets != null) && (entry.SecondaryColorSets.Count > 0))
					{
						if ((colorSet < 0) || (colorSet >= entry.SecondaryColorSets.Count)) colorSet = 0;

						IconColor1 = entry.SecondaryColorSets[colorSet];
						IconColor1.Scale(entry.IconColorMulSecondary);
						IconColor1.Add(entry.IconColorAddSecondary);
					}
				}
				else // override
				{
					IconColor1.Add(entry.IconColorAddSecondary);
				}

				if ((Items[i].Color2.A == 1) && (Items[i].Color2.R == 0) && (Items[i].Color2.G == 0) && (Items[i].Color2.B == 0))
				{
					// use color set
					int colorSet = Items[i].PrimaryColorSet;
					if ((entry.PrimaryColorSets != null) && (entry.PrimaryColorSets.Count > 0))
					{
						if ((colorSet < 0) || (colorSet >= entry.PrimaryColorSets.Count)) colorSet = 0;

						IconColor2 = entry.PrimaryColorSets[colorSet];
						IconColor2.Scale(entry.IconColorMulPrimary);
						IconColor2.Add(entry.IconColorAddPrimary);
					}
				}
				else // override
				{
					IconColor2.Add(entry.IconColorAddPrimary);
				}			
			}
			Items[i].IconColor1 = IconColor1;
			Items[i].IconColor2 = IconColor2;

			int c1r = (int)Items[i].Color1.R * 100;
			int c1g = (int)Items[i].Color1.G * 100;
			int c1b = (int)Items[i].Color1.B * 100;
			int c1a = (int)Items[i].Color1.B * 100;
			int c2r = (int)Items[i].Color2.R * 100;
			int c2g = (int)Items[i].Color2.G * 100;
			int c2b = (int)Items[i].Color2.B * 100;
			int c2a = (int)Items[i].Color1.B * 100;
			int flags = ((Items[i].bNoSell > 0 ? 1 : 0) + (Items[i].bNoDrop > 0 ? 2 : 0)) + (Items[i].Stats[0] << 4);

			// in the future use stat0 and not maxlevel
			string dataForHash = $"{Items[i].Description}.{Items[i].Template}.{(Items[i].MaxLevel < 2000 ? Items[i].MaxLevel:0)}.{c1r}{c1g}{c1b}{c1a}.{c2r}{c2g}{c2b}{c2a}.{Items[i].NameVariantIdx}{Items[i].NameQualityIdx}{Items[i].NameResistIdx}.{flags}";

			uint hash = ItemHash.StringToInt30(dataForHash);
			
			Items[i].FunHashString = ItemHash.Int30ToPhrase(hash);
			

			string gen = Items[i].GeneratedName.Trim();		

			// exclude magicite

			if ((( gen == "Crystal Heart") || (gen == "Diamond"))&& (Items[i].EventItemValue == 0))
			{
				bool cappedDamage = (Items[i].Stats[(int)DDStat.TowerDamage] == 800);
				int cappedStats = ((Items[i].Stats[(int)DDStat.TowerHealth] == 800) ? 1 : 0) +
									((Items[i].Stats[(int)DDStat.TowerRange] == 800) ? 1 : 0) +
									((Items[i].Stats[(int)DDStat.TowerDamage] == 800) ? 1 : 0 )+
								((Items[i].Stats[(int)DDStat.TowerRate] == 800) ? 1 : 0);
								

				if (cappedStats == 0) Items[i].EventItemValue = 5;
				else if ((cappedStats == 1) && cappedDamage) Items[i].EventItemValue = 10;
				else if ((cappedStats == 2) && cappedDamage)  Items[i].EventItemValue = 15;
				else if ((cappedStats == 3) && cappedDamage)  Items[i].EventItemValue = 300;
				else if (cappedStats == 4) Items[i].EventItemValue = 1000; // magicite likely
			}
			
			Items[i].Type = type;
			Items[i].Set = set;

			Items[i].bIsEvent = (EventPriceGuide.HashToEventInfo.ContainsKey(hash));

			string n = (Items[i].UserEquipName.Trim() == "") ? Items[i].GeneratedName : Items[i].UserEquipName;

			/*
			if (n.StartsWith("Dragon"))
			{
				Console.WriteLine("Dragon : " + Items[i].Description);
			}
			if (Items[i].FunHashString == "Grim Tactical Monk Explodes Luminous Destroyer")
			{
				Console.WriteLine("Dragon : " + Items[i].Description); 
			}
				string n = (Items[i].UserEquipName.Trim() == "") ? Items[i].GeneratedName : Items[i].UserEquipName;
				string s = n + ", \"" + Items[i].FunHashString + "\"";
				if (!AllEventHashes.Contains(s) && (n.Contains("Speedy Cloud Bracers")))
				{
					AllEventHashes.Add(s);
					Console.WriteLine(s);
				}
			*/

			if (Items[i].bIsEvent)
			{
				Items[i].EventItemValue = EventPriceGuide.HashToEventInfo[hash].Price;
			}
			Items[i].bIsMissingResists = isArmor && ((Items[i].ResistAmt[0] == 0) || (Items[i].ResistAmt[1] == 0) || (Items[i].ResistAmt[2] == 0) || (Items[i].ResistAmt[3] == 0));
			//Console.WriteLine(Items[i].Template+ " " + Items[i].UserEquipName + " " +  Items[i].Color1.R+ " " + Items[i].Color1.G + " " + Items[i].Color1.B+ " " + Items[i].Color1.R+ " " + Items[i].Color1.G+ " " + Items[i].Color1.B);
		}
		var itemsByLocation = Items
			.GroupBy(x => x.Location)
			.ToDictionary(
				g => g.Key,
				g => g.OrderByDescending(x => x.MaxLevel).ToList()
			);

		for (int i = 0; i < Items.Count; i++)
		{
			var currentItem = Items[i];			
			// Look up the pre-sorted list for this specific location
			Items[i].IndexInFolder = itemsByLocation[currentItem.Location].IndexOf(currentItem);			
		}
	}



	public void CalculateSubFolderCounts(string inventory, Dictionary<int, DDFolder> folders)
	{
		
		foreach (var folder in folders.Values)
		{
			// 1. Construct the hierarchy name (e.g., "GrandParent > Parent > Child")
			string path = GetFolderPath(folder.Index, folders, inventory);

			// 2. Count children: How many folders have this folder as their ParentIndex?
			int childrenCount = folders.Values.Count(f => f.ParentIndex == folder.Index);

			SubfolderCount[path] = childrenCount;
		}
	}

	// ------------------ LOCATIONS ----------------------
	private void BuildItemLocations( string inventory, Dictionary<int, DDFolder> folders )
	{
		for (int i = 0; i < Items.Count; i++)
		{
			if (Items[i].Location == inventory)
			{
				string FolderName = GetFolderPath(Items[i].FolderID, folders, inventory);
				Items[i].Location = FolderName;
			}
		}
	}

	private string GetFolderPath(int folderID, Dictionary<int, DDFolder> folderDictionary, string baseName)
	{
		if (folderID == -1) return baseName;
		if (!folderDictionary.ContainsKey(folderID)) return "";

		string parentPath = GetFolderPath(folderDictionary[folderID].ParentIndex, folderDictionary, baseName);
		string myName = folderDictionary[folderID].Name;
		return (parentPath == "") ? myName : parentPath + " > " + myName;
	}

	// ------------------ PARSE HERO ----------------------
	private DDHeroInfo ReadHero(BinaryReader reader )
	{
		DDHeroInfo h = new DDHeroInfo();
		h.IsInitialized = reader.ReadByte() > 0 ? true : false;

		for (int i = 0; i < 10; i++) h.Stats[i] = reader.ReadInt32();

		// 41 bytes until here 
		h.HeroLevel = reader.ReadInt32();        // 0x9f1
		h.HeroExperience = reader.ReadInt32();   // 0x9f5
		h.ManaPower = reader.ReadInt32();        // 0x9f9

		h.GUID1 = reader.ReadInt32(); // 0x9fd
		h.GUID2 = reader.ReadInt32(); // 0xa01
		h.GUID3 = reader.ReadInt32(); // 0xa05
		h.GUID4 = reader.ReadInt32(); // 0xa09

		h.CostumeIndex = reader.ReadInt32(); // 0xa0d

		h.C1 = ReadLinearColor(reader);
		h.C2 = ReadLinearColor(reader);
		h.C3 = ReadLinearColor(reader);

		h.bDidRespec = reader.ReadByte(); // 0xa41
		h.bGaveExpBonus = reader.ReadByte(); // 0xa42
		h.bAllowRename = reader.ReadByte(); // 0xa43

		h.Name = ReadFString(reader); // 0xa44
		h.Template = ReadFString(reader);

		// discard hotkeys
		for (int i = 0; i < 10; i++) ReadFString(reader);

		_ = reader.ReadInt32(); // unsure

		h.EquipmentCount = reader.ReadInt32(); // 0xb5f in test

		return h;
	}

	// ------------------ PARSE EQUIPMENT ----------------------
	private DDEquipmentInfo ReadEquipment(BinaryReader reader)
	{
		var e = new DDEquipmentInfo();

		e.IsInitialized = reader.ReadByte() < 1 ? false : true; // 0xb63

		for (int i = 0; i < 4; i++)
			e.ResistIdx[i] = reader.ReadByte();

		for (int i = 0; i < 4; i++)
			e.ResistAmt[i] = reader.ReadByte() - 127;

		// is this resists?
		
		for (int i = 0; i < 24; i++) e.Stats[i] = reader.ReadInt32() - 127; // 0xb6c- 0xbcb, +127

		e.WeaponDamageBonus = reader.ReadInt32(); // 0xbcc 			 
		e.WeaponProjectiles = reader.ReadByte(); //
		e.WeaponSpeedType = reader.ReadByte(); // 

		//e.WeaponAdditionalDamage = reader.ReadInt32(); // where does this live?

		e.DrawScale = reader.ReadSingle(); // 0xbd2
		e.SwingSpeed = reader.ReadSingle(); // 0xbd6

		// Many DD files store Level as int32.
		e.Level = reader.ReadInt32(); // 0xbda

		e.StoredMana = reader.ReadInt32(); // 0xbde

		for (int i = 0; i < 7; i++)
		{
			e.WeaponBonuses[i] = reader.ReadByte();
		}
		for (int i = 0; i < 14; i++)
			_ = reader.ReadByte();


		e.NameVariantIdx = reader.ReadByte();
		e.NameResistIdx = reader.ReadByte();
		e.NameQualityIdx = reader.ReadByte();

		e.PrimaryColorSet = reader.ReadByte(); // 0xbfa
		e.SecondaryColorSet = reader.ReadByte(); // 0xbfb

		// 26 bytes from b32

		e.ID1 = reader.ReadInt32();  // 0xbfc
		e.ID2 = reader.ReadInt32();  // 0xc00

		e.MinSell = reader.ReadInt32(); // 0xc04
		e.MaxSell = reader.ReadInt32(); // 0xc08
		e.MaxLevel = reader.ReadInt32(); // 0xc0c

		e.LocX = reader.ReadInt32(); // 0xc10
		e.LocY = reader.ReadInt32();
		e.LocZ = reader.ReadInt32();

		e.bUpgradable = reader.ReadByte(); // 0xc1c
		e.bRenameAtMax = reader.ReadByte();
		e.bNoDrop = reader.ReadByte();
		e.bNoSell = reader.ReadByte();
		e.bAutoLock = reader.ReadByte();
		e.bOnceEffect = reader.ReadByte();
		e.bLocked = reader.ReadByte();
		e.ManualLR = reader.ReadByte();

		e.Color1 = ReadLinearColor(reader); // 0xc24
		e.Color2 = ReadLinearColor(reader); // 0xc34

		e.UserEquipName = ReadFString(reader); // 0xc44
		e.ForgerName = ReadFString(reader);   // 0xc4a
		e.Description = ReadFString(reader);   // 0xc55
		e.Template = ReadFString(reader);      // 0xc59
		_ = ReadFString(reader); // timestamp

		e.FolderID = reader.ReadInt32();
		e.bIsSecondary = reader.ReadByte() > 0 ? true : false;
		
		_ = reader.ReadBytes(40); // equipment_ids
		_ = reader.ReadBytes(40); // equipment_tiers
		_ = ReadLinearColor(reader); // linear color
		_ = ReadFString(reader); // feature string

		_ = reader.ReadByte(); // hide quality descriptor
		_ = reader.ReadByte(); // feature byte 1
		_ = reader.ReadByte(); // feature byte 2

		_  = reader.ReadBytes(40);  // feature array

		return e;
	}

	// ------------------ PARSE FOLDER ----------------------

	private void ReadFolderArray( BinaryReader r, Dictionary<int, DDFolder> folders )
	{
		int count = r.ReadInt32();
		for (int i = 0; i < count; i++)
		{
			DDFolder folder = new();
			folder.ParentIndex = r.ReadInt32(); // parent folder
			folder.Index = r.ReadInt32(); // folder
			folder.Name = ReadFString(r); // name
			folders.Add(folder.Index, folder);
			r.ReadByte(); // ? 
		}
	}

	private void ReadOptions(BinaryReader reader)
	{ 
		// Version info
		_ = reader.ReadInt32();
		_ = reader.ReadInt32();

		// Options
		for (int i = 0; i < 5; i++) reader.ReadByte();        // Bools
		for (int i = 0; i < 10; i++) reader.ReadInt32();      // 0x00d - ShownTutorials[10]
		for (int i = 0; i < 4; i++) reader.ReadSingle();      // 0x035 - Volumes
		for (int i = 0; i < 3; i++) reader.ReadByte();        // 0x045 - Voice bools
		for (int i = 0; i < 3; i++) reader.ReadSingle();      // 0x048 - Gamma/Scale
		for (int i = 0; i < 3; i++) reader.ReadByte();        // 0x054 - Camera bools

		reader.ReadInt32();                                   // 0x057 - Fullscreen + config?
		reader.ReadByte();                                    // 0x05b - Difficulty		 
		reader.ReadByte();                                    // 0x05c - Lock 
		reader.ReadByte();                                    // 0x05d - Chase

		for (int i = 0; i < 3; i++) reader.ReadSingle();      // 0x05e - Camera dist/speed

		reader.ReadInt32();                                   // 0x06a - MinLevel
		reader.ReadInt32();                                   // 0x06e - ?
		reader.ReadInt32();                                   // 0x072 - ?
		reader.ReadInt32();                                   // 0x076 - ?
		reader.ReadByte();                                    // 0x07a - ?

		ReadAndDiscardByteArray(reader);                                // 0x07b (test) - CustomGameMetaFlags
		ReadAndDiscardIntArray(reader);                                 // 0x864 (test) - CustomUnlocks  
		ReadAndDiscardIntArray(reader);                                 // 0x93c (test) - HeroUnlocks

		// OptionsInfo strings
		ReadFString(reader);                                 // 0x96c (test) - resolution
		ReadFString(reader);                                 // lastLevelTag
		ReadFString(reader);                                 // Username
		ReadFString(reader);                                 // Password

		// SearchFilterSettings
		ReadAndDiscardIntArray(reader);                                 // Level indices
		ReadAndDiscardIntArray(reader);                                 // Difficulties
		for (int i = 0; i < 9; i++) reader.ReadByte();        // Filter bytes

		ReadAndDiscardIntArray(reader);                                 // installedDLCEquipments
	}

	private MemoryStream? UnpackUE3Archive(byte[] byteArray)
	{
		//---------------------------------
		// Find zip file within .dun file
		//----------------------------------
		using var ms = new MemoryStream(byteArray);
		using var reader = new BinaryReader(ms);

		// look for where the UE compressed tag starts
		long tagPos = FindAlignedTag(reader, UE3_COMPRESSED_TAG, scanBytes: 1024);
		if (tagPos < 0)
		{
			Status = "Preload: File does not appear to be a .dun file";
			return null;
		}

		ms.Position = tagPos + 4;		
		int blockSize = reader.ReadInt32();
		int totalCompressed = reader.ReadInt32();
		int totalUncompressed = reader.ReadInt32();
		int blockCount = (totalUncompressed + blockSize - 1) / blockSize;
		var blockSizes = new (int compressed, int uncompressed)[blockCount];

		for (int i = 0; i < blockCount; i++)
		{
			int compressedBlockSize = reader.ReadInt32();
			int uncompressedBlockSize = reader.ReadInt32();

			if (compressedBlockSize <= 0 || uncompressedBlockSize <= 0 || uncompressedBlockSize > blockSize)
			{
				Status = "Preload: Malformed compressed section";
				return null;
			}

			blockSizes[i] = (compressedBlockSize, uncompressedBlockSize);
		}

		var outMemoryStream = new MemoryStream(totalUncompressed);
		for (int i = 0; i < blockCount; i++)
		{
			var (compressedBlockSize, uncompressedBlockSize) = blockSizes[i];

			byte[] compressedBytes = reader.ReadBytes(compressedBlockSize);
			if (compressedBytes.Length != compressedBlockSize)
			{
				Status = "Preload: Mismatch reading compressed block";
				return null;
			}

			byte[] decodedBytes = DecompressZlib(compressedBytes);

			// Critical sanity check: match expected block size to avoid silent desync.
			if (decodedBytes.Length != uncompressedBlockSize)
			{
				Status = "Preload: Decoded size does not match header";
				return null;
			}

			outMemoryStream.Write(decodedBytes, 0, decodedBytes.Length);
		}

		if (outMemoryStream.Length != totalUncompressed)
		{
			Status = $"Preload: Uncompressed file does not match header:  {outMemoryStream.Length}!={totalUncompressed} {blockCount} {blockSize}";
			return null;
		}

		outMemoryStream.Position = 0;
		return outMemoryStream;
	}
	
	//---------------------- UTILITY FUNCTIONS -------------------------
	private static long FindAlignedTag(BinaryReader br, uint tag, int scanBytes)
	{
		long start = br.BaseStream.Position;
		long end = Math.Min(br.BaseStream.Length, start + scanBytes);

		for (long pos = start; pos + 4 <= end; pos += 4)
		{
			br.BaseStream.Position = pos;
			if (br.ReadUInt32() == tag)
			{
				br.BaseStream.Position = start;
				return pos;
			}
		}

		br.BaseStream.Position = start;
		return -1;
	}

	private static byte[] DecompressZlib(byte[] zlibData)
	{
		using var inputStream = new MemoryStream(zlibData);
		using var zlib = new ZLibStream(inputStream, CompressionMode.Decompress, leaveOpen: false);
		using var outputStream = new MemoryStream();
		zlib.CopyTo(outputStream);
		return outputStream.ToArray();
	}

	private static void ReadAndDiscardIntArray(BinaryReader r)
	{
		int count = r.ReadInt32();
		for (int i = 0; i < count; i++) r.ReadInt32();
	}

	private static void ReadAndDiscardByteArray(BinaryReader r)
	{
		int count = r.ReadInt32();
		r.ReadBytes(count);
	}

	private DDLinearColor ReadLinearColor(BinaryReader r)
	{
		DDLinearColor color = new DDLinearColor();
		color.R = r.ReadSingle();
		color.G = r.ReadSingle();
		color.B = r.ReadSingle();
		color.A = r.ReadSingle();
		return color;
	}

	public static string ReadFString(BinaryReader br)
	{
		int len = br.ReadInt32();
		if (len == 0) return string.Empty;

		if (len < 0)
		{
			int charCount = -len;             // includes null terminator
			int byteCount = charCount * 2;
			byte[] data = br.ReadBytes(byteCount);
			if (data.Length != byteCount) throw new EndOfStreamException();
			return Encoding.Unicode.GetString(data).TrimEnd('\0');
		}
		else
		{
			// UE3 "ANSI" is single-byte. Latin1 preserves bytes 0x80-0xFF (ASCII would mangle them).
			byte[] data = br.ReadBytes(len);     // includes null terminator
			if (data.Length != len) throw new EndOfStreamException();
			return Encoding.Latin1.GetString(data).TrimEnd('\0');
		}
	}

	private static void ReadLevelProgressList(BinaryReader r)
	{
		int count = r.ReadInt32();
		for (int i = 0; i < count; i++)
		{
			ReadFString(r);
			r.ReadInt32();
		}
	}

}



