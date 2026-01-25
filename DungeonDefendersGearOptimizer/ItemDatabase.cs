namespace DDUP
{
	public enum WeaponType
	{
		None = 0,
		Squire,
		Monk,
		Huntress,
		Apprentice
	};

	public enum EquipmentType
	{
		Weapon = 0,
		Torso = 1,
		Helmet = 2,
		Boots = 3,
		Gloves = 4,
		Familiar = 5,
		Brooch = 7, // Brooch
		Bracers = 8, // Bracers
		Shield = 9, // Shield
		Mask = 10,
		Currency = 11, 
	}

	public enum EquipmentSet
	{
		None = 0,
		Leather = 1,
		Mail = 2,
		Chain = 3,
		Plate = 4, 		
		Pristine = 5,	
		Zamira = 6,		
		Any = 255,
	}

	public class ItemEntry
	{
		public List<string> Names = [];
		public string Description = "";
		public string CustomCategory = "";
		public string BaseForgerName = "";
		public int IconX = 0;
		public int IconY = 0;
		public int IconX1 = 0;
		public int IconY1 = 0;
		public int IconX2 = 0;
		public int IconY2 = 0;
		public DDLinearColor IconColorAddPrimary;
		public DDLinearColor IconColorAddSecondary;
		public float IconColorMulPrimary = 1.0f;
		public float IconColorMulSecondary = 1.0f;
		public List<DDLinearColor> PrimaryColorSets = new();
		public List<DDLinearColor> SecondaryColorSets = new();

		public EquipmentSet EquipmentSet = EquipmentSet.None;
		public EquipmentType EquipmentType = EquipmentType.Weapon;
		public WeaponType WeaponType = WeaponType.None;
	}

}
