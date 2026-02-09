using System.Numerics;
using System.Xml.Linq;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace DDUP
{
	// Sort of obsolete
	public struct Vector3 
	{
		public float x;
		public float y;
		public float z;
	}

	public class FamiliarTemplate
	{
		public string FamiliarClassName = "";  											 

		public string ProjectileTemplate = "";
		public float ProjectileDamageMultiplier = 1.0f;
		public float AbsoluteDamageMultiplier = 1.0f;
		public float NightmareDamageMultiplier = 17.0f;
		public float ExtraNightmareDamageMultiplier = 0.65f;

		public bool ScaleHeroDamage = false;
		public float ScaleDamageStatExponent = 0.75f;

		public bool bMythicalScaleHeroDamage = false;
		public float MythicalScaleDamageStatExponent = 0.575f;

		public float ProjectileShootInterval = 3.0f;
		public float ShotsPerSecondExponent = 1.5f;

		// MULTIPLE PROJECTILES
		public List<string> ProjectileTemplates = new();
		public bool bChooseRandomProjectileTemplate = false;

		// ALT PROJECTILE (some familiars switch projectiles based on range)
		public bool bUseAltProjectile = false;
		public string ProjectileTemplateAlt = "";
		public float AltProjectileMinimumRange = 0f;

		// ATTACK SPEED LIMITS
		public float MaxAttackAnimationSpeed = 2.4f;
		public bool bDoShotsPerSecondBonusCap = false;
		public int ShotsPerSecondBonusCap = 0;

		// TARGETING (affects effective DPS)
		public float TargetRange = 1000.0f;
		public bool DoLineOfSightCheck = false;  // Wall piercing related!

		// ARCHETYPE STACKING (damage per duplicate)
		public bool bUseStackingDamagePerArchetype = false;
		public float PercIncreasePerStack = 0.1f;

		// STANCE INTERACTION
		public bool AllowBarbStanceDamageReduction = false;
		public float BarbStanceDamageMulti = 1.0f;

		// MELEE PROPERTIES
		public bool ScaleMeleeDamageForHero = false;
		public float MeleeRange = 0f;
		public float MeleeHitRadius = 0f;
		public string MeleeDamageType = "";
		public float ExtraNightmareMeleeDamageMultiplier = 1.0f;

		// RANDOMIZED DAMAGE
		public bool bUseRandomizedDamage = false;
		public int RandomizedDamageMultiplierMaximum = 0;
		public float RandomizedDamageMultiplierDivisor = 1.0f;

		// MELEE + PROJECTILE
		public bool bAlsoShootProjectile = false;

		// HEALING FLAGS
		public bool bChooseHealingTarget = false;
		public bool bDoMeleeHealing = false;

		// HEALING AMOUNTS
		public float BaseHealAmount = 0f;
		public float HealAmountMultiplier = 1.0f;
		public float NightmareHealingMultiplier = 1.0f;

		// HEALING TIMING
		public float HealInterval = 5.5f;
		public bool bUseFixedHealSpeed = false;

		// HEALING AOE/RANGE
		public float HealRange = 650.0f;
		public float FalloffExponent = 0.55f;
		public float MinimumHealDistancePercent = 0.2f;

		// MELEE HEALING CONVERSION
		public float BaseDamageToHealRatio = 0f;
		public float MaxHealPerDamage = 0f;
		public float MinHealPerDamage = 0f;
		public float MaxHealMultiplierExponent = 1.0f;
		public float DamageHealMultiplierExponent = 1.0f;

		public bool bAddManaForDamage = false;
		public float BaseDamageToManaRatio = 0f;
		public float ManaMultiplier = 1.0f;
		public float MaxManaPerDamage = 0f;
		public float MinManaPerDamage = 0f;
		public float MaxManaMultiplierExponent = 1.0f;
		public float DamageManaMultiplierExponent = 1.0f;
		public bool bAddHealthCost = false;
		public bool bAddHealthCostToDamage = false;
		public float HealthCostPercentage = 0f;
		public float HealthCostToDamageMultiplier = 1.0f;
		public bool bSlowEnemyTarget = false;
		public bool bWeakenEnemyTarget = false;
		public float WeakenEnemyTargetPercentage = 0.6f;  // 60% weaken = enemy takes 60% more damage!
		public float EnemyClearWeakenTime = 5.0f;
	}


	public struct MeleeSwingInfo
	{
		public float DamageMultiplier;
		public float SwingAnimationDuration;
		public float AnimSpeed;

		public float TimeBeforeEndToAllowNextCombo;
		public float TimeAfterEndToAllowNextCombo;		
	}

	public class Projectile
	{
		public bool bUseProjetilePerDistanceScaling;
		public string ProjectileTemplate = "";
		public float ProjDamage;
		public int AdditionalDamageAmount;
		public string ProjDamageType = "";
		public string AdditionalDamageType = "";
		public float ProjDamageRadius;
		public float DamageRadiusFallOffExponent;

		public bool ScaleHeroDamage;
		public bool MultiplyProjectileDamageByWeaponDamage;
		public bool MultiplyProjectileDamageByPrimaryWeaponSwingSpeed;

		//public LevelUpValueType ScaleDamageStatType;
		public float ScaleDamageStatExponent;
		//public LevelUpValueType SecondScaleDamageStatType;
		public bool bSecondScaleDamageStatType;
		public bool BSecondScaleDamageStatOnAdditionalDamage;

		public float ProjectileSpeed;
		public float ProjectileMaxSpeed;
		public float ProjectileLifespan;

		public bool bLimitDistance;
		public float LimitDistanceAmount;
		public float LimitDistanceGRIMultiplier;
		public bool bUseProjectilePerDistanceScaling;
		public float ProjectileDamagePerDistanceTravelled;

		public float TowerDamageMultiplier;
		public float ProjectileDamageByWeaponDamageDivider;
		public bool bScaleDamagePerLevel;
		public bool bAlwaysUseRandomDamageType;
		public List<string> RandomDamageTypes = new();
		public bool bApplyBuffsOnAoe;
		public List<string> BuffsToApplyOnImpact = new();

		// _MagicBolt
		public float MyChargePercentage;
		public float TheDamageMinScale;
		public float TheDamageMaxScale;
		public float ExtraDamageMaxScale;

		public float RadiusMinScale;
		public float RadiusMaxScale;

		public float CollisionMinSize;
		public float CollisionMaxSize;

		// _Bouncing
		public int MaxHits;
		public float ScalingPerBounceMultiplier;
		public List<float> ScalingPerBounce = new();
		public float BounceRange;

		// Situational
		public bool bAutoChooseNewTarget;
		public bool bCalcBestTargetBasedOnDistance;
		public bool bExcludePreviousTargets;
		public bool bChangeTargetWhenCurrentTargetDies;
		public bool bOnlyBounceOnAimTarget;
		public int MaxHitToSameTarget;

		// Healing (some bouncing projectiles heal)
		public bool bHealingProjectile;

		// Wall bouncing
		public bool bBounceOffWalls;
		public int MaxWallBounces;

		// Hover behavior
		public bool bHoverWhenNoTarget;
		public bool bMoveForwardWhileHover;
		public float HoverExpireTime;
		public float ChooseNewTargetDelay;

		// Bounce velocity
		public float BounceVelocityMultiplier;
	}



	public class WeaponTemplate
	{
		// DunDefWeapon EquipmentWeaponTemplate
		public string WeaponTemplateName = "";
		public int BaseDamage;
		public float WeaponDamageMultiplier;
		public string ProjectileTemplate = "";
		public int AdditionalDamageAmount;
		public string AdditionalDamageType = "";
		public int BaseShotsPerSecond;

		public int BaseAltDamage;
		public bool bIsMeleeWeapon;
		public List<string> ExtraProjectileTemplates = new();
		public int BaseTotalAmmo;

		public bool bUseAdditionalProjectileDamage;
		public float EquipmentSwingSpeedMultiplier;
		public float ProjectileSpeedBonusMultiplier;
		public float ProjectileSpeedAddition;
		public float MinimumProjectileSpeed;
		public bool bUseAltDamageForProjectileBaseDamage;
		public int NumProjectiles;

		public float FullchargeRefireInterval;
		public float FullChargeTime;              // Default: 2.0
		public float FullAltChargeTime;
		public float BaseChargeSpeed;             // Default: 1.0
		public float ChargeSpeedBonusLinearScale; // Default: 0.2
		public float ChargeSpeedBonusExpScale;    // Default: 0.9

		public float SpeedMultiplier;                      // Default: 1.0
		public float ExtraSpeedMultiplier;                  // Default: 0.8
		public float DamageIncreaseForSwingSpeedFactor;    // Default: 1.55
		public float SpeedMultiplierDamageExponent;        // Default: 0.75
		public float DamageMultiplier;                     // Default: 1.0
		public float MomentumMultiplier;                    // Default: 1.0
		public float MeleeDamageMomentum;                   // 50000
		public string BaseMeleeDamageType = "";
		public Vector3 MeleeSwingExtent;
		public bool bShootMeleeProjectile;
		public bool bUseWeaponDamageForProjectileDamage;
		public float WeaponProjectileDamageMultiplier;
		public float ProjectileDamageHeroStatExponentMultiplier;
		public bool bOnlyShootProjectilesAtFullHealth;
		public float MinimumShootProjectileDotProduct;

		public bool bSlowEnemyTarget;
		public bool bWeakenEnemyTarget;
		public float SlowEnemyTargetPercentage;     // Slow amount (default: 0.5 = 50%)
		public float WeakenEnemyTargetPercentage;   // Weaken amount (default: 0.6 = 60%)
		public float EnemyClearSlowTime;            // Slow duration (default: 5.0s)
		public float EnemyClearWeakenTime;          // Weaken duration (default: 5.0s)
		public float MinimumSwingDamageTime;
		public float MinimumSwingTime;
		public List<MeleeSwingInfo> MeleeSwingInfos = new();

		public int BaseNumProjectiles;
		public float FireIntervalMultiplier = 1.0f;
		public bool bUseHighShotPerSecond = false;
		public float BaseReloadSpeed;
		public float ReloadSpeedBonusLinearScale;
		public float ReloadSpeedBonusExpScale;
		public float ReloadSpeedMultiplier = 1.0f;
		public float MinimumReloadTime;
		public bool bUseFixedReloadSpeed;
		public float FixedReloadSpeed;
		public int AmmoConsumptionPerShot = 1;
		public float ReloadSpeedNoAmmoMultiplier;

	}


	public class ItemEntry
	{
		// fix up from dictionary after
		public string TemplateName = "";

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
	
		// Fields for Damage Calculations;
		public string WeaponTemplate = ""; // DunDefWeapon or derived
		public float ScaleDamageStatExponent;
		public float MythicalScaleDamageStatExponent;
		public string ProjectileTemplate = "";
		public List<string> ExtraProjectileTemplate = new();
		public float ProjectileSpeedBonusMultiplier;
		
		public bool bUseExtraQualityDamage;		
		public bool bUseSecondExtraQualityDamage;
		public bool bWeaponAdditionalDamageTypeNotPoison;
		public float TranscendentLevelBoostAmount;
		public float SupremeLevelBoostAmount;
		public float UltimateLevelBoostAmount;
		public float ElementalDamageMultiplier;
		public float ElementalDamageIncreasePerLevelMultiplier;
		public float DamageIncreasePerLevelMultiplier;
		public float UltimateDamageIncreasePerLevelMultiplier;
		public float ExtraQualityDamageIncreasePerLevelMultiplier;
		public float SecondExtraQualityDamageIncreasePerLevelMultiplier;
		public float SecondExtraQualityMaxDamageIncreasePerLevel;
		
		public float MaxElementalDamageIncreasePerLevel;
		public float MinElementalDamageIncreasePerLevel;
		public float MaxDamageIncreasePerLevel;
		public float UltimateMaxDamageIncreasePerLevel;
		public float ExtraQualityMaxDamageIncreasePerLevel;
		public float AltDamageIncreasePerLevelMultiplier;
		public float WeaponDamageDisplayValueScale;
		public float WeaponAltDamageDisplayValueScale;
		public float PlayerSpeedMultiplier;
		public float WeaponDamageMultiplier;
		public float AltWeaponDamageMultiplier;
		

		
	}

	


}
