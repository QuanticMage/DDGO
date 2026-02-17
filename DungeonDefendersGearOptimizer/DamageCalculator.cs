using Microsoft.VisualBasic;
using System.ComponentModel;
using System.Reflection.Emit;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;
using System.Security.Claims;
using static System.Net.Mime.MediaTypeNames;

namespace DDUP
{
	// Inputs needed
	// 

	// Outputs:
	// Theoretical DPS vs. training dummy
	// How good this item is vs. a max drop on weapon damage.  How many drops I would need before I got something better.
	// Piercing, AOE, Heal, Mana, Wall info


	public class HeroInfo
	{
		public int[] TotalStats = new int[11];     // regular stats		
		public DunDefHero_Data TemplateData;
		public DunDefPlayer_Data PlayerTemplateData;
		public float GlobalDamageMultiplier = 0.155f;        // nightmare mode, unclear but definitely there modifier
		public float PlayerElementalWeaponDamageMultiplier = 1.0f;

		public float MeleeAttack1LargeAnimDuration = 1.0f;
		public float MeleeAttack2LargeAnimDuration = 1.0f;
		public float MeleeAttack3LargeAnimDuration = 0.5f;
		public float MeleeAttack1MediumAnimDuration = 0.7f;
		public float MeleeAttack2MediumAnimDuration = 0.533f;
		public float MeleeAttack3MediumAnimDuration = 1.033f;


		public float MeleeAttack1LargeAnimDamageStart = 0.0f;
		public float MeleeAttack2LargeAnimDamageStart = 0.0f;
		public float MeleeAttack3LargeAnimDamageStart = 0.0f;
		public float MeleeAttack1MediumAnimDamageStart = 0.0f;
		public float MeleeAttack2MediumAnimDamageStart = 0.0f;
		public float MeleeAttack3MediumAnimDamageStart = 0.0f;
	}


	public class DamageCalculator
	{
		public TemplateDatabase? tdb = null;
		public DamageCalculator(TemplateDatabase _tdb)
		{
			tdb = _tdb;
		}

		public float QuantizeToAnimFrameTime( float value )
		{
			return (MathF.Ceiling(value * 30.0f) / 30.0f);
		}

		public float Hero_GetHeroDamageMult(ref HeroInfo heroInfo)
		{
			return Hero_GetHeroStatMult((int)LevelUpValueType.HeroDamage, ref heroInfo);			
		}

		public float Hero_GetHeroStatMult(int statType, ref HeroInfo heroInfo)
		{
			float stat = 0;
			float initMult = 0.0f;
			float initExp = 0.0f;
			float fullMult = 0.0f;
			float fullExp = 0.0f;
			float initialComponent = 0.0f;
			float fullComponent = 0.0f;			
			switch ((LevelUpValueType)statType)
			{
				case LevelUpValueType.HeroDamage:
					// Damage has a typo - missing ().  We persist it here
					stat = heroInfo.TotalStats[(int)DDStat.HeroDamage];
					initialComponent = heroInfo.TemplateData.StatMultInitial_HeroDamage *
						((MathF.Pow(4, (heroInfo.TemplateData.StatExpInitial_HeroDamage * 1.1f)) - 1.0f) +
						heroInfo.TemplateData.StatMultFull_HeroDamage *
						 (MathF.Pow(stat + 1, (heroInfo.TemplateData.StatExpFull_HeroDamage * 1.1f)) - 1.0f));
					return 1.0f + initialComponent;
				case LevelUpValueType.HeroHealth:
					stat = heroInfo.TotalStats[(int)DDStat.HeroHealth];
					float playerHealth = 1;// heroInfo.PlayerTemplateData.Health; // TODO: need a calculation for health
					fullExp = heroInfo.TemplateData.HeroHealthExponentialFactor;
					fullMult = heroInfo.TemplateData.HeroHealthLinearFactor;
					return (float)(playerHealth * (1.0000000 + fullMult * Math.Pow(stat, fullExp)));
				case LevelUpValueType.HeroCastRate:
					stat = heroInfo.TotalStats[(int)DDStat.HeroCastRate];
					initMult = heroInfo.PlayerTemplateData.StatMultInitial_HeroCastingRate;
					initExp = heroInfo.PlayerTemplateData.StatExpInitial_HeroCastingRate;
					fullMult = heroInfo.PlayerTemplateData.StatMultFull_HeroCastingRate;
					fullExp = heroInfo.PlayerTemplateData.StatExpFull_HeroCastingRate;
					break; // use standard formula
				case LevelUpValueType.Ability1:
					stat = heroInfo.TotalStats[(int)DDStat.HeroAbility1];
					initMult = heroInfo.TemplateData.StatMultInitial_HeroAbilityOne;
					initExp = heroInfo.TemplateData.StatExpInitial_HeroAbilityOne;
					fullMult = heroInfo.TemplateData.StatMultFull_HeroAbilityOne;
					fullExp = heroInfo.TemplateData.StatExptFull_HeroAbilityOne;
					break; // use standard formula
				case LevelUpValueType.Ability2:
					stat = heroInfo.TotalStats[(int)DDStat.HeroAbility2];
					initMult = heroInfo.TemplateData.StatMultInitial_HeroAbilityTwo;
					initExp = heroInfo.TemplateData.StatExpInitial_HeroAbilityTwo;
					fullMult = heroInfo.TemplateData.StatMultFull_HeroAbilityTwo;
					fullExp = heroInfo.TemplateData.StatExptFull_HeroAbilityTwo;
					break; // use standard formula
				case LevelUpValueType.TowerHealth:
					stat = heroInfo.TotalStats[(int)DDStat.TowerHealth];
					initMult = heroInfo.TemplateData.StatMultInitial_DefenseHealth;
					initExp = heroInfo.TemplateData.StatExpInitial_DefenseHealth;
					fullMult = heroInfo.TemplateData.StatMultFull_DefenseHealth;
					fullExp = heroInfo.TemplateData.StatExptFull_DefenseHealth;
					break; // use standard formula
				case LevelUpValueType.TowerDamage:
					stat = heroInfo.TotalStats[(int)DDStat.TowerDamage];
					initMult = heroInfo.TemplateData.StatMultInitial_DefenseDamage;
					initExp = heroInfo.TemplateData.StatExpInitial_DefenseDamage;
					fullMult = heroInfo.TemplateData.StatMultFull_DefenseDamage;
					fullExp = heroInfo.TemplateData.StatExptFull_DefenseDamage;
					break; // use standard formula
				case LevelUpValueType.TowerRange:
					stat = heroInfo.TotalStats[(int)DDStat.TowerRange];
					initMult = heroInfo.TemplateData.StatMultInitial_DefenseAOE;
					initExp = heroInfo.TemplateData.StatExpInitial_DefenseAOE;
					fullMult = heroInfo.TemplateData.StatMultFull_DefenseAOE;
					fullExp = heroInfo.TemplateData.StatExptFull_DefenseAOE;
					break; // use standard formula
				case LevelUpValueType.TowerRate:
					stat = heroInfo.TotalStats[(int)DDStat.TowerRange];
					initMult = heroInfo.TemplateData.StatMultInitial_DefenseAOE;
					initExp = heroInfo.TemplateData.StatExpInitial_DefenseAOE;
					fullMult = heroInfo.TemplateData.StatMultFull_DefenseAOE;
					fullExp = heroInfo.TemplateData.StatExptFull_DefenseAOE;
					initialComponent = heroInfo.TemplateData.StatMultInitial_HeroDamage *
						(MathF.Pow(4, heroInfo.TemplateData.StatExpInitial_HeroDamage) - 1.0f);
					fullComponent = heroInfo.TemplateData.StatMultFull_HeroDamage *
						(MathF.Pow(stat + 1, heroInfo.TemplateData.StatExpFull_HeroDamage ) - 1.0f);
					return 1.0f / (1.0f + initialComponent + fullComponent);

				// all other types follow this same formula, but with the parenthesis in the right location
				default:
					Console.WriteLine($"Need to handle stat {statType}");					
					break;					
			}
			initialComponent = initMult * ((float)MathF.Pow(4, initExp) - 1.0f);
			fullComponent = fullMult * ((float)MathF.Pow(stat + 1, fullExp) - 1.0f); 				
			return (1.0f + initialComponent + fullComponent);
		}


		public float Player_GetPawnDamageMult(ref HeroInfo heroInfo)
		{
			return heroInfo.PlayerTemplateData.ExtraPlayerDamageMultiplier *
				   heroInfo.PlayerTemplateData.PlayerWeaponDamageMultiplier *
				   heroInfo.GlobalDamageMultiplier *
				   (1.0f + heroInfo.PlayerTemplateData.DamageMultiplierAdditional);
		}

		float GetEquipmentDamageBonus(ItemViewRow viewRow, bool bUseUpgraded, ref HeroEquipment_Data equipTemplate)
		{
			int instanceWeaponDamageBonus = (bUseUpgraded) ? viewRow.UpgradedWeaponDamageBonus : viewRow.WeaponDamageBonus;

			// Step 1: Apply multipliers to bonus
			float floatValue = equipTemplate.WeaponDamageMultiplier  * instanceWeaponDamageBonus;

			return floatValue;
		}		

		public float GetEquipmentDamageBonusNormalized(float Normalizer, ItemViewRow viewRow, bool bUseUpgraded, ref HeroInfo heroInfo, ref DunDefWeapon_Data weaponTemplate, ref HeroEquipment_Data equipTemplate)
		{
			return 
				(float)Math.Pow(weaponTemplate.BaseDamage * weaponTemplate.WeaponDamageMultiplier * (int)equipTemplate.WeaponDamageMultiplier + GetEquipmentDamageBonus(viewRow, bUseUpgraded, ref equipTemplate),
				Normalizer) * Hero_GetHeroDamageMult(ref heroInfo) * Player_GetPawnDamageMult(ref heroInfo);
		}

		float GetEquipmentAltDamageBonus(ItemViewRow viewRow, bool bUseUpgraded, ref HeroEquipment_Data equipTemplate)
		{
			int instanceWeaponAltDamageBonus = (bUseUpgraded) ? viewRow.UpgradedWeaponAltDamageBonus : viewRow.WeaponAltDamageBonus;
			if (equipTemplate.WeaponAltDamageBonusUse == 0)
				return 0.0f;
			return equipTemplate.WeaponAltDamageMultiplier * instanceWeaponAltDamageBonus;
        }

		float GetEquipmentAdditionalDamageAmount(ItemViewRow viewRow, bool bUseUpgraded, ref HeroInfo heroInfo, ref HeroEquipment_Data equipTemplate)
		{
			int instanceWeaponAdditionalDamageAmount = (bUseUpgraded) ? viewRow.UpgradedWeaponAdditionalDamageAmount : viewRow.WeaponAdditionalDamageAmount;

			return equipTemplate.ElementalDamageMultiplier  * heroInfo.PlayerElementalWeaponDamageMultiplier * instanceWeaponAdditionalDamageAmount;			
		}

		(float,float,float) GetProjectileArrayDamage(int nProjectiles, float ProjectileMainDamage, float ProjectileAdditionalDamage, float scaleDamageExponentMultiplier, float chargeAmount, ItemViewRow viewRow, bool bUseUpgraded, ref HeroInfo heroInfo, ref DunDefWeapon_Data weaponTemplate, ref HeroEquipment_Data equipTemplate)
		{
			float totalDamage = 0.0f;
			float standardProjDamage = 0.0f;
			float standardAdditionalDamage = 0.0f;
			if (weaponTemplate.ProjectileTemplate != -1)
			{
				var projectileTemplate = tdb!.GetDunDefProjectile(weaponTemplate.ProjectileTemplate);
				(standardProjDamage, standardAdditionalDamage) = GetProjectileDamage(ProjectileMainDamage, ProjectileAdditionalDamage, scaleDamageExponentMultiplier, chargeAmount, viewRow, bUseUpgraded, ref heroInfo, ref projectileTemplate, ref weaponTemplate, ref equipTemplate);
			}
					
			if ((weaponTemplate.bRandomizeProjectileTemplate == 1) && weaponTemplate.RandomizedProjectileTemplate.Count > 0)
			{
				totalDamage = 0.0f;
				for (int i = 0; i < weaponTemplate.RandomizedProjectileTemplate.Count; i++)
				{
					var randomProjectile = tdb!.GetDunDefProjectile(weaponTemplate.RandomizedProjectileTemplate.Start + i);
					(float main, float extra) = GetProjectileDamage(ProjectileMainDamage, ProjectileAdditionalDamage, scaleDamageExponentMultiplier, chargeAmount, viewRow, bUseUpgraded, ref heroInfo, ref randomProjectile, ref weaponTemplate, ref equipTemplate);
					totalDamage += main + extra;
				}
				totalDamage = totalDamage / (float)weaponTemplate.RandomizedProjectileTemplate.Count * nProjectiles;
			}
			else
			{
				for (int i = 0; i < nProjectiles; i++)
				{
					if (weaponTemplate.ExtraProjectileTemplates.Count > i)
					{
						var extraProjectile = tdb!.GetDunDefProjectile(weaponTemplate.ExtraProjectileTemplates.Start + i);
						(float main, float extra) = GetProjectileDamage(ProjectileMainDamage, ProjectileAdditionalDamage, scaleDamageExponentMultiplier, chargeAmount, viewRow, bUseUpgraded, ref heroInfo, ref extraProjectile, ref weaponTemplate, ref equipTemplate);
						totalDamage += main + extra;
					}
					else
					{
						totalDamage += standardProjDamage + standardAdditionalDamage;
					}
				}				
			}
			return (totalDamage, standardProjDamage, standardAdditionalDamage);
		}


		(float, float) GetProjectileDamage(float ProjectileMainDamage, float ProjectileAdditionalDamage, float scaleDamageExponentMultiplier, float chargeAmount, ItemViewRow viewRow, bool bUseUpgraded, ref HeroInfo heroInfo, ref DunDefProjectile_Data projTemplate, ref DunDefWeapon_Data weaponTemplate, ref HeroEquipment_Data equipTemplate)
		{
			float baseDamage = ProjectileMainDamage;
			float additionalDamage = ProjectileAdditionalDamage;
			float instanceWeaponSwingSpeedMultiplier = viewRow.WeaponSwingSpeedMultiplier;

			// TODO - base damage based on level override
			if (projTemplate.ScaleHeroDamage == 1)
			{
				if (projTemplate.MultiplyProjectileDamageByWeaponDamage == 1)
				{
					float weaponDamageNormalized = GetEquipmentDamageBonusNormalized(projTemplate.ProjectileDamageByWeaponDamageDivider, viewRow, bUseUpgraded, ref heroInfo, ref weaponTemplate, ref equipTemplate);
					float damageMult = weaponDamageNormalized;
					if (weaponTemplate.bUseDamageReductionForAbilities == 1)
					{
						if (weaponDamageNormalized > 7500000.0f)
						{
							float excess = weaponDamageNormalized - 7500000.0f;
							damageMult = (7500000.0f + (excess * 0.75f));
						}
					}
					baseDamage *= damageMult;
				}

				baseDamage *= (float)Math.Pow(Hero_GetHeroStatMult(projTemplate.ScaleDamageStatType, ref heroInfo), projTemplate.ScaleDamageStatExponent * scaleDamageExponentMultiplier);

				if (projTemplate.MultiplyProjectileDamageByPrimaryWeaponSwingSpeed == 1)
				{
					baseDamage *= MathF.Max(instanceWeaponSwingSpeedMultiplier, 1.0f);
				}

				float scalingFactor = 1.0f;
				if (projTemplate.bSecondScaleDamageStatType == 1)
				{
					scalingFactor = (float)Math.Pow(Hero_GetHeroStatMult(projTemplate.SecondScaleDamageStatType, ref heroInfo), projTemplate.ScaleDamageStatExponent * scaleDamageExponentMultiplier);
					// End:0x337
					if (projTemplate.bSecondScaleDamageStatOnAdditionalDamage == 1)
					{
						additionalDamage *= scalingFactor;
					}
					else
					{
						baseDamage *= scalingFactor;
					}
				}

				if (chargeAmount != -1)
				{

					float damageChargeScale = (projTemplate.TheDamageMinScale + (chargeAmount * ((projTemplate.ExtraDamageMaxScale * projTemplate.TheDamageMaxScale) - projTemplate.TheDamageMinScale)));
					baseDamage *= damageChargeScale;
					additionalDamage *= damageChargeScale;
				}

				if (projTemplate.FireDamageScale > 0 )
				{
					// hardcoded because it would be a lot to thread this through.  Someday maybe
					float baseFireDamage = 10.0f;
					int numTicks = 24;

					float fireDamage = baseFireDamage * projTemplate.FireDamageScale * baseDamage;
					// this leaves fire, like MM or Phantom Destroyer
					baseDamage += fireDamage * numTicks;
				}

			}
			return (baseDamage, additionalDamage);
		}

		//====================================================================================================
		// STAFF DAMAGE CALCULATIONS
		//======================================================================================================
		public (float, string) GetStaffWeaponDamage(ItemViewRow viewRow, bool bUseUpgraded, ref HeroInfo heroInfo, ref HeroEquipment_Data equipTemplate)
		{
			
			int instanceWeaponDamageBonus = (bUseUpgraded) ? viewRow.UpgradedWeaponDamageBonus : viewRow.WeaponDamageBonus;
			int instanceWeaponShotsPerSecondBonus = (bUseUpgraded) ? viewRow.UpgradedWeaponShotsPerSecondBonus : viewRow.WeaponShotsPerSecondBonus;
			int instanceWeaponNumberOfProjectilesBonus = (bUseUpgraded) ? viewRow.UpgradedWeaponNumberOfProjectilesBonus : viewRow.WeaponNumberOfProjectilesBonus;
			int instanceWeaponChargeSpeedBonus = (bUseUpgraded) ? viewRow.UpgradedWeaponChargeSpeedBonus : viewRow.WeaponChargeSpeedBonus;
			int instanceWeaponSpeedOfProjectilesBonus = (bUseUpgraded) ? viewRow.UpgradedWeaponSpeedOfProjectilesBonus : viewRow.WeaponSpeedOfProjectilesBonus;
			float instanceWeaponSwingSpeedMultiplier = viewRow.WeaponSwingSpeedMultiplier;
			int instanceWeaponAdditionalDamageAmount = (bUseUpgraded) ? viewRow.UpgradedWeaponAdditionalDamageAmount : viewRow.WeaponAdditionalDamageAmount;
			int instanceWeaponAltDamageBonus = (bUseUpgraded) ? viewRow.UpgradedWeaponAltDamageBonus : viewRow.WeaponAltDamageBonus;


			var weaponTemplate = tdb!.GetDunDefWeapon(equipTemplate.EquipmentWeaponTemplate);

			float EquipmentBaseDamage = (weaponTemplate.BaseDamage * weaponTemplate.WeaponDamageMultiplier * equipTemplate.WeaponDamageMultiplier);
			float EquipmentDamage = instanceWeaponDamageBonus * weaponTemplate.WeaponDamageMultiplier * equipTemplate.WeaponDamageMultiplier + EquipmentBaseDamage;

			float PawnDamageMult = Player_GetPawnDamageMult(ref heroInfo);

			float DamagePerProjectile = EquipmentDamage * PawnDamageMult;

			int NumProjectiles = (weaponTemplate.NumProjectiles + (instanceWeaponNumberOfProjectilesBonus));

			float AdditionalDamagePerProjectile = (weaponTemplate.bUseAdditionalProjectileDamage == 1) ? GetEquipmentAdditionalDamageAmount(viewRow, bUseUpgraded, ref heroInfo, ref equipTemplate) * PawnDamageMult : 0;

			if (weaponTemplate.bEmberorMoon == 0)
			{
				DamagePerProjectile *= weaponTemplate.BonusDamageMulti;
				AdditionalDamagePerProjectile *= weaponTemplate.BonusDamageMulti;
			}


			float chargeTarget = 0.7f;
			float chargeSpeed = MathF.Max(weaponTemplate.BaseChargeSpeed + (weaponTemplate.ChargeSpeedBonusLinearScale * MathF.Pow(instanceWeaponChargeSpeedBonus, weaponTemplate.ChargeSpeedBonusExpScale)), 0.1f);
			float chargeTime = QuantizeToAnimFrameTime((weaponTemplate.FullChargeTime * chargeTarget * weaponTemplate.WeaponSpeedMultiplier) / chargeSpeed);
			float actualChargeTarget = chargeTime * chargeSpeed / weaponTemplate.FullChargeTime / weaponTemplate.WeaponSpeedMultiplier;

			float minimumRefireDelay = tdb.GetFloatArray(weaponTemplate.FireInterval)[0] * weaponTemplate.WeaponSpeedMultiplier;

			float fireInterval = chargeTime +  minimumRefireDelay;

			float shotsPerSecond = 1.0f / fireInterval;
			
			(float damage, float perHitDamage, float perHitAdditionalDamage) = GetProjectileArrayDamage(NumProjectiles, DamagePerProjectile, AdditionalDamagePerProjectile, 1.0f, actualChargeTarget, viewRow, bUseUpgraded, ref heroInfo, ref weaponTemplate, ref equipTemplate);

			string damageStr = DDEquipmentInfo.FormatCompact(perHitDamage + perHitAdditionalDamage);

			return (damage * shotsPerSecond, $"{damageStr} " + ((NumProjectiles > 1) ? $"x {NumProjectiles} projectiles " : "") + $"every {(1.0f / shotsPerSecond):F2} seconds");			
		}


		//====================================================================================================
		// SPEAR DAMAGE CALCULATIONS
		//======================================================================================================
		public (float, string) GetSpearWeaponDamage(ItemViewRow viewRow, bool bUseUpgraded, bool bCalculateAltDamage, ref HeroInfo heroInfo, ref HeroEquipment_Data equipTemplate)
		{
			int instanceWeaponDamageBonus = (bUseUpgraded) ? viewRow.UpgradedWeaponDamageBonus : viewRow.WeaponDamageBonus;
			int instanceWeaponShotsPerSecondBonus = (bUseUpgraded) ? viewRow.UpgradedWeaponShotsPerSecondBonus : viewRow.WeaponShotsPerSecondBonus;
			int instanceWeaponNumberOfProjectilesBonus = (bUseUpgraded) ? viewRow.UpgradedWeaponNumberOfProjectilesBonus : viewRow.WeaponNumberOfProjectilesBonus;
			int instanceWeaponChargeSpeedBonus = (bUseUpgraded) ? viewRow.UpgradedWeaponChargeSpeedBonus : viewRow.WeaponChargeSpeedBonus;
			int instanceWeaponSpeedOfProjectilesBonus = (bUseUpgraded) ? viewRow.UpgradedWeaponSpeedOfProjectilesBonus : viewRow.WeaponSpeedOfProjectilesBonus;
			float instanceWeaponSwingSpeedMultiplier = viewRow.WeaponSwingSpeedMultiplier;
			int instanceWeaponAdditionalDamageAmount = (bUseUpgraded) ? viewRow.UpgradedWeaponAdditionalDamageAmount : viewRow.WeaponAdditionalDamageAmount;
			int instanceWeaponAltDamageBonus = (bUseUpgraded) ? viewRow.UpgradedWeaponAltDamageBonus : viewRow.WeaponAltDamageBonus;


			var weaponTemplate = tdb!.GetDunDefWeapon(equipTemplate.EquipmentWeaponTemplate);

			if (!bCalculateAltDamage)
			{
				(float meleeAmount, string meleeTip) = GetMeleeWeaponDamage(viewRow, bUseUpgraded, ref heroInfo, ref equipTemplate);
				return (meleeAmount, meleeTip);
			}

			// calculate ranged version
			float spearShootInterval = QuantizeToAnimFrameTime(  weaponTemplate.ShootInterval / weaponTemplate.WeaponSpeedMultiplier);

			float DamagePerProjectile = (weaponTemplate.BaseAltDamage + GetEquipmentAltDamageBonus(viewRow, bUseUpgraded, ref equipTemplate)) * Player_GetPawnDamageMult(ref heroInfo);
			float PawnDamageMult = Player_GetPawnDamageMult(ref heroInfo);

			int NumProjectiles = (1 + (instanceWeaponNumberOfProjectilesBonus));
			float AdditionalDamagePerProjectile = (weaponTemplate.bUseAdditionalProjectileDamage == 1) ? GetEquipmentAdditionalDamageAmount(viewRow, bUseUpgraded, ref heroInfo, ref equipTemplate) * PawnDamageMult : 0;
			(float damage, float perHitDamage, float perHitAdditionalDamage) = GetProjectileArrayDamage(NumProjectiles, DamagePerProjectile, AdditionalDamagePerProjectile, 1.0f, -1.0f, viewRow, bUseUpgraded, ref heroInfo, ref weaponTemplate, ref equipTemplate);

			float rangedAmount = damage / spearShootInterval;

			
				string damageStr = DDEquipmentInfo.FormatCompact(perHitDamage + perHitAdditionalDamage);
			string projStr = $"{damageStr} " + ((NumProjectiles > 1) ? $"x {NumProjectiles} projectiles " : "") + $"every {(spearShootInterval):F2} seconds";

			return (rangedAmount, projStr);			
		}

		//====================================================================================================
		// RANGED DAMAGE CALCULATIONS
		//======================================================================================================
		public (float, string) GetCrossbowWeaponDamage(ItemViewRow viewRow, bool bUseUpgraded, ref HeroInfo heroInfo, ref HeroEquipment_Data equipTemplate)
		{
			int instanceWeaponDamageBonus = (bUseUpgraded) ? viewRow.UpgradedWeaponDamageBonus : viewRow.WeaponDamageBonus;
			int instanceWeaponShotsPerSecondBonus = (bUseUpgraded) ? viewRow.UpgradedWeaponShotsPerSecondBonus : viewRow.WeaponShotsPerSecondBonus;
			int instanceWeaponNumberOfProjectilesBonus = (bUseUpgraded) ? viewRow.UpgradedWeaponNumberOfProjectilesBonus : viewRow.WeaponNumberOfProjectilesBonus;
			int instanceWeaponChargeSpeedBonus = (bUseUpgraded) ? viewRow.UpgradedWeaponChargeSpeedBonus : viewRow.WeaponChargeSpeedBonus;
			int instanceWeaponSpeedOfProjectilesBonus = (bUseUpgraded) ? viewRow.UpgradedWeaponSpeedOfProjectilesBonus : viewRow.WeaponSpeedOfProjectilesBonus;
			float instanceWeaponSwingSpeedMultiplier = viewRow.WeaponSwingSpeedMultiplier;
			int instanceWeaponAdditionalDamageAmount = (bUseUpgraded) ? viewRow.UpgradedWeaponAdditionalDamageAmount : viewRow.WeaponAdditionalDamageAmount;
			int instanceWeaponAltDamageBonus = (bUseUpgraded) ? viewRow.UpgradedWeaponAltDamageBonus : viewRow.WeaponAltDamageBonus;


			var weaponTemplate = tdb!.GetDunDefWeapon(equipTemplate.EquipmentWeaponTemplate);
			
			float EquipmentBaseDamage = (weaponTemplate.BaseDamage * weaponTemplate.WeaponDamageMultiplier * equipTemplate.WeaponDamageMultiplier);
			float EquipmentDamage = instanceWeaponDamageBonus * weaponTemplate.WeaponDamageMultiplier * equipTemplate.WeaponDamageMultiplier + EquipmentBaseDamage;

			float PawnDamageMult = Player_GetPawnDamageMult(ref heroInfo);

			float DamagePerProjectile = EquipmentDamage  * PawnDamageMult;

			float ShotsPerSec = (weaponTemplate.BaseShotsPerSecond + (instanceWeaponShotsPerSecondBonus)) / (weaponTemplate.FireIntervalMultiplier * weaponTemplate.WeaponSpeedMultiplier);
			float quantizeShotsPerSec = 1.0f / QuantizeToAnimFrameTime(1.0f / ShotsPerSec);

			int NumProjectiles = (weaponTemplate.BaseNumProjectiles + (instanceWeaponNumberOfProjectilesBonus));

			float AdditionalDamagePerProjectile = (weaponTemplate.bUseAdditionalProjectileDamage == 1) ? GetEquipmentAdditionalDamageAmount(viewRow, bUseUpgraded, ref heroInfo, ref equipTemplate) * PawnDamageMult : 0;


			(float damage, float perHitDamage, float perHitAdditionalDamage) = GetProjectileArrayDamage(NumProjectiles, DamagePerProjectile, AdditionalDamagePerProjectile, 1.0f, -1.0f, viewRow, bUseUpgraded, ref heroInfo, ref weaponTemplate, ref equipTemplate);

			string damageStr = DDEquipmentInfo.FormatCompact(perHitDamage + perHitAdditionalDamage);
			
			return (damage * quantizeShotsPerSec, $"{damageStr} " + ((NumProjectiles > 1) ? $"x {NumProjectiles} projectiles ":"") + $"every {(1.0f/quantizeShotsPerSec):F2} seconds");
		}

		//====================================================================================================
		// MELEE DAMAGE CALCULATIONS
		//======================================================================================================
		public (float, string) GetMeleeWeaponDamage(ItemViewRow viewRow, bool bUseUpgraded, ref HeroInfo heroInfo, ref HeroEquipment_Data equipTemplate)
		{
			int instanceWeaponDamageBonus = (bUseUpgraded) ? viewRow.UpgradedWeaponDamageBonus : viewRow.WeaponDamageBonus;
			int instanceWeaponShotsPerSecondBonus = (bUseUpgraded) ? viewRow.UpgradedWeaponShotsPerSecondBonus : viewRow.WeaponShotsPerSecondBonus;
			int instanceWeaponNumberOfProjectilesBonus = (bUseUpgraded) ? viewRow.UpgradedWeaponNumberOfProjectilesBonus : viewRow.WeaponNumberOfProjectilesBonus;
			int instanceWeaponChargeSpeedBonus = (bUseUpgraded) ? viewRow.UpgradedWeaponChargeSpeedBonus : viewRow.WeaponChargeSpeedBonus;
			int instanceWeaponSpeedOfProjectilesBonus = (bUseUpgraded) ? viewRow.UpgradedWeaponSpeedOfProjectilesBonus : viewRow.WeaponSpeedOfProjectilesBonus;
			float instanceWeaponSwingSpeedMultiplier = viewRow.WeaponSwingSpeedMultiplier;
			int instanceWeaponAdditionalDamageAmount = (bUseUpgraded) ? viewRow.UpgradedWeaponAdditionalDamageAmount : viewRow.WeaponAdditionalDamageAmount;
			int instanceWeaponAltDamageBonus = (bUseUpgraded) ? viewRow.UpgradedWeaponAltDamageBonus : viewRow.WeaponAltDamageBonus;


			var weaponTemplate = tdb!.GetDunDefWeapon(equipTemplate.EquipmentWeaponTemplate);

			int BaseDamage = weaponTemplate.BaseDamage;

			float EquipmentBaseDamage = (int)(MathF.Max((float)BaseDamage * weaponTemplate.WeaponProjectileDamageMultiplier * ((int)equipTemplate.WeaponDamageMultiplier) + 
												  GetEquipmentDamageBonus(viewRow, bUseUpgraded, ref equipTemplate) * weaponTemplate.WeaponDamageMultiplier, 1.0f));			
			float SwingAdjustment = MathF.Max((float)MathF.Pow(weaponTemplate.DamageIncreaseForSwingSpeedFactor / (weaponTemplate.SpeedMultiplier * instanceWeaponSwingSpeedMultiplier), weaponTemplate.SpeedMultiplierDamageExponent),1.0f);
			float HeroDamageMult = Hero_GetHeroDamageMult(ref heroInfo);
			float PawnDamageMult = Player_GetPawnDamageMult(ref heroInfo);

			float AdditionalDamage = instanceWeaponAdditionalDamageAmount * PawnDamageMult * equipTemplate.ElementalDamageMultiplier;			

			float MainDamage = EquipmentBaseDamage * SwingAdjustment * HeroDamageMult * PawnDamageMult;


			Array_Data swingInfoRef = weaponTemplate.MeleeSwingInfos;
			Array_Data playerSwingInfoRef = heroInfo.PlayerTemplateData.MeleeSwingInfoMultipliers;
			Array_Data mainHandSwingInfoRef = heroInfo.PlayerTemplateData.MainHandSwingInfoMultipliers;
			Array_Data offHandSwingInfoRef = heroInfo.PlayerTemplateData.OffHandSwingInfoMultipliers;
			float swingTimeSum = 0.0f;
			float swingDamageSum = 0.0f;
			float swingDamageExtraSum = 0.0f;
			float totalSpeedMult = instanceWeaponSwingSpeedMultiplier * weaponTemplate.SpeedMultiplier * weaponTemplate.ExtraSpeedMultiplier * weaponTemplate.WeaponSpeedMultiplier;

			int numSwings = 3;
			if (heroInfo.PlayerTemplateData.OffHandSwingInfoMultipliers.Count > 0)
				numSwings = 4;
			Span<float> swingDurations = stackalloc float[4];
			Span<float> swingFinalTimes = stackalloc float[4];
			Span<float> swingDamageStartTimes = stackalloc float[4];
			Span<int> swingInfoIndex = stackalloc int[4];
			

			swingDurations[0] = (viewRow.Set == "Squire") ? heroInfo.MeleeAttack1MediumAnimDuration : heroInfo.MeleeAttack1LargeAnimDuration;
			swingDurations[1] = (viewRow.Set == "Squire") ? heroInfo.MeleeAttack2MediumAnimDuration : heroInfo.MeleeAttack2LargeAnimDuration;
			swingDurations[2] = (viewRow.Set == "Squire") ? heroInfo.MeleeAttack3MediumAnimDuration : heroInfo.MeleeAttack3LargeAnimDuration;			
			swingDurations[3] = 0.0f;

			swingDamageStartTimes[0] = (viewRow.Set == "Squire") ? heroInfo.MeleeAttack1MediumAnimDamageStart : heroInfo.MeleeAttack1LargeAnimDamageStart;
			swingDamageStartTimes[1] = (viewRow.Set == "Squire") ? heroInfo.MeleeAttack2MediumAnimDamageStart : heroInfo.MeleeAttack2LargeAnimDamageStart;
			swingDamageStartTimes[2] = (viewRow.Set == "Squire") ? heroInfo.MeleeAttack3MediumAnimDamageStart : heroInfo.MeleeAttack3LargeAnimDamageStart;
			swingDamageStartTimes[3] = 0.0f;


			swingInfoIndex[0] = swingInfoRef.Start;
			swingInfoIndex[1] = swingInfoRef.Start + 1;
			swingInfoIndex[2] = swingInfoRef.Start + 2;
			swingInfoIndex[3] = 0;

			if (numSwings == 4)
			{
				// barbarian - need to test
				swingDurations[0] = 0.6667f;
				swingDurations[1] = 0.6667f;
				swingDurations[2] = 0.6667f;
				swingDurations[3] = 0.6667f;
				swingInfoIndex[0] = mainHandSwingInfoRef.Start;
				swingInfoIndex[1] = offHandSwingInfoRef.Start;
				swingInfoIndex[2] = mainHandSwingInfoRef.Start + 1;
				swingInfoIndex[3] = offHandSwingInfoRef.Start + 1;
			}

			for (int i = 0; i < numSwings; i++)
			{				
				var swingInfo = tdb.GetMeleeSwingInfo(swingInfoIndex[i]);				
				float playerMultiplier = 1.0f;
				if (playerSwingInfoRef.Count > i)
					playerMultiplier = tdb.GetMeleeSwingInfo(playerSwingInfoRef.Start + i).DamageMultiplier;

				float AnimSpeed = swingInfo.AnimSpeed * totalSpeedMult;
				float SwingTime = (swingDurations[i] - swingInfo.TimeBeforeEndToAllowNextCombo) / AnimSpeed;
				float DamageStartTime = swingDamageStartTimes[i] / AnimSpeed;
				float SwingDamage = MainDamage * swingInfo.DamageMultiplier * weaponTemplate.DamageMultiplier * playerMultiplier;
				float SwingExtraDamage = AdditionalDamage * swingInfo.DamageMultiplier;
			/*	if (SwingTime < weaponTemplate.MinimumSwingTime)
					SwingTime = weaponTemplate.MinimumSwingTime;
				if (SwingTime < DamageStartTime + weaponTemplate.MinimumSwingDamageTime)
					SwingTime = DamageStartTime + weaponTemplate.MinimumSwingDamageTime;*/

				swingFinalTimes[i] = SwingTime;

				swingTimeSum += QuantizeToAnimFrameTime(SwingTime);  // quantize up to 30th of a second
		
				swingDamageSum += SwingDamage;
				swingDamageExtraSum += SwingExtraDamage;
			}

			float totalProjectileDamage = 0.0f;
			string projectileDPSTooltip = "";
			if (weaponTemplate.bShootMeleeProjectile == 1)
			{
				// shoot a particle with each swing
				float ProjectileMeleeDamage = (weaponTemplate.BaseAltDamage + GetEquipmentAltDamageBonus(viewRow, bUseUpgraded, ref equipTemplate)) * PawnDamageMult;
				float ProjectileAdditionalDamage = 0.0f; 
				if (weaponTemplate.bUseAdditionalProjectileDamage == 1)
				{
					ProjectileAdditionalDamage = GetEquipmentAdditionalDamageAmount(viewRow, bUseUpgraded, ref heroInfo, ref equipTemplate) * PawnDamageMult;
				}
				int numProjectiles = 1 + equipTemplate.WeaponNumberOfProjectilesBonus;
				if (weaponTemplate.bUseWeaponDamageForProjectileDamage == 1)
				{
					ProjectileMeleeDamage = MainDamage * weaponTemplate.WeaponProjectileDamageMultiplier;
				}
				float scaleDamageExponentMultiplier = weaponTemplate.ProjectileDamageHeroStatExponentMultiplier;

				(float projDamage, float hitDamage, float hitAdditionalDamage) = GetProjectileArrayDamage(numProjectiles, ProjectileMeleeDamage, ProjectileAdditionalDamage, scaleDamageExponentMultiplier, -1.0f, viewRow, bUseUpgraded, ref heroInfo, ref weaponTemplate, ref equipTemplate);
				totalProjectileDamage = projDamage * numSwings;
				string hitDamageStr = DDEquipmentInfo.FormatCompact(hitDamage + hitAdditionalDamage);				

				projectileDPSTooltip = $"{hitDamageStr} x {numSwings} from projectiles\r\n";
			}

			string swingDamageSumStr = DDEquipmentInfo.FormatCompact(swingDamageSum);
			string swingDamageExtraSumStr = DDEquipmentInfo.FormatCompact(swingDamageExtraSum);

			float totalMeleeDamage = (swingTimeSum == 0.0f) ? MainDamage + AdditionalDamage : (swingDamageSum + swingDamageExtraSum + totalProjectileDamage) / swingTimeSum;
			return (totalMeleeDamage, $"{swingDamageSumStr} + {swingDamageExtraSumStr} over {numSwings} swings in {swingTimeSum:F1} seconds\r\n" + projectileDPSTooltip);
		}

		public void CalculateUpgradedWeaponStats(ItemViewRow viewRow, bool bUpgradeForAltDamage, ref HeroEquipment_Data equipTemplate)
		{
			int numUpgradesAvailable = viewRow.UpgradesLeftForWeaponStats;
			viewRow.UpgradedWeaponDamageBonus = viewRow.WeaponDamageBonus;
			viewRow.UpgradedWeaponShotsPerSecondBonus = viewRow.WeaponShotsPerSecondBonus;
			viewRow.UpgradedWeaponNumberOfProjectilesBonus = viewRow.WeaponNumberOfProjectilesBonus;
			viewRow.UpgradedWeaponChargeSpeedBonus = viewRow.WeaponChargeSpeedBonus;
			viewRow.UpgradedWeaponSpeedOfProjectilesBonus = viewRow.WeaponSpeedOfProjectilesBonus;
			viewRow.UpgradedWeaponAdditionalDamageAmount = viewRow.WeaponAdditionalDamageAmount;
			viewRow.UpgradedWeaponAltDamageBonus = viewRow.WeaponAltDamageBonus;			
		
			var maxNumberOfProjectilesBonus = (int)(tdb!.GetEG_StatRandomizer(equipTemplate.WeaponNumberOfProjectilesBonusRandomizer).MaxRandomValue) + equipTemplate.WeaponNumberOfProjectilesBonus;
			var maxShotsPerSecondBonus = (int)(tdb!.GetEG_StatRandomizer(equipTemplate.WeaponShotsPerSecondBonusRandomizer).MaxRandomValue) + equipTemplate.WeaponShotsPerSecondBonus;
			var maxSpeedOfProjectilesBonus = 30000;
			var maxChargeSpeedBonus = 128;

			// TODO: Deal with partial upgrades 

			// you can only upgrade when (level + 1) % 5 == 0. This is not perfect, but it's an approximation.
			maxNumberOfProjectilesBonus = (int)MathF.Min(maxNumberOfProjectilesBonus, viewRow.UpgradedWeaponNumberOfProjectilesBonus + (numUpgradesAvailable / 5));
			
			if (viewRow.UpgradedWeaponNumberOfProjectilesBonus < maxNumberOfProjectilesBonus)
			{
				int upgradesToUse = Math.Min(numUpgradesAvailable, maxNumberOfProjectilesBonus - viewRow.UpgradedWeaponNumberOfProjectilesBonus);
				viewRow.UpgradedWeaponNumberOfProjectilesBonus += upgradesToUse;
				numUpgradesAvailable -= upgradesToUse;
			}

			// you can only upgrade when (level + 1) % 4 == 0.  This is not perfect, but it's an approximation.
			maxShotsPerSecondBonus = (int)MathF.Min(maxShotsPerSecondBonus, viewRow.UpgradedWeaponShotsPerSecondBonus + (numUpgradesAvailable / 4));

			// upgrade shots per second
			if (viewRow.UpgradedWeaponShotsPerSecondBonus < maxShotsPerSecondBonus)
			{
				int upgradesToUse = Math.Min(numUpgradesAvailable, maxShotsPerSecondBonus - viewRow.UpgradedWeaponShotsPerSecondBonus);
				viewRow.UpgradedWeaponShotsPerSecondBonus += upgradesToUse;
				numUpgradesAvailable -= upgradesToUse;
			}

			while (viewRow.UpgradedWeaponChargeSpeedBonus < maxChargeSpeedBonus && numUpgradesAvailable > 0)
			{
				int current = viewRow.UpgradedWeaponChargeSpeedBonus;

				int inc = (int)(0.25f * Math.Abs(current)); // truncates like UnrealScript int()
				inc = Math.Clamp(inc, 1, 4);

				viewRow.UpgradedWeaponChargeSpeedBonus = Math.Min(
					maxChargeSpeedBonus,
					viewRow.UpgradedWeaponChargeSpeedBonus + inc
				);

				numUpgradesAvailable--;
			}

			// upgrade speed to 30000
			while ((viewRow.UpgradedWeaponSpeedOfProjectilesBonus < maxSpeedOfProjectilesBonus) && (numUpgradesAvailable > 0))
			{
				int AmountToUpgrade = (int)Math.Clamp(Math.Abs(viewRow.UpgradedWeaponSpeedOfProjectilesBonus)/4, 100, 1200);
				viewRow.UpgradedWeaponSpeedOfProjectilesBonus += AmountToUpgrade;
				numUpgradesAvailable--;
			}

			// now upgrade either weapon damage or alt damage, based on flag			

			// check for elemental damage scaling
			bool elementalDamageScaling = false;

			var weaponTemplate = tdb!.GetDunDefWeapon(equipTemplate.EquipmentWeaponTemplate);
			if (weaponTemplate.ProjectileTemplate != -1)
			{
				// if crash, maybe here
				var projTemplate = tdb!.GetDunDefProjectile(weaponTemplate.ProjectileTemplate);
				elementalDamageScaling = (projTemplate.bSecondScaleDamageStatType != 0);
			}
  
			
			if (bUpgradeForAltDamage && (viewRow.UpgradedWeaponAltDamageBonus > 0))
			{
				while (numUpgradesAvailable > 0)
				{
					if (elementalDamageScaling && (viewRow.UpgradedWeaponAdditionalDamageAmount < viewRow.UpgradedWeaponAltDamageBonus + weaponTemplate.BaseAltDamage) && elementalDamageScaling)
					{
						viewRow.UpgradedWeaponAdditionalDamageAmount +=
							Math.Clamp((int)Math.Ceiling(equipTemplate.ElementalDamageIncreasePerLevelMultiplier * Math.Abs(viewRow.UpgradedWeaponAdditionalDamageAmount)),
										(int)equipTemplate.MinElementalDamageIncreasePerLevel,
										(int)equipTemplate.MaxElementalDamageIncreasePerLevel);
					}
					else
					{
						viewRow.UpgradedWeaponAltDamageBonus +=
							Math.Clamp((int)Math.Ceiling(equipTemplate.AltDamageIncreasePerLevelMultiplier * Math.Abs(viewRow.UpgradedWeaponAltDamageBonus + weaponTemplate.BaseAltDamage)),
										(int)1,
										(int)equipTemplate.AltMaxDamageIncreasePerLevel);
					}

					numUpgradesAvailable--;
				}
			}
			else if (viewRow.UpgradedWeaponDamageBonus > 0)
			{
				while (numUpgradesAvailable > 0)
				{
					if (elementalDamageScaling && (viewRow.UpgradedWeaponAdditionalDamageAmount < viewRow.UpgradedWeaponDamageBonus + weaponTemplate.BaseDamage) )
					{
						viewRow.UpgradedWeaponAdditionalDamageAmount +=
							Math.Clamp((int)Math.Ceiling(equipTemplate.ElementalDamageIncreasePerLevelMultiplier * Math.Abs(viewRow.UpgradedWeaponAdditionalDamageAmount)),
										(int)equipTemplate.MinElementalDamageIncreasePerLevel,
										(int)equipTemplate.MaxElementalDamageIncreasePerLevel);
					}
					else
					{
						if ((equipTemplate.bUseExtraQualityDamage == 1) &&
								viewRow.QualityRank == equipTemplate.ExtraQualityUpgradeDamageNumberDescriptor)
						{
							float multiplier =
								(equipTemplate.ExtraQualityDamageIncreasePerLevelMultiplier < equipTemplate.DamageIncreasePerLevelMultiplier)
									? equipTemplate.DamageIncreasePerLevelMultiplier
									: equipTemplate.ExtraQualityDamageIncreasePerLevelMultiplier;

							int maxIncreasePerLevel = (int)
								((equipTemplate.ExtraQualityMaxDamageIncreasePerLevel < equipTemplate.MaxDamageIncreasePerLevel)
									? equipTemplate.MaxDamageIncreasePerLevel
									: equipTemplate.ExtraQualityMaxDamageIncreasePerLevel);

							viewRow.UpgradedWeaponDamageBonus += (int)Math.Clamp(
								Math.Ceiling(multiplier * Math.Abs(weaponTemplate.BaseDamage + viewRow.UpgradedWeaponDamageBonus)),
								1,
								maxIncreasePerLevel
							);
						}
						else if ((equipTemplate.bUseSecondExtraQualityDamage == 1) &&
								 viewRow.QualityRank == equipTemplate.SecondExtraQualityUpgradeDamageNumberDescriptor)
						{
							float multiplier =
								(equipTemplate.SecondExtraQualityDamageIncreasePerLevelMultiplier < equipTemplate.DamageIncreasePerLevelMultiplier)
									? equipTemplate.DamageIncreasePerLevelMultiplier
									: equipTemplate.SecondExtraQualityDamageIncreasePerLevelMultiplier;

							int maxIncreasePerLevel = (int)
								((equipTemplate.SecondExtraQualityMaxDamageIncreasePerLevel < equipTemplate.MaxDamageIncreasePerLevel)
									? equipTemplate.MaxDamageIncreasePerLevel
									: equipTemplate.SecondExtraQualityMaxDamageIncreasePerLevel);

							viewRow.UpgradedWeaponDamageBonus += (int)Math.Clamp(
								Math.Ceiling(multiplier * Math.Abs(weaponTemplate.BaseDamage + viewRow.UpgradedWeaponDamageBonus)),
								1,
								maxIncreasePerLevel
							);
						}
						else
						{
							// Mirrors:
							// Value = WeaponDamageBonus + Min(FCeil(Max(int(((cond) ? Damage : Ultimate) * Abs(GetWeaponDamage())), 1))), int(((cond) ? MaxDamage : UltimateMaxDamage)));
							bool useBaseScaling = (viewRow.QualityRank < 16);

							float multiplier =
								(useBaseScaling || equipTemplate.UltimateDamageIncreasePerLevelMultiplier < equipTemplate.DamageIncreasePerLevelMultiplier)
									? equipTemplate.DamageIncreasePerLevelMultiplier
									: equipTemplate.UltimateDamageIncreasePerLevelMultiplier;

							int maxIncreasePerLevel = (int)								
								((useBaseScaling || equipTemplate.UltimateMaxDamageIncreasePerLevel < equipTemplate.MaxDamageIncreasePerLevel)
									? equipTemplate.MaxDamageIncreasePerLevel
									: equipTemplate.UltimateMaxDamageIncreasePerLevel);

							viewRow.UpgradedWeaponDamageBonus += (int)Math.Clamp(
								Math.Ceiling(multiplier * Math.Abs(weaponTemplate.BaseDamage + viewRow.UpgradedWeaponDamageBonus)),
								1,
								maxIncreasePerLevel
							);
						}
						if (viewRow.UpgradedWeaponDamageBonus == 0)
						{
							viewRow.UpgradedWeaponDamageBonus = 1;
						}					
					}
					numUpgradesAvailable--;
				}
			}
		}

		public int GetDisplayWeaponDamage(ItemViewRow viewRow, bool bShowUpgraded, ref HeroInfo heroInfo, ref HeroEquipment_Data equipTemplate)		
		{
			var weaponTemplate = tdb!.GetDunDefWeapon(equipTemplate.EquipmentWeaponTemplate);

			float totalRawDamage = weaponTemplate.BaseDamage + (bShowUpgraded ? viewRow.UpgradedWeaponDamageBonus : viewRow.WeaponDamageBonus);

			float playerMult = heroInfo.PlayerTemplateData.PlayerWeaponDamageMultiplier;
			float displayScale = equipTemplate.WeaponDamageDisplayValueScale;

			float scaledDamage = MathF.Max((int)(playerMult * displayScale * totalRawDamage), 1.0f);

			float multiplier = weaponTemplate.WeaponDamageMultiplier * equipTemplate.WeaponDamageMultiplier;

			if (weaponTemplate.DamageIncreaseForSwingSpeedFactor > 0)
			{
				float denominator = weaponTemplate.SpeedMultiplier * viewRow.WeaponSwingSpeedMultiplier;

				// Safety check for division by zero
				if (denominator == 0) denominator = 0.0001f;

				float swingScale = MathF.Pow(
					weaponTemplate.DamageIncreaseForSwingSpeedFactor / denominator,
					weaponTemplate.SpeedMultiplierDamageExponent
				);

				multiplier *= MathF.Max(swingScale, 1.0f);
			}

			return (int)(multiplier * scaledDamage);
		}

		public int GetDisplayAltDamage(ItemViewRow viewRow, bool bShowUpgraded, ref HeroInfo heroInfo, ref HeroEquipment_Data equipTemplate)
		{
			var weaponTemplate = tdb!.GetDunDefWeapon(equipTemplate.EquipmentWeaponTemplate);
			float totalRawAlt = weaponTemplate.BaseAltDamage + (bShowUpgraded ? viewRow.UpgradedWeaponAltDamageBonus : viewRow.WeaponAltDamageBonus);

			float playerMult = heroInfo.PlayerTemplateData.PlayerWeaponDamageMultiplier;
			float displayScale = equipTemplate.WeaponAltDamageDisplayValueScale;

			float scaledAlt = MathF.Max((int)(displayScale * playerMult * totalRawAlt), 1.0f);

			return (int)(equipTemplate.WeaponAltDamageMultiplier * scaledAlt);
		}

		public int GetDisplayAdditionalDamage(ItemViewRow viewRow, bool bShowUpgraded, ref HeroInfo heroInfo, ref HeroEquipment_Data equipTemplate)
		{
			float bonusElemental = bShowUpgraded ? (float)viewRow.UpgradedWeaponAdditionalDamageAmount : (float)viewRow.WeaponAdditionalDamageAmount;

			float calculatedValue = bonusElemental
				* equipTemplate.ElementalDamageMultiplier
				* heroInfo.PlayerElementalWeaponDamageMultiplier
				* heroInfo.PlayerTemplateData.PlayerWeaponDamageMultiplier
				* equipTemplate.WeaponDamageDisplayValueScale; 

			return (int)calculatedValue;
		}

		public int GetDisplayShotsPerSecond(ItemViewRow viewRow, bool bShowUpgraded, ref HeroInfo heroInfo, ref HeroEquipment_Data equipTemplate)
		{
			var weaponTemplate = tdb!.GetDunDefWeapon(equipTemplate.EquipmentWeaponTemplate);
			int shotsPerSecBonus = bShowUpgraded ? viewRow.UpgradedWeaponShotsPerSecondBonus : viewRow.WeaponShotsPerSecondBonus;
			return shotsPerSecBonus + Math.Max(weaponTemplate.BaseShotsPerSecond, 1);
		}

		public int GetDisplayNumProjectiles(ItemViewRow viewRow, bool bShowUpgraded, ref HeroInfo heroInfo, ref HeroEquipment_Data equipTemplate)
		{
			var weaponTemplate = tdb!.GetDunDefWeapon(equipTemplate.EquipmentWeaponTemplate);
			int projBonus = bShowUpgraded ? viewRow.UpgradedWeaponNumberOfProjectilesBonus : viewRow.WeaponNumberOfProjectilesBonus;
			return projBonus + Math.Max(weaponTemplate.BaseNumProjectiles, 1);
		}
	}
}
