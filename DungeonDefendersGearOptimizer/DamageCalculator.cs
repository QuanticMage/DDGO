using Microsoft.VisualBasic;
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
		public float PlayerWeaponDamageMultiplier = 0.8f;
		public DunDefHero_Data TemplateData;
		public DunDefPlayer_Data PlayerTemplateData;
		public float GlobalDamageMultiplier = 0.155f; 		 // nightmare mode
	}


	public class DamageCalculator
	{
		public TemplateDatabase? tdb = null;
		public DamageCalculator(TemplateDatabase _tdb)
		{
			tdb = _tdb;
		}

		public float GetHeroDamageMult(ref HeroInfo heroInfo, bool Debug)
		{
			float damageStat = heroInfo.TotalStats[(int)DDStat.HeroDamage]+ 1;

			float initialComponent = heroInfo.TemplateData.StatMultInitial_HeroDamage *
				(MathF.Pow(MathF.Min(damageStat, heroInfo.TemplateData.StatBoostCapInitial_HeroDamage),
						   (heroInfo.TemplateData.StatExpInitial_HeroDamage * 1.1f)) - 1.0f);

			float fullComponent = heroInfo.TemplateData.StatMultFull_HeroDamage *
				(MathF.Pow(damageStat, (heroInfo.TemplateData.StatExpFull_HeroDamage * 1.1f)) - 1.0f);

			if (Debug)
			{
				Console.WriteLine($"Hero Damage Init: {initialComponent} {heroInfo.TemplateData.StatMultInitial_HeroDamage} {damageStat} {heroInfo.TemplateData.StatBoostCapInitial_HeroDamage} {heroInfo.TemplateData.StatExpInitial_HeroDamage}");
				Console.WriteLine($"Hero Damage Full: {fullComponent} {heroInfo.TemplateData.StatMultFull_HeroDamage} {heroInfo.TemplateData.StatExpFull_HeroDamage}");
			}
			return 1.0f + initialComponent + fullComponent;
		}

		public float GetPawnDamageMult(ref HeroInfo heroInfo)
		{
			return heroInfo.PlayerTemplateData.ExtraPlayerDamageMultiplier *
				   heroInfo.PlayerTemplateData.PlayerWeaponDamageMultiplier *
				   heroInfo.GlobalDamageMultiplier;
		}

		float GetEquipmentDamageBonus(DDEquipmentInfo instance, ref DunDefWeapon_Data weaponTemplate, ref HeroEquipment_Data equipTemplate)
		{
			// Step 1: Apply multipliers to bonus
			float floatValue = (equipTemplate.WeaponDamageMultiplier *  weaponTemplate.WeaponDamageMultiplier) *
							   (float)instance.WeaponDamageBonus;

			// Step 2: Convert to int (or ceiling if < 1)
			int value = (floatValue >= 1.0f) ? (int)floatValue : (int)MathF.Ceiling(floatValue);

			return MathF.Ceiling((float)value);
		}

		// TODO : melee weapons that fire projectiles
		public float GetProjectileDamage(float baseDamage, float equipDamage, ref DunDefProjectile_Data p, ref HeroInfo heroInfo, ref HeroEquipment_Data equipTemplate)
		{
			var weaponTemplate = tdb.GetDunDefWeapon(equipTemplate.EquipmentWeaponTemplate);
		
			if (p.MultiplyProjectileDamageByWeaponDamage == 1)
			{
				float weaponDamageNormalized = (float)Math.Pow(equipDamage, p.ProjectileDamageByWeaponDamageDivider) *
					GetHeroDamageMult(ref heroInfo, false) * GetPawnDamageMult(ref heroInfo);
				float damageMult = weaponDamageNormalized;

				if (weaponTemplate.bUseDamageReductionForAbilities == 1)
				{
					if ( weaponDamageNormalized > 7500000.0f)
					{
						float excess = weaponDamageNormalized - 7500000.0f;
						damageMult = (7500000.0f + (excess * 0.75f));
					}
				}

				baseDamage *= damageMult;
			}
			return baseDamage;

			/*float heroStatMod = p.ScaleDamageStatType
			
			float projDamage = Math.Max(
				(baseDamage * weaponTemplate.WeaponDamageMultiplier * equipTemplate.WeaponDamageMultiplier + equipDamageBonus)
				* pawnDamageMod,
				1.0f
			);
			DamagePerProjectile*/
		}

		public float GetCrossbowWeaponDamage(DDEquipmentInfo instance, ref HeroInfo heroInfo, ref HeroEquipment_Data equipTemplate)
		{
			

			var weaponTemplate = tdb.GetDunDefWeapon(equipTemplate.EquipmentWeaponTemplate);

			float EquipDamage = ((float)weaponTemplate.BaseDamage * weaponTemplate.WeaponDamageMultiplier * equipTemplate.WeaponDamageMultiplier +
									   instance.WeaponDamageBonus * weaponTemplate.WeaponDamageMultiplier * equipTemplate.WeaponDamageMultiplier);
			float HeroDamageMult = GetHeroDamageMult(ref heroInfo, false);
			float PawnDamageMult = GetPawnDamageMult(ref heroInfo);

			float DamagePerProjectile = EquipDamage * PawnDamageMult;

			float FireRate = (weaponTemplate.BaseShotsPerSecond + (instance.WeaponShotsPerSecondBonus-127)) / weaponTemplate.FireIntervalMultiplier;

			int NumProjectiles = (weaponTemplate.BaseNumProjectiles + (instance.WeaponNumberOfProjectilesBonus-127));


			float TotalDamage = 0.0f;
			if (weaponTemplate.ProjectileTemplate == -1)
			{
				Console.WriteLine("ERROR: Missing Projectile Template!!");
			}
			float AdditionalDamagePerProjectile = (weaponTemplate.bUseAdditionalProjectileDamage == 1) ? weaponTemplate.AdditionalDamageAmount * PawnDamageMult: 0.0f;

			
			var projectile = tdb.GetDunDefProjectile(weaponTemplate.ProjectileTemplate);
			float baseProjDamage = GetProjectileDamage(DamagePerProjectile, EquipDamage, ref projectile, ref heroInfo, ref equipTemplate);

			Console.WriteLine($"{instance.GeneratedName} {instance.Template}: {baseProjDamage} {AdditionalDamagePerProjectile} {HeroDamageMult} {PawnDamageMult}");

			float SumDamage = 0.0f;

			if ((weaponTemplate.bRandomizeProjectileTemplate==1) && weaponTemplate.RandomizedProjectileTemplate.Count > 0)
			{
				
				for (int i = 0; i < weaponTemplate.RandomizedProjectileTemplate.Count; i++)
				{
					var randomProjectile = tdb.GetDunDefProjectile(weaponTemplate.RandomizedProjectileTemplate.Start + i);
					SumDamage += GetProjectileDamage(DamagePerProjectile, EquipDamage, ref randomProjectile, ref heroInfo, ref equipTemplate);
				}
				TotalDamage = SumDamage / (float)weaponTemplate.RandomizedProjectileTemplate.Count * NumProjectiles;
			}
			else
			{
				for (int i = 0; i < weaponTemplate.NumProjectiles; i++)
				{
					if (weaponTemplate.ExtraProjectileTemplates.Count > i)
					{
						var extraProjectile = tdb.GetDunDefProjectile(weaponTemplate.ExtraProjectileTemplates.Start + i);
						SumDamage += GetProjectileDamage(DamagePerProjectile, EquipDamage, ref extraProjectile, ref heroInfo, ref equipTemplate);
					}						
					else
					{
						SumDamage += baseProjDamage;
					}						
				}
				TotalDamage = SumDamage;
			}
			Console.WriteLine($"{instance.GeneratedName} {instance.Template}: {TotalDamage} {AdditionalDamagePerProjectile} {NumProjectiles} {FireRate}");
			
			return ((TotalDamage + AdditionalDamagePerProjectile) * NumProjectiles) * FireRate;
		}


		public float GetMeleeWeaponDamage(DDEquipmentInfo instance, ref HeroInfo heroInfo, ref HeroEquipment_Data equipTemplate)
		{
			var weaponTemplate = tdb.GetDunDefWeapon(equipTemplate.EquipmentWeaponTemplate);

			int BaseDamage = weaponTemplate.BaseDamage;
		
			// equipTemplate.WeaponDamageMultiplier is cast to an int in a function call.  PAINFUL
			float EquipmentBaseDamage = MathF.Max((float)BaseDamage * weaponTemplate.WeaponProjectileDamageMultiplier * ((int)equipTemplate.WeaponDamageMultiplier) + 
												  GetEquipmentDamageBonus(instance, ref weaponTemplate, ref equipTemplate), 1.0f);			
			float SwingAdjustment = MathF.Max((float)MathF.Pow(weaponTemplate.DamageIncreaseForSwingSpeedFactor / (weaponTemplate.SpeedMultiplier * instance.WeaponSwingSpeedMultiplier), weaponTemplate.SpeedMultiplierDamageExponent),1.0f);
			float HeroDamageMult = GetHeroDamageMult(ref heroInfo, ((instance.MaxLevel == 435) || (instance.MaxLevel == 496) || (instance.MaxLevel == 446)));
			float PawnDamageMult = GetPawnDamageMult(ref heroInfo);

			float AdditionalDamage = instance.WeaponAdditionalDamageAmount * PawnDamageMult * equipTemplate.ElementalDamageMultiplier;			

			//Console.WriteLine($"Damage {instance.GeneratedName} : {BaseDamage} {weaponTemplate.WeaponProjectileDamageMultiplier} {weaponTemplate.DamageMultiplier} {EquipmentWeaponDamageBonus} : {EquipmentBaseDamage} {SwingAdjustment} {HeroDamageMult} {PawnDamageMult}");
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
				// barbarian
				swingDurations[0] = 0.6667f;
				swingDurations[1] = 0.6667f;
				swingDurations[2] = 0.6667f;
				swingDurations[3] = 0.6667f;
				swingInfoIndex[0] = mainHandSwingInfoRef.Start;
				swingInfoIndex[1] = offHandSwingInfoRef.Start;
				swingInfoIndex[2] = mainHandSwingInfoRef.Start + 1;
				swingInfoIndex[3] = offHandSwingInfoRef.Start + 1;
			}
			if ((instance.MaxLevel == 435) || (instance.MaxLevel == 496) || (instance.MaxLevel == 446))
			{
				Console.WriteLine($"----- {instance.GeneratedName} {instance.Template}-----");
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

				if ((instance.MaxLevel == 435) || (instance.MaxLevel == 496) || (instance.MaxLevel == 446))
					Console.WriteLine($"Swing {i}: {SwingDamage} + {SwingExtraDamage} in {SwingTime} seconds");

				swingTimeSum += SwingTime;
				swingDamageSum += SwingDamage;
			}

			DunDefWeapon_Data weapon = weaponTemplate;
			HeroInfo hero = heroInfo;
			HeroEquipment_Data he = equipTemplate;


			if ((instance.MaxLevel == 435) || (instance.MaxLevel == 496) || (instance.MaxLevel == 446))
			{
				Console.WriteLine($"Swing Adjustment: {weaponTemplate.DamageIncreaseForSwingSpeedFactor} / ({weaponTemplate.SpeedMultiplier} * {instance.WeaponSwingSpeedMultiplier}) ^ {weaponTemplate.SpeedMultiplierDamageExponent}");

				Console.WriteLine($"Damage {BaseDamage} {weaponTemplate.WeaponProjectileDamageMultiplier} * {equipTemplate.WeaponDamageMultiplier} + {instance.WeaponDamageBonus * weaponTemplate.WeaponDamageMultiplier * equipTemplate.WeaponDamageMultiplier}");
				Console.WriteLine($"Damage {MainDamage} = {EquipmentBaseDamage} * {SwingAdjustment} * {HeroDamageMult} * {PawnDamageMult} ");
				Console.WriteLine($"Additional Damage {AdditionalDamage}");
				Console.WriteLine($"Weapon Template DamageMult {weaponTemplate.DamageMultiplier} {weaponTemplate.WeaponDamageMultiplier} { instance.WeaponDamageBonus}");
				Console.WriteLine($"Anim Speed {weaponTemplate.WeaponSpeedMultiplier} {tdb.GetMeleeSwingInfo(swingInfoIndex[0]).AnimSpeed} {tdb.GetMeleeSwingInfo(swingInfoIndex[1]).AnimSpeed} {tdb.GetMeleeSwingInfo(swingInfoIndex[2]).AnimSpeed}");
				Console.WriteLine($"Speed {totalSpeedMult} = {instance.WeaponSwingSpeedMultiplier} * {weaponTemplate.SpeedMultiplier} * {weaponTemplate.ExtraSpeedMultiplier}");
				Console.WriteLine($"Durations {swingDurations[0]} {swingDurations[1]} {swingDurations[2]} {swingDurations[3]}");
				Console.WriteLine($"Anim Speed {tdb.GetMeleeSwingInfo(swingInfoIndex[0]).AnimSpeed} {tdb.GetMeleeSwingInfo(swingInfoIndex[1]).AnimSpeed} {tdb.GetMeleeSwingInfo(swingInfoIndex[2]).AnimSpeed}");
				Console.WriteLine($"DamageMult Weapon {tdb.GetMeleeSwingInfo(swingInfoIndex[0]).DamageMultiplier} {tdb.GetMeleeSwingInfo(swingInfoIndex[1]).DamageMultiplier} {tdb.GetMeleeSwingInfo(swingInfoIndex[2]).DamageMultiplier}");
				Console.WriteLine($"DamageMult Weapon {tdb.GetMeleeSwingInfo(swingInfoIndex[0]).TimeBeforeEndToAllowNextCombo} {tdb.GetMeleeSwingInfo(swingInfoIndex[1]).TimeBeforeEndToAllowNextCombo} {tdb.GetMeleeSwingInfo(swingInfoIndex[2]).TimeBeforeEndToAllowNextCombo}");
				Console.WriteLine($"DamageMult Player {tdb.GetMeleeSwingInfo(playerSwingInfoRef.Start).DamageMultiplier} {tdb.GetMeleeSwingInfo(playerSwingInfoRef.Start+1).DamageMultiplier} {tdb.GetMeleeSwingInfo(playerSwingInfoRef.Start+2).DamageMultiplier}");
				Console.WriteLine($"Swings {swingInfoRef.Count}: {swingTimeSum} {swingDamageSum}");
			}
			return (swingTimeSum == 0.0f) ? MainDamage + AdditionalDamage : (swingDamageSum / swingTimeSum);			
		}
	}
}
