using DDUP;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static DDUP.ExportedTemplateDatabase;

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
		EndPrimaryItems = 6,
		Brooch = 7,
		Bracers = 8,
		Shield = 9,
		Mask = 10,
		Currency = 11,

		None = 12,
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

	public enum LevelUpValueType
	{
		None = 0,
		HeroHealth = 1,
		HeroSpeed = 2,
		HeroDamage = 3,
		HeroCastRate = 4,
		Ability1 = 5,
		Ability2 = 6,
		TowerHealth = 7,
		TowerRate = 8,
		TowerDamage = 9,
		TowerRange = 10,
		WeaponBaseDamage = 11,
		WeaponAltDamage = 12,
		WeaponElementalDamage = 13,
		DamageVulnerability = 14,
		AttackSpeed = 15,
		Tenacity = 16,
		Max = 17,
	}

	public static class ImportMaps
	{
		static public Dictionary<string, WeaponType> _weaponType = new()
		{
			{ "EWeaponType.EWT_WEAPON_APPRENTICE", WeaponType.Apprentice },
			{ "EWeaponType.EWT_WEAPON_INITIATE",   WeaponType.Huntress },
			{ "EWeaponType.EWT_WEAPON_RECRUIT",    WeaponType.Monk },
			{ "EWeaponType.EWT_WEAPON_SQUIRE",     WeaponType.Squire },
		};

		static public Dictionary<string, EquipmentType> _EquipmentType = new()
		{
			{ "EEquipmentType.EQT_WEAPON",           EquipmentType.Weapon },
			{ "EEquipmentType.EQT_ARMOR_TORSO",      EquipmentType.Torso },
			{ "EEquipmentType.EQT_ARMOR_PANTS",      EquipmentType.Helmet },
			{ "EEquipmentType.EQT_ARMOR_BOOTS",      EquipmentType.Boots},
			{ "EEquipmentType.EQT_ARMOR_GLOVES",     EquipmentType.Gloves},
			{ "EEquipmentType.EQT_ENDPRIMARYITEMS",  EquipmentType.EndPrimaryItems},
			{ "EEquipmentType.EQT_FAMILIAR",         EquipmentType.Familiar},
			{ "EEquipmentType.EQT_ACCESSORY1",       EquipmentType.Brooch},
			{ "EEquipmentType.EQT_ACCESSORY2",       EquipmentType.Bracers},
			{ "EEquipmentType.EQT_ACCESSORY3",       EquipmentType.Shield},
			{ "EEquipmentType.EQT_MASK",             EquipmentType.Mask},
		};

		static public Dictionary<string, LevelUpValueType> _LevelUpValueTypes = new()
		{
			{ "LevelUpValueType.LU_NONE",                        LevelUpValueType.None },
			{ "LevelUpValueType.LU_HEALTH",                      LevelUpValueType.HeroHealth },
			{ "LevelUpValueType.LU_SPEED",                       LevelUpValueType.HeroSpeed },
			{ "LevelUpValueType.LU_DAMAGE",                      LevelUpValueType.HeroDamage },
			{ "LevelUpValueType.LU_CASTINGRATE",                 LevelUpValueType.HeroCastRate },
			{ "LevelUpValueType.LU_HEROABILITYONE",              LevelUpValueType.Ability1 },
			{ "LevelUpValueType.LU_HEROABILITYTWO",              LevelUpValueType.Ability2 },
			{ "LevelUpValueType.LU_DEFENSEHEALTH",               LevelUpValueType.TowerHealth },
			{ "LevelUpValueType.LU_DEFENSEATTACKRATE",           LevelUpValueType.TowerRate },
			{ "LevelUpValueType.LU_DEFENSEBASEDAMAGE",           LevelUpValueType.TowerDamage },
			{ "LevelUpValueType.LU_DEFENSEAOE",                  LevelUpValueType.TowerRange },
			{ "LevelUpValueType.LU_WEAPONBASEDAMAGE",            LevelUpValueType.WeaponBaseDamage },
			{ "LevelUpValueType.LU_WEAPONALTDAMAGE",             LevelUpValueType.WeaponAltDamage},
			{ "LevelUpValueType.LU_WEAPONELEMENTALDAMAGE",       LevelUpValueType.WeaponElementalDamage},
			{ "LevelUpValueType.LU_DAMAGEVULNERABILITY",         LevelUpValueType.DamageVulnerability },
			{ "LevelUpValueType.LU_ATTACKSPEED",                 LevelUpValueType.AttackSpeed},
			{ "LevelUpValueType.LU_TENACITY",                    LevelUpValueType.Tenacity },
			{ "LevelUpValueType.LU_MAX",                         LevelUpValueType.Max }
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
	}

	// All of the array types
	public enum VarType
	{
		Int = 0,
		Float,
		FString,
		ULinearColor,
		EG_StatRandomizer,
		EG_StatMatchingString,
		DamageReduction,
		MeleeSwingInfo,
		DunDefDamageType,
		DunDefPlayer,
		DunDefProjectile,
		DunDefWeapon,
		HeroEquipment,
		HeroEquipment_Familiar,
		Max
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct FileHeader
	{
		public uint ID;
		public uint Version;

		public FileHeader(Dictionary<string, string> propertyMap)
		{
			ID = Parse.UInt(propertyMap, "ID");
			Version = Parse.UInt(propertyMap, "Version");
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct IndexEntry
	{
		public int TemplateName;
		public int ClassName;
		public VarType Type;
		public int ObjIndex;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct ULinearColor_Data
	{
		public float R;
		public float G;
		public float B;
		public float A;

		public ULinearColor_Data(string propertyString)
		{
			var propertyMap = PropertyParser.Parse(propertyString);

			R = Parse.Float(propertyMap, "R");
			G = Parse.Float(propertyMap, "G");
			B = Parse.Float(propertyMap, "B");
			A = Parse.Float(propertyMap, "A");
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct Array_Data
	{
		public VarType Type;
		public int Count;
		public int Start;

		public Array_Data(int start, int count, VarType tp)
		{
			Type = tp;
			Count = count;
			Start = start;
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct EG_StatRandomizer_Data
	{
		public float MaxRandomValue;
		public float MaxRandomValueNegative;
		public float RandomPower;
		public float RandomPowerOverrideIfNegative;
		public float RandomNegativeThreshold;
		public float RandomInclusionThreshold;
		public float InclusionThresholdOverrideIfNegative;
		public float NegativeThresholdQualityPercentMultiplier;
		public float MinimumPercentageValue;
		public float NegativeMinimumPercentageValue;

		public EG_StatRandomizer_Data(string propertyString)
		{
			var propertyMap = PropertyParser.Parse(propertyString);

			MaxRandomValue = Parse.Float(propertyMap, "MaxRandomValue");
			MaxRandomValueNegative = Parse.Float(propertyMap, "MaxRandomValueNegative");
			RandomPower = Parse.Float(propertyMap, "RandomPower");
			RandomPowerOverrideIfNegative = Parse.Float(propertyMap, "RandomPowerOverrideIfNegative");
			RandomNegativeThreshold = Parse.Float(propertyMap, "RandomNegativeThreshold");
			RandomInclusionThreshold = Parse.Float(propertyMap, "RandomInclusionThreshold");
			InclusionThresholdOverrideIfNegative = Parse.Float(propertyMap, "InclusionThresholdOverrideIfNegative");
			NegativeThresholdQualityPercentMultiplier = Parse.Float(propertyMap, "NegativeThresholdQualityPercentMultiplier");
			MinimumPercentageValue = Parse.Float(propertyMap, "MinimumPercentageValue");
			NegativeMinimumPercentageValue = Parse.Float(propertyMap, "NegativeMinimumPercentageValue");
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct EG_StatMatchingString_Data
	{
		public float ValueThreshold;
		public float PetValueThreshold;
		public float ArmorValueThreshold;
		public int StringValue; // localized string

		public EG_StatMatchingString_Data(string propertyString, ExportedTemplateDatabase db)
		{
			if (propertyString == "none")
			{
				StringValue = db.AddString("none");
				return;
			}
			
			var propertyMap = PropertyParser.Parse(propertyString);

			ValueThreshold = Parse.Float(propertyMap, "ValueThreshold");
			PetValueThreshold = Parse.Float(propertyMap, "PetValueThreshold");
			ArmorValueThreshold = Parse.Float(propertyMap, "ArmorValueThreshold");
			StringValue = db.AddString(propertyMap.ContainsKey("StringValue")? propertyMap["StringValue"] : "");
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct DamageReduction_Data
	{
		public int ForDamageType; // DunDefDamageType
		public byte PercentageReduction;

		public DamageReduction_Data(string propertyString, ExportedTemplateDatabase db)
		{
			var propertyMap = PropertyParser.Parse(propertyString);

			if (!propertyMap.ContainsKey("ForDamageType"))
				ForDamageType = -1;
			else
				ForDamageType = db.GetDunDefDamageTypeIndex(propertyMap["ForDamageType"]);
			PercentageReduction = (byte)Parse.Int(propertyMap, "PercentageReduction");
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct MeleeSwingInfo_Data
	{
		public float DamageMultiplier;
		public float MomentumMultiplier;
		public float SwingAnimationDuration;
		public float AnimSpeed;

		public MeleeSwingInfo_Data(string propertyString)
		{
			var propertyMap = PropertyParser.Parse(propertyString);

			DamageMultiplier = Parse.Float(propertyMap, "DamageMultiplier");
			MomentumMultiplier = Parse.Float(propertyMap, "MomentumMultiplier");
			SwingAnimationDuration = Parse.Float(propertyMap, "SwingAnimationDuration");
			AnimSpeed = Parse.Float(propertyMap, "AnimSpeed");
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct DunDefDamageType_Data
	{
		public int AdjectiveName; // localized string
		public int FriendlyName;  // localized string
		public byte UseForNotPoisonElementalDamage; // bool
		public byte UseForRandomElementalDamage; // bool
		public int DamageTypeArrayIndex;

		public DunDefDamageType_Data(Dictionary<string, string> propertyMap, ExportedTemplateDatabase db)
		{
			AdjectiveName = db.AddString(propertyMap["AdjectiveName"]);
			FriendlyName = db.AddString(propertyMap["FriendlyName"]);
			UseForNotPoisonElementalDamage = Parse.BoolByte(propertyMap, "UseForNotPoisonElementalDamage");
			UseForRandomElementalDamage = Parse.BoolByte(propertyMap, "UseForRandomElementalDamage");
			DamageTypeArrayIndex = Parse.Int(propertyMap, "DamageTypeArrayIndex");
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct DunDefPlayer_Data
	{
		public float AdditionalSpeedMultiplier;
		public float ExtraPlayerDamageMultiplier;
		public float HeroBonusPetDamageMultiplier;
		public int HeroBoostHealAmount;
		public float HeroBoostSpeedMultiplier;
		public float NightmareModePlayerHealthMultiplier;
		public float PlayerWeaponDamageMultiplier;
		public float StatExpFull_HeroCastingRate;
		public float StatExpInitial_HeroCastingRate;
		public float StatMultFull_HeroCastingRate;
		public float StatMultInitial_HeroCastingRate;
		public float AnimSpeedMultiplier;

		public Array_Data MeleeSwingInfoMultipliers;            // MeleeSwingInfo

		// DunDefPawn
		public float DamageMultiplierAdditional;

		// DunDefPlayer_DualMelee
		public Array_Data MainHandSwingInfoMultipliers;     // MeleeSwingInfo[]
		public Array_Data OffHandSwingInfoMultipliers;      // MeleeSwingInfo[]

		public DunDefPlayer_Data(Dictionary<string, string> propertyMap, ExportedTemplateDatabase db)
		{
			AdditionalSpeedMultiplier = Parse.Float(propertyMap, "AdditionalSpeedMultiplier");
			ExtraPlayerDamageMultiplier = Parse.Float(propertyMap, "ExtraPlayerDamageMultiplier");
			HeroBonusPetDamageMultiplier = Parse.Float(propertyMap, "HeroBonusPetDamageMultiplier");
			HeroBoostHealAmount = Parse.Int(propertyMap, "HeroBoostHealAmount");
			HeroBoostSpeedMultiplier = Parse.Float(propertyMap, "HeroBoostSpeedMultiplier");
			NightmareModePlayerHealthMultiplier = Parse.Float(propertyMap, "NightmareModePlayerHealthMultiplier");
			PlayerWeaponDamageMultiplier = Parse.Float(propertyMap, "PlayerWeaponDamageMultiplier");
			StatExpFull_HeroCastingRate = Parse.Float(propertyMap, "StatExpFull_HeroCastingRate");
			StatExpInitial_HeroCastingRate = Parse.Float(propertyMap, "StatExpInitial_HeroCastingRate");
			StatMultFull_HeroCastingRate = Parse.Float(propertyMap, "StatMultFull_HeroCastingRate");
			StatMultInitial_HeroCastingRate = Parse.Float(propertyMap, "StatMultInitial_HeroCastingRate");
			AnimSpeedMultiplier = Parse.Float(propertyMap, "AnimSpeedMultiplier");

			MeleeSwingInfoMultipliers = db.BuildArray(propertyMap["MeleeSwingInfoMultipliers"], VarType.MeleeSwingInfo);

			DamageMultiplierAdditional = Parse.Float(propertyMap, "DamageMultiplierAdditional");

			MainHandSwingInfoMultipliers = db.BuildArray(propertyMap["MainHandSwingInfoMultipliers"], VarType.MeleeSwingInfo);
			OffHandSwingInfoMultipliers = db.BuildArray(propertyMap["OffHandSwingInfoMultipliers"], VarType.MeleeSwingInfo);
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct DunDefProjectile_Data
	{
		public int AdditionalDamageAmount;
		public int AdditionalDamageType;                                // DunDefDamageType_Data
		public float DamageRadiusFallOffExponent;
		public byte MultiplyProjectileDamageByPrimaryWeaponSwingSpeed;  // bool
		public byte MultiplyProjectileDamageByWeaponDamage;             // bool
		public byte OnlyCollideWithIgnoreClasses;
		public byte ScaleHeroDamage;
		public int ScaleDamageStatType;
		public float ScaleDamageStatExponent;
		public float ProjDamage;
		public float ProjDamageRadius;
		public int ProjDamageType;                                      // DunDefDamageType_Data
		public float ProjectileDamageByWeaponDamageDivider;
		public float ProjectileDamagePerDistanceTravelled;
		public float ProjectileLifespan;
		public float ProjectileMaxSpeed;
		public float ProjectileSpeed;
		public Array_Data RandomDamageTypes;                            // DunDefDamageType_Data[]
		public byte bAlwaysUseRandomDamageType;                         // bool
		public byte bApplyBuffsOnAoe;
		public byte bReplicateWeaponProjectile;
		public byte bUseProjectilePerDistanceScaling;
		public byte bUseProjectilePerDistanceSizeScaling;

		// Homing Projectile
		public byte bPierceEnemies;
		public int NumAllowedPassThrough;

		public float TowerDamageMultiplier;
		public float HomingInterpSpeed;
		public byte bDamageOnTouch;

		public DunDefProjectile_Data(Dictionary<string, string> propertyMap, ExportedTemplateDatabase db)
		{
			AdditionalDamageAmount = Parse.Int(propertyMap, "AdditionalDamageAmount");
			AdditionalDamageType = Parse.Int(propertyMap, "AdditionalDamageType");
			DamageRadiusFallOffExponent = Parse.Float(propertyMap, "DamageRadiusFallOffExponent");
			MultiplyProjectileDamageByPrimaryWeaponSwingSpeed = Parse.BoolByte(propertyMap, "MultiplyProjectileDamageByPrimaryWeaponSwingSpeed");
			MultiplyProjectileDamageByWeaponDamage = Parse.BoolByte(propertyMap, "MultiplyProjectileDamageByWeaponDamage");
			OnlyCollideWithIgnoreClasses = Parse.BoolByte(propertyMap, "OnlyCollideWithIgnoreClasses");
			ScaleHeroDamage = Parse.BoolByte(propertyMap, "ScaleHeroDamage");

			ScaleDamageStatType = ImportMaps._LevelUpValueTypes.ContainsKey(propertyMap["ScaleDamageStatType"])
				? (int)ImportMaps._LevelUpValueTypes[propertyMap["ScaleDamageStatType"]]
				: Parse.Int(propertyMap, "ScaleDamageStatType");

			ScaleDamageStatExponent = Parse.Float(propertyMap, "ScaleDamageStatExponent");
			ProjDamage = Parse.Float(propertyMap, "ProjDamage");
			ProjDamageRadius = Parse.Float(propertyMap, "ProjDamageRadius");
			ProjDamageType = db.GetDunDefDamageTypeIndex(propertyMap["ProjDamageType"]);
			ProjectileDamageByWeaponDamageDivider = Parse.Float(propertyMap, "ProjectileDamageByWeaponDamageDivider");
			ProjectileDamagePerDistanceTravelled = Parse.Float(propertyMap, "ProjectileDamagePerDistanceTravelled");
			ProjectileLifespan = Parse.Float(propertyMap, "ProjectileLifespan");
			ProjectileMaxSpeed = Parse.Float(propertyMap, "ProjectileMaxSpeed");
			ProjectileSpeed = Parse.Float(propertyMap, "ProjectileSpeed");
			RandomDamageTypes = db.BuildArray(propertyMap["RandomDamageTypes"], VarType.DunDefDamageType);
			bAlwaysUseRandomDamageType = Parse.BoolByte(propertyMap, "bAlwaysUseRandomDamageType");
			bApplyBuffsOnAoe = Parse.BoolByte(propertyMap, "bApplyBuffsOnAoe");
			bReplicateWeaponProjectile = Parse.BoolByte(propertyMap, "bReplicateWeaponProjectile");
			bUseProjectilePerDistanceScaling = Parse.BoolByte(propertyMap, "bUseProjectilePerDistanceScaling");
			bUseProjectilePerDistanceSizeScaling = Parse.BoolByte(propertyMap, "bUseProjectilePerDistanceSizeScaling");

			bPierceEnemies = Parse.BoolByte(propertyMap, "bPierceEnemies");
			NumAllowedPassThrough = Parse.Int(propertyMap, "NumAllowedPassThrough");

			TowerDamageMultiplier = Parse.Float(propertyMap, "TowerDamageMultiplier");
			HomingInterpSpeed = Parse.Float(propertyMap, "HomingInterpSpeed");
			bDamageOnTouch = Parse.BoolByte(propertyMap, "bDamageOnTouch");
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct DunDefWeapon_Data
	{
		public int AdditionalDamageAmount;
		public int AdditionalDamageType;                // DunDefDamageType
		public int BaseAltDamage;
		public int BaseDamage;
		public int BaseShotsPerSecond;
		public int BaseTotalAmmo;
		public Array_Data ExtraProjectileTemplates;     // DunDefProjectile[]
		public float WeaponDamageMultiplier;
		public float WeaponSpeedMultiplier;
		public byte bIsMeleeWeapon;
		public byte bRandomizeProjectileTemplate;
		public byte bUseAdditionalProjectileDamage;
		public byte bUseAltDamageForProjectileBaseDamage;
		public float MinimumProjectileSpeed;
		public float ProjectileSpeedAddition;
		public float ProjectileSpeedBonusMultiplier;
		public int RandomizedProjectileTemplate; // DunDefProjectile

		public int ProjectileTemplate; // DunDefProjectile

		//  DunDefWeapon_Crossbow
		public int BaseNumProjectiles;
		public float BaseReloadSpeed;
		public int ClipAmmo;
		public float FireIntervalMultiplier;
		public byte bUseHighShotPerSecond;

		//  DunDefWeapon_MagicStaff
		public int AbilityCooldownTime;
		public float BaseChargeSpeed;
		public float BonusDamageMulti;
		public int CooldownDuration;
		public float ElementalDamageForRightClickScalar;
		public float FullAltChargeTime;
		public float FullChargeTime;
		public float FullchargeRefireInterval;
		public float MediumChargeFFThreshold;
		public int NumProjectiles;
		public byte bIsRainMaker;
		public byte bEmberorMoon;
		public byte bUseAttackCD;
		public byte bUseElementalScallingForRightClick;

		// DunDefWeapon_MagicStaff_Channeling
		public float ChannelingProjectileDamageMultiplier;
		public float ChannelingProjectileFireSpeed;
		public int ChannelingProjectileTemplate;         // DunDefProjectile
		public float ChannelingRangeMultiplier;

		// DunDefWeapon_MeleeSword
		public int BaseMeleeDamageType;     // DunDefDamageType
		public float DamageIncreaseForSwingSpeedFactor;
		public float DamageMultiplier;
		public float ExtraSpeedMultiplier;
		public byte IsSwingingWeapon;
		public float MaxMomentumMultplierByDamage;
		public float MaxTotalMomentumMultiplier;
		public float MeleeDamageMomentum;
		public Array_Data MeleeSwingInfos;  // MeleeSwingInfo[]
		public float MinimumSwingDamageTime;
		public float MinimumSwingTime;
		public float MomentumMultiplier;
		public float ProjectileDamageHeroStatExponentMultiplier;
		public Array_Data RainbowDamageTypeArrays; // DunDefDamageType
		public float SpeedMultiplier;
		public float SpeedMultiplierDamageExponent;
		public float WeakenEnemyTargetPercentage;
		public float WeaponProjectileDamageMultiplier;
		public byte bShootMeleeProjectile;
		public byte bUseRainbowDamageType;
		public byte bUseWeaponDamageForProjectileDamage;
		public float BlockingMomentumExponent;
		public float AdditionalMomentumExponent;

		// DunDefWeapon_Minigun
		public float MinigunProjectileDamageMultiplier;
		public float SpeedPerDelta;

		// DunDefWeapon_MonkSpear
		public float ShootInterval;

		// DunDefWeapon_NessieLauncher
		public float Multiplier;
		public float NessieCooldown;

		public DunDefWeapon_Data(Dictionary<string, string> propertyMap, ExportedTemplateDatabase db)
		{
			AdditionalDamageAmount = Parse.Int(propertyMap, "AdditionalDamageAmount");
			AdditionalDamageType = db.GetDunDefDamageTypeIndex(propertyMap["AdditionalDamageType"]);
			BaseAltDamage = Parse.Int(propertyMap, "BaseAltDamage");
			BaseDamage = Parse.Int(propertyMap, "BaseDamage");
			BaseShotsPerSecond = Parse.Int(propertyMap, "BaseShotsPerSecond");
			BaseTotalAmmo = Parse.Int(propertyMap, "BaseTotalAmmo");
			ExtraProjectileTemplates = db.BuildArray(propertyMap["ExtraProjectileTemplates"], VarType.DunDefProjectile);
			WeaponDamageMultiplier = Parse.Float(propertyMap, "WeaponDamageMultiplier");
			WeaponSpeedMultiplier = Parse.Float(propertyMap, "WeaponSpeedMultiplier");
			bIsMeleeWeapon = Parse.BoolByte(propertyMap, "bIsMeleeWeapon");
			bRandomizeProjectileTemplate = Parse.BoolByte(propertyMap, "bRandomizeProjectileTemplate");
			bUseAdditionalProjectileDamage = Parse.BoolByte(propertyMap, "bUseAdditionalProjectileDamage");
			bUseAltDamageForProjectileBaseDamage = Parse.BoolByte(propertyMap, "bUseAltDamageForProjectileBaseDamage");
			MinimumProjectileSpeed = Parse.Float(propertyMap, "MinimumProjectileSpeed");
			ProjectileSpeedAddition = Parse.Float(propertyMap, "ProjectileSpeedAddition");
			ProjectileSpeedBonusMultiplier = Parse.Float(propertyMap, "ProjectileSpeedBonusMultiplier");
			RandomizedProjectileTemplate = db.GetDunDefProjectileIndex(propertyMap["RandomizedProjectileTemplate"]);

			ProjectileTemplate = db.GetDunDefProjectileIndex(propertyMap["ProjectileTemplate"]);

			BaseNumProjectiles = Parse.Int(propertyMap, "BaseNumProjectiles");
			BaseReloadSpeed = Parse.Float(propertyMap, "BaseReloadSpeed");
			ClipAmmo = Parse.Int(propertyMap, "ClipAmmo");
			FireIntervalMultiplier = Parse.Float(propertyMap, "FireIntervalMultiplier");
			bUseHighShotPerSecond = Parse.BoolByte(propertyMap, "bUseHighShotPerSecond");

			AbilityCooldownTime = Parse.Int(propertyMap, "AbilityCooldownTime");
			BaseChargeSpeed = Parse.Float(propertyMap, "BaseChargeSpeed");
			BonusDamageMulti = Parse.Float(propertyMap, "BonusDamageMulti");
			CooldownDuration = Parse.Int(propertyMap, "CooldownDuration");
			ElementalDamageForRightClickScalar = Parse.Float(propertyMap, "ElementalDamageForRightClickScalar");
			FullAltChargeTime = Parse.Float(propertyMap, "FullAltChargeTime");
			FullChargeTime = Parse.Float(propertyMap, "FullChargeTime");
			FullchargeRefireInterval = Parse.Float(propertyMap, "FullchargeRefireInterval");
			MediumChargeFFThreshold = Parse.Float(propertyMap, "MediumChargeFFThreshold");
			NumProjectiles = Parse.Int(propertyMap, "NumProjectiles");
			bIsRainMaker = Parse.BoolByte(propertyMap, "bIsRainMaker");
			bEmberorMoon = Parse.BoolByte(propertyMap, "bEmberorMoon");
			bUseAttackCD = Parse.BoolByte(propertyMap, "bUseAttackCD");
			bUseElementalScallingForRightClick = Parse.BoolByte(propertyMap, "bUseElementalScallingForRightClick");

			ChannelingProjectileDamageMultiplier = Parse.Float(propertyMap, "ChannelingProjectileDamageMultiplier");
			ChannelingProjectileFireSpeed = Parse.Float(propertyMap, "ChannelingProjectileFireSpeed");
			ChannelingProjectileTemplate = db.GetDunDefProjectileIndex(propertyMap["ChannelingProjectileTemplate"]);
			ChannelingRangeMultiplier = Parse.Float(propertyMap, "ChannelingRangeMultiplier");

			BaseMeleeDamageType = db.GetDunDefDamageTypeIndex(propertyMap["BaseMeleeDamageType"]);
			DamageIncreaseForSwingSpeedFactor = Parse.Float(propertyMap, "DamageIncreaseForSwingSpeedFactor");
			DamageMultiplier = Parse.Float(propertyMap, "DamageMultiplier");
			ExtraSpeedMultiplier = Parse.Float(propertyMap, "ExtraSpeedMultiplier");
			IsSwingingWeapon = Parse.BoolByte(propertyMap, "IsSwingingWeapon");
			MaxMomentumMultplierByDamage = Parse.Float(propertyMap, "MaxMomentumMultplierByDamage");
			MaxTotalMomentumMultiplier = Parse.Float(propertyMap, "MaxTotalMomentumMultiplier");
			MeleeDamageMomentum = Parse.Float(propertyMap, "MeleeDamageMomentum");
			MeleeSwingInfos = db.BuildArray(propertyMap["MeleeSwingInfos"], VarType.MeleeSwingInfo);
			MinimumSwingDamageTime = Parse.Float(propertyMap, "MinimumSwingDamageTime");
			MinimumSwingTime = Parse.Float(propertyMap, "MinimumSwingTime");
			MomentumMultiplier = Parse.Float(propertyMap, "MomentumMultiplier");
			ProjectileDamageHeroStatExponentMultiplier = Parse.Float(propertyMap, "ProjectileDamageHeroStatExponentMultiplier");
			RainbowDamageTypeArrays = db.BuildArray(propertyMap["RainbowDamageTypeArrays"], VarType.DunDefDamageType);
			SpeedMultiplier = Parse.Float(propertyMap, "SpeedMultiplier");
			SpeedMultiplierDamageExponent = Parse.Float(propertyMap, "SpeedMultiplierDamageExponent");
			WeakenEnemyTargetPercentage = Parse.Float(propertyMap, "WeakenEnemyTargetPercentage");
			WeaponProjectileDamageMultiplier = Parse.Float(propertyMap, "WeaponProjectileDamageMultiplier");
			bShootMeleeProjectile = Parse.BoolByte(propertyMap, "bShootMeleeProjectile");
			bUseRainbowDamageType = Parse.BoolByte(propertyMap, "bUseRainbowDamageType");
			bUseWeaponDamageForProjectileDamage = Parse.BoolByte(propertyMap, "bUseWeaponDamageForProjectileDamage");
			BlockingMomentumExponent = Parse.Float(propertyMap, "BlockingMomentumExponent");
			AdditionalMomentumExponent = Parse.Float(propertyMap, "AdditionalMomentumExponent");

			MinigunProjectileDamageMultiplier = Parse.Float(propertyMap, "MinigunProjectileDamageMultiplier");
			SpeedPerDelta = Parse.Float(propertyMap, "SpeedPerDelta");

			ShootInterval = Parse.Float(propertyMap, "ShootInterval");

			Multiplier = Parse.Float(propertyMap, "Multiplier");
			NessieCooldown = Parse.Float(propertyMap, "NessieCooldown");
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct HeroEquipment_Data
	{
		public Array_Data StatModifiers;  // int array
		public Array_Data DamageReductions; // DamageReduction
		public Array_Data DamageReductionRandomizers; // Randomizers for damage reduction

		
		public int AdditionalDescription;       // localized string
		public float AdditionalWeaponDamageBonusRandomizerMultiplier;
		public byte AllowNameRandomization;
		public float AltDamageIncreasePerLevelMultiplier;
		public float AltDamageRandomizerMult;
		public float AltMaxDamageIncreasePerLevel;
		public int BaseForgerName;              // localized string
		public byte CountsForAllArmorSets;      // bool
		public int DamageDescription;           // localized string
		public float DamageIncreasePerLevelMultiplier;
		public int Description; // string
		public float ElementalDamageIncreasePerLevelMultiplier;
		public float ElementalDamageMultiplier;
		public int EquipmentID1;
		public int EquipmentID2;
		public int EquipmentName; // localized string
		public byte EquipmentSetID;
		public int EquipmentTemplate;           // parent - HeroEquipment
		public int EquipmentWeaponTemplate;     // DunDefWeapon
		public float ExtraQualityDamageIncreasePerLevelMultiplier;
		public float ExtraQualityMaxDamageIncreasePerLevel;
		public int ExtraQualityUpgradeDamageNumberDescriptor;
		public int ForgedByDescription; // localized string
		public float FullEquipmentSetStatMultiplier;
		public int HeroStatUpgradeLimit;
		public float HighLevelManaCostPerLevelExponentialFactorAdditional;
		public float HighLevelManaCostPerLevelMaxQualityMultiplierAdditional;
		public float HighLevelRequirementRatingThreshold;
		public float HighLevelThreshold;
		public int Level;
		public int LevelRequirementIndex;
		public int LevelString; // localized string
		public float MaxDamageIncreasePerLevel;
		public int MaxEquipmentLevel;
		public int MaxEquipmentLevelRandomizer; // EG_StatRandomizer
		public int MaxHeroStatValue;
		public int MaxLevel;
		public int MaxNonTranscendentStatRollValue;
		public float MaxRandomValue;
		public float MaxRandomValueNegative;
		public int MaxUpgradeableSpeedOfProjectilesBonus;
		public int MinDamageBonus;
		public float MinElementalDamageIncreasePerLevel;
		public float MinEquipmentLevels;
		public int MinLevel;
		public int MinSupremeLevel;
		public int MinTranscendentLevel;
		public int MinUltimateLevel;
		public float MinimumPercentageValue;
		public int MinimumSellWorth;
		public float MythicalFullEquipmentSetStatMultiplier;
		public int Name; // string
		public byte NameIndex_Base;
		public byte NameIndex_DamageReduction;
		public byte NameIndex_QualityDescriptor;
		public float NegativeMinimumPercentageValue;
		public float NegativeThresholdQualityPecentMultiplier;
		public byte OnlyRandomizeBaseName; // bool
		public float PlayerSpeedMultiplier;
		public Array_Data QualityDescriptorNames; // EG_StatMatchingString[]
		public Array_Data QualityDescriptorRealNames;// EG_StatMatchingString[]
		public float QualityThreshold;
		public Array_Data RandomBaseNames; // EG_StatMatchingString[]
		public float RandomNegativeThreshold;
		public float RandomPower;
		public float RandomPowerOverrideIfNegative;
		public float RandomizerQualityMultiplier;
		public float RandomizerStatModifierGoNegativeChance;
		public float RandomizerStatModifierGoNegativeMultiplier;
		public int RandomizerStatModifierGoNegativeThreshold;
		public int RequiredClassString; // string
		public float RuthlessUltimate93Chance;
		public float RuthlessUltimatePlusChance;
		public float RuthlessUltimatePlusPlusChance;
		public float SecondExtraQualityDamageIncreasePerLevelMultiplier;
		public float SecondExtraQualityMaxDamageIncreasePerLevel;
		public int SecondExtraQualityUpgradeDamageNumberDescriptor;
		public float StackedStatModifier;
		public Array_Data StatEquipmentIDs; // int 10
		public Array_Data StatEquipmentTiers; // int 10
		public Array_Data StatModifierRandomizers; // EG_StatRandomizer	 - 11
		public Array_Data StatObjectArray; // StatObject_Equipment
		public int StoredMana;
		public float SupremeFullEquipmentSetStatMultiplier;
		public float SupremeLevelBoostAmount;
		public float SupremeLevelBoostRandomizerPower;
		public int SupremeMaxHeroStatValue;
		public float TotalRandomizerMultiplier;
		public float TranscendentFullEquipmentSetStatMultiplier;
		public float TranscendentLevelBoostAmount;
		public float TranscendentLevelBoostRandomizerPower;
		public int TranscendentMaxHeroStatValue;
		public float Ultimate93Chance;
		public float UltimateDamageIncreasePerLevelMultiplier;
		public float UltimateFullEquipmentSetStatMultiplier;
		public float UltimateLevelBoostAmount;
		public float UltimateLevelBoostRandomizerPower;
		public float UltimateMaxDamageIncreasePerLevel;
		public int UltimateMaxHeroStatValue;
		public float UltimatePlusChance;
		public int UltimatePlusMaxHeroStatValue;
		public float UltimatePlusPlusChance;
		public int UserEquipmentName; // string
		public int UserForgerName; // string
		public float Values;
		public int WeaponAdditionalDamageAmount;
		public int WeaponAdditionalDamageAmountRandomizer; // EG_StatRandomizer
		public int WeaponAdditionalDamageType; // DunDefDamageType
		public byte WeaponAdditionalDamageTypeNotPoison;
		public int WeaponAltDamageBonus;
		public int WeaponAltDamageBonusRandomizer; // EG_StatRandomizer
		public float WeaponAltDamageMultiplier;
		public byte WeaponBlockingBonus;
		public int WeaponBlockingBonusRandomizer; // EG_StatRandomizer
		public byte WeaponChargeSpeedBonus;
		public int WeaponChargeSpeedBonusRandomizer; // EG_StatRandomizer
		public int WeaponClipAmmoBonus;
		public int WeaponClipAmmoBonusRandomizer; // EG_StatRandomizer
		public int WeaponDamageBonus;
		public int WeaponDamageBonusRandomizer; // EG_StatRandomizer
		public float WeaponDamageBonusRandomizerMultiplier;
		public float WeaponDamageMultiplier;
		public float WeaponEquipmentRatingPercentBase;
		public byte WeaponKnockbackBonus;
		public int WeaponKnockbackBonusRandomizer; // EG_StatRandomizer
		public int WeaponKnockbackMax;
		public byte WeaponNumberOfProjectilesBonus;
		public int WeaponNumberOfProjectilesBonusRandomizer; // EG_StatRandomizer
		public int WeaponNumberOfProjectilesQualityBaseline;
		public byte WeaponReloadSpeedBonus;
		public int WeaponReloadSpeedBonusRandomizer; // EG_StatRandomizer
		public byte WeaponShotsPerSecondBonus;
		public int WeaponShotsPerSecondBonusRandomizer; // EG_StatRandomizer
		public int WeaponSpeedOfProjectilesBonus;
		public int WeaponSpeedOfProjectilesBonusRandomizer; // EG_StatRandomizer
		public float WeaponSwingSpeedMultiplier;
		public byte bCanBeEquipped;
		public byte bCantBeDropped;
		public byte bCantBeSold;
		public byte bDisableRandomization;
		public byte bEquipmentFeatureByte1;
		public byte bEquipmentFeatureByte2;
		public byte bForceAllowDropping;
		public byte bForceAllowSelling;
		public byte bForceRandomizerWithMinEquipmentLevel;
		public byte bHideQualityDescriptors;
		public byte bIsConsumable;
		public byte bIsSecondary;
		public byte bNoNegativeRandomizations;
		public byte bUseBonusStatsFromStacking;
		public byte bUseExtraQualityDamage;
		public byte bUseSecondExtraQualityDamage;
		public int weaponType; // _DataTypes.EWeaponType

		// Native
		public int EquipmentDescription; // localized string
		public int EquipmentType; // HeroEquipmentNative.EEquipmentType
		public int ForDamageType; // DunDefDamageType
		public float MaxRandomElementalDamageMultiplier;
		public float MyRating;
		public float MyRatingPercent;
		public int PercentageReduction;
		public int UserID;
		public byte WeaponAltDamageBonusUse;
		public byte WeaponBlockingBonusUse;
		public byte WeaponChargeSpeedBonusUse;
		public byte WeaponClipAmmoBonusUse;
		public byte WeaponKnockbackBonusUse;
		public byte WeaponReloadSpeedBonusUse;
		public byte WeaponShotsPerSecondBonusUse;
		public byte bDisableTheRandomization;
		public byte bForceUseParentTemplate;
		public byte UseWeaponCoreStats;
		public byte bForceToMinElementalScale;
		public byte bForceToMaxElementalScale;
		
		
		// Icon section
		public int IconColorAddPrimary; // linearColor
		public int IconColorAddSecondary; // linearColor
		public float IconColorMultPrimary;
		public float IconColorMultSecondary;

		public byte UseColorSets;
		public Array_Data PrimaryColorSets; // linearColor
		public Array_Data SecondaryColorSets; // linearColor

		public int IconX;
		public int IconY;
		public int IconX1;
		public int IconY1;
		public int IconX2;
		public int IconY2;

		public int FamiliarDataIndex;  // HeroEquipment_Familiar_Data

		public HeroEquipment_Data(Dictionary<string, string> propertyMap, ExportedTemplateDatabase db)
		{
			StatModifiers = db.BuildArray(propertyMap["StatModifiers"], VarType.Int);
			DamageReductions = db.BuildArray(propertyMap["DamageReductions"], VarType.DamageReduction);
			DamageReductionRandomizers = db.BuildArray(propertyMap["DamageReductionRandomizers"], VarType.EG_StatRandomizer);

			AdditionalDescription = db.AddString(propertyMap["AdditionalDescription"]);
			AdditionalWeaponDamageBonusRandomizerMultiplier = Parse.Float(propertyMap, "AdditionalWeaponDamageBonusRandomizerMultiplier");
			AllowNameRandomization = Parse.BoolByte(propertyMap, "AllowNameRandomization");
			AltDamageIncreasePerLevelMultiplier = Parse.Float(propertyMap, "AltDamageIncreasePerLevelMultiplier");
			AltDamageRandomizerMult = Parse.Float(propertyMap, "AltDamageRandomizerMult");
			AltMaxDamageIncreasePerLevel = Parse.Float(propertyMap, "AltMaxDamageIncreasePerLevel");
			BaseForgerName = db.AddString(propertyMap["BaseForgerName"]);
			CountsForAllArmorSets = Parse.BoolByte(propertyMap, "CountsForAllArmorSets");
			DamageDescription = db.AddString(propertyMap["DamageDescription"]);
			DamageIncreasePerLevelMultiplier = Parse.Float(propertyMap, "DamageIncreasePerLevelMultiplier");
			Description = db.AddString(propertyMap["Description"]);
			ElementalDamageIncreasePerLevelMultiplier = Parse.Float(propertyMap, "ElementalDamageIncreasePerLevelMultiplier");
			ElementalDamageMultiplier = Parse.Float(propertyMap, "ElementalDamageMultiplier");
			EquipmentID1 = Parse.Int(propertyMap, "EquipmentID1");
			EquipmentID2 = Parse.Int(propertyMap, "EquipmentID2");
			EquipmentName = db.AddString(propertyMap["EquipmentName"]);
			EquipmentSetID = (byte)Parse.Int(propertyMap, "EquipmentSetID");
			EquipmentTemplate = db.AddString(propertyMap["EquipmentTemplate"]);
			EquipmentWeaponTemplate = db.GetDunDefWeaponIndex(propertyMap["EquipmentWeaponTemplate"]);
			ExtraQualityDamageIncreasePerLevelMultiplier = Parse.Float(propertyMap, "ExtraQualityDamageIncreasePerLevelMultiplier");
			ExtraQualityMaxDamageIncreasePerLevel = Parse.Float(propertyMap, "ExtraQualityMaxDamageIncreasePerLevel");
			ExtraQualityUpgradeDamageNumberDescriptor = Parse.Int(propertyMap, "ExtraQualityUpgradeDamageNumberDescriptor");
			ForgedByDescription = db.AddString(propertyMap["ForgedByDescription"]);
			FullEquipmentSetStatMultiplier = Parse.Float(propertyMap, "FullEquipmentSetStatMultiplier");
			HeroStatUpgradeLimit = Parse.Int(propertyMap, "HeroStatUpgradeLimit");
			HighLevelManaCostPerLevelExponentialFactorAdditional = Parse.Float(propertyMap, "HighLevelManaCostPerLevelExponentialFactorAdditional");
			HighLevelManaCostPerLevelMaxQualityMultiplierAdditional = Parse.Float(propertyMap, "HighLevelManaCostPerLevelMaxQualityMultiplierAdditional");
			HighLevelRequirementRatingThreshold = Parse.Float(propertyMap, "HighLevelRequirementRatingThreshold");
			HighLevelThreshold = Parse.Float(propertyMap, "HighLevelThreshold");
			Level = Parse.Int(propertyMap, "Level");
			LevelRequirementIndex = Parse.Int(propertyMap, "LevelRequirementIndex");
			LevelString = db.AddString(propertyMap["LevelString"]);
			MaxDamageIncreasePerLevel = Parse.Float(propertyMap, "MaxDamageIncreasePerLevel");
			MaxEquipmentLevel = Parse.Int(propertyMap, "MaxEquipmentLevel");
			MaxEquipmentLevelRandomizer = db.AddEG_StatRandomizer(new EG_StatRandomizer_Data(propertyMap["MaxEquipmentLevelRandomizer"]));
			MaxHeroStatValue = Parse.Int(propertyMap, "MaxHeroStatValue");
			MaxLevel = Parse.Int(propertyMap, "MaxLevel");
			MaxNonTranscendentStatRollValue = Parse.Int(propertyMap, "MaxNonTranscendentStatRollValue");
			MaxRandomValue = Parse.Float(propertyMap, "MaxRandomValue");
			MaxRandomValueNegative = Parse.Float(propertyMap, "MaxRandomValueNegative");
			MaxUpgradeableSpeedOfProjectilesBonus = Parse.Int(propertyMap, "MaxUpgradeableSpeedOfProjectilesBonus");
			MinDamageBonus = Parse.Int(propertyMap, "MinDamageBonus");
			MinElementalDamageIncreasePerLevel = Parse.Float(propertyMap, "MinElementalDamageIncreasePerLevel");
			MinEquipmentLevels = Parse.Float(propertyMap, "MinEquipmentLevels");
			MinLevel = Parse.Int(propertyMap, "MinLevel");
			MinSupremeLevel = Parse.Int(propertyMap, "MinSupremeLevel");
			MinTranscendentLevel = Parse.Int(propertyMap, "MinTranscendentLevel");
			MinUltimateLevel = Parse.Int(propertyMap, "MinUltimateLevel");
			MinimumPercentageValue = Parse.Float(propertyMap, "MinimumPercentageValue");
			MinimumSellWorth = Parse.Int(propertyMap, "MinimumSellWorth");
			MythicalFullEquipmentSetStatMultiplier = Parse.Float(propertyMap, "MythicalFullEquipmentSetStatMultiplier");
			Name = Parse.Int(propertyMap, "Name");
			NameIndex_Base = Parse.BoolByte(propertyMap, "NameIndex_Base");
			NameIndex_DamageReduction = (byte)Parse.Int(propertyMap, "NameIndex_DamageReduction");
			NameIndex_QualityDescriptor = (byte)Parse.Int(propertyMap, "NameIndex_QualityDescriptor");
			NegativeMinimumPercentageValue = Parse.Float(propertyMap, "NegativeMinimumPercentageValue");
			NegativeThresholdQualityPecentMultiplier = Parse.Float(propertyMap, "NegativeThresholdQualityPercentMultiplier"); // (kept original key spelling if needed)
			OnlyRandomizeBaseName = Parse.BoolByte(propertyMap, "OnlyRandomizeBaseName");
			PlayerSpeedMultiplier = Parse.Float(propertyMap, "PlayerSpeedMultiplier");
			QualityDescriptorNames = db.BuildArray(propertyMap["QualityDescriptorNames"], VarType.EG_StatMatchingString);
			QualityDescriptorRealNames = db.BuildArray(propertyMap["QualityDescriptorRealNames"], VarType.EG_StatMatchingString);
			QualityThreshold = Parse.Float(propertyMap, "QualityThreshold");
			RandomBaseNames = db.BuildArray(propertyMap["RandomBaseNames"], VarType.EG_StatMatchingString);
			RandomNegativeThreshold = Parse.Float(propertyMap, "RandomNegativeThreshold");
			RandomPower = Parse.Float(propertyMap, "RandomPower");
			RandomPowerOverrideIfNegative = Parse.Float(propertyMap, "RandomPowerOverrideIfNegative");
			RandomizerQualityMultiplier = Parse.Float(propertyMap, "RandomizerQualityMultiplier");
			RandomizerStatModifierGoNegativeChance = Parse.Float(propertyMap, "RandomizerStatModifierGoNegativeChance");
			RandomizerStatModifierGoNegativeMultiplier = Parse.Float(propertyMap, "RandomizerStatModifierGoNegativeMultiplier");
			RandomizerStatModifierGoNegativeThreshold = Parse.Int(propertyMap, "RandomizerStatModifierGoNegativeThreshold");
			RequiredClassString = db.AddString(propertyMap["RequiredClassString"]);
			RuthlessUltimate93Chance = Parse.Float(propertyMap, "RuthlessUltimate93Chance");
			RuthlessUltimatePlusChance = Parse.Float(propertyMap, "RuthlessUltimatePlusChance");
			RuthlessUltimatePlusPlusChance = Parse.Float(propertyMap, "RuthlessUltimatePlusPlusChance");
			SecondExtraQualityDamageIncreasePerLevelMultiplier = Parse.Float(propertyMap, "SecondExtraQualityDamageIncreasePerLevelMultiplier");
			SecondExtraQualityMaxDamageIncreasePerLevel = Parse.Float(propertyMap, "SecondExtraQualityMaxDamageIncreasePerLevel");
			SecondExtraQualityUpgradeDamageNumberDescriptor = Parse.Int(propertyMap, "SecondExtraQualityUpgradeDamageNumberDescriptor");
			StackedStatModifier = Parse.Float(propertyMap, "StackedStatModifier");
			StatEquipmentIDs = db.BuildArray(propertyMap["StatEquipmentIDs"], VarType.Int);
			StatEquipmentTiers = db.BuildArray(propertyMap["StatEquipmentTiers"], VarType.Int);
			StatModifierRandomizers = db.BuildArray(propertyMap["StatModifierRandomizers"], VarType.EG_StatRandomizer);
			StatObjectArray = db.BuildArray(propertyMap["StatObjectArray"], VarType.Int); // StatObject_Equipment not in VarType enum
			StoredMana = Parse.Int(propertyMap, "StoredMana");
			SupremeFullEquipmentSetStatMultiplier = Parse.Float(propertyMap, "SupremeFullEquipmentSetStatMultiplier");
			SupremeLevelBoostAmount = Parse.Float(propertyMap, "SupremeLevelBoostAmount");
			SupremeLevelBoostRandomizerPower = Parse.Float(propertyMap, "SupremeLevelBoostRandomizerPower");
			SupremeMaxHeroStatValue = Parse.Int(propertyMap, "SupremeMaxHeroStatValue");
			TotalRandomizerMultiplier = Parse.Float(propertyMap, "TotalRandomizerMultiplier");
			TranscendentFullEquipmentSetStatMultiplier = Parse.Float(propertyMap, "TranscendentFullEquipmentSetStatMultiplier");
			TranscendentLevelBoostAmount = Parse.Float(propertyMap, "TranscendentLevelBoostAmount");
			TranscendentLevelBoostRandomizerPower = Parse.Float(propertyMap, "TranscendentLevelBoostRandomizerPower");
			TranscendentMaxHeroStatValue = Parse.Int(propertyMap, "TranscendentMaxHeroStatValue");
			Ultimate93Chance = Parse.Float(propertyMap, "Ultimate93Chance");
			UltimateDamageIncreasePerLevelMultiplier = Parse.Float(propertyMap, "UltimateDamageIncreasePerLevelMultiplier");
			UltimateFullEquipmentSetStatMultiplier = Parse.Float(propertyMap, "UltimateFullEquipmentSetStatMultiplier");
			UltimateLevelBoostAmount = Parse.Float(propertyMap, "UltimateLevelBoostAmount");
			UltimateLevelBoostRandomizerPower = Parse.Float(propertyMap, "UltimateLevelBoostRandomizerPower");
			UltimateMaxDamageIncreasePerLevel = Parse.Float(propertyMap, "UltimateMaxDamageIncreasePerLevel");
			UltimateMaxHeroStatValue = Parse.Int(propertyMap, "UltimateMaxHeroStatValue");
			UltimatePlusChance = Parse.Float(propertyMap, "UltimatePlusChance");
			UltimatePlusMaxHeroStatValue = Parse.Int(propertyMap, "UltimatePlusMaxHeroStatValue");
			UltimatePlusPlusChance = Parse.Float(propertyMap, "UltimatePlusPlusChance");
			UserEquipmentName = db.AddString(propertyMap["UserEquipmentName"]);
			UserForgerName = db.AddString(propertyMap["UserForgerName"]);
			Values = Parse.Float(propertyMap, "Values");
			WeaponAdditionalDamageAmount = Parse.Int(propertyMap, "WeaponAdditionalDamageAmount");
			WeaponAdditionalDamageAmountRandomizer = db.AddEG_StatRandomizer(new EG_StatRandomizer_Data(propertyMap["WeaponAdditionalDamageAmountRandomizer"]));
			WeaponAdditionalDamageType = db.GetDunDefDamageTypeIndex(propertyMap["WeaponAdditionalDamageType"]); 
			WeaponAdditionalDamageTypeNotPoison = Parse.BoolByte(propertyMap, "WeaponAdditionalDamageTypeNotPoison");
			WeaponAltDamageBonus = Parse.Int(propertyMap, "WeaponAltDamageBonus");
			WeaponAltDamageBonusRandomizer = db.AddEG_StatRandomizer(new EG_StatRandomizer_Data(propertyMap["WeaponAltDamageBonusRandomizer"])); 
			WeaponAltDamageMultiplier = Parse.Float(propertyMap, "WeaponAltDamageMultiplier");
			WeaponBlockingBonus = (byte)Parse.Int(propertyMap, "WeaponBlockingBonus");
			WeaponBlockingBonusRandomizer = db.AddEG_StatRandomizer(new EG_StatRandomizer_Data(propertyMap["WeaponBlockingBonusRandomizer"]));
			WeaponChargeSpeedBonus = (byte)Parse.Int(propertyMap, "WeaponChargeSpeedBonus");
			WeaponChargeSpeedBonusRandomizer = db.AddEG_StatRandomizer(new EG_StatRandomizer_Data(propertyMap["WeaponChargeSpeedBonusRandomizer"]));
			WeaponClipAmmoBonus = Parse.Int(propertyMap, "WeaponClipAmmoBonus");
			WeaponClipAmmoBonusRandomizer = db.AddEG_StatRandomizer(new EG_StatRandomizer_Data(propertyMap["WeaponClipAmmoBonusRandomizer"]));
			WeaponDamageBonus = Parse.Int(propertyMap, "WeaponDamageBonus");
			WeaponDamageBonusRandomizer = db.AddEG_StatRandomizer(new EG_StatRandomizer_Data(propertyMap["WeaponDamageBonusRandomizer"])); 
			WeaponDamageBonusRandomizerMultiplier = Parse.Float(propertyMap, "WeaponDamageBonusRandomizerMultiplier");
			WeaponDamageMultiplier = Parse.Float(propertyMap, "WeaponDamageMultiplier");
			WeaponEquipmentRatingPercentBase = Parse.Float(propertyMap, "WeaponEquipmentRatingPercentBase");
			WeaponKnockbackBonus = (byte)Parse.Int(propertyMap, "WeaponKnockbackBonus");
			WeaponKnockbackBonusRandomizer = db.AddEG_StatRandomizer(new EG_StatRandomizer_Data(propertyMap["WeaponKnockbackBonusRandomizer"]));
			WeaponKnockbackMax = Parse.Int(propertyMap, "WeaponKnockbackMax");
			WeaponNumberOfProjectilesBonus = (byte)Parse.Int(propertyMap, "WeaponNumberOfProjectilesBonus");
			WeaponNumberOfProjectilesBonusRandomizer = db.AddEG_StatRandomizer(new EG_StatRandomizer_Data(propertyMap["WeaponNumberOfProjectilesBonusRandomizer"]));
			WeaponNumberOfProjectilesQualityBaseline = Parse.Int(propertyMap, "WeaponNumberOfProjectilesQualityBaseline");
			WeaponReloadSpeedBonus = (byte)Parse.Int(propertyMap, "WeaponReloadSpeedBonus");
			WeaponReloadSpeedBonusRandomizer = db.AddEG_StatRandomizer(new EG_StatRandomizer_Data(propertyMap["WeaponReloadSpeedBonusRandomizer"]));
			WeaponShotsPerSecondBonus = (byte)Parse.Int(propertyMap, "WeaponShotsPerSecondBonus");
			WeaponShotsPerSecondBonusRandomizer = db.AddEG_StatRandomizer(new EG_StatRandomizer_Data(propertyMap["WeaponShotsPerSecondBonusRandomizer"]));
			WeaponSpeedOfProjectilesBonus = Parse.Int(propertyMap, "WeaponSpeedOfProjectilesBonus");
			WeaponSpeedOfProjectilesBonusRandomizer = db.AddEG_StatRandomizer(new EG_StatRandomizer_Data(propertyMap["WeaponSpeedOfProjectilesBonusRandomizer"]));
			WeaponSwingSpeedMultiplier = Parse.Float(propertyMap, "WeaponSwingSpeedMultiplier");
			bCanBeEquipped = Parse.BoolByte(propertyMap, "bCanBeEquipped");
			bCantBeDropped = Parse.BoolByte(propertyMap, "bCantBeDropped");
			bCantBeSold = Parse.BoolByte(propertyMap, "bCantBeSold");
			bDisableRandomization = Parse.BoolByte(propertyMap, "bDisableRandomization");
			bEquipmentFeatureByte1 = Parse.BoolByte(propertyMap, "bEquipmentFeatureByte1");
			bEquipmentFeatureByte2 = Parse.BoolByte(propertyMap, "bEquipmentFeatureByte2");
			bForceAllowDropping = Parse.BoolByte(propertyMap, "bForceAllowDropping");
			bForceAllowSelling = Parse.BoolByte(propertyMap, "bForceAllowSelling");
			bForceRandomizerWithMinEquipmentLevel = Parse.BoolByte(propertyMap, "bForceRandomizerWithMinEquipmentLevel");
			bHideQualityDescriptors = Parse.BoolByte(propertyMap, "bHideQualityDescriptors");
			bIsConsumable = Parse.BoolByte(propertyMap, "bIsConsumable");
			bIsSecondary = Parse.BoolByte(propertyMap, "bIsSecondary");
			bNoNegativeRandomizations = Parse.BoolByte(propertyMap, "bNoNegativeRandomizations");
			bUseBonusStatsFromStacking = Parse.BoolByte(propertyMap, "bUseBonusStatsFromStacking");
			bUseExtraQualityDamage = Parse.BoolByte(propertyMap, "bUseExtraQualityDamage");
			bUseSecondExtraQualityDamage = Parse.BoolByte(propertyMap, "bUseSecondExtraQualityDamage");

			// Parse weaponType enum
			weaponType = ImportMaps._weaponType.ContainsKey(propertyMap["weaponType"])
				? (int)ImportMaps._weaponType[propertyMap["weaponType"]]
				: Parse.Int(propertyMap, "weaponType");

			EquipmentDescription = db.AddString(propertyMap["EquipmentDescription"]);

			// Parse EquipmentType enum
			EquipmentType = ImportMaps._EquipmentType.ContainsKey(propertyMap["EquipmentType"])
				? (int)ImportMaps._EquipmentType[propertyMap["EquipmentType"]]
				: Parse.Int(propertyMap, "EquipmentType");

			ForDamageType = Parse.Int(propertyMap, "ForDamageType");
			MaxRandomElementalDamageMultiplier = Parse.Float(propertyMap, "MaxRandomElementalDamageMultiplier");
			MyRating = Parse.Float(propertyMap, "MyRating");
			MyRatingPercent = Parse.Float(propertyMap, "MyRatingPercent");
			PercentageReduction = Parse.Int(propertyMap, "PercentageReduction");
			UserID = Parse.Int(propertyMap, "UserID");
			WeaponAltDamageBonusUse = Parse.BoolByte(propertyMap, "WeaponAltDamageBonusUse");
			WeaponBlockingBonusUse = Parse.BoolByte(propertyMap, "WeaponBlockingBonusUse");
			WeaponChargeSpeedBonusUse = Parse.BoolByte(propertyMap, "WeaponChargeSpeedBonusUse");
			WeaponClipAmmoBonusUse = Parse.BoolByte(propertyMap, "WeaponClipAmmoBonusUse");
			WeaponKnockbackBonusUse = Parse.BoolByte(propertyMap, "WeaponKnockbackBonusUse");
			WeaponReloadSpeedBonusUse = Parse.BoolByte(propertyMap, "WeaponReloadSpeedBonusUse");
			WeaponShotsPerSecondBonusUse = Parse.BoolByte(propertyMap, "WeaponShotsPerSecondBonusUse");
			bDisableTheRandomization = Parse.BoolByte(propertyMap, "bDisableTheRandomization");
			bForceUseParentTemplate = Parse.BoolByte(propertyMap, "bForceUseParentTemplate");
			UseWeaponCoreStats = Parse.BoolByte(propertyMap, "UseWeaponCoreStats");
			bForceToMinElementalScale = Parse.BoolByte(propertyMap, "bForceToMinElementalScale");
			bForceToMaxElementalScale = Parse.BoolByte(propertyMap, "bForceToMaxElementalScale");

			IconColorAddPrimary = db.AddULinearColor(new ULinearColor_Data(propertyMap["IconColorAddPrimary"]));
			IconColorAddSecondary = db.AddULinearColor(new ULinearColor_Data(propertyMap["IconColorAddSecondary"]));
			IconColorMultPrimary = Parse.Float(propertyMap, "IconColorMultPrimary");
			IconColorMultSecondary = Parse.Float(propertyMap, "IconColorMultSecondary");

			UseColorSets = Parse.BoolByte(propertyMap, "UseColorSets");			
			PrimaryColorSets = db.BuildArray(propertyMap["PrimaryColorSets"], VarType.ULinearColor);		
			SecondaryColorSets = db.BuildArray(propertyMap["SecondaryColorSets"], VarType.ULinearColor);

			IconX = Parse.Int(propertyMap, "IconX");
			IconY = Parse.Int(propertyMap, "IconY");
			IconX1 = Parse.Int(propertyMap, "IconX1");
			IconY1 = Parse.Int(propertyMap, "IconY1");
			IconX2 = Parse.Int(propertyMap, "IconX2");
			IconY2 = Parse.Int(propertyMap, "IconY2");

			FamiliarDataIndex = Parse.Int(propertyMap, "FamiliarData");
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct HeroEquipment_Familiar_Data
	{
		// HeroEquipment_Familiar_AoeBuffer
		public float BuffRange;

		// HeroEquipment_Familiar_Corehealer
		public float HealAmountBase;
		public float HealAmountExtraMultiplier;
		public float HealAmountMaxPercent;
		public float HealInterval;
		public float HealRangeBase;
		public float HealRangeStatBase;
		public float HealRangeStatExponent;
		public float HealRangeStatMultiplier;
		public float MinimumCoreHealthPercent;
		public int StringHealAmount;                                   // localized string
		public int StringHealRange;                                    // localized string
		public int StringHealSpeed;                                    // localized string

		// HeroEquipment_Familiar_Melee_TowerScaling
		public float BaseDamageToHealRatio;
		public float DamageHealMultiplierExponent;
		public float ExtraNightmareMeleeDamageMultiplier;
		public float MaxHealMultiplierExponent;
		public float MaxHealPerDamage;
		public float MaxKnockbackMuliplier;
		public float MeleeDamageMomentum;
		public int MeleeDamageType; // DunDefDamageType
		public float MeleeHitRadius;
		public float MinHealPerDamage;
		public float RandomizedDamageMultiplierDivisor;
		public int RandomizedDamageMultiplierMaximum;
		public byte bAlsoShootProjectile;
		public byte bDoMeleeHealing;
		public byte bUseRandomizedDamage;

		// HeroEquipment_Familiar_PawnBooster
		public float BaseBoost;
		public float BoostRangeStatBase;
		public float BoostRangeStatExponent;
		public float BoostRangeStatMultiplier;
		public float BoostStatBase;
		public float BoostStatExponent;
		public float BoostStatMultiplier;
		public int BoostStatUpgradeInterval;
		public float FirstBoostInterval;
		public float MaxBoostStat;
		public int MaxNumberOfPawnsToBoost;
		public byte ProModeFocused;
		public int SoftMaxNumberOfPawnsToBoost;

		// HeroEquipment_Familiar_PlayerHealer
		public float FalloffExponent;
		public float HealRange;
		public float MinimumHealDistancePercent;
		public byte bUseFixedHealSpeed;

		// HeroEquipment_Familiar_TADPS
		public int AdditionalName; // string
		public byte bFixedProjSpeed;
		public float dpsTreshold;
		public int fixedprojspeedbonus;

		// HeroEquipment_Familiar_TowerBooster
		public float BaseBoostRange;
		public float BoostAmountMultiplier;
		public float BoostRangeExponent;
		public float ETBAttackRangeExponent;
		public float ETBAttackRateExponent;
		public float ETBDamageExponent;
		public float ETBResistanceExponent;
		public int MaxBoostStatValue;
		public int MaxNumberOfTowersToBoost;
		public float MaxRangeBoostStat;
		public int MaxTowerBoostStat;
		public int SoftMaxNumberOfTowersToBoost;

		// HeroEquipment_Familiar_TowerDamageScaling
		public float AbsoluteDamageMultiplier;
		public float AltProjectileMinimumRange;
		public float BaseDamageToManaRatio;
		public float BaseHealAmount;
		public float Damage;
		public float DamageManaMultiplierExponent;
		public byte DoLineOfSightCheck;
		public float ExtraNightmareDamageMultiplier;
		public float HealAmountMultiplier;
		public float HealingPriorityHealthPercentage;
		public float ManaMultiplier;
		public float MaxManaMultiplierExponent;
		public float MaxManaPerDamage;
		public float MinManaPerDamage;
		public float MinimumProjectileSpeed;
		public float NightmareDamageMultiplier;
		public float NightmareHealingMultiplier;
		public int Projectile; // DunDefProjectile
		public float ProjectileDamageMultiplier;
		public Array_Data ProjectileDelays; // float
		public float ProjectileShootInterval;
		public float ProjectileSpeedBonusMultiplier;
		public int ProjectileTemplate; //DunDefProjectile
		public int ProjectileTemplateAlt; // DunDefProjectile
		public Array_Data ProjectileTemplates;  // DunDefProjectiles
		public int ShotsPerSecondBonusCap;
		public float ShotsPerSecondExponent;
		public float TargetRange;
		public float WeakenEnemyTargetPercentage;
		public byte bAddManaForDamage;
		public byte bChooseHealingTarget;
		public byte bDoShotsPerSecondBonusCap;
		public byte bUseAltProjectile;
		public byte bUseFixedShootSpeed;
		public byte bWeakenEnemyTarget;

		// HeroEquipment_Familiar_TowerHealer
		public float HealRadius;
		public byte bHealOverRadius;

		// HeroEquipment_Familiar
		public float BarbStanceDamageMulti;

		public HeroEquipment_Familiar_Data(Dictionary<string, string> propertyMap, ExportedTemplateDatabase db)
		{
			BuffRange = Parse.Float(propertyMap, "BuffRange");

			HealAmountBase = Parse.Float(propertyMap, "HealAmountBase");
			HealAmountExtraMultiplier = Parse.Float(propertyMap, "HealAmountExtraMultiplier");
			HealAmountMaxPercent = Parse.Float(propertyMap, "HealAmountMaxPercent");
			HealInterval = Parse.Float(propertyMap, "HealInterval");
			HealRangeBase = Parse.Float(propertyMap, "HealRangeBase");
			HealRangeStatBase = Parse.Float(propertyMap, "HealRangeStatBase");
			HealRangeStatExponent = Parse.Float(propertyMap, "HealRangeStatExponent");
			HealRangeStatMultiplier = Parse.Float(propertyMap, "HealRangeStatMultiplier");
			MinimumCoreHealthPercent = Parse.Float(propertyMap, "MinimumCoreHealthPercent");
			StringHealAmount = db.AddString(propertyMap["StringHealAmount"]);
			StringHealRange = db.AddString(propertyMap["StringHealRange"]);
			StringHealSpeed = db.AddString(propertyMap["StringHealSpeed"]);

			BaseDamageToHealRatio = Parse.Float(propertyMap, "BaseDamageToHealRatio");
			DamageHealMultiplierExponent = Parse.Float(propertyMap, "DamageHealMultiplierExponent");
			ExtraNightmareMeleeDamageMultiplier = Parse.Float(propertyMap, "ExtraNightmareMeleeDamageMultiplier");
			MaxHealMultiplierExponent = Parse.Float(propertyMap, "MaxHealMultiplierExponent");
			MaxHealPerDamage = Parse.Float(propertyMap, "MaxHealPerDamage");
			MaxKnockbackMuliplier = Parse.Float(propertyMap, "MaxKnockbackMuliplier");
			MeleeDamageMomentum = Parse.Float(propertyMap, "MeleeDamageMomentum");
			MeleeDamageType = Parse.Int(propertyMap, "MeleeDamageType");
			MeleeHitRadius = Parse.Float(propertyMap, "MeleeHitRadius");
			MinHealPerDamage = Parse.Float(propertyMap, "MinHealPerDamage");
			RandomizedDamageMultiplierDivisor = Parse.Float(propertyMap, "RandomizedDamageMultiplierDivisor");
			RandomizedDamageMultiplierMaximum = Parse.Int(propertyMap, "RandomizedDamageMultiplierMaximum");
			bAlsoShootProjectile = Parse.BoolByte(propertyMap, "bAlsoShootProjectile");
			bDoMeleeHealing = Parse.BoolByte(propertyMap, "bDoMeleeHealing");
			bUseRandomizedDamage = Parse.BoolByte(propertyMap, "bUseRandomizedDamage");

			BaseBoost = Parse.Float(propertyMap, "BaseBoost");
			BoostRangeStatBase = Parse.Float(propertyMap, "BoostRangeStatBase");
			BoostRangeStatExponent = Parse.Float(propertyMap, "BoostRangeStatExponent");
			BoostRangeStatMultiplier = Parse.Float(propertyMap, "BoostRangeStatMultiplier");
			BoostStatBase = Parse.Float(propertyMap, "BoostStatBase");
			BoostStatExponent = Parse.Float(propertyMap, "BoostStatExponent");
			BoostStatMultiplier = Parse.Float(propertyMap, "BoostStatMultiplier");
			BoostStatUpgradeInterval = Parse.Int(propertyMap, "BoostStatUpgradeInterval");
			FirstBoostInterval = Parse.Float(propertyMap, "FirstBoostInterval");
			MaxBoostStat = Parse.Float(propertyMap, "MaxBoostStat");
			MaxNumberOfPawnsToBoost = Parse.Int(propertyMap, "MaxNumberOfPawnsToBoost");
			ProModeFocused = Parse.BoolByte(propertyMap, "ProModeFocused");
			SoftMaxNumberOfPawnsToBoost = Parse.Int(propertyMap, "SoftMaxNumberOfPawnsToBoost");

			FalloffExponent = Parse.Float(propertyMap, "FalloffExponent");
			HealRange = Parse.Float(propertyMap, "HealRange");
			MinimumHealDistancePercent = Parse.Float(propertyMap, "MinimumHealDistancePercent");
			bUseFixedHealSpeed = Parse.BoolByte(propertyMap, "bUseFixedHealSpeed");

			AdditionalName = Parse.Int(propertyMap, "AdditionalName");
			bFixedProjSpeed = Parse.BoolByte(propertyMap, "bFixedProjSpeed");
			dpsTreshold = Parse.Float(propertyMap, "dpsTreshold");
			fixedprojspeedbonus = Parse.Int(propertyMap, "fixedprojspeedbonus");

			BaseBoostRange = Parse.Float(propertyMap, "BaseBoostRange");
			BoostAmountMultiplier = Parse.Float(propertyMap, "BoostAmountMultiplier");
			BoostRangeExponent = Parse.Float(propertyMap, "BoostRangeExponent");
			ETBAttackRangeExponent = Parse.Float(propertyMap, "ETBAttackRangeExponent");
			ETBAttackRateExponent = Parse.Float(propertyMap, "ETBAttackRateExponent");
			ETBDamageExponent = Parse.Float(propertyMap, "ETBDamageExponent");
			ETBResistanceExponent = Parse.Float(propertyMap, "ETBResistanceExponent");
			MaxBoostStatValue = Parse.Int(propertyMap, "MaxBoostStatValue");
			MaxNumberOfTowersToBoost = Parse.Int(propertyMap, "MaxNumberOfTowersToBoost");
			MaxRangeBoostStat = Parse.Float(propertyMap, "MaxRangeBoostStat");
			MaxTowerBoostStat = Parse.Int(propertyMap, "MaxTowerBoostStat");
			SoftMaxNumberOfTowersToBoost = Parse.Int(propertyMap, "SoftMaxNumberOfTowersToBoost");

			AbsoluteDamageMultiplier = Parse.Float(propertyMap, "AbsoluteDamageMultiplier");
			AltProjectileMinimumRange = Parse.Float(propertyMap, "AltProjectileMinimumRange");
			BaseDamageToManaRatio = Parse.Float(propertyMap, "BaseDamageToManaRatio");
			BaseHealAmount = Parse.Float(propertyMap, "BaseHealAmount");
			Damage = Parse.Float(propertyMap, "Damage");
			DamageManaMultiplierExponent = Parse.Float(propertyMap, "DamageManaMultiplierExponent");
			DoLineOfSightCheck = Parse.BoolByte(propertyMap, "DoLineOfSightCheck");
			ExtraNightmareDamageMultiplier = Parse.Float(propertyMap, "ExtraNightmareDamageMultiplier");
			HealAmountMultiplier = Parse.Float(propertyMap, "HealAmountMultiplier");
			HealingPriorityHealthPercentage = Parse.Float(propertyMap, "HealingPriorityHealthPercentage");
			ManaMultiplier = Parse.Float(propertyMap, "ManaMultiplier");
			MaxManaMultiplierExponent = Parse.Float(propertyMap, "MaxManaMultiplierExponent");
			MaxManaPerDamage = Parse.Float(propertyMap, "MaxManaPerDamage");
			MinManaPerDamage = Parse.Float(propertyMap, "MinManaPerDamage");
			MinimumProjectileSpeed = Parse.Float(propertyMap, "MinimumProjectileSpeed");
			NightmareDamageMultiplier = Parse.Float(propertyMap, "NightmareDamageMultiplier");
			NightmareHealingMultiplier = Parse.Float(propertyMap, "NightmareHealingMultiplier");
			Projectile = Parse.Int(propertyMap, "Projectile");
			ProjectileDamageMultiplier = Parse.Float(propertyMap, "ProjectileDamageMultiplier");
			ProjectileDelays = db.BuildArray(propertyMap["ProjectileDelays"], VarType.Float);
			ProjectileShootInterval = Parse.Float(propertyMap, "ProjectileShootInterval");
			ProjectileSpeedBonusMultiplier = Parse.Float(propertyMap, "ProjectileSpeedBonusMultiplier");
			ProjectileTemplate = Parse.Int(propertyMap, "ProjectileTemplate");
			ProjectileTemplateAlt = Parse.Int(propertyMap, "ProjectileTemplateAlt");
			ProjectileTemplates = db.BuildArray(propertyMap["ProjectileTemplates"], VarType.DunDefProjectile);
			ShotsPerSecondBonusCap = Parse.Int(propertyMap, "ShotsPerSecondBonusCap");
			ShotsPerSecondExponent = Parse.Float(propertyMap, "ShotsPerSecondExponent");
			TargetRange = Parse.Float(propertyMap, "TargetRange");
			WeakenEnemyTargetPercentage = Parse.Float(propertyMap, "WeakenEnemyTargetPercentage");
			bAddManaForDamage = Parse.BoolByte(propertyMap, "bAddManaForDamage");
			bChooseHealingTarget = Parse.BoolByte(propertyMap, "bChooseHealingTarget");
			bDoShotsPerSecondBonusCap = Parse.BoolByte(propertyMap, "bDoShotsPerSecondBonusCap");
			bUseAltProjectile = Parse.BoolByte(propertyMap, "bUseAltProjectile");
			bUseFixedShootSpeed = Parse.BoolByte(propertyMap, "bUseFixedShootSpeed");
			bWeakenEnemyTarget = Parse.BoolByte(propertyMap, "bWeakenEnemyTarget");

			HealRadius = Parse.Float(propertyMap, "HealRadius");
			bHealOverRadius = Parse.BoolByte(propertyMap, "bHealOverRadius");

			BarbStanceDamageMulti = Parse.Float(propertyMap, "BarbStanceDamageMulti");
		}
	}
	// Culture-invariant parsing everywhere (recommended for data files)
	internal static class Parse
	{
		// Culture-invariant parsing everywhere
		private static readonly CultureInfo CI = CultureInfo.InvariantCulture;

		// Key = The property name (e.g., "WeaponDamage")
		// Value = The string value that failed to parse (e.g., "100.55")
		// We use a Dictionary to ensure keys are unique (recorded only once)
		public static Dictionary<string, string> UniqueFailedKeys = new();

		public static int Int(Dictionary<string, string> map, string key, int fallback = 0)
		{
			if (map == null) return fallback;

			if (map.TryGetValue(key, out var s))
			{
				if (int.TryParse(s, NumberStyles.Integer, CI, out var v))
				{
					return v;
				}

				// If this key hasn't been logged yet, add it
				if (!UniqueFailedKeys.ContainsKey(key))
				{
					UniqueFailedKeys.Add(key, $"Expected Int, got: '{s}'");
				}
			}

			return fallback;
		}

		public static uint UInt(Dictionary<string, string> map, string key, uint fallback = 0u)
		{
			if (map == null) return fallback;

			if (map.TryGetValue(key, out var s))
			{
				if (uint.TryParse(s, NumberStyles.Integer, CI, out var v))
				{
					return v;
				}

				if (!UniqueFailedKeys.ContainsKey(key))
				{
					UniqueFailedKeys.Add(key, $"Expected UInt, got: '{s}'");
				}
			}

			return fallback;
		}

		public static float Float(Dictionary<string, string> map, string key, float fallback = 0f)
		{
			if (map == null) return fallback;

			if (map.TryGetValue(key, out var s))
			{
				if (float.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CI, out var v))
				{
					return v;
				}

				if (!UniqueFailedKeys.ContainsKey(key))
				{
					UniqueFailedKeys.Add(key, $"Expected Float, got: '{s}'");
				}
			}

			return fallback;
		}

		public static byte BoolByte(Dictionary<string, string> map, string key, byte fallback = 0)
		{
			if (map == null) return fallback;

			if (map.TryGetValue(key, out var s))
			{
				// Standard boolean parsing (True/False)
				if (bool.TryParse(s, out var result))
				{
					return result ? (byte)1 : (byte)0;
				}

				// Handling 0/1 as booleans (common in some formats)
				if (s == "1") return 1;
				if (s == "0") return 0;

				if (!UniqueFailedKeys.ContainsKey(key))
				{
					UniqueFailedKeys.Add(key, $"Expected Bool, got: '{s}'");
				}
			}

			return fallback;
		}
	}
}
