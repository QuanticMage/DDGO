using System.Reflection.Metadata.Ecma335;
using System.Security.Claims;

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

		public float GetHeroDamageMult(ref HeroInfo heroInfo)
		{
			float damageStat = heroInfo.TotalStats[(int)DDStat.HeroDamage]+ 1;

			float initialComponent = heroInfo.TemplateData.StatMultInitial_HeroDamage *
				(MathF.Pow(MathF.Min(damageStat, heroInfo.TemplateData.StatBoostCapInitial_HeroDamage),
						   (heroInfo.TemplateData.StatExpInitial_HeroDamage * 1.1f)) - 1.0f);

			float fullComponent = heroInfo.TemplateData.StatMultFull_HeroDamage *
				(MathF.Pow(damageStat, (heroInfo.TemplateData.StatExpFull_HeroDamage * 1.1f)) - 1.0f);

			return 1.0f + initialComponent + fullComponent;
		}

		public int GetEquipmentDamageBonus( ref DDEquipmentInfo instance, ref HeroInfo heroInfo, ref HeroEquipment_Data equip, ref DunDefWeapon_Data weaponTemplate) // stat 0
		{
			if (equip.EquipmentWeaponTemplate == -1)
				return 0;

			float damage = weaponTemplate.WeaponDamageMultiplier * equip.WeaponDamageMultiplier * instance.WeaponDamageBonus;		
			return (int)((damage >= 1.0f) ? MathF.Floor(damage) : MathF.Ceiling(damage));
		}

		public float GetPawnDamageMult(ref HeroInfo heroInfo)
		{
			return heroInfo.PlayerTemplateData.ExtraPlayerDamageMultiplier *
				   heroInfo.PlayerTemplateData.PlayerWeaponDamageMultiplier *
				   heroInfo.GlobalDamageMultiplier;
		}

		//public double GetAverageDamage( ExportedTemplateDatabase tdb, DDDatabase db, DunDefWeapon_Info weaponInfo, DDHeroInfo currentHero)
		public float GetWeaponDamage(DDEquipmentInfo instance, ref HeroInfo heroInfo, ref HeroEquipment_Data equipTemplate)
		{
			var weaponTemplate = tdb.GetDunDefWeapon(equipTemplate.EquipmentWeaponTemplate);

			int BaseDamage = weaponTemplate.BaseDamage;
			float EquipmentWeaponDamageBonus = GetEquipmentDamageBonus(ref instance, ref heroInfo, ref equipTemplate, ref weaponTemplate );         
		
			float EquipmentBaseDamage = MathF.Max((float)BaseDamage * weaponTemplate.WeaponProjectileDamageMultiplier * equipTemplate.WeaponDamageMultiplier + EquipmentWeaponDamageBonus, 1.0f);			
			float SwingAdjustment = MathF.Max((float)MathF.Pow(weaponTemplate.DamageIncreaseForSwingSpeedFactor / (weaponTemplate.SpeedMultiplier * instance.WeaponSwingSpeedMultiplier), weaponTemplate.SpeedMultiplierDamageExponent),1.0f);
			float HeroDamageMult = GetHeroDamageMult(ref heroInfo);
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
			float totalSpeedMult = instance.WeaponSwingSpeedMultiplier * weaponTemplate.SpeedMultiplier;

			int numSwings = 3;
			if (heroInfo.PlayerTemplateData.OffHandSwingInfoMultipliers.Count > 0)
				numSwings = 4;
			Span<float> swingDurations = stackalloc float[4];
			Span<int> swingInfoIndex = stackalloc int[4];

			if (instance.MaxLevel == 496 )
			{
				int x = 1;
			}

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

			for (int i = 0; i < numSwings; i++)
			{				
				var swingInfo = tdb.GetMeleeSwingInfo(swingInfoIndex[i]);				
				float playerMultiplier = 1.0f;
				if (playerSwingInfoRef.Count > i)
					playerMultiplier = tdb.GetMeleeSwingInfo(playerSwingInfoRef.Start + i).DamageMultiplier;
				
				
				swingTimeSum += (swingDurations[i] - swingInfo.TimeBeforeEndToAllowNextCombo) / totalSpeedMult;
				swingDamageSum += MainDamage * swingInfo.DamageMultiplier * playerMultiplier + AdditionalDamage * swingInfo.DamageMultiplier;				
			}
			if (instance.MaxLevel == 435)
			{
				Console.WriteLine($"Damage {BaseDamage} {weaponTemplate.WeaponProjectileDamageMultiplier} * {equipTemplate.WeaponDamageMultiplier} + {EquipmentWeaponDamageBonus}");
				Console.WriteLine($"Damage {MainDamage} = {EquipmentBaseDamage} * {SwingAdjustment} * {HeroDamageMult} * {PawnDamageMult} ");
				Console.WriteLine($"Additional Damage {AdditionalDamage}");
				Console.WriteLine($"Speed {totalSpeedMult} = {equipTemplate.WeaponSwingSpeedMultiplier} * {weaponTemplate.SpeedMultiplier}");
				Console.WriteLine($"Durations {swingDurations[0]} {swingDurations[1]} {swingDurations[2]} {swingDurations[3]}");
				Console.WriteLine($"DamageMult Weapon {tdb.GetMeleeSwingInfo(swingInfoIndex[0]).DamageMultiplier} {tdb.GetMeleeSwingInfo(swingInfoIndex[1]).DamageMultiplier} {tdb.GetMeleeSwingInfo(swingInfoIndex[2]).DamageMultiplier}");
				Console.WriteLine($"DamageMult Weapon {tdb.GetMeleeSwingInfo(swingInfoIndex[0]).TimeBeforeEndToAllowNextCombo} {tdb.GetMeleeSwingInfo(swingInfoIndex[1]).TimeBeforeEndToAllowNextCombo} {tdb.GetMeleeSwingInfo(swingInfoIndex[2]).TimeBeforeEndToAllowNextCombo}");
				Console.WriteLine($"DamageMult Player {tdb.GetMeleeSwingInfo(playerSwingInfoRef.Start).DamageMultiplier} {tdb.GetMeleeSwingInfo(playerSwingInfoRef.Start+1).DamageMultiplier} {tdb.GetMeleeSwingInfo(playerSwingInfoRef.Start+2).DamageMultiplier}");
				Console.WriteLine($"Swings {swingInfoRef.Count}: {swingTimeSum} {swingDamageSum}");
			}
			return (swingTimeSum == 0.0f) ? MainDamage + AdditionalDamage : (swingDamageSum / swingTimeSum);			
		}
	}
}
