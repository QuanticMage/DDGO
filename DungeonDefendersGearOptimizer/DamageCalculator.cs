using Microsoft.VisualBasic;
using System.ComponentModel;
using System.Reflection.Metadata.Ecma335;
using System.Security.Claims;
using static System.Net.Mime.MediaTypeNames;

namespace DDUP
{
	// Inputs needed
	// 

	// Outputs:
	// Theoretical DPS vs. training dummy
	// Ideal upgrade path 
	// How good this item is vs. a max drop on weapon damage.  How many drops I would need before I got something better.
	// Piercing, AOE, Heal, Mana, Wall info

	// Display of stats is (e.WDamageMult * e.WDamageDisplayValueScale * 0.8) * et.(w.Base + i.Bonus)
	// for additional stats it's same without e.WDamageMult

	// crystal tracker showing up as 424874 dmg + 1060 extra
	// 1060 appears to be 4.8 times higher than (7131 * 0.8 * 0.25) - the predicted amount w/ global elemental scaling - 1.2x ev!

	// 1.2x skin multiplier (EV 1.85)!!!! x weapon damage modifier (4)
	// TODO: need a calculation for health - See GetHeroStatMult


	public class HeroInfo
	{
		public int[] TotalStats = new int[11];     // regular stats		
		public DunDefHero_Data TemplateData;
		public DunDefPlayer_Data PlayerTemplateData;
		public float GlobalDamageMultiplier = 0.155f;        // nightmare mode, unclear but definitely there modifier
		public float PlayerElementalWeaponDamageMultiplier = 1.0f;
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

		float GetEquipmentDamageBonus(DDEquipmentInfo instance,ref HeroEquipment_Data equipTemplate)
		{
			// Step 1: Apply multipliers to bonus
			float floatValue = equipTemplate.WeaponDamageMultiplier  * instance.WeaponDamageBonus;

			return floatValue;
		}		

		public float GetEquipmentDamageBonusNormalized(float Normalizer, DDEquipmentInfo instance, ref HeroInfo heroInfo, ref DunDefWeapon_Data weaponTemplate, ref HeroEquipment_Data equipTemplate)
		{
			return 
				(float)Math.Pow(weaponTemplate.BaseDamage * weaponTemplate.WeaponDamageMultiplier * (int)equipTemplate.WeaponDamageMultiplier + GetEquipmentDamageBonus(instance, ref equipTemplate),
				Normalizer) * Hero_GetHeroDamageMult(ref heroInfo) * Player_GetPawnDamageMult(ref heroInfo);
		}

		float GetEquipmentAltDamageBonus(DDEquipmentInfo instance,ref HeroEquipment_Data equipTemplate)
		{			
			if (equipTemplate.WeaponAltDamageBonusUse == 0)
				return 0.0f;
			return equipTemplate.WeaponAltDamageMultiplier * instance.WeaponAltDamageBonus;
        }

		float GetEquipmentAdditionalDamageAmount(DDEquipmentInfo instance, ref HeroInfo heroInfo, ref HeroEquipment_Data equipTemplate)
		{			
			return equipTemplate.ElementalDamageMultiplier  * heroInfo.PlayerElementalWeaponDamageMultiplier * instance.WeaponAdditionalDamageAmount;			
		}

		(float,float,float) GetProjectileArrayDamage(int nProjectiles, float ProjectileMainDamage, float ProjectileAdditionalDamage, float scaleDamageExponentMultiplier, DDEquipmentInfo instance, ref HeroInfo heroInfo, ref DunDefWeapon_Data weaponTemplate, ref HeroEquipment_Data equipTemplate)
		{
			float totalDamage = 0.0f;
			float standardProjDamage = 0.0f;
			float standardAdditionalDamage = 0.0f;
			if (weaponTemplate.ProjectileTemplate != -1)
			{
				var projectileTemplate = tdb.GetDunDefProjectile(weaponTemplate.ProjectileTemplate);
				(standardProjDamage, standardAdditionalDamage) = GetProjectileDamage(ProjectileMainDamage, ProjectileAdditionalDamage, scaleDamageExponentMultiplier, instance, ref heroInfo, ref projectileTemplate, ref weaponTemplate, ref equipTemplate);
			}
					
			if ((weaponTemplate.bRandomizeProjectileTemplate == 1) && weaponTemplate.RandomizedProjectileTemplate.Count > 0)
			{
				totalDamage = 0.0f;
				for (int i = 0; i < weaponTemplate.RandomizedProjectileTemplate.Count; i++)
				{
					var randomProjectile = tdb.GetDunDefProjectile(weaponTemplate.RandomizedProjectileTemplate.Start + i);
					(float main, float extra) = GetProjectileDamage(ProjectileMainDamage, ProjectileAdditionalDamage, scaleDamageExponentMultiplier, instance, ref heroInfo, ref randomProjectile, ref weaponTemplate, ref equipTemplate);
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
						var extraProjectile = tdb.GetDunDefProjectile(weaponTemplate.ExtraProjectileTemplates.Start + i);
						(float main, float extra) = GetProjectileDamage(ProjectileMainDamage, ProjectileAdditionalDamage, scaleDamageExponentMultiplier, instance, ref heroInfo, ref extraProjectile, ref weaponTemplate, ref equipTemplate);
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


		(float, float) GetProjectileDamage(float ProjectileMainDamage, float ProjectileAdditionalDamage, float scaleDamageExponentMultiplier, DDEquipmentInfo instance, ref HeroInfo heroInfo, ref DunDefProjectile_Data projTemplate, ref DunDefWeapon_Data weaponTemplate, ref HeroEquipment_Data equipTemplate)
		{
			float baseDamage = ProjectileMainDamage;
			float additionalDamage = ProjectileAdditionalDamage;

			// TODO - base damage based on level override
			if (projTemplate.ScaleHeroDamage == 1)
			{
				if (projTemplate.MultiplyProjectileDamageByWeaponDamage == 1)
				{
					float weaponDamageNormalized = GetEquipmentDamageBonusNormalized(projTemplate.ProjectileDamageByWeaponDamageDivider, instance, ref heroInfo, ref weaponTemplate, ref equipTemplate);
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
					baseDamage *= MathF.Max(instance.WeaponSwingSpeedMultiplier, 1.0f);
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
				//if ((instance.MaxLevel == 351))
				//{
				//	DunDefWeapon_Data wdata = weaponTemplate;
				//	DunDefProjectile_Data pdata = projTemplate;
				//	HeroEquipment_Data edata = equipTemplate;
				//	Console.WriteLine($"{instance.GeneratedName}: {pdata.ScaleDamageStatType} {projTemplate.ScaleDamageStatExponent} {projTemplate.MultiplyProjectileDamageByWeaponDamage} {projTemplate.ScaleHeroDamage} {Hero_GetHeroStatMult(projTemplate.ScaleDamageStatType, ref heroInfo)}");
				//	Console.WriteLine($"{instance.GeneratedName}: {baseDamage} {additionalDamage} {projTemplate.bScaleDamagePerLevel} {scalingFactor} {projTemplate.bSecondScaleDamageStatType} {projTemplate.ScaleDamageStatExponent} { wdata.SpeedMultiplier}");
				//}
			}
			return (baseDamage, additionalDamage);
		}
		


		//====================================================================================================
		// SPEAR DAMAGE CALCULATIONS
		//======================================================================================================
		public (float, string) GetSpearWeaponDamage(DDEquipmentInfo instance, ref HeroInfo heroInfo, ref HeroEquipment_Data equipTemplate)
		{
			var weaponTemplate = tdb.GetDunDefWeapon(equipTemplate.EquipmentWeaponTemplate);

			(float meleeAmount, string meleeTip) = GetMeleeWeaponDamage(instance, ref heroInfo, ref equipTemplate);
			
			// calculate ranged version
			float spearShootInterval = QuantizeToAnimFrameTime(  weaponTemplate.ShootInterval / weaponTemplate.WeaponSpeedMultiplier);
			
			float EquipmentBaseDamage = (weaponTemplate.BaseDamage * weaponTemplate.WeaponDamageMultiplier * equipTemplate.WeaponDamageMultiplier);
			float EquipmentDamage = instance.WeaponDamageBonus * weaponTemplate.WeaponDamageMultiplier * equipTemplate.WeaponDamageMultiplier + EquipmentBaseDamage;
			float PawnDamageMult = Player_GetPawnDamageMult(ref heroInfo);
			float DamagePerProjectile = EquipmentDamage * PawnDamageMult;
			int NumProjectiles = (weaponTemplate.BaseNumProjectiles + (instance.WeaponNumberOfProjectilesBonus - 127));
			float AdditionalDamagePerProjectile = (weaponTemplate.bUseAdditionalProjectileDamage == 1) ? GetEquipmentAdditionalDamageAmount(instance, ref heroInfo, ref equipTemplate) * PawnDamageMult : 0;
			(float damage, float perHitDamage, float perHitAdditionalDamage) = GetProjectileArrayDamage(NumProjectiles, DamagePerProjectile, AdditionalDamagePerProjectile, 1.0f, instance, ref heroInfo, ref weaponTemplate, ref equipTemplate);

			float rangedAmount = damage / spearShootInterval;

			if (rangedAmount > meleeAmount)
				return (rangedAmount, $"Ranged DPS: {rangedAmount} {perHitDamage} {perHitAdditionalDamage}\nMelee DPS: {meleeAmount} {meleeTip}");
			else
				return (meleeAmount, $"Ranged DPS: {rangedAmount} {perHitDamage} {perHitAdditionalDamage}\nMelee DPS: {meleeAmount} {meleeTip}");
		}

		//====================================================================================================
		// RANGED DAMAGE CALCULATIONS
		//======================================================================================================
		public (float, string) GetCrossbowWeaponDamage(DDEquipmentInfo instance, ref HeroInfo heroInfo, ref HeroEquipment_Data equipTemplate)
		{			
			var weaponTemplate = tdb.GetDunDefWeapon(equipTemplate.EquipmentWeaponTemplate);
			
			float EquipmentBaseDamage = (weaponTemplate.BaseDamage * weaponTemplate.WeaponDamageMultiplier * equipTemplate.WeaponDamageMultiplier);
			float EquipmentDamage = instance.WeaponDamageBonus * weaponTemplate.WeaponDamageMultiplier * equipTemplate.WeaponDamageMultiplier + EquipmentBaseDamage;

			float PawnDamageMult = Player_GetPawnDamageMult(ref heroInfo);

			float DamagePerProjectile = EquipmentDamage  * PawnDamageMult;

			float ShotsPerSec = (weaponTemplate.BaseShotsPerSecond + (instance.WeaponShotsPerSecondBonus - 127)) / (weaponTemplate.FireIntervalMultiplier * weaponTemplate.WeaponSpeedMultiplier);
			float quantizeShotsPerSec = 1.0f / QuantizeToAnimFrameTime(1.0f / ShotsPerSec);

			int NumProjectiles = (weaponTemplate.BaseNumProjectiles + (instance.WeaponNumberOfProjectilesBonus-127));

			float AdditionalDamagePerProjectile = (weaponTemplate.bUseAdditionalProjectileDamage == 1) ? GetEquipmentAdditionalDamageAmount(instance, ref heroInfo, ref equipTemplate) * PawnDamageMult : 0;


			(float damage, float perHitDamage, float perHitAdditionalDamage) = GetProjectileArrayDamage(NumProjectiles, DamagePerProjectile, AdditionalDamagePerProjectile, 1.0f, instance, ref heroInfo, ref weaponTemplate, ref equipTemplate);

			return (damage * quantizeShotsPerSec, $"{damage} {quantizeShotsPerSec} {perHitDamage} {perHitAdditionalDamage}");
		}		

		//====================================================================================================
		// MELEE DAMAGE CALCULATIONS
		//======================================================================================================
		public (float, string) GetMeleeWeaponDamage(DDEquipmentInfo instance, ref HeroInfo heroInfo, ref HeroEquipment_Data equipTemplate)
		{
			var weaponTemplate = tdb.GetDunDefWeapon(equipTemplate.EquipmentWeaponTemplate);

			int BaseDamage = weaponTemplate.BaseDamage;

			float EquipmentBaseDamage = (int)(MathF.Max((float)BaseDamage * weaponTemplate.WeaponProjectileDamageMultiplier * ((int)equipTemplate.WeaponDamageMultiplier) + 
												  GetEquipmentDamageBonus(instance, ref equipTemplate) * weaponTemplate.WeaponDamageMultiplier, 1.0f));			
			float SwingAdjustment = MathF.Max((float)MathF.Pow(weaponTemplate.DamageIncreaseForSwingSpeedFactor / (weaponTemplate.SpeedMultiplier * instance.WeaponSwingSpeedMultiplier), weaponTemplate.SpeedMultiplierDamageExponent),1.0f);
			float HeroDamageMult = Hero_GetHeroDamageMult(ref heroInfo);
			float PawnDamageMult = Player_GetPawnDamageMult(ref heroInfo);

			float AdditionalDamage = instance.WeaponAdditionalDamageAmount * PawnDamageMult * equipTemplate.ElementalDamageMultiplier;			

			float MainDamage = EquipmentBaseDamage * SwingAdjustment * HeroDamageMult * PawnDamageMult;


			Array_Data swingInfoRef = weaponTemplate.MeleeSwingInfos;
			Array_Data playerSwingInfoRef = heroInfo.PlayerTemplateData.MeleeSwingInfoMultipliers;
			Array_Data mainHandSwingInfoRef = heroInfo.PlayerTemplateData.MainHandSwingInfoMultipliers;
			Array_Data offHandSwingInfoRef = heroInfo.PlayerTemplateData.OffHandSwingInfoMultipliers;
			float swingTimeSum = 0.0f;
			float swingDamageSum = 0.0f;
			float totalSpeedMult = instance.WeaponSwingSpeedMultiplier * weaponTemplate.SpeedMultiplier * weaponTemplate.ExtraSpeedMultiplier * weaponTemplate.WeaponSpeedMultiplier;

			int numSwings = 3;
			if (heroInfo.PlayerTemplateData.OffHandSwingInfoMultipliers.Count > 0)
				numSwings = 4;
			Span<float> swingDurations = stackalloc float[4];
			Span<int> swingInfoIndex = stackalloc int[4];


			swingDurations[0] = (heroInfo.TemplateData.MyHeroType == (int)HeroType.Squire) ?
				heroInfo.PlayerTemplateData.MeleeAttack1MediumAnimDuration:
				heroInfo.PlayerTemplateData.MeleeAttack1LargeAnimDuration;
			swingDurations[1] = (heroInfo.TemplateData.MyHeroType == (int)HeroType.Squire) ?
				heroInfo.PlayerTemplateData.MeleeAttack2MediumAnimDuration :
				heroInfo.PlayerTemplateData.MeleeAttack2LargeAnimDuration;
			swingDurations[2] = (heroInfo.TemplateData.MyHeroType == (int)HeroType.Squire) ?
				heroInfo.PlayerTemplateData.MeleeAttack3MediumAnimDuration :
				heroInfo.PlayerTemplateData.MeleeAttack3LargeAnimDuration;
			swingDurations[3] = 0.0f;


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
				float SwingDamage = MainDamage * swingInfo.DamageMultiplier * weaponTemplate.DamageMultiplier * playerMultiplier;
				float SwingExtraDamage = AdditionalDamage * swingInfo.DamageMultiplier;

				swingTimeSum += QuantizeToAnimFrameTime(SwingTime);  // quantize up to 30th of a second
				swingDamageSum += SwingDamage + SwingExtraDamage;			
			}

			float totalProjectileDamage = 0.0f;
			string projectileDPSTooltip = "";
			if (weaponTemplate.bShootMeleeProjectile == 1)
			{
				// shoot a particle with each swing
				float ProjectileMeleeDamage = (weaponTemplate.BaseAltDamage + GetEquipmentAltDamageBonus(instance, ref equipTemplate)) * PawnDamageMult;
				float ProjectileAdditionalDamage = 0.0f; 
				if (weaponTemplate.bUseAdditionalProjectileDamage == 1)
				{
					ProjectileAdditionalDamage = GetEquipmentAdditionalDamageAmount(instance, ref heroInfo, ref equipTemplate) * PawnDamageMult;
				}
				int numProjectiles = 1 + equipTemplate.WeaponNumberOfProjectilesBonus;
				if (weaponTemplate.bUseWeaponDamageForProjectileDamage == 1)
				{
					ProjectileMeleeDamage = MainDamage * weaponTemplate.WeaponProjectileDamageMultiplier;
				}
				float scaleDamageExponentMultiplier = weaponTemplate.ProjectileDamageHeroStatExponentMultiplier;

				(float projDamage, float hitDamage, float hitAdditionalDamage) = GetProjectileArrayDamage(numProjectiles, ProjectileMeleeDamage, ProjectileAdditionalDamage, scaleDamageExponentMultiplier, instance, ref heroInfo, ref weaponTemplate, ref equipTemplate);
				totalProjectileDamage = projDamage * numSwings;
				projectileDPSTooltip = $" + {hitDamage} {hitAdditionalDamage}";
			}

			float totalMeleeDamage = (swingTimeSum == 0.0f) ? MainDamage + AdditionalDamage : (swingDamageSum + totalProjectileDamage) / swingTimeSum;
			return (totalMeleeDamage, $"{MainDamage} + {AdditionalDamage}" + projectileDPSTooltip);
		}
	}
}
