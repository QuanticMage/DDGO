using System.Reflection;

namespace DDUP
{
	public class FieldNode
	{
		public string Name = "";
		public string DisplayValue = "";

		// Expandable cross-reference (int field → IndexEntry)
		public bool IsRef;
		public int RefIndexEntry = -1;
		public VarType RefType;

		// Special case: FamiliarDataIndex is a direct span index, not an IndexEntry index
		public bool IsFamiliarRef;
		public int FamiliarDirectIndex = -1;

		// Expansion state
		public bool IsExpanded;
		public bool IsDirectPoolRef;  // index is into a direct pool, not IndexEntry
		public List<FieldNode>? Children;   // null until expanded

		// Array_Data fields
		public bool IsArray;
		public bool IsHeader;               // section-divider label, not a real field
		public bool IsArrayCrossRef;        // elements are IndexEntry indices
		public VarType ArrayElemType;
		public Array_Data ArrayData;
	}

	public record SearchResult(
		string TemplateName,
		string ClassName,
		int IndexEntryIdx,      // -1 for familiars
		int FamiliarDirectIdx,  // -1 for non-familiars
		object StructData,
		string[] ColumnValues);

	public static class DbSearchHelper
	{
		public static readonly (string Name, VarType? IndexedType, Type DataType)[] StructTypes =
		{
			("HeroEquipment",                  VarType.HeroEquipment,                          typeof(HeroEquipment_Data)),
			("DunDefWeapon",                   VarType.DunDefWeapon,                            typeof(DunDefWeapon_Data)),
			("DunDefProjectile",               VarType.DunDefProjectile,                        typeof(DunDefProjectile_Data)),
			("DunDefDamageType",               VarType.DunDefDamageType,                        typeof(DunDefDamageType_Data)),
			("DunDefHero",                     VarType.DunDefHero,                              typeof(DunDefHero_Data)),
			("DunDefPlayer",                   VarType.DunDefPlayer,                            typeof(DunDefPlayer_Data)),
			("DunDefEnemy",                    VarType.DunDefEnemy,                             typeof(DunDefEnemy_Data)),
			("SeqData_GiveEquipmentToPlayers", VarType.DunDef_SeqAct_GiveEquipmentToPlayers,   typeof(DunDef_SeqAct_GiveEquipmentToPlayers_Data)),
		};

		// (struct type, field name) → VarType of the referenced IndexEntry
		private static readonly Dictionary<(Type, string), VarType> CrossRefs = new()
		{
			{ (typeof(DunDefWeapon_Data),           "ProjectileTemplate"),           VarType.DunDefProjectile },
			{ (typeof(DunDefWeapon_Data),           "ChannelingProjectileTemplate"),  VarType.DunDefProjectile },
			{ (typeof(DunDefWeapon_Data),           "AdditionalDamageType"),          VarType.DunDefDamageType },
			{ (typeof(DunDefWeapon_Data),           "BaseMeleeDamageType"),           VarType.DunDefDamageType },
			{ (typeof(HeroEquipment_Data),          "EquipmentWeaponTemplate"),       VarType.DunDefWeapon },
			{ (typeof(HeroEquipment_Data),          "WeaponAdditionalDamageType"),    VarType.DunDefDamageType },
			{ (typeof(HeroEquipment_Data),          "EquipmentTemplate"),             VarType.HeroEquipment },
			{ (typeof(HeroEquipment_Familiar_Data), "ProjectileTemplate"),            VarType.DunDefProjectile },
			{ (typeof(HeroEquipment_Familiar_Data), "ProjectileTemplateAlt"),         VarType.DunDefProjectile },
			{ (typeof(HeroEquipment_Familiar_Data), "Projectile"),                   VarType.DunDefProjectile },
			{ (typeof(HeroEquipment_Familiar_Data), "MeleeDamageType"),              VarType.DunDefDamageType },
			{ (typeof(DunDefProjectile_Data),       "ProjDamageType"),               VarType.DunDefDamageType },
			{ (typeof(DunDefProjectile_Data),       "AdditionalDamageType"),          VarType.DunDefDamageType },
			{ (typeof(DunDefHero_Data),             "PlayerTemplate"),               VarType.DunDefPlayer },
			{ (typeof(DunDefEnemy_Data),            "EquipmentTemplate"),             VarType.HeroEquipment },
			{ (typeof(DunDefEnemy_Data),            "ElementalDamageType"),           VarType.DunDefDamageType },
			{ (typeof(DunDefEnemy_Data),            "DamageType"),                   VarType.DunDefDamageType },
			{ (typeof(DunDefEnemy_Data),            "HeroArchetype"),                VarType.DunDefHero },
			{ (typeof(DunDefEnemy_Data),            "EquipmentArchetype"),            VarType.HeroEquipment },
			{ (typeof(DamageReduction_Data),        "ForDamageType"),                 VarType.DunDefDamageType },
		{ (typeof(GiveEquipmentEntry_Data),     "HeroArchetype"),                 VarType.DunDefHero },
		{ (typeof(GiveEquipmentEntry_Data),     "EquipmentArchetype"),            VarType.HeroEquipment },
		};

		// Array_Data fields whose elements are IndexEntry indices
		private static readonly Dictionary<(Type, string), VarType> ArrayCrossRefs = new()
		{
			{ (typeof(DunDefWeapon_Data),           "ExtraProjectileTemplates"),      VarType.DunDefProjectile },
			{ (typeof(DunDefWeapon_Data),           "RandomizedProjectileTemplate"),  VarType.DunDefProjectile },
			{ (typeof(HeroEquipment_Familiar_Data), "ProjectileTemplates"),           VarType.DunDefProjectile },
		{ (typeof(GiveEquipmentEntry_Data),     "EquipmentArchetypesRandom"),     VarType.HeroEquipment },
		};

		private static readonly HashSet<string> StringIndexFields = new(StringComparer.Ordinal)
		{
			"Template", "Class", "EquipmentName", "Description", "AdditionalDescription",
			"BaseForgerName", "DamageDescription", "ForgedByDescription", "LevelString",
			"RequiredClassString", "Name", "AdjectiveName", "FriendlyName", "AdditionalName",
			"StringHealAmount", "StringHealRange", "StringHealSpeed", "EquipmentDescription",
			"AttackAnimationAlt",
			// HeroEquipment user-visible name/forger strings
			"UserEquipmentName", "UserForgerName",
			// DunDefHero strings
			"GivenCostumeString", "HeroClassDisplayName", "HeroClassDescription",
			// DunDefEnemy
			"DescriptiveName",
			// EG_StatMatchingString
			"StringValue",
			// HeroCostumeTemplate
			"CostumeName",
		};

		private static bool IsBoolByte(string name) =>
			name.StartsWith("b", StringComparison.Ordinal) ||
			name.EndsWith("Use", StringComparison.Ordinal) ||
			name.EndsWith("Check", StringComparison.Ordinal);

		private static bool IsRandomizerField(string name) =>
			name.EndsWith("Randomizer", StringComparison.Ordinal);

		internal static bool IsDirectPoolArrayType(VarType t) => t is
			VarType.EG_StatRandomizer or VarType.ULinearColor or
			VarType.EG_StatMatchingString or VarType.DamageReduction or
			VarType.MeleeSwingInfo or VarType.HeroCostumeTemplate or
			VarType.GiveEquipmentEntry;

		private static readonly Dictionary<(Type, string), VarType> DirectPoolRefs = new()
		{
			{ (typeof(HeroEquipment_Data), "IconColorAddPrimary"),  VarType.ULinearColor },
			{ (typeof(HeroEquipment_Data), "IconColorAddSecondary"), VarType.ULinearColor },
			{ (typeof(DunDefHero_Data),    "ClassNameColor"),        VarType.ULinearColor },
		};

		private static string SummarizePoolStruct(object s) => s switch
		{
			EG_StatRandomizer_Data r     => $"Max={r.MaxRandomValue:G4} Pow={r.RandomPower:G4} Thresh={r.RandomInclusionThreshold:G4}",
			ULinearColor_Data c          => $"R={c.R:G3} G={c.G:G3} B={c.B:G3} A={c.A:G3}",
			EG_StatMatchingString_Data m => $"Thresh={m.ValueThreshold:G4}",
			DamageReduction_Data d       => $"{d.PercentageReduction}%",
			MeleeSwingInfo_Data sw       => $"Dmg={sw.DamageMultiplier:G4} Mom={sw.MomentumMultiplier:G4}",
			GiveEquipmentEntry_Data g    => $"Equip={g.EquipmentArchetype} Hero={g.HeroArchetype} Q={g.BaseForceRandomizationQuality:G3}-{g.MaxRandomizationQuality:G3}",
			_ => s.ToString() ?? "",
		};

		internal static object? GetDirectPoolStruct(ExportedTemplateDatabase db, VarType type, int index) => type switch
		{
			VarType.EG_StatRandomizer     => (object)db.GetEG_StatRandomizer(index),
			VarType.ULinearColor          => (object)db.GetULinearColor(index),
			VarType.EG_StatMatchingString => (object)db.GetEG_StatMatchingString(index),
			VarType.DamageReduction       => (object)db.GetDamageReduction(index),
			VarType.MeleeSwingInfo        => (object)db.GetMeleeSwingInfo(index),
			VarType.HeroCostumeTemplate   => (object)db.GetHeroCostumeTemplate(index),
			VarType.GiveEquipmentEntry    => (object)db.GetGiveEquipmentEntry(index),
			_ => null,
		};

		// ======================== DB ACCESS ========================

		public static object? GetStructByIndexEntry(ExportedTemplateDatabase db, int indexEntryIdx)
		{
			if (indexEntryIdx < 0 || indexEntryIdx >= db.GetIndexEntryCount()) return null;
			ref readonly var entry = ref db.GetIndexEntry(indexEntryIdx);
			return entry.Type switch
			{
				VarType.HeroEquipment    => (object)db.GetHeroEquipment(indexEntryIdx),
				VarType.DunDefWeapon     => (object)db.GetDunDefWeapon(indexEntryIdx),
				VarType.DunDefProjectile => (object)db.GetDunDefProjectile(indexEntryIdx),
				VarType.DunDefDamageType => (object)db.GetDunDefDamageType(indexEntryIdx),
				VarType.DunDefHero       => (object)db.GetDunDefHero(indexEntryIdx),
				VarType.DunDefPlayer     => (object)db.GetDunDefPlayer(indexEntryIdx),
				VarType.DunDefEnemy      => (object)db.GetDunDefEnemy(indexEntryIdx),
				VarType.DunDef_SeqAct_GiveEquipmentToPlayers => (object)db.GetGiveEquipmentToPlayers(indexEntryIdx),
				_ => null,
			};
		}

		// ======================== FIELD RESOLUTION ========================

		// Resolve dot-notation path to raw value (for filter comparisons)
		public static object? ResolveToRaw(object structObj, string fieldPath, ExportedTemplateDatabase db,
			object? extraStruct = null)
		{
			string[] parts = fieldPath.Split('.', 2);
			var field = structObj.GetType().GetField(parts[0], BindingFlags.Public | BindingFlags.Instance);
			if (field == null)
				return extraStruct != null ? ResolveToRaw(extraStruct, fieldPath, db) : null;

			var value = field.GetValue(structObj);
			if (parts.Length == 1) return value;

			if (value is int idx && idx >= 0)
			{
				if (parts[0] == "FamiliarDataIndex")
					return ResolveToRaw((object)db.GetHeroEquipment_Familiar(idx), parts[1], db);

				if (CrossRefs.ContainsKey((structObj.GetType(), parts[0])))
				{
					var refStruct = GetStructByIndexEntry(db, idx);
					if (refStruct != null) return ResolveToRaw(refStruct, parts[1], db);
				}
			}
			return null;
		}

		// Resolve dot-notation path to a display string (for table cells)
		public static string ResolveToDisplay(object structObj, string fieldPath, ExportedTemplateDatabase db,
			object? extraStruct = null)
		{
			string[] parts = fieldPath.Split('.', 2);
			var type = structObj.GetType();
			var field = type.GetField(parts[0], BindingFlags.Public | BindingFlags.Instance);
			if (field == null)
				return extraStruct != null ? ResolveToDisplay(extraStruct, fieldPath, db) : "?";

			var value = field.GetValue(structObj);
			if (parts.Length == 1) return FormatForDisplay(parts[0], value, type, db);

			if (value is int idx && idx >= 0)
			{
				if (parts[0] == "FamiliarDataIndex")
					return ResolveToDisplay((object)db.GetHeroEquipment_Familiar(idx), parts[1], db);

				if (CrossRefs.ContainsKey((type, parts[0])))
				{
					var refStruct = GetStructByIndexEntry(db, idx);
					if (refStruct != null) return ResolveToDisplay(refStruct, parts[1], db);
				}
			}
			return "?";
		}

		private static string FormatForDisplay(string fieldName, object? value, Type structType, ExportedTemplateDatabase db)
		{
			if (value == null) return "";
			if (value is int iv)
			{
				if (iv < 0) return iv.ToString();
				if (StringIndexFields.Contains(fieldName))
					return $"\"{db.ExtractQuotedString(db.GetString(iv))}\"";
				if (CrossRefs.ContainsKey((structType, fieldName)) && iv < db.GetIndexEntryCount())
					return db.ExtractQuotedString(db.GetString(db.GetIndexEntry(iv).TemplateName));
				if (fieldName == "FamiliarDataIndex") return $"[familiar:{iv}]";
				if (IsRandomizerField(fieldName)) return $"[EG_StatRandomizer:{iv}]";
				if (DirectPoolRefs.TryGetValue((structType, fieldName), out var dpType)) return $"[{dpType}:{iv}]";
				return iv.ToString();
			}
			if (value is byte bv) return IsBoolByte(fieldName) ? (bv != 0 ? "true" : "false") : bv.ToString();
			if (value is float fv) return fv.ToString("G6");
			if (value is Array_Data ad)
			{
				if (ArrayCrossRefs.TryGetValue((structType, fieldName), out var elemType))
					return $"[{ad.Count} × {elemType}]";
				return $"[{ad.Count} × {ad.Type}]";
			}
			return value.ToString() ?? "";
		}

		// ======================== ENUMERATION ========================

		public static IEnumerable<(string tname, string cname, int indexEntryIdx, int familiarDirectIdx, object structData)>
			EnumerateEntries(ExportedTemplateDatabase db, int structTypeIdx)
		{
			var (_, indexedType, _) = StructTypes[structTypeIdx];
			VarType target = indexedType!.Value;
			for (int i = 0; i < db.GetIndexEntryCount(); i++)
			{
				// Copy struct to avoid ref-in-iterator restriction (C# 12)
				var entry = db.GetIndexEntry(i);
				if (entry.Type != target) continue;
				string tname = db.ExtractQuotedString(db.GetString(entry.TemplateName));
				string cname = db.ExtractQuotedString(db.GetString(entry.ClassName));
				var structData = GetStructByIndexEntry(db, i)!;
				int famIdx = structData is HeroEquipment_Data equip && equip.FamiliarDataIndex >= 0
					? equip.FamiliarDataIndex : -1;
				yield return (tname, cname, i, famIdx, structData);
			}
		}

		// ======================== FIELD NODE TREE ========================

		public static List<FieldNode> BuildNodes(object structObj, ExportedTemplateDatabase db,
			object? extraStruct = null, object? placeholderExtraStruct = null)
		{
			var nodes = BuildNodesInternal(structObj, db);
			var extra = extraStruct ?? placeholderExtraStruct;
			if (extra != null)
			{
				string label = extra.GetType().Name.Replace("_Data", "");
				nodes.Add(new FieldNode { Name = $"── {label} ──", IsHeader = true });
				nodes.AddRange(extraStruct != null
					? BuildNodesInternal(extraStruct, db)
					: BuildPlaceholderNodes(extra));
			}
			return nodes;
		}

		private static List<FieldNode> BuildPlaceholderNodes(object schema)
		{
			var nodes = new List<FieldNode>();
			foreach (var field in schema.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
				nodes.Add(new FieldNode { Name = field.Name, DisplayValue = "—" });
			return nodes;
		}

		private static List<FieldNode> BuildNodesInternal(object structObj, ExportedTemplateDatabase db)
		{
			var nodes = new List<FieldNode>();
			var type = structObj.GetType();

			foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				var value = field.GetValue(structObj);
				var node = new FieldNode { Name = field.Name };

				if (value is Array_Data ad)
				{
					node.IsArray = true;
					node.ArrayData = ad;
					if (ArrayCrossRefs.TryGetValue((type, field.Name), out var elemType))
					{
						node.IsArrayCrossRef = true;
						node.ArrayElemType = elemType;
						node.DisplayValue = $"[{ad.Count} × {elemType}]";
					}
					else
					{
						node.DisplayValue = $"[{ad.Count} × {ad.Type}]";
					}

					// Auto-expand GiveEquipmentEntry arrays so struct fields are visible immediately
					if (node.IsArray && ad.Type == VarType.GiveEquipmentEntry && ad.Count > 0)
					{
						node.IsExpanded = true;
						node.Children = LoadChildren(node, db);
						foreach (var child in node.Children)
						{
							child.IsExpanded = true;
							child.Children = LoadChildren(child, db);
						}
					}
				}
				else if (value is int iv)
				{
					if (iv >= 0 && StringIndexFields.Contains(field.Name))
					{
						node.DisplayValue = $"\"{db.ExtractQuotedString(db.GetString(iv))}\"";
					}
					else if (field.Name == "FamiliarDataIndex" && iv >= 0)
					{
						node.IsFamiliarRef = true;
						node.FamiliarDirectIndex = iv;
						node.DisplayValue = $"[familiar:{iv}]";
					}
					else if (iv >= 0 && IsRandomizerField(field.Name))
					{
						node.IsRef = true;
						node.IsDirectPoolRef = true;
						node.RefIndexEntry = iv;
						node.RefType = VarType.EG_StatRandomizer;
						node.DisplayValue = $"[EG_StatRandomizer:{iv}]";
					}
					else if (iv >= 0 && DirectPoolRefs.TryGetValue((type, field.Name), out var dpRefType))
					{
						node.IsRef = true;
						node.IsDirectPoolRef = true;
						node.RefIndexEntry = iv;
						node.RefType = dpRefType;
						node.DisplayValue = $"[{dpRefType}:{iv}]";
					}
					else if (iv >= 0 && CrossRefs.TryGetValue((type, field.Name), out var refType))
					{
						node.IsRef = true;
						node.RefIndexEntry = iv;
						node.RefType = refType;
						string refName = iv < db.GetIndexEntryCount()
							? db.ExtractQuotedString(db.GetString(db.GetIndexEntry(iv).TemplateName))
							: $"invalid:{iv}";
						node.DisplayValue = $"[→ {refName}]";
					}
					else
					{
						node.DisplayValue = iv.ToString();
					}
				}
				else if (value is byte bv)
				{
					node.DisplayValue = IsBoolByte(field.Name) ? (bv != 0 ? "true" : "false") : bv.ToString();
				}
				else if (value is float fv)
				{
					node.DisplayValue = fv.ToString("G6");
				}
				else
				{
					node.DisplayValue = value?.ToString() ?? "";
				}

				nodes.Add(node);
			}
			return nodes;
		}

		public static List<FieldNode> LoadChildren(FieldNode node, ExportedTemplateDatabase db)
		{
			if (node.IsFamiliarRef && node.FamiliarDirectIndex >= 0)
				return BuildNodes((object)db.GetHeroEquipment_Familiar(node.FamiliarDirectIndex), db);

			if (node.IsRef && node.IsDirectPoolRef && node.RefIndexEntry >= 0)
			{
				var poolStruct = GetDirectPoolStruct(db, node.RefType, node.RefIndexEntry);
				if (poolStruct != null) return BuildNodes(poolStruct, db);
			}

			if (node.IsRef && !node.IsDirectPoolRef && node.RefIndexEntry >= 0)
			{
				var refStruct = GetStructByIndexEntry(db, node.RefIndexEntry);
				if (refStruct != null) return BuildNodes(refStruct, db);
			}

			if (node.IsArray)
			{
				var children = new List<FieldNode>();
				var ad = node.ArrayData;
				if (ad.Count == 0) return children;

				if (node.IsArrayCrossRef)
				{
					var span = db.GetIntArrayElemsSpan(ad.Start, ad.Count);
					for (int i = 0; i < span.Length; i++)
					{
						int idx = span[i];
						if (idx < 0 || idx >= db.GetIndexEntryCount())
						{
							children.Add(new FieldNode { Name = $"[{i}]", DisplayValue = $"invalid:{idx}" });
							continue;
						}
						string refName = db.ExtractQuotedString(db.GetString(db.GetIndexEntry(idx).TemplateName));
						children.Add(new FieldNode
						{
							Name = $"[{i}]",
							DisplayValue = $"[→ {refName}]",
							IsRef = true,
							RefIndexEntry = idx,
							RefType = node.ArrayElemType,
						});
					}
				}
				else
				{
					switch (ad.Type)
					{
						case VarType.Float:
						{
							var span = db.GetFloatArrayElemsSpan(ad.Start, ad.Count);
							for (int i = 0; i < span.Length; i++)
								children.Add(new FieldNode { Name = $"[{i}]", DisplayValue = span[i].ToString("G6") });
							break;
						}
						case VarType.String:
						{
							var span = db.GetIntArrayElemsSpan(ad.Start, ad.Count);
							for (int i = 0; i < span.Length; i++)
								children.Add(new FieldNode { Name = $"[{i}]", DisplayValue = db.GetString(span[i]) });
							break;
						}
						case VarType.EG_StatRandomizer:
						case VarType.ULinearColor:
						case VarType.EG_StatMatchingString:
						case VarType.DamageReduction:
						case VarType.MeleeSwingInfo:
						case VarType.HeroCostumeTemplate:
						case VarType.GiveEquipmentEntry:
						{
							for (int i = 0; i < ad.Count; i++)
							{
								var elem = GetDirectPoolStruct(db, ad.Type, ad.Start + i);
								string summary = elem != null ? SummarizePoolStruct(elem) : $"[{ad.Type}:{i}]";
								children.Add(new FieldNode
								{
									Name = $"[{i}]",
									DisplayValue = summary,
									IsRef = true,
									IsDirectPoolRef = true,
									RefIndexEntry = ad.Start + i,
									RefType = ad.Type,
								});
							}
							break;
						}
						default:
						{
							var span = db.GetIntArrayElemsSpan(ad.Start, ad.Count);
							for (int i = 0; i < span.Length; i++)
								children.Add(new FieldNode { Name = $"[{i}]", DisplayValue = span[i].ToString() });
							break;
						}
					}
				}
				return children;
			}

			return new List<FieldNode>();
		}

		// Flatten tree for rendering: returns (node, indentDepth, fullPath) for every visible node
		public static List<(FieldNode node, int depth, string path)> Flatten(
			IEnumerable<FieldNode> nodes, int depth = 0, string parentPath = "")
		{
			var list = new List<(FieldNode, int, string)>();
			foreach (var node in nodes)
			{
				string path = parentPath.Length == 0 ? node.Name : $"{parentPath}.{node.Name}";
				list.Add((node, depth, path));
				if (node.IsExpanded && node.Children != null)
					list.AddRange(Flatten(node.Children, depth + 1, path));
			}
			return list;
		}

		// Build a full flat value map (path → displayValue) for diff comparison
		public static Dictionary<string, string> BuildValueMap(
			object structObj, ExportedTemplateDatabase db, int maxDepth = 3, string prefix = "",
			object? extraStruct = null, object? placeholderExtraStruct = null)
		{
			var map = new Dictionary<string, string>(StringComparer.Ordinal);
			if (maxDepth <= 0) return map;

			var type = structObj.GetType();
			foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				var value = field.GetValue(structObj);
				string path = prefix.Length == 0 ? field.Name : $"{prefix}.{field.Name}";
				map[path] = FormatForDisplay(field.Name, value, type, db);

				if (value is int iv && iv >= 0 && maxDepth > 1)
				{
					if (field.Name == "FamiliarDataIndex")
					{
						foreach (var kv in BuildValueMap((object)db.GetHeroEquipment_Familiar(iv), db, maxDepth - 1, path))
							map[kv.Key] = kv.Value;
					}
					else if (CrossRefs.ContainsKey((type, field.Name)))
					{
						var refStruct = GetStructByIndexEntry(db, iv);
						if (refStruct != null)
							foreach (var kv in BuildValueMap(refStruct, db, maxDepth - 1, path))
								map[kv.Key] = kv.Value;
					}
					else if (IsRandomizerField(field.Name))
					{
						var ps = GetDirectPoolStruct(db, VarType.EG_StatRandomizer, iv);
						if (ps != null)
							foreach (var kv in BuildValueMap(ps, db, maxDepth - 1, path))
								map[kv.Key] = kv.Value;
					}
					else if (DirectPoolRefs.TryGetValue((type, field.Name), out var dpType2))
					{
						var ps = GetDirectPoolStruct(db, dpType2, iv);
						if (ps != null)
							foreach (var kv in BuildValueMap(ps, db, maxDepth - 1, path))
								map[kv.Key] = kv.Value;
					}
				}
				else if (value is Array_Data ad && ad.Count > 0)
				{
					if (ArrayCrossRefs.ContainsKey((type, field.Name)))
					{
						var span = db.GetIntArrayElemsSpan(ad.Start, ad.Count);
						for (int i = 0; i < span.Length; i++)
						{
							int idx = span[i];
							string refName = idx >= 0 && idx < db.GetIndexEntryCount()
								? db.ExtractQuotedString(db.GetString(db.GetIndexEntry(idx).TemplateName))
								: $"invalid:{idx}";
							map[$"{path}[{i}]"] = refName;
						}
					}
					else if (ad.Type == VarType.Float)
					{
						var span = db.GetFloatArrayElemsSpan(ad.Start, ad.Count);
						for (int i = 0; i < span.Length; i++)
							map[$"{path}[{i}]"] = span[i].ToString("G6");
					}
					else if (IsDirectPoolArrayType(ad.Type))
					{
						for (int i = 0; i < ad.Count; i++)
						{
							var ps = GetDirectPoolStruct(db, ad.Type, ad.Start + i);
							if (ps != null)
								foreach (var kv in BuildValueMap(ps, db, maxDepth - 1, $"{path}[{i}]"))
									map[kv.Key] = kv.Value;
						}
					}
					else
					{
						var span = db.GetIntArrayElemsSpan(ad.Start, ad.Count);
						for (int i = 0; i < span.Length; i++)
							map[$"{path}[{i}]"] = span[i].ToString();
					}
				}
			}

			if (extraStruct != null)
				foreach (var kv in BuildValueMap(extraStruct, db, maxDepth, prefix))
					map[kv.Key] = kv.Value;

			if (placeholderExtraStruct != null)
				foreach (var field in placeholderExtraStruct.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
				{
					string path = prefix.Length == 0 ? field.Name : $"{prefix}.{field.Name}";
					map.TryAdd(path, "—");
				}

			return map;
		}
	}

	// ======================== FILTER EXPRESSION EVALUATOR ========================

	public class DbSearchFilter
	{
		private readonly ExportedTemplateDatabase _db;
		private string[] _tokens = Array.Empty<string>();
		private int _pos;
		private object? _extraStruct;

		public DbSearchFilter(ExportedTemplateDatabase db) => _db = db;

		public bool Evaluate(object structObj, string expression, object? extraStruct = null)
		{
			if (string.IsNullOrWhiteSpace(expression)) return true;
			_tokens = Tokenize(expression);
			_pos = 0;
			_extraStruct = extraStruct;
			try { return ParseOr(structObj); }
			catch { return false; }
		}

		private static string[] Tokenize(string expr)
		{
			var tokens = new List<string>();
			int i = 0;
			while (i < expr.Length)
			{
				if (char.IsWhiteSpace(expr[i])) { i++; continue; }

				if (expr[i] == '"' || expr[i] == '\'')
				{
					char q = expr[i++];
					int start = i;
					while (i < expr.Length && expr[i] != q) i++;
					tokens.Add(expr[start..i]);
					if (i < expr.Length) i++;
					continue;
				}

				if (i + 1 < expr.Length && expr.Substring(i, 2) is ">=" or "<=" or "!=" or "!:")
				{ tokens.Add(expr.Substring(i, 2)); i += 2; continue; }

				if ("()<>=!:,".Contains(expr[i]))
				{ tokens.Add(expr[i].ToString()); i++; continue; }

				int ws = i;
				while (i < expr.Length && !char.IsWhiteSpace(expr[i]) && !"()<>=!:,\"'".Contains(expr[i]))
					i++;
				tokens.Add(expr[ws..i]);
			}
			return tokens.ToArray();
		}

		private string Peek() => _pos < _tokens.Length ? _tokens[_pos] : "";
		private string Next() => _tokens[_pos++];

		private bool ParseOr(object s)
		{
			bool v = ParseAnd(s);
			while (Peek().Equals("or", StringComparison.OrdinalIgnoreCase)) { Next(); v |= ParseAnd(s); }
			return v;
		}

		private bool ParseAnd(object s)
		{
			bool v = ParseNot(s);
			while (Peek().Equals("and", StringComparison.OrdinalIgnoreCase)) { Next(); v &= ParseNot(s); }
			return v;
		}

		private bool ParseNot(object s)
		{
			if (Peek().Equals("not", StringComparison.OrdinalIgnoreCase)) { Next(); return !ParseNot(s); }
			return ParseAtom(s);
		}

		private bool ParseAtom(object s)
		{
			if (Peek() == "(") { Next(); bool r = ParseOr(s); if (Peek() == ")") Next(); return r; }
			return ParseComparison(s);
		}

		private bool ParseComparison(object s)
		{
			string field = Next();
			string op = Next();
			string rhs = Next();

			int wi = field.IndexOf("[].", System.StringComparison.Ordinal);
			if (wi >= 0)
				return MatchAnyArrayElement(s, field[..wi], field[(wi + 3)..], op, rhs);

			var raw = DbSearchHelper.ResolveToRaw(s, field, _db, _extraStruct);
			return Compare(raw, op, rhs);
		}

		private bool MatchAnyArrayElement(object s, string arrayField, string elemField, string op, string rhs)
		{
			var f = s.GetType().GetField(arrayField, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
			if (f == null)
				return _extraStruct != null && MatchAnyArrayElement(_extraStruct, arrayField, elemField, op, rhs);

			if (f.GetValue(s) is not Array_Data ad || ad.Count == 0) return false;

			for (int i = 0; i < ad.Count; i++)
			{
				object? elem = null;
				if (DbSearchHelper.IsDirectPoolArrayType(ad.Type))
					elem = DbSearchHelper.GetDirectPoolStruct(_db, ad.Type, ad.Start + i);
				else if (ad.Type != VarType.Float && ad.Type != VarType.String)
				{
					var span = _db.GetIntArrayElemsSpan(ad.Start, ad.Count);
					int idx = span[i];
					if (idx >= 0) elem = DbSearchHelper.GetStructByIndexEntry(_db, idx);
				}
				if (elem == null) continue;
				if (Compare(DbSearchHelper.ResolveToRaw(elem, elemField, _db), op, rhs)) return true;
			}
			return false;
		}

		private bool Compare(object? lhs, string op, string rhs)
		{
			double num = lhs is float f ? f : lhs is int i ? i : lhs is byte b ? b : double.NaN;
			double rhsNum = rhs.Equals("true", StringComparison.OrdinalIgnoreCase) ? 1.0
				: rhs.Equals("false", StringComparison.OrdinalIgnoreCase) ? 0.0
				: double.TryParse(rhs, out double parsed) ? parsed : double.NaN;
			if (!double.IsNaN(num) && !double.IsNaN(rhsNum))
				return op switch
				{
					">"  => num > rhsNum,
					"<"  => num < rhsNum,
					">=" => num >= rhsNum,
					"<=" => num <= rhsNum,
					"==" or "=" => num == rhsNum,
					"!=" => num != rhsNum,
					_    => false,
				};

			string lhsStr = lhs is int idx && idx >= 0
				? _db.ExtractQuotedString(_db.GetString(idx))
				: lhs?.ToString() ?? "";
			string clean = rhs.Trim('"').Trim('\'');

			return op switch
			{
				":"          => lhsStr.Contains(clean, StringComparison.OrdinalIgnoreCase),
				"!:"         => !lhsStr.Contains(clean, StringComparison.OrdinalIgnoreCase),
				"==" or "="  => lhsStr.Equals(clean, StringComparison.OrdinalIgnoreCase),
				"!="         => !lhsStr.Equals(clean, StringComparison.OrdinalIgnoreCase),
				_            => false,
			};
		}
	}
}
