using DDUP;

namespace DDUP
{

	public static class ImportMaps
	{
		static public Dictionary<string, WeaponType> _weaponType = new()
		{
			{ "EWT_WEAPON_APPRENTICE", WeaponType.Apprentice },
			{ "EWT_WEAPON_INITIATE",   WeaponType.Huntress },
			{ "EWT_WEAPON_RECURIT",    WeaponType.Monk },
			{ "EWT_WEAPON_SQUIRE",     WeaponType.Squire },
		};

		static public Dictionary<string, EquipmentType> _EquipmentType = new()
		{
			{ "EQT_WEAPON",         EquipmentType.Weapon },
			{ "EQT_ARMOR_TORSO",    EquipmentType.Torso },
			{ "EQT_ARMOR_PANTS",    EquipmentType.Helmet },
			{ "EQT_ARMOR_BOOTS",    EquipmentType.Boots},
			{ "EQT_ARMOR_GLOVES",   EquipmentType.Gloves},
			{ "EQT_FAMILIAR",       EquipmentType.Familiar},
			{ "EQT_ACCESSORY1",     EquipmentType.Brooch},
			{ "EQT_ACCESSORY2",     EquipmentType.Bracers},
			{ "EQT_ACCESSORY3",     EquipmentType.Shield},
			{ "EQT_MASK",           EquipmentType.Mask},

		};

		static Dictionary<string, EquipmentSet> _EquipmentSet = new()
		{
			{ "0", EquipmentSet.Any },
			{ "1", EquipmentSet.Leather },
			{ "2", EquipmentSet.Mail },
			{ "3", EquipmentSet.Chain },
			{ "4", EquipmentSet.Plate },
			{ "5", EquipmentSet.Pristine },
			{ "6", EquipmentSet.Zamira },
		};

		// MAP: ULinearColor => DDLinearColor
	}
 
	// DDLinearColor: LinearColor	

	public partial class HeroEquipment
	{		
		static public Dictionary<string, HeroEquipment> Lookup = new Dictionary<string, HeroEquipment>();
		public string Template = "";
					
		public string _EquipmentName = "";
		public string _EquipmentDescription = "";
		public List<string> _RandomBaseNames = [];
		public bool _AllowNameRandomization = false;

		public EquipmentSet _EquipmentSetID = EquipmentSet.None;
		public EquipmentType _EquipmentType = EquipmentType.Weapon;
		public WeaponType _weaponType = WeaponType.None;

		public bool _CountsForAllArmorSets = false;
		public string _BaseForgerName = "";
		public DDLinearColor _IconColorAddPrimary;
		public DDLinearColor _IconColorAddSecondary;
		public float _IconColorMulPrimary = 1.0f;
		public float _IconColorMulSecondary = 1.0f;

		public bool _UseColorSets = false;
		public List<DDLinearColor>? _PrimaryColorSets;
		public List<DDLinearColor>? _SecondaryColorSets;
		
		public int IconX = 0;
		public int IconY = 0;
		public int IconX1 = 0;
		public int IconY1 = 0;
		public int IconX2 = 0;
		public int IconY2 = 0;			
	}

	public partial class HeroEquipment_Familiar : HeroEquipment
	{
	}

	public partial class HeroEquipment_Familiar_AoeBuffer : HeroEquipment
	{
	}

	public partial class HeroEquipment_Familiar_Buff_Spawner : HeroEquipment
	{
	}

	public partial class HeroEquipment_Familiar_CoreHealer : HeroEquipment
	{
	}

	public partial class HeroEquipment_Familiar_Melee : HeroEquipment
	{
	}

	public partial class HeroEquipment_Familiar_MiniQueen : HeroEquipment
	{
	}

	
	public partial class HeroEquipment_Familiar_MoneyGiver : HeroEquipment
	{
	}

	public partial class HeroEquipment_Familiar_PawnBooster : HeroEquipment
	{
	}

	public partial class HeroEquipment_Familiar_PlayerHealer : HeroEquipment
	{
	}
	public partial class HeroEquipment_Familiar_TADPS : HeroEquipment
	{
	}

	public partial class HeroEquipment_Familiar_TowerBooster : HeroEquipment
	{
	}
	public partial class HeroEquipment_Familiar_TowerDamageScaling : HeroEquipment
	{
	}
	public partial class HeroEquipment_Familiar_Melee_TowerScaling : HeroEquipment
	{
	}

	public partial class HeroEquipment_Familiar_TowerHealer : HeroEquipment
	{
	}
	public partial class HeroEquipment_Familiar_WithProjectileAI : HeroEquipment
	{
	}

	public partial class DunDefWeapon
	{

	}

	public partial class DunDefWeapon_Crossbow : DunDefWeapon
	{
	}

	public partial class DunDefWeapon_NessieLauncher : DunDefWeapon_Crossbow
	{
	}

	public partial class DunDefWeapon_Minigun : DunDefWeapon_Crossbow
	{
	}

	public partial class DunDefWeapon_MagicStaff : DunDefWeapon
	{
	}
	public partial class DunDefWeapon_MagicStaff_Channeling : DunDefWeapon_MagicStaff
	{
	}
	public partial class DunDefWeapon_MagicStaff_CustomRightClick : DunDefWeapon_MagicStaff
	{
	}

	public partial class DunDefWeapon_MagicStaff_Dot : DunDefWeapon_MagicStaff
	{
	}

	public partial class DunDefWeapon_MeleeSword : DunDefWeapon
	{
	}

	public partial class DunDefWeapon_HoloDword : DunDefWeapon_MeleeSword
	{
	}

	public partial class DunDefWeapon_HoloDword : DunDefWeapon_MeleeSword
	{
	}

	public partial class DunDefWeapon_MonkSpear : DunDefWeapon_MeleeSword
	{
	}

	public partial class DunDefWeapon_PortalGun : DunDefWeapon
	{
	}

	public partial class DunDefHero
	{
	}


	public partial class DunDefPlayer
	{
	
	}
	public partial class DunDefPlayer_DualMelee : DunDefPlayer
	{
	}

	public partial class DunDefPlayer_Hovering : DunDefPlayer
	{
	}

	public partial class DunDefPlayer_Summoner : DunDefPlayer_Hovering
	{
	}

	public partial class DunDefPlayer_Jester : DunDefPlayer
	{
	}

	public partial class DunDefPlayer_SeriesEv : DunDefPlayer
	{
	}

	// DunDefProjectile Hierarchy

	public partial class DunDefProjectile
	{
	}
	public partial class DunDefHomingProjectile : DunDefProjectile
	{
	}
	public partial class DunDefHomingProjectile_V2 : DunDefHomingProjectile
	{
	}
	public partial class DunDefProjectileHoming_Piercing_GroundBound : DunDefProjectile_Harpoon
	{
	}
	public partial class DunDefProjectileHoming_Piercing_GroundBound_Pulse : DunDefProjectileHoming_Piercing_GroundBound
	{
	}
	public partial class DunDefProjectile_Arrow : DunDefProjectile
	{
	}
	public partial class DunDefProjectile_Bouncing : DunDefHomingProjectile
	{
	}
	public partial class DunDefProjectile_BowlingBall : DunDefProjectile_Harpoon
	{
	}
	public partial class DunDefProjectile_BowlingBolt : DunDefProjectile_MagicBolt
	{
	}
	public partial class DunDefProjectile_Falling : DunDefProjectile
	{
	}
	public partial class DunDefProjectile_Fireball : DunDefProjectile
	{
	}
	public partial class DunDefProjectile_Harpoon : DunDefProjectile
	{
	}
	public partial class DunDefProjectile_HarpoonDot : DunDefProjectile_Harpoon
	{
	}
	public partial class DunDefProjectile_MagicBolt : DunDefProjectile
	{
	}
	public partial class DunDefProjectile_MagicMissile : DunDefProjectile
	{
	}
	public partial class DunDefProjectile_Meteor : DunDefProjectile
	{
	}
	public partial class DunDefProjectile_Meteor_HeroScaling : DunDefProjectile_Meteor
	{
	}
	public partial class DunDefProjectile_Orb : DunDefProjectileHoming_Piercing_GroundBound_Pulse
	{
	}
	public partial class DunDefProjectile_StaffDot : DunDefProjectile_MagicBolt
	{
	}
	public partial class DunDefWebProjectile : DunDefHomingProjectile
	{
	}
}




