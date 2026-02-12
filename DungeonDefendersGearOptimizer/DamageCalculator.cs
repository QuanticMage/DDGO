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

	public class HeroInfo
	{
		public int[] TotalStats = new int[11];     // regular stats
		public float PlayerWeaponDamageMultiplier = 0.8f ;
		public DunDefHero_Data TemplateData;
		public DunDefPlayer_Data PlayerTemplateData;
		public float GlobalDamageMultiplier = 0.155f;        // nightmare mode, unclear but definitely there modifier
		public float PlayerElementalWeaponDamageMultiplier = 0.25f;
	}


	public class DamageCalculator
	{
		public TemplateDatabase? tdb = null;
		public DamageCalculator(TemplateDatabase _tdb)
		{
			tdb = _tdb;
		}

		public float GetHeroDamageMult(ref HeroInfo heroInfo)
		{
			return GetHeroStatMult((int)LevelUpValueType.HeroDamage, ref heroInfo);			
		}

		public float GetHeroStatMult(int statType, ref HeroInfo heroInfo)
		{
			float initialComponent = 0.0f;
			float fullComponent = 0.0f;			
			switch ((LevelUpValueType)statType)
			{				
				case LevelUpValueType.HeroDamage:
					float stat = heroInfo.TotalStats[(int)DDStat.HeroDamage] + 1;
					initialComponent = heroInfo.TemplateData.StatMultInitial_HeroDamage_Competitive *
						MathF.Pow(4, (heroInfo.TemplateData.StatExpInitial_HeroDamage_Competitive * 1.1f)) - 1.0f;

					fullComponent = heroInfo.TemplateData.StatMultFull_HeroDamage_Competitive *
						(MathF.Pow(stat, (heroInfo.TemplateData.StatExpFull_HeroDamage_Competitive * 1.1f)) - 1.0f);
					break;

					// all other types follow this same formula, but with the parenthesis in the right location
				default:
					Console.WriteLine($"Need to handle stat {statType}");					
					break;					
			}
					
			return (1.0f + initialComponent + fullComponent);
		}


		public float GetPawnDamageMult(ref HeroInfo heroInfo)
		{
			return heroInfo.PlayerTemplateData.ExtraPlayerDamageMultiplier *
				   heroInfo.PlayerTemplateData.PlayerWeaponDamageMultiplier *
				   heroInfo.GlobalDamageMultiplier;
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
				(float)Math.Pow(weaponTemplate.BaseDamage * weaponTemplate.WeaponDamageMultiplier * equipTemplate.WeaponDamageMultiplier + GetEquipmentDamageBonus(instance, ref equipTemplate),
				Normalizer) * GetHeroDamageMult(ref heroInfo) * GetPawnDamageMult(ref heroInfo);
		}

		float GetEquipmentAltDamageBonus(DDEquipmentInfo instance,ref HeroEquipment_Data equipTemplate)
		{			
			if (equipTemplate.WeaponAltDamageBonusUse == 0)
				return 0.0f;
			return equipTemplate.WeaponAltDamageMultiplier * instance.WeaponAltDamageBonus;
        }

		float GetEquipmentAdditionalDamageAmount(DDEquipmentInfo instance, ref HeroInfo heroInfo, ref HeroEquipment_Data equipTemplate)
		{
			return equipTemplate.ElementalDamageMultiplier * heroInfo.PlayerElementalWeaponDamageMultiplier * instance.WeaponAdditionalDamageAmount;			
		}

		float GetProjectileArrayDamage(int nProjectiles, float ProjectileMainDamage, float ProjectileAdditionalDamage, float scaleDamageExponentMultiplier, DDEquipmentInfo instance, ref HeroInfo heroInfo, ref DunDefWeapon_Data weaponTemplate, ref HeroEquipment_Data equipTemplate)
		{
			float totalDamage = 0.0f;
			float standardProjDamage = 0.0f;
			if (weaponTemplate.ProjectileTemplate != -1)
			{
				var projectileTemplate = tdb.GetDunDefProjectile(weaponTemplate.ProjectileTemplate);
				standardProjDamage = GetProjectileDamage(ProjectileMainDamage, ProjectileAdditionalDamage, scaleDamageExponentMultiplier, instance, ref heroInfo, ref projectileTemplate, ref weaponTemplate, ref equipTemplate);
			}
					
			if ((weaponTemplate.bRandomizeProjectileTemplate == 1) && weaponTemplate.RandomizedProjectileTemplate.Count > 0)
			{
				totalDamage = 0.0f;
				for (int i = 0; i < weaponTemplate.RandomizedProjectileTemplate.Count; i++)
				{
					var randomProjectile = tdb.GetDunDefProjectile(weaponTemplate.RandomizedProjectileTemplate.Start + i);
					totalDamage += GetProjectileDamage(ProjectileMainDamage, ProjectileAdditionalDamage, scaleDamageExponentMultiplier, instance, ref heroInfo, ref randomProjectile, ref weaponTemplate, ref equipTemplate);
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
						totalDamage += GetProjectileDamage(ProjectileMainDamage, ProjectileAdditionalDamage, scaleDamageExponentMultiplier, instance, ref heroInfo, ref extraProjectile, ref weaponTemplate, ref equipTemplate);
					}
					else
					{
						totalDamage += standardProjDamage;
					}
				}				
			}
			return totalDamage;
		}


		float GetProjectileDamage(float ProjectileMainDamage, float ProjectileAdditionalDamage, float scaleDamageExponentMultiplier, DDEquipmentInfo instance, ref HeroInfo heroInfo, ref DunDefProjectile_Data projTemplate, ref DunDefWeapon_Data weaponTemplate, ref HeroEquipment_Data equipTemplate)
		{
			float baseDamage = ProjectileMainDamage;
			float additionalDamage = ProjectileAdditionalDamage;
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

				baseDamage *= (float)Math.Pow(GetHeroStatMult(projTemplate.ScaleDamageStatType, ref heroInfo), projTemplate.ScaleDamageStatExponent * scaleDamageExponentMultiplier);

				if (projTemplate.MultiplyProjectileDamageByPrimaryWeaponSwingSpeed == 1)
				{
					baseDamage *= MathF.Max(instance.WeaponSwingSpeedMultiplier, 1.0f);
				}

				float scalingFactor = 1.0f;
				if (projTemplate.bSecondScaleDamageStatType == 1)
				{
					scalingFactor = (float)Math.Pow(GetHeroStatMult(projTemplate.SecondScaleDamageStatType, ref heroInfo), projTemplate.ScaleDamageStatExponent * scaleDamageExponentMultiplier);
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
				if ((instance.MaxLevel == 325) || (instance.MaxLevel == 370))
				{
					Console.WriteLine($"{instance.GeneratedName} {instance.Template}:{baseDamage} {additionalDamage} {scalingFactor}");
				}
			}
			return baseDamage + additionalDamage;
		}


		//====================================================================================================
		// RANGED DAMAGE CALCULATIONS
		//======================================================================================================
		public float GetCrossbowWeaponDamage(DDEquipmentInfo instance, ref HeroInfo heroInfo, ref HeroEquipment_Data equipTemplate)
		{
			// tODO: pass in AdditionalDamage instead of EquipDamage
		
			var weaponTemplate = tdb.GetDunDefWeapon(equipTemplate.EquipmentWeaponTemplate);


			
			float EquipmentBaseDamage = (weaponTemplate.BaseDamage * weaponTemplate.WeaponDamageMultiplier * equipTemplate.WeaponDamageMultiplier);
			float EquipmentDamage = equipTemplate.WeaponDamageMultiplier * instance.WeaponDamageBonus + EquipmentBaseDamage;

			float PawnDamageMult = GetPawnDamageMult(ref heroInfo);

			float DamagePerProjectile = EquipmentDamage * PawnDamageMult;

			float FireRate = (weaponTemplate.BaseShotsPerSecond + (instance.WeaponShotsPerSecondBonus-127)) / weaponTemplate.FireIntervalMultiplier;

			int NumProjectiles = (weaponTemplate.BaseNumProjectiles + (instance.WeaponNumberOfProjectilesBonus-127));

			float AdditionalDamagePerProjectile = (weaponTemplate.bUseAdditionalProjectileDamage == 1) ?
											instance.WeaponAdditionalDamageAmount * heroInfo.PlayerElementalWeaponDamageMultiplier * PawnDamageMult *  equipTemplate.ElementalDamageMultiplier : 0.0f;
				 
			

			float damage = GetProjectileArrayDamage(NumProjectiles, DamagePerProjectile, AdditionalDamagePerProjectile, 1.0f, instance, ref heroInfo, ref weaponTemplate, ref equipTemplate);

			if ((instance.MaxLevel == 325) || (instance.MaxLevel == 370))
			{
					Console.WriteLine($"{instance.GeneratedName} {instance.Template}: {damage} {NumProjectiles} {DamagePerProjectile} {AdditionalDamagePerProjectile} {FireRate}");
			}


			return damage * FireRate;
		}

		//====================================================================================================
		// MELEE DAMAGE CALCULATIONS
		//======================================================================================================
		public float GetMeleeWeaponDamage(DDEquipmentInfo instance, ref HeroInfo heroInfo, ref HeroEquipment_Data equipTemplate)
		{
			var weaponTemplate = tdb.GetDunDefWeapon(equipTemplate.EquipmentWeaponTemplate);

			int BaseDamage = weaponTemplate.BaseDamage;

			float EquipmentBaseDamage = MathF.Max((float)BaseDamage * weaponTemplate.WeaponProjectileDamageMultiplier * ((int)equipTemplate.WeaponDamageMultiplier) + 
												  GetEquipmentDamageBonus(instance, ref equipTemplate) * weaponTemplate.WeaponDamageMultiplier, 1.0f);			
			float SwingAdjustment = MathF.Max((float)MathF.Pow(weaponTemplate.DamageIncreaseForSwingSpeedFactor / (weaponTemplate.SpeedMultiplier * instance.WeaponSwingSpeedMultiplier), weaponTemplate.SpeedMultiplierDamageExponent),1.0f);
			float HeroDamageMult = GetHeroDamageMult(ref heroInfo);
			float PawnDamageMult = GetPawnDamageMult(ref heroInfo);

			float AdditionalDamage = instance.WeaponAdditionalDamageAmount * PawnDamageMult * equipTemplate.ElementalDamageMultiplier;			

			float MainDamage = EquipmentBaseDamage * SwingAdjustment * HeroDamageMult * PawnDamageMult;


			Array_Data swingInfoRef = weaponTemplate.MeleeSwingInfos;
			Array_Data playerSwingInfoRef = heroInfo.PlayerTemplateData.MeleeSwingInfoMultipliers;
			Array_Data mainHandSwingInfoRef = heroInfo.PlayerTemplateData.MainHandSwingInfoMultipliers;
			Array_Data offHandSwingInfoRef = heroInfo.PlayerTemplateData.OffHandSwingInfoMultipliers;
			float swingTimeSum = 0.0f;
			float swingDamageSum = 0.0f;
			float totalSpeedMult = instance.WeaponSwingSpeedMultiplier * weaponTemplate.SpeedMultiplier * weaponTemplate.ExtraSpeedMultiplier ;

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

				float AnimSpeed = swingInfo.AnimSpeed  * totalSpeedMult;
				float SwingTime = (swingDurations[i] - swingInfo.TimeBeforeEndToAllowNextCombo) / AnimSpeed;
				float SwingDamage = MainDamage * swingInfo.DamageMultiplier * weaponTemplate.DamageMultiplier * playerMultiplier;
				float SwingExtraDamage = AdditionalDamage * swingInfo.DamageMultiplier;

				swingTimeSum += SwingTime;
				swingDamageSum += SwingDamage + SwingExtraDamage;						
			}

			float totalProjectileDamage = 0.0f;
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
					ProjectileMeleeDamage = (MainDamage + AdditionalDamage) * weaponTemplate.WeaponProjectileDamageMultiplier;
				}
				float scaleDamageExponentMultiplier = weaponTemplate.ProjectileDamageHeroStatExponentMultiplier;

				float projDamage = GetProjectileArrayDamage(numProjectiles, ProjectileMeleeDamage, ProjectileAdditionalDamage, scaleDamageExponentMultiplier, instance, ref heroInfo, ref weaponTemplate, ref equipTemplate);
				totalProjectileDamage = projDamage * numSwings;
			}

			float totalMeleeDamage = (swingTimeSum == 0.0f) ? MainDamage + AdditionalDamage : (swingDamageSum + totalProjectileDamage) / swingTimeSum;
			return totalMeleeDamage;
		}
	}
}
