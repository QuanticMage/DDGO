using System.Diagnostics;
using System.Text.RegularExpressions;
using UELib;
using UELib.Core;
using UELib.Services;
using UELib.Types;

LibServices.LogService = new SilentLogService();

UnrealConfig.VariableTypes = new Dictionary<string, Tuple<string, PropertyType>>
{
	// Common UE3 arrays - add more as you discover them
	["Components"] = Tuple.Create("Engine.Actor.Components", PropertyType.ObjectProperty),
	["Skins"] = Tuple.Create("Engine.Actor.Skins", PropertyType.ObjectProperty),
	["AnimSets"] = Tuple.Create("Engine.SkeletalMeshComponent.AnimSets", PropertyType.ObjectProperty),
	["InputLinks"] = Tuple.Create("Engine.SequenceOp.InputLinks", PropertyType.StructProperty),
	["OutputLinks"] = Tuple.Create("Engine.SequenceOp.OutputLinks", PropertyType.StructProperty),
	["VariableLinks"] = Tuple.Create("Engine.SequenceOp.VariableLinks", PropertyType.StructProperty),
	["Targets"] = Tuple.Create("Engine.SequenceAction.Targets", PropertyType.ObjectProperty),
	["Controls"] = Tuple.Create("XInterface.GUIComponent.Controls", PropertyType.ObjectProperty),
	["Expressions"] = Tuple.Create("Engine.Material.Expressions", PropertyType.ObjectProperty),
	["Modules"] = Tuple.Create("Engine.ParticleEmitter.Modules", PropertyType.ObjectProperty),
	["Emitters"] = Tuple.Create("Engine.ParticleSystem.Emitters", PropertyType.ObjectProperty),
	["InstanceParameters"] = Tuple.Create("Engine.ParticleSystemComponent.InstanceParameters", PropertyType.StructProperty),

	// Game-specific arrays
	["PrimaryColorSets"] = Tuple.Create("UDKGame.HeroEquipment.PrimaryColorSets", PropertyType.StructProperty),
	["SecondaryColorSets"] = Tuple.Create("UDKGame.HeroEquipment.SecondaryColorSets", PropertyType.StructProperty),
	["RandomBaseNames"] = Tuple.Create("UDKGame.HeroEquipment.RandomBaseNames", PropertyType.StructProperty),
	["MaxLevelRangeDifficultyArray"] = Tuple.Create("UDKGame.HeroEquipment.MaxLevelRangeDifficultyArray", PropertyType.StructProperty),
	["QualityDescriptorNames"] = Tuple.Create("UDKGame.HeroEquipment.QualityDescriptorNames", PropertyType.StructProperty),
	["LevelRequirementOverrides"] = Tuple.Create("UDKGame.HeroEquipment.LevelRequirementOverrides", PropertyType.StructProperty),

};

var keysWeWant = new List<string> {
	"EquipmentDescription",
	"EquipmentName",
	"UserEquipmentName",
	"UserForgerName",
	"BaseForgerName",
	"RandomBaseNames",
	"AllowNameRandomization",
	"EquipmentType",
	"weaponType",
	"EquipmentSetID",
	"CountsForAllArmorSets"
};

/*
var keysWeWant = new List<string> {
    "EquipmentDescription",
    "MaxElementalDamageIncreasePerLevel",
    "MinElementalDamageIncreasePerLevel",
    "ElementalDamageIncreasePerLevelMultiplier",
    "bDoShotsPerSecondBonusCap",
	"ShotsPerSecondBonusCap",
    "SizeScalerMaximumLevel",
    "MaximumLevelScaleMultiplier",
    "SizeScalerPower",
    "SizeScalerMaximumLevel",
    "PlayerSpeedMultiplier",
    "MythicalScaleDamageStatExponent",
    "ScaleDamageStatExponent",
    "RandomizerStatModifierGoNegativeMultiplier",
    "RandomizerStatModifierGoNegativeThreshold",
    "EquipmentWeaponTemplate",
    "ProjectileTemplate",
    "AbsolutePath",
    "ElementalDamageMultiplier",
    "WeaponDamageDisplayValueScale",
    "WeaponDamageMultiplier",
    "NameIndex_Base",
    "RandomBaseNames",
    "EquipmentName",
    "OnlyRandomizeBaseName",
    "AllowNameRandomization",
    "UserEquipmentName",
    //"IsArmor",
    "StatModifiers",
    "StatModifierRandomizers",
    "QualityDescriptorNames",
    "EquipmentType",
    "weaponType",
    "TotalRandomizerMultiplier",
    "bDisableTheRandomization",
    "RandomizerQualityMultiplier",
    "bSetRandomizerMultipliers",
    "WeaponDamageBonusRandomizer",
    "WeaponDamageBonusRandomizerMultiplier",
    "AdditionalWeaponDamageBonusRandomizerMultiplier",
    "WeaponAltDamageBonusRandomizer",
    "AltDamageRandomizerMult",
    "MaxRandomElementalDamageMultiplier",
    "RandomizerStatModifierGoNegativeChance",
    "RandomizerStatModifierGoNegativeMultiplier",
    "DamageReductionRandomizers",
    "bMaxEquipLevelUseAltCalc",
    "MaxLevelRangeDifficultyArray",
    "MaxEquipmentLevelRandomizer",
    "UseWeaponCoreStats",
    "WeaponDamageBonus",
    "WeaponNumberOfProjectilesBonusRandomizer",
    "WeaponNumberOfProjectilesBonus",
    "WeaponSpeedOfProjectilesBonusRandomizer",
    "WeaponSpeedOfProjectilesBonus",
    "WeaponSwingSpeedMultiplier",
    "WeaponAdditionalDamageAmountRandomizer",
    "WeaponAdditionalDamageAmount",
    "WeaponAdditionalDamageType",
    "WeaponAltDamageBonusUse",
    "WeaponAltDamageBonus",
    "WeaponBlockingBonusUse",
    "WeaponBlockingBonusRandomizer",
    "WeaponBlockingBonus",
    "WeaponClipAmmoBonusUse",
    "WeaponClipAmmoBonusRandomizer",
    "WeaponClipAmmoBonus",
    "WeaponShotsPerSecondBonusUse",
    "WeaponShotsPerSecondBonusRandomizer",
    "bUseShotsPerSecondRandomizerMult",
    "WeaponShotsPerSecondBonus",
    "WeaponReloadSpeedBonusUse",
    "WeaponReloadSpeedBonusRandomizer",
    "WeaponReloadSpeedBonus",
    "WeaponKnockbackBonusUse",
    "WeaponKnockbackBonusRandomizer",
    "WeaponKnockbackBonus",
    "WeaponChargeSpeedBonusUse",
    "WeaponChargeSpeedBonusRandomizer",
    "WeaponChargeSpeedBonus",
    "bNoNegativeRandomizations",
    "WeaponDrawScaleMultiplierRandomizer",
    "WeaponDrawScaleMultiplier",
    "WeaponDrawScaleRandomizerExtraMultiplier",
    "WeaponDrawScaleGlobalMultiplier",
    "MinWeaponScale",
    "bDontCalculateLevelRequirement",
    "bUseLevelRequirementOverrides",
    "LevelRequirementOverrides",
    "HighLevelRequirementsRatingThreshold",
    "LevelRequirementRatingOffset",
    "MinTranscendentLevel",
    "MinSupremeLevel",
    "MinUltimateLevel",
    "bDoTranscendentLevelBoost",
    "UltimateLevelBoostAmount",
    "UltimateLevelBoostRandomizerPower",
    "UltimateMaxDamageIncreasePerLevel",
    "MaxDamageIncreasePerLevel",
    "AltDamageIncreasePerLevelMultiplier",
    "AltMaxDamageIncreasePerLevel",
    "NameIndex_QualityDescriptor",
    "SupremeLevelBoostAmount",
    "SupremeLevelBoostRandomizerPower",
    "TranscendentLevelBoostAmount",
    "TranscendentLevelBoostRandomizerPower",
    "RandomizeColorSets",
    "PrimaryColorSets",
    "SecondaryColorSets",
    "MinDamageBonus",
    "MaxEquipmentLevel",
    "MinEquipmentLevels",
    "MaxNonTranscendentStatRollValue",
    "bForceRandomizerWithMinEquipmentLevel",
    "Ultimate93Chance",
    "UltimatePlusChance",
    "UltimatePlusPlusChance",
    "RuthlessUltimate93Chance",
    "RuthlessUltimatePlusChance",
    "RuthlessUltimatePlusPlusChance",
};
*/

// ===== PACKAGE CACHE FOR CROSS-PACKAGE CLASS RESOLUTION =====
// Cache loaded packages so we can resolve imported class definitions
var packageCache = new Dictionary<string, UnrealPackage>(StringComparer.OrdinalIgnoreCase);

// Helper: Load a package by name from the same directory as the main package
UnrealPackage? LoadPackageByName(string packageName, string baseDirectory)
{
	if (packageCache.ContainsKey(packageName))
	{
		return packageCache[packageName];
	}

	// Try common extensions
	string[] extensions = { ".upk", ".u" };
	foreach (var ext in extensions)
	{
		string packagePath = Path.Combine(baseDirectory, packageName + ext);
		if (File.Exists(packagePath))
		{
			try
			{
				var pkg = UnrealLoader.LoadPackage(packagePath, System.IO.FileAccess.Read);
				pkg.InitializePackage();
				packageCache[packageName] = pkg;
				return pkg;
			}
			catch (Exception ex)
			{
			}
		}
	}

	return null;
}

// Helper: Find the package name from an import's outer chain
string? FindPackageNameFromImport(UObject importObj)
{
	// Walk up the outer chain until we find a package
	var current = importObj;
	while (current != null)
	{
		if (current.ImportTable != null)
		{
			var import = current.ImportTable;
			// If the outer is 0, this is a top-level import (the package itself)
			if (import.OuterIndex == 0)
			{
				return current.Name;
			}
		}
		current = current.Outer;
	}
	return null;
}

// Helper: Resolve the class definition for an object, even if it's imported
UClass? ResolveClass(UObject obj, string baseDirectory)
{
	// If the object itself is a UStruct, return it
	if (obj is UStruct structObj)
	{
		return structObj as UClass;
	}

	if (obj.Class == null)
	{
		return null;
	}

	// If the class is in this package (export), return it directly
	if (obj.Class.ExportTable != null)
	{
		obj.Class.Load();
		return obj.Class;
	}

	// Class is imported - need to resolve it from another package
	if (obj.Class.ImportTable != null)
	{
		string className = obj.Class.Name;

		// Find the package name by walking up the outer chain
		string? packageName = FindPackageNameFromImport(obj.Class);

		if (packageName != null)
		{

			// Load the package containing the class definition
			var pkg = LoadPackageByName(packageName, baseDirectory);
			if (pkg != null)
			{
				// Find the class in the loaded package
				var resolvedClass = pkg.FindObject<UClass>(className);
				if (resolvedClass != null)
				{
					resolvedClass.Load();
					return resolvedClass;
				}
				else
				{
				}
			}
		}
		else
		{
		}
	}

	return null;
}

string dumpObject(UObject obj, string baseDirectory)
{
	if (obj == null)
	{
		return "";
	}

	obj.Load();

	// Build a map of property names to their values
	// We'll fill this from multiple sources, with priority:
	// 1. Class Default (base values)
	// 2. Archetype (template values, override defaults)
	// 3. This object (instance values, override everything)
	var propertyMap = new Dictionary<string, string>();

	// STEP 1: Get class default properties (lowest priority)
	var resolvedClass = ResolveClass(obj, baseDirectory);
	if (resolvedClass != null)
	{
		// For UStruct/UClass, there's a Default object with default property values
		UObject? classDefault = null;

		if (resolvedClass is UStruct structClass)
		{
			classDefault = structClass.Default;
		}

		if (classDefault != null && classDefault != obj)
		{
			classDefault.Load();
			if (classDefault.Properties != null)
			{
				foreach (var prop in classDefault.Properties)
				{
					try
					{
						string decompiled = prop.Decompile();
						propertyMap[prop.Name] = decompiled;
					}
					catch (Exception ex)
					{
						propertyMap[prop.Name] = $"{prop.Name}=<error: {ex.Message}>";
					}
				}
			}
		}
	}

	// STEP 2: Get archetype properties (medium priority, overrides class defaults)
	UObject? archetype = obj.Archetype;
	if (archetype != null && archetype != obj)
	{
		archetype.Load();
		if (archetype.Properties != null)
		{
			foreach (var prop in archetype.Properties)
			{
				try
				{
					string decompiled = prop.Decompile();
					propertyMap[prop.Name] = decompiled;
				}
				catch (Exception ex)
				{
					propertyMap[prop.Name] = $"{prop.Name}=<error: {ex.Message}>";
				}
			}
		}
	}

	// STEP 3: Get this object's properties (highest priority, overrides everything)
	if (obj.Properties != null)
	{
		// First, let's see all the color set properties
		/*
        var colorProps = obj.Properties.Where(p => p.Name.Contains("ColorSets")).ToList();
        if (colorProps.Any())
        {
            Console.WriteLine($"[DEBUG] Found {colorProps.Count} color set properties:");
            foreach (var cp in colorProps)
            {
                Console.WriteLine($"  {cp.Name}[{cp.ArrayIndex}]: Type={cp.Type}, Size={cp.Size}, InnerTypeName='{cp.InnerTypeName}', StructName='{cp.StructName}'");
            }
            Console.WriteLine();
        }
        */

		foreach (var prop in obj.Properties)
		{
			string decompiled = prop.Decompile();
			propertyMap[prop.Name] = decompiled;
		}
	}
	string objPath = obj.GetReferencePath();

	var objTemplateMatch = Regex.Match(objPath, @"'([^']*)'");
	var randomNameMatch= new Regex(@"""([^""]*)""");
	var equipmentTypeMatch = new Regex(@"=(.*)");
	// ------------------ WRITE OUT .cs -----------------



	string csString = "[\"" + objTemplateMatch.Groups[1].Value + "\"] = new ItemEntry { ";
	bool bDontAllowNameRandomization = false;
	bDontAllowNameRandomization = (propertyMap.ContainsKey("AllowNameRandomization") ? (propertyMap["AllowNameRandomization"] == "AllowNameRandomization=false") : false);
	bool bAllowNameRandomization = !bDontAllowNameRandomization;
	string[] randomizationArray = new string[100];
	
	string baseName = propertyMap.ContainsKey("EquipmentName") ? equipmentTypeMatch.Match(propertyMap["EquipmentName"]).Groups[1].Value.Replace("\"","") : "Missing Name";
	
	if (bAllowNameRandomization)
	{
		if (propertyMap.ContainsKey("RandomBaseNames"))
		{
			var value = propertyMap["RandomBaseNames"];
			csString += "Names = new List<string> { ";
			List<string> values = randomNameMatch.Matches(value).Select(m => m.Groups[1].Value).ToList();
			for (int i = 0; i < values.Count; i++)
			{
				csString += "\"" + values[i] + "\"" + ((i == values.Count - 1) ? "" : ", ");
			}
			csString += "}";
		}
		else
			csString += "Names = new List<string> { \"" + baseName + "\"}";
	}
	else
		csString += "Names = new List<string> { \"" + baseName + "\"}";

	string description = propertyMap.ContainsKey("EquipmentDescription") ? equipmentTypeMatch.Match(propertyMap["EquipmentDescription"]).Groups[1].Value.Replace("\"", "").Replace("\\","") : "";
	csString += ", Description = \"" + description + "\"";

	string equipmentType = propertyMap.ContainsKey("EquipmentType") ? equipmentTypeMatch.Match(propertyMap["EquipmentType"]).Groups[1].Value : "EEquipmentType.EQT_WEAPON";
	string weaponType = propertyMap.ContainsKey("weaponType") ? equipmentTypeMatch.Match(propertyMap["weaponType"]).Groups[1].Value : "";


	string equipmentSet = propertyMap.ContainsKey("EquipmentSetID") ? equipmentTypeMatch.Match(propertyMap["EquipmentSetID"]).Groups[1].Value : "0";

	if (propertyMap.ContainsKey("CountsForAllArmorSets") && (propertyMap["CountsForAllArmorSets"] == "CountsForAllArmorSets=true"))
		equipmentSet = "255";
	
	if (weaponType == "EWeaponType.EWT_WEAPON_APPRENTICE") csString += ", WeaponType = WeaponType.Apprentice";
	else if (weaponType == "EWeaponType.EWT_WEAPON_INITIATE") csString += ", WeaponType = WeaponType.Huntress";
	else if (weaponType == "EWeaponType.EWT_WEAPON_RECRUIT") csString += ", WeaponType = WeaponType.Monk";
	else if (weaponType == "EWeaponType.EWT_WEAPON_SQUIRE") csString += ", WeaponType = WeaponType.Squire";

	

	if (equipmentType == "EEquipmentType.EQT_WEAPON") csString += ", EquipmentType = EquipmentType.Weapon";
	else if (equipmentType == "EEquipmentType.EQT_ARMOR_TORSO") csString += ", EquipmentType = EquipmentType.Torso";
	else if (equipmentType == "EEquipmentType.EQT_ARMOR_PANTS") csString += ", EquipmentType = EquipmentType.Helmet";
	else if (equipmentType == "EEquipmentType.EQT_ARMOR_BOOTS") csString += ", EquipmentType = EquipmentType.Boots";
	else if (equipmentType == "EEquipmentType.EQT_ARMOR_GLOVES") csString += ", EquipmentType = EquipmentType.Gloves";
	else if (equipmentType == "EEquipmentType.EQT_FAMILIAR") csString += ", EquipmentType = EquipmentType.Familiar";
	else if (equipmentType == "EEquipmentType.EQT_ACCESSORY1") csString += ", EquipmentType = EquipmentType.Brooch";
	else if (equipmentType == "EEquipmentType.EQT_ACCESSORY2") csString += ", EquipmentType = EquipmentType.Bracers";
	else if (equipmentType == "EEquipmentType.EQT_ACCESSORY3") csString += ", EquipmentType = EquipmentType.Shield";
	else if (equipmentType == "EEquipmentType.EQT_MASK") csString += ", EquipmentType = EquipmentType.Mask";
	else
	{
		Console.WriteLine("ERROR!");
	}


	if (equipmentSet == "0") csString += ", EquipmentSet = EquipmentSet.Any";
	else if (equipmentSet == "1") csString += ", EquipmentSet = EquipmentSet.Leather";
	else if (equipmentSet == "2") csString += ", EquipmentSet = EquipmentSet.Mail";
	else if (equipmentSet == "3") csString += ", EquipmentSet = EquipmentSet.Chain";
	else if (equipmentSet == "4") csString += ", EquipmentSet = EquipmentSet.Plate";
	else if (equipmentSet == "5") csString += ", EquipmentSet = EquipmentSet.Pristine";
	else if (equipmentSet == "6") csString += ", EquipmentSet = EquipmentSet.Zamira";
	else if (equipmentSet == "255") csString += ", EquipmentSet = EquipmentSet.Any";
	else
	{
		Console.WriteLine("ERROR!");
	}

	string userForgerName = propertyMap.ContainsKey("BaseForgerName") ? equipmentTypeMatch.Match(propertyMap["BaseForgerName"]).Groups[1].Value.Replace("\"","") : "";
	if (userForgerName != "")
		csString += ", BaseForgerName = \"" + userForgerName + "\"";	
	csString += " },";
	return csString;
}


var upk = "Startup_INT.upk";
var packagePath = @"G:\SteamLibrary\steamapps\common\Dungeon Defenders\UDKGame\CookedPCConsole\";
var uncompressedPath = @"E:\Temp\";

var process = Process.Start(@"e:\ddgotools\decompress.exe", " -out=" + uncompressedPath + " \"" + packagePath + upk + "\"");
process.WaitForExit();

var package = UnrealLoader.LoadPackage(@"E:\Temp\Startup_INT.upk", System.IO.FileAccess.Read);
package.InitializePackage();

// Add the main package to the cache
packageCache[package.PackageName] = package;

List<string> lines = new();
lines.Add("namespace DDUP");
lines.Add("{");
lines.Add("\tpublic static class ItemTemplateInfo");
lines.Add("\t{");
lines.Add("\t\tpublic static readonly Dictionary<string, ItemEntry> Map = new()");
lines.Add("\t\t{");
foreach (var obj in package.Objects)
{
	if (obj.GetReferencePath().StartsWith("HeroEquipment"))
	{
		lines.Add("\t\t\t"+ dumpObject(obj, uncompressedPath));
	}
}
lines.Add("\t\t};");
lines.Add("\t}");
lines.Add("}");
File.WriteAllLines(@"E:\DDGO\GeneratedItemTable.cs", lines);

class SilentLogService : ILogService
{
	public void Log(string text) { }
	public void Log(string format, params object[] arg) { }
	public void SilentException(Exception exception) { }
	public void SilentAssert(bool assert, string message) { }
}
