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
		public float GlobalDamageMultiplier = 1.0f; 		
	}


	public class DamageCalculator
	{
		public ExportedTemplateDatabase tdb;
		public void Initialize(ExportedTemplateDatabase _tdb)
		{
			tdb = _tdb;
		}

		public float GetHeroDamageMult(ref HeroInfo heroInfo)
		{
			int damageStat = heroInfo.TotalStats[(int)DDStat.HeroDamage] + 1;

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
			if (instance.Type != "Weapon")
			{
				// set to 0 
				return 0.0f;
			}			

			var weaponTemplate = tdb.GetDunDefWeapon(equipTemplate.EquipmentWeaponTemplate);

			int BaseDamage = weaponTemplate.BaseDamage;
			float EquipmentWeaponDamageBonus = GetEquipmentDamageBonus(ref instance, ref heroInfo, ref equipTemplate, ref weaponTemplate );         
		
			float EquipmentBaseDamage = MathF.Max((float)BaseDamage * weaponTemplate.WeaponProjectileDamageMultiplier * weaponTemplate.DamageMultiplier + EquipmentWeaponDamageBonus, 1.0f);			
			float SwingAdjustment = MathF.Max((float)MathF.Pow(weaponTemplate.DamageIncreaseForSwingSpeedFactor * weaponTemplate.SpeedMultiplier * equipTemplate.WeaponSwingSpeedMultiplier, weaponTemplate.SpeedMultiplierDamageExponent),1.0f);
			float HeroDamageMult = GetHeroDamageMult(ref heroInfo);
			float PawnDamageMult = GetPawnDamageMult(ref heroInfo);

			return EquipmentBaseDamage * SwingAdjustment * HeroDamageMult * PawnDamageMult;
			Console.WriteLine($"Damage {instance.GeneratedName} : {EquipmentBaseDamage} {SwingAdjustment} {HeroDamageMult} {PawnDamageMult}");
		}


	}
}
