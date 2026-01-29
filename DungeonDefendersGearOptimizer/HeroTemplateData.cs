namespace DDUP
{
	public class HeroTemplateData
	{
		public string[] BaseIcons = new string[]
		{
			"png/Hero Health.png",
			"png/Hero Damage.png",
			"png/Move Speed.png",
			"png/Cast Rate.png",
			"png/AB1/Monk AB1.png",
			"png/AB2/Hero Boost Monk AB2.png",
			"png/Tower Health.png",
			"png/Tower Damage.png",
			"png/Tower Range.png",
			"png/Tower Rate.png"
		};

		// Apprentice, Squire, Huntress, Monk
		// Adept, Countess, Ranger, Initiate
		// Jester, Barbarian, Series EV, Summoner
		// Hermit, Gunwitch, Warden
				
		public List<string> StatIcons = new();
		public string ClassName = "";
		public string ClassIcon ="";
		public string ExtraType = "None";
		public string WeaponType = "None";
		public int Index;		

		public static readonly Dictionary<string, HeroTemplateData> Map = new()
		{
			// ========= APPRENTICE ===========
			["DunDefPlayers.HeroTemplateApprentice"] = new HeroTemplateData
			{
				Index = 0,
				ClassName = "Apprentice",
				StatIcons = new() {
					"png/Hero Health.png",
					"png/Move Speed.png",
					"png/Hero Damage.png",
					"png/Cast Rate.png",
					"png/AB1/Apprentice AB1.png",
					"png/AB2/Apprentice AB2.png",
					"png/Tower Health.png",
					"png/Tower Damage.png",
					"png/Tower Rate.png",
					"png/Tower Range.png",
				},
				ClassIcon = "png/Apprentice_TinyIcon.png",
				ExtraType = "None",
				WeaponType = "Apprentice"
			},

			// ========= SQUIRE ===========
			["DunDefPlayers.HeroTemplateSquire"] = new HeroTemplateData
			{
				Index = 1,
				ClassName = "Squire",
				StatIcons = new() {
					"png/Hero Health.png",
					"png/Move Speed.png",
					"png/Hero Damage.png",
					"png/Cast Rate.png",
					"png/AB1/Squire AB1.png",
					"png/AB2/Squire AB2.png",
					"png/Tower Health.png",
					"png/Tower Damage.png",
					"png/Tower Rate.png",
					"png/Tower Range.png",
				},
				ClassIcon = "png/Squire_TinyIcon.png",
				ExtraType = "Shield",
				WeaponType = "Squire"
			},
			// ========= HUNTRESS ===========
			["DunDefPlayers.HeroTemplateInitiate"] = new HeroTemplateData
			{
				Index = 2,
				ClassName = "Huntress",
				StatIcons = new() {
					"png/Hero Health.png",
					"png/Move Speed.png",
					"png/Hero Damage.png",
					"png/Cast Rate.png",
					"png/AB1/Huntress AB1.png",
					"png/AB2/Huntress AB2.png",
					"png/Tower Health.png",
					"png/Tower Damage.png",
					"png/Tower Rate.png",
					"png/Tower Range.png",
				},
				ClassIcon = "png/Huntress_TinyIcon.png",
				ExtraType = "None",
				WeaponType = "Huntress"
			},

			// ============== MONK ==============
			["DunDefPlayers.HeroTemplateRecruit"] = new HeroTemplateData
			{
				Index = 3,
				ClassName = "Monk",
				StatIcons = new() {
					"png/Hero Health.png",
					"png/Move Speed.png",
					"png/Hero Damage.png",
					"png/Cast Rate.png",
					"png/AB1/Monk AB1.png",
					"png/AB2/Hero Boost Monk AB2.png",
					"png/Tower Health.png",
					"png/Tower Damage.png",
					"png/Tower Rate.png",
					"png/Tower Range.png",
				},
				ClassIcon = "png/Monk_TinyIcon.png",
				ExtraType = "None",
				WeaponType = "Monk"
			},

			// ========= ADEPT ===========
			["DunDefNewHeroes.HeroTemplateSorceress"] = new HeroTemplateData
			{
				Index = 4,
				ClassName = "Adept",
				StatIcons = new() {
					"png/Hero Health.png",
					"png/Move Speed.png",
					"png/Hero Damage.png",
					"png/Cast Rate.png",
					"png/AB1/Adept AB1.png",
					"png/AB2/Adept AB2.png",
					"png/Tower Health.png",
					"png/Tower Damage.png",
					"png/Tower Rate.png",
					"png/Tower Range.png",
				},
				ClassIcon = "png/Sorceress_tinyIcon.png",
				ExtraType = "None",
				WeaponType = "Apprentice"
			},

			// ========= COUNTESS ===========
			["DunDefNewHeroes.HeroTemplateLadyKnight"] = new HeroTemplateData
			{
				Index = 5,
				ClassName = "Countess",
				StatIcons = new() {
					"png/Hero Health.png",
					"png/Move Speed.png",
					"png/Hero Damage.png",
					"png/Cast Rate.png",
					"png/AB1/Countess AB1.png",
					"png/AB2/Jouse Countess AB2.png",
					"png/Tower Health.png",
					"png/Tower Damage.png",
					"png/Tower Rate.png",
					"png/Tower Range.png",
				},
				ClassIcon = "png/ladyKnight_tinyIcon.png",
				ExtraType = "Shield",
				WeaponType = "Squire"
			},
			// ========= HUNTER ===========
			["DunDefNewHeroes.HeroTemplateRanger"] = new HeroTemplateData
			{
				Index = 6,
				ClassName = "Ranger",
				StatIcons = new() {
					"png/Hero Health.png",
					"png/Move Speed.png",
					"png/Hero Damage.png",
					"png/Cast Rate.png",
					"png/AB1/Ranger AB1.png",
					"png/AB2/Ranger AB2.png",
					"png/Tower Health.png",
					"png/Tower Damage.png",
					"png/Tower Rate.png",
					"png/Tower Range.png",
				},
				ClassIcon = "png/hunter_Tinyicon.png",
				ExtraType = "None",
				WeaponType = "Huntress"
			},

			// ============== INITIATE ==============
			["DunDefNewHeroes.HeroTemplateMonkette"] = new HeroTemplateData
			{
				Index = 7,
				ClassName = "Initiate",
				StatIcons = new() {
					"png/Hero Health.png",
					"png/Move Speed.png",
					"png/Hero Damage.png",
					"png/Cast Rate.png",
					"png/AB1/Initiate AB1.png",
					"png/AB2/Initiate AB2.png",
					"png/Tower Health.png",
					"png/Tower Damage.png",
					"png/Tower Rate.png",
					"png/Tower Range.png",
				},
				ClassIcon = "png/monkette_tinyIcon.png",
				ExtraType = "None",
				WeaponType = "Monk"
			},


			// ============== JESTER ==============
			["DunDefNewHeroes.HeroTemplateJester"] = new HeroTemplateData
			{
				Index = 8,
				ClassName = "Jester",
				StatIcons = new() {
					"png/Hero Health.png",
					"png/Move Speed.png",
					"png/Hero Damage.png",
					"png/Cast Rate.png",
					"png/AB1/Jester AB1.png",
					"png/AB2/Jester AB2.png",
					"png/Tower Health.png",
					"png/Tower Damage.png",
					"png/Tower Rate.png",
					"png/Tower Range.png",
				},
				ClassIcon = "png/Jester_TinyIcon.png",
				ExtraType = "Weapon",
				WeaponType = "Monk|Huntress|Squire|Apprentice"
			},


			// ============== SUMMONER ==============
			["DunDefNewHeroes.HeroTemplateSummoner"] = new HeroTemplateData
			{
				Index = 9,
				ClassName = "Summoner",
				StatIcons = new() {
					"png/Hero Health.png",
					"png/Move Speed.png",
					"png/Hero Damage.png",
					"png/Cast Rate.png",
					"png/AB1/Summoner AB1.png",
					"png/AB2/Summoner AB2.png",
					"png/Tower Health.png",
					"png/Tower Damage.png",
					"png/Tower Rate.png",
					"png/Tower Range.png",
				},
				ClassIcon = "png/Summoner_TinyIcon.png",
				ExtraType = "Pet",
				WeaponType = "None"
			},


			// ============== EV ==============
			["DunDefNewHeroes.HeroTemplateRobotGirl"] = new HeroTemplateData
			{
				Index = 10,
				ClassName = "Series EV",
				StatIcons = new() {
					"png/Hero Health.png",
					"png/Move Speed.png",
					"png/Hero Damage.png",
					"png/Cast Rate.png",
					"png/AB1/Series EV AB1.png",
					"png/AB2/Series EV AB2.png",
					"png/Tower Health.png",
					"png/Tower Damage.png",
					"png/Tower Rate.png",
					"png/SEV_StunDuration_icon.png",
				},
				ClassIcon = "png/Robo_TinyIcon.png",
				ExtraType = "Weapon",
				WeaponType = "Apprentice|Huntress"
			},

			// ============== Barbarian ==============
			["DunDefNewHeroes.HeroTemplateBarbarian"] = new HeroTemplateData
			{
				Index = 11,
				ClassName = "Barbarian",
				StatIcons = new() {
					"png/Hero Health.png",
					"png/Move Speed.png",
					"png/Hero Damage.png",
					"png/Cast Rate.png",
					"png/AB1/Barbarian AB1.png",
					"png/AB2/Barbarian AB2.png",
					"png/Tower Health Barbarian.png",
					"png/Tower Damage Barbarian.png",
					"png/Tower Rate Barbarian.png",
					"png/Tower Range Barbarian.png",
				},
				ClassIcon = "png/barbarian_tinyIcon.png",
				ExtraType = "Weapon",
				WeaponType = "Squire"
			},

			// ============== Hermit ==============
			["Hermit.hero.HeroArchetypes.HeroTemplateHermit"] = new HeroTemplateData
			{
				Index = 12,
				ClassName = "Hermit",
				StatIcons = new() {
					"png/Hero Health.png",
					"png/Move Speed.png",
					"png/Hero Damage.png",
					"png/Cast Rate.png",
					"png/AB1/Hermit AB1.png",
					"png/AB2/Hermit AB2.png",
					"png/Tower Health.png",
					"png/Tower Damage.png",
					"png/Tower Rate.png",
					"png/Tower Range.png",
				},
				ClassIcon = "png/Hermit_Icon_Tiny.png",
				ExtraType = "Shield",
				WeaponType = "Monk"
			},


			// ============== Gunwitch ==============
			["Gunwitch.hero.HeroArchetypes.HeroTemplateGunwitch"] = new HeroTemplateData
			{
				Index = 13,
				ClassName = "Gunwitch",
				StatIcons = new() {
					"png/Hero Health.png",
					"png/Move Speed.png",
					"png/Hero Damage.png",
					"png/Cast Rate.png",
					"png/AB1/Gunwitch AB1.png",
					"png/AB2/Gunwitch AB2.png",
					"png/Tower Health Gunwitch.png",
					"png/Tower Damage Gunwitch.png",
					"png/Tower Rate Gunwitch.png",
					"png/Tower Range Gunwitch.png",
				},
				ClassIcon = "png/Gunwitch_TinyIcon.png",
				ExtraType = "None",
				WeaponType = "Huntress"
			},

			// ============== Warden ==============
			["Warden.hero.Archetype.HeroTemplateWarden"] = new HeroTemplateData
			{
				Index = 14,
				ClassName = "Warden",
				StatIcons = new() {
					"png/Hero Health.png",
					"png/Move Speed.png",
					"png/Hero Damage.png",
					"png/Cast Rate.png",
					"png/AB1/Warden AB1.png",
					"png/AB2/Warden AB2.png",
					"png/Tower Health.png",
					"png/Tower Damage.png",
					"png/Tower Rate.png",
					"png/Tower Range.png",
				},
				ClassIcon = "png/warden_Tinyicon.png",
				ExtraType = "None",
				WeaponType = "Monk"
			},
		};
	}

}