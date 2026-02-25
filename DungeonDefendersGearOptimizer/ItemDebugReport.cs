using System.Reflection;
using System.Text;

namespace DDUP
{
	/// <summary>
	/// Debug utility: builds a full plain-text report for an item, including all
	/// ItemViewRow properties, raw save data (DDEquipmentInfo), and every field of
	/// every linked template (HeroEquipment, Weapon, Familiar, Projectile).
	/// </summary>
	public static class ItemDebugReport
	{
		// Fields whose int value is a string index into ExportedTemplateDatabase.GetString()
		private static readonly HashSet<string> _stringIndexFields = new(StringComparer.Ordinal)
		{
			"Template", "Class",
			"EquipmentName", "Description", "AdditionalDescription",
			"BaseForgerName", "DamageDescription", "ForgedByDescription",
			"LevelString", "RequiredClassString",
			"UserEquipmentName", "UserForgerName", "EquipmentTemplate",
			"Name", "AdjectiveName", "FriendlyName",
			"AdditionalName",
			"StringHealAmount", "StringHealRange", "StringHealSpeed",
			"EquipmentDescription",
		};

		// Byte fields whose name implies a bool (treated as true/false in output)
		private static bool IsBoolByte(string name) =>
			name.StartsWith("b", StringComparison.Ordinal) ||
			name.EndsWith("Use", StringComparison.Ordinal) ||
			name.EndsWith("Check", StringComparison.Ordinal);

		// ======================== PUBLIC ENTRY POINT ========================

		public static string BuildReport(
			ItemViewRow row,
			DDEquipmentInfo? equip,
			ExportedTemplateDatabase? db)
		{
			var sb = new StringBuilder();
			sb.AppendLine("=== ITEM DEBUG REPORT ===");
			sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");

			// --- ItemViewRow ---
			Section(sb, "ItemViewRow");
			AppendItemViewRow(sb, row);

			// --- Raw save data ---
			if (equip != null)
			{
				Section(sb, "DDEquipmentInfo (Raw Save Data)");
				AppendDDEquipmentInfo(sb, equip);
			}

			// --- Template data ---
			if (equip != null && db != null && db.TryGetTemplateIndex(equip.Template, out int templateIdx))
			{
				var heroEquipCopy = db.GetHeroEquipment(templateIdx);
				ref readonly var indexEntry = ref db.GetIndexEntry(templateIdx);

				Section(sb, "HeroEquipment Template");
				F(sb, "TemplateName", db.GetString(indexEntry.TemplateName));
				F(sb, "ClassName", db.GetString(indexEntry.ClassName));
				AppendStructFields(sb, (object)heroEquipCopy, db);

				// Weapon template
				int weaponIdx = heroEquipCopy.EquipmentWeaponTemplate;
				if (weaponIdx >= 0 && weaponIdx < db.GetIndexEntryCount())
				{
					Section(sb, "DunDefWeapon Template");
					F(sb, "TemplateName", GetEntryName(db, weaponIdx));
					var weaponCopy = db.GetDunDefWeapon(weaponIdx);
					AppendStructFields(sb, (object)weaponCopy, db);

					// Primary projectile
					int projIdx = weaponCopy.ProjectileTemplate;
					if (projIdx >= 0 && projIdx < db.GetIndexEntryCount())
					{
						Section(sb, "DunDefProjectile Template (Primary)");
						F(sb, "TemplateName", GetEntryName(db, projIdx));
						AppendStructFields(sb, (object)db.GetDunDefProjectile(projIdx), db);
					}

					// Channeling projectile
					int chanIdx = weaponCopy.ChannelingProjectileTemplate;
					if (chanIdx >= 0 && chanIdx < db.GetIndexEntryCount())
					{
						Section(sb, "DunDefProjectile Template (Channeling)");
						F(sb, "TemplateName", GetEntryName(db, chanIdx));
						AppendStructFields(sb, (object)db.GetDunDefProjectile(chanIdx), db);
					}

					// Extra projectile templates (array)
					if (weaponCopy.ExtraProjectileTemplates.Count > 0)
					{
						Section(sb, "DunDefProjectile Templates (Extra)");
						var extraSpan = db.GetIntArrayElemsSpan(weaponCopy.ExtraProjectileTemplates.Start, weaponCopy.ExtraProjectileTemplates.Count);
						for (int i = 0; i < extraSpan.Length; i++)
						{
							int epIdx = extraSpan[i];
							if (epIdx >= 0 && epIdx < db.GetIndexEntryCount())
							{
								sb.AppendLine($"  --- Extra[{i}]: {GetEntryName(db, epIdx)} ---");
								AppendStructFields(sb, (object)db.GetDunDefProjectile(epIdx), db);
							}
						}
					}
				}

				// Familiar template
				int familiarIdx = heroEquipCopy.FamiliarDataIndex;
				if (familiarIdx >= 0)
				{
					Section(sb, "HeroEquipment_Familiar Data");
					var familiarCopy = db.GetHeroEquipment_Familiar(familiarIdx);
					AppendStructFields(sb, (object)familiarCopy, db);

					// Familiar primary projectile
					int fpIdx = familiarCopy.ProjectileTemplate;
					if (fpIdx >= 0 && fpIdx < db.GetIndexEntryCount())
					{
						Section(sb, "DunDefProjectile Template (Familiar Primary)");
						F(sb, "TemplateName", GetEntryName(db, fpIdx));
						AppendStructFields(sb, (object)db.GetDunDefProjectile(fpIdx), db);
					}

					// Familiar alt projectile
					int fpAltIdx = familiarCopy.ProjectileTemplateAlt;
					if (fpAltIdx >= 0 && fpAltIdx < db.GetIndexEntryCount())
					{
						Section(sb, "DunDefProjectile Template (Familiar Alt)");
						F(sb, "TemplateName", GetEntryName(db, fpAltIdx));
						AppendStructFields(sb, (object)db.GetDunDefProjectile(fpAltIdx), db);
					}
				}
			}
			else if (equip != null && db != null)
			{
				sb.AppendLine();
				sb.AppendLine($"  [No template found for: {equip.Template}]");
			}

			return sb.ToString();
		}

		// ======================== SECTION HELPERS ========================

		private static void Section(StringBuilder sb, string title)
		{
			sb.AppendLine();
			sb.AppendLine($"--- {title} ---");
		}

		private static void F(StringBuilder sb, string name, object? value, string indent = "  ")
		{
			sb.AppendLine($"{indent}{name}: {value}");
		}

		private static string GetEntryName(ExportedTemplateDatabase db, int entryIdx)
		{
			if (entryIdx < 0 || entryIdx >= db.GetIndexEntryCount()) return "(none)";
			ref readonly var entry = ref db.GetIndexEntry(entryIdx);
			return db.GetString(entry.TemplateName);
		}

		// ======================== ITEMVIEWROW ========================

		private static readonly string[] _statNames =
			{ "Resistance", "HeroHealth", "HeroSpeed", "HeroDamage", "HeroCastRate", "Ability1", "Ability2", "TowerHealth", "TowerRate", "TowerDamage", "TowerRange" };
		private static readonly string[] _resistNames =
			{ "Generic", "Poison", "Fire", "Lightning" };

		private static void AppendItemViewRow(StringBuilder sb, ItemViewRow r)
		{
			F(sb, "Idx", r.Idx);
			F(sb, "Name", r.Name);
			F(sb, "PlainName", r.PlainName);
			F(sb, "GeneratedName", r.GeneratedName);
			F(sb, "FunHashString", r.FunHashString);
			F(sb, "Location", r.Location);
			F(sb, "Quality", r.Quality);
			F(sb, "QualityRank", r.QualityRank);
			F(sb, "Type", r.Type);
			F(sb, "Set", r.Set);
			F(sb, "Level", r.Level);
			F(sb, "MaxLevel", r.MaxLevel);
			F(sb, "Rating", r.Rating);
			F(sb, "Sides", r.Sides);
			sb.AppendLine();

			for (int i = 0; i < r.Stats.Length; i++)
			{
				if (r.Stats[i] != 0 || r.UpgradedStats[i] != 0)
				{
					string label = i < _statNames.Length ? _statNames[i] : i.ToString();
					F(sb, $"Stats[{i}] {label}", $"{r.Stats[i]}  (upgraded: {r.UpgradedStats[i]})");
				}
			}
			for (int i = 0; i < r.Resists.Length; i++)
			{
				string label = i < _resistNames.Length ? _resistNames[i] : i.ToString();
				F(sb, $"Resist[{i}] {label}", $"{r.Resists[i]}  (upgraded: {r.UpgradedResists[i]})");
			}
			sb.AppendLine();

			F(sb, "WeaponDamageBonus", r.WeaponDamageBonus);
			F(sb, "WeaponShotsPerSecondBonus", r.WeaponShotsPerSecondBonus);
			F(sb, "WeaponNumberOfProjectilesBonus", r.WeaponNumberOfProjectilesBonus);
			F(sb, "WeaponChargeSpeedBonus", r.WeaponChargeSpeedBonus);
			F(sb, "WeaponSpeedOfProjectilesBonus", r.WeaponSpeedOfProjectilesBonus);
			F(sb, "WeaponSwingSpeedMultiplier", r.WeaponSwingSpeedMultiplier);
			F(sb, "WeaponAdditionalDamageAmount", r.WeaponAdditionalDamageAmount);
			F(sb, "WeaponAltDamageBonus", r.WeaponAltDamageBonus);
			F(sb, "UpgradedWeaponDamageBonus", r.UpgradedWeaponDamageBonus);
			F(sb, "UpgradedWeaponShotsPerSecondBonus", r.UpgradedWeaponShotsPerSecondBonus);
			F(sb, "UpgradedWeaponNumberOfProjectilesBonus", r.UpgradedWeaponNumberOfProjectilesBonus);
			F(sb, "UpgradedWeaponChargeSpeedBonus", r.UpgradedWeaponChargeSpeedBonus);
			F(sb, "UpgradedWeaponSpeedOfProjectilesBonus", r.UpgradedWeaponSpeedOfProjectilesBonus);
			F(sb, "UpgradedWeaponAdditionalDamageAmount", r.UpgradedWeaponAdditionalDamageAmount);
			F(sb, "UpgradedWeaponAltDamageBonus", r.UpgradedWeaponAltDamageBonus);
			F(sb, "UpgradesLeftForWeaponStats", r.UpgradesLeftForWeaponStats);
			sb.AppendLine();

			F(sb, "IsHidden", r.IsHidden);
			F(sb, "IsEvent", r.IsEvent);
			F(sb, "IsMissingResists", r.IsMissingResists);
			F(sb, "IsEquipped", r.IsEquipped);
			F(sb, "IsArmor", r.IsArmor);
			F(sb, "IsEligibleForBest", r.IsEligibleForBest);
			F(sb, "BrokenResists", r.BrokenResists);
			sb.AppendLine();

			F(sb, "Value", r.Value);
			F(sb, "BestFor", r.BestFor);
			F(sb, "SetBonus", $"{r.SetBonus:G6}");
			F(sb, "ResistanceTarget", r.ResistanceTarget);
			F(sb, "MaxStat", r.MaxStat);
			F(sb, "IndexInFolder", r.IndexInFolder);
			F(sb, "UpgradesRequiredForResists", r.UpgradesRequiredForResists);
			sb.AppendLine();

			F(sb, "Color1", r.Color1);
			F(sb, "Color2", r.Color2);
			F(sb, "HasCustomColor", r.HasCustomColor);
			F(sb, "IconX/Y", $"{r.IconX}, {r.IconY}");
			F(sb, "IconX1/Y1", $"{r.IconX1}, {r.IconY1}");
			F(sb, "IconX2/Y2", $"{r.IconX2}, {r.IconY2}");
			sb.AppendLine();

			F(sb, "DPS", $"{r.DPS:G6}");
			F(sb, "CachedDPSString", r.CachedDPSString);
			F(sb, "CurrentEquippedSlot", r.CurrentEquippedSlot);
		}

		// ======================== DDEQUIPMENTINFO ========================

		private static void AppendDDEquipmentInfo(StringBuilder sb, DDEquipmentInfo e)
		{
			F(sb, "Template", e.Template);
			F(sb, "UserEquipName", e.UserEquipName);
			F(sb, "ForgerName", e.ForgerName);
			F(sb, "GeneratedName", e.GeneratedName);
			F(sb, "Description", e.Description);
			F(sb, "Location", e.Location);
			F(sb, "Quality", e.Quality);
			F(sb, "Type", e.Type);
			F(sb, "Set", e.Set);
			F(sb, "Level / MaxLevel", $"{e.Level} / {e.MaxLevel}");
			F(sb, "StoredMana", e.StoredMana);
			sb.AppendLine();

			for (int i = 0; i < e.Stats.Length; i++)
			{
				string label = i < _statNames.Length ? _statNames[i] : i.ToString();
				F(sb, $"Stats[{i}] {label}", $"{e.Stats[i]}  (spawn: {e.SpawnStats[i]})");
			}
			F(sb, "ResistIdx", string.Join(", ", e.ResistIdx));
			F(sb, "ResistAmt (raw, -127 offset)", string.Join(", ", e.ResistAmt));
			sb.AppendLine();

			F(sb, "WeaponDamageBonus", e.WeaponDamageBonus);
			F(sb, "WeaponNumberOfProjectilesBonus (raw)", e.WeaponNumberOfProjectilesBonus);
			F(sb, "WeaponSpeedOfProjectilesBonus", e.WeaponSpeedOfProjectilesBonus);
			F(sb, "WeaponAdditionalDamageTypeIndex", e.WeaponAdditionalDamageTypeIndex);
			F(sb, "WeaponAdditionalDamageAmount", e.WeaponAdditionalDamageAmount);
			F(sb, "WeaponDrawScaleMultiplier", $"{e.WeaponDrawScaleMultiplier:G6}");
			F(sb, "WeaponSwingSpeedMultiplier", $"{e.WeaponSwingSpeedMultiplier:G6}");
			F(sb, "WeaponBlockingBonus", e.WeaponBlockingBonus);
			F(sb, "WeaponAltDamageBonus", e.WeaponAltDamageBonus);
			F(sb, "WeaponClipAmmoBonus", e.WeaponClipAmmoBonus);
			F(sb, "WeaponReloadSpeedBonus (raw)", e.WeaponReloadSpeedBonus);
			F(sb, "WeaponKnockbackBonus", e.WeaponKnockbackBonus);
			F(sb, "WeaponChargeSpeedBonus (raw)", e.WeaponChargeSpeedBonus);
			F(sb, "WeaponShotsPerSecondBonus (raw)", e.WeaponShotsPerSecondBonus);
			sb.AppendLine();

			F(sb, "SpawnQuality", $"{e.SpawnQuality:G6}");
			F(sb, "SpawnRandomizerMultiplier", $"{e.SpawnRandomizerMultiplier:G6}");
			F(sb, "NameVariantIdx", e.NameVariantIdx);
			F(sb, "NameResistIdx", e.NameResistIdx);
			F(sb, "NameQualityIdx", e.NameQualityIdx);
			F(sb, "PrimaryColorSet", e.PrimaryColorSet);
			F(sb, "SecondaryColorSet", e.SecondaryColorSet);
			sb.AppendLine();

			F(sb, "ID1 / ID2", $"{e.ID1} / {e.ID2}");
			F(sb, "MinSell / MaxSell", $"{e.MinSell} / {e.MaxSell}");
			F(sb, "Loc (X, Y, Z)", $"{e.LocX}, {e.LocY}, {e.LocZ}");
			sb.AppendLine();

			F(sb, "bUpgradable", e.bUpgradable);
			F(sb, "bRenameAtMax", e.bRenameAtMax);
			F(sb, "bNoDrop", e.bNoDrop);
			F(sb, "bNoSell", e.bNoSell);
			F(sb, "bAutoLock", e.bAutoLock);
			F(sb, "bOnceEffect", e.bOnceEffect);
			F(sb, "bLocked", e.bLocked);
			F(sb, "ManualLR", e.ManualLR);
			sb.AppendLine();

			F(sb, "Color1 (RGBA)", $"{e.Color1.R:F4}, {e.Color1.G:F4}, {e.Color1.B:F4}, {e.Color1.A:F4}");
			F(sb, "Color2 (RGBA)", $"{e.Color2.R:F4}, {e.Color2.G:F4}, {e.Color2.B:F4}, {e.Color2.A:F4}");
			F(sb, "IconColorPrimary (RGBA)", $"{e.IconColorPrimary.R:F4}, {e.IconColorPrimary.G:F4}, {e.IconColorPrimary.B:F4}, {e.IconColorPrimary.A:F4}");
			F(sb, "IconColorSecondary (RGBA)", $"{e.IconColorSecondary.R:F4}, {e.IconColorSecondary.G:F4}, {e.IconColorSecondary.B:F4}, {e.IconColorSecondary.A:F4}");
			sb.AppendLine();

			F(sb, "FolderID / IndexInFolder", $"{e.FolderID} / {e.IndexInFolder}");
			F(sb, "bIsSecondary", e.bIsSecondary);
			F(sb, "bIsArmor", e.bIsArmor);
			F(sb, "bIsEvent", e.bIsEvent);
			F(sb, "bIsMissingResists", e.bIsMissingResists);
			F(sb, "bIsEquipped", e.bIsEquipped);
			F(sb, "UserSellPrice", e.UserSellPrice);
			F(sb, "EventItemValue", e.EventItemValue);
			F(sb, "FunHashString", e.FunHashString);
			F(sb, "IconX/Y", $"{e.IconX}, {e.IconY}");
			F(sb, "IconX1/Y1", $"{e.IconX1}, {e.IconY1}");
			F(sb, "IconX2/Y2", $"{e.IconX2}, {e.IconY2}");
		}

		// ======================== GENERIC STRUCT DUMPER (via reflection) ========================

		private static void AppendStructFields(StringBuilder sb, object structObj, ExportedTemplateDatabase db)
		{
			var type = structObj.GetType();
			foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				var value = field.GetValue(structObj);
				if (value is Array_Data arr)
				{
					AppendArrayData(sb, field.Name, arr, db);
				}
				else if (value is int intVal)
				{
					if (_stringIndexFields.Contains(field.Name) && intVal >= 0)
						sb.AppendLine($"  {field.Name}: {intVal}  [\"{db.GetString(intVal)}\"]");
					else if (field.Name.EndsWith("Randomizer", StringComparison.Ordinal) && intVal >= 0)
						AppendStatRandomizerInline(sb, field.Name, intVal, db);
					else
						sb.AppendLine($"  {field.Name}: {intVal}");
				}
				else if (value is byte byteVal)
				{
					if (IsBoolByte(field.Name))
						sb.AppendLine($"  {field.Name}: {(byteVal != 0 ? "true" : "false")}");
					else
						sb.AppendLine($"  {field.Name}: {byteVal}");
				}
				else if (value is float floatVal)
				{
					sb.AppendLine($"  {field.Name}: {floatVal:G6}");
				}
				else
				{
					sb.AppendLine($"  {field.Name}: {value}");
				}
			}
		}

		private static void AppendStatRandomizerInline(StringBuilder sb, string fieldName, int idx, ExportedTemplateDatabase db)
		{
			ref readonly var r = ref db.GetEG_StatRandomizer(idx);
			sb.AppendLine($"  {fieldName}: (idx={idx})");
			sb.AppendLine($"    MaxRandomValue:                          {r.MaxRandomValue:G6}");
			sb.AppendLine($"    MaxRandomValueNegative:                  {r.MaxRandomValueNegative:G6}");
			sb.AppendLine($"    RandomPower:                             {r.RandomPower:G6}");
			sb.AppendLine($"    RandomPowerOverrideIfNegative:           {r.RandomPowerOverrideIfNegative:G6}");
			sb.AppendLine($"    RandomNegativeThreshold:                 {r.RandomNegativeThreshold:G6}");
			sb.AppendLine($"    RandomInclusionThreshold:                {r.RandomInclusionThreshold:G6}");
			sb.AppendLine($"    InclusionThresholdOverrideIfNegative:    {r.InclusionThresholdOverrideIfNegative:G6}");
			sb.AppendLine($"    NegativeThresholdQualityPercentMultiplier: {r.NegativeThresholdQualityPercentMultiplier:G6}");
			sb.AppendLine($"    MinimumPercentageValue:                  {r.MinimumPercentageValue:G6}");
			sb.AppendLine($"    NegativeMinimumPercentageValue:          {r.NegativeMinimumPercentageValue:G6}");
		}

		private static void AppendArrayData(StringBuilder sb, string fieldName, Array_Data arr, ExportedTemplateDatabase db)
		{
			if (arr.Count == 0)
			{
				sb.AppendLine($"  {fieldName}: []");
				return;
			}

			sb.AppendLine($"  {fieldName}: [{arr.Count}] (Type={arr.Type})");

			switch (arr.Type)
			{
				case VarType.Int:
				{
					var span = db.GetIntArrayElemsSpan(arr.Start, arr.Count);
					for (int i = 0; i < span.Length; i++)
						sb.AppendLine($"    [{i}]: {span[i]}");
					break;
				}
				case VarType.Float:
				{
					var span = db.GetFloatArrayElemsSpan(arr.Start, arr.Count);
					for (int i = 0; i < span.Length; i++)
						sb.AppendLine($"    [{i}]: {span[i]:G6}");
					break;
				}
				case VarType.String:
				{
					// Elements are stored as int indices into the string table
					var span = db.GetIntArrayElemsSpan(arr.Start, arr.Count);
					for (int i = 0; i < span.Length; i++)
						sb.AppendLine($"    [{i}]: \"{db.GetString(span[i])}\"");
					break;
				}
				case VarType.ULinearColor:
				{
					for (int i = 0; i < arr.Count; i++)
					{
						ref readonly var c = ref db.GetULinearColor(arr.Start + i);
						sb.AppendLine($"    [{i}]: R={c.R:G4} G={c.G:G4} B={c.B:G4} A={c.A:G4}");
					}
					break;
				}
				case VarType.EG_StatRandomizer:
				{
					for (int i = 0; i < arr.Count; i++)
					{
						ref readonly var r = ref db.GetEG_StatRandomizer(arr.Start + i);
						sb.AppendLine($"    [{i}]:");
						sb.AppendLine($"      MaxRandomValue:                          {r.MaxRandomValue:G6}");
						sb.AppendLine($"      MaxRandomValueNegative:                  {r.MaxRandomValueNegative:G6}");
						sb.AppendLine($"      RandomPower:                             {r.RandomPower:G6}");
						sb.AppendLine($"      RandomPowerOverrideIfNegative:           {r.RandomPowerOverrideIfNegative:G6}");
						sb.AppendLine($"      RandomNegativeThreshold:                 {r.RandomNegativeThreshold:G6}");
						sb.AppendLine($"      RandomInclusionThreshold:                {r.RandomInclusionThreshold:G6}");
						sb.AppendLine($"      InclusionThresholdOverrideIfNegative:    {r.InclusionThresholdOverrideIfNegative:G6}");
						sb.AppendLine($"      NegativeThresholdQualityPercentMultiplier: {r.NegativeThresholdQualityPercentMultiplier:G6}");
						sb.AppendLine($"      MinimumPercentageValue:                  {r.MinimumPercentageValue:G6}");
						sb.AppendLine($"      NegativeMinimumPercentageValue:          {r.NegativeMinimumPercentageValue:G6}");
					}
					break;
				}
				case VarType.EG_StatMatchingString:
				{
					for (int i = 0; i < arr.Count; i++)
					{
						ref readonly var s = ref db.GetEG_StatMatchingString(arr.Start + i);
						sb.AppendLine($"    [{i}]: \"{db.GetString(s.StringValue)}\"  ValThresh={s.ValueThreshold:G4} PetThresh={s.PetValueThreshold:G4} ArmorThresh={s.ArmorValueThreshold:G4}");
					}
					break;
				}
				case VarType.DamageReduction:
				{
					for (int i = 0; i < arr.Count; i++)
					{
						ref readonly var d = ref db.GetDamageReduction(arr.Start + i);
						string dtName = d.ForDamageType >= 0 ? GetEntryName(db, d.ForDamageType) : "(none)";
						sb.AppendLine($"    [{i}]: {d.PercentageReduction}%  DamageType={dtName}");
					}
					break;
				}
				case VarType.MeleeSwingInfo:
				{
					for (int i = 0; i < arr.Count; i++)
					{
						ref readonly var m = ref db.GetMeleeSwingInfo(arr.Start + i);
						sb.AppendLine($"    [{i}]: DmgMult={m.DamageMultiplier:G4} MomMult={m.MomentumMultiplier:G4} SwingDur={m.SwingAnimationDuration:G4} AnimSpd={m.AnimSpeed:G4} BeforeEnd={m.TimeBeforeEndToAllowNextCombo:G4} AfterEnd={m.TimeAfterEndToAllowNextCombo:G4}");
					}
					break;
				}
				case VarType.DunDefDamageType:
				case VarType.DunDefProjectile:
				case VarType.DunDefWeapon:
				case VarType.DunDefHero:
				case VarType.HeroEquipment:
				{
					// Elements are IndexEntry indices stored in the int array
					var span = db.GetIntArrayElemsSpan(arr.Start, arr.Count);
					for (int i = 0; i < span.Length; i++)
					{
						int idx = span[i];
						string name = (idx >= 0 && idx < db.GetIndexEntryCount()) ? GetEntryName(db, idx) : $"(invalid:{idx})";
						sb.AppendLine($"    [{i}]: {name}");
					}
					break;
				}
				default:
				{
					// Fallback: show raw ints
					var span = db.GetIntArrayElemsSpan(arr.Start, arr.Count);
					for (int i = 0; i < span.Length; i++)
						sb.AppendLine($"    [{i}]: {span[i]}");
					break;
				}
			}
		}
	}
}
