using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace DDUP
{
	public class ExportedTemplateDatabase
	{
		FileHeader FileHeader;
		byte[] AllData = new byte[1];

		// These are only used when building up the database initially
		private List<int> IntArrayElems = new List<int>();
		private List<float> FloatArrayElems = new List<float>();
		private List<string> Strings = new();
		private List<ULinearColor_Data> ULinearColors = new();
		private List<EG_StatRandomizer_Data> EG_StatRandomizers = new();
		private List<EG_StatMatchingString_Data> EG_StatMatchingStrings = new();
		private List<DamageReduction_Data> DamageReductions = new();
		private List<MeleeSwingInfo_Data> MeleeSwingInfos = new();

		private List<DunDefDamageType_Data> DunDefDamageType_Datas = new();
		private List<DunDefPlayer_Data> DunDefPlayer_Datas = new();	
		private List<DunDefWeapon_Data> DunDefWeapon_Datas = new();
		private List<DunDefProjectile_Data> DunDefProjectile_Datas = new();
		private List<HeroEquipment_Data> HeroEquipment_Datas = new();
		private List<HeroEquipment_Familiar_Data> HeroEquipment_Familiar_Datas = new();

		private List<IndexEntry> IndexEntries = new();

		private Dictionary<string, int> StringList = new();
		private Dictionary<string, int> DunDefPlayer_IndexMap = new Dictionary<string, int>();
		private Dictionary<string, int> DunDefWeapon_IndexMap = new Dictionary<string, int>();
		private Dictionary<string, int> HeroEquipment_IndexMap = new Dictionary<string, int>();
		private Dictionary<string, int> DunDefProjectile_IndexMap = new Dictionary<string, int>();
		private Dictionary<string, int> DunDefDamageType_IndexMap = new Dictionary<string, int>();		

		// these variables are for managing the db, once loaded
		// TODO

		public byte[] SaveToRaw()
		{
			// write out FileHeader (ID = 0xe47a46e, Version = 1)
			// write out IndexEntries array, prepended by count
			// write out each List<type> above, prepended by count			
		}

		public void LoadFromRaw(byte[] bytes)
		{
			// assign bytes to AllData
			// we want to use Span<> to make reading things very fast			
		}
		
		// after loading, we want accessor functions available to read the structs in place (through the spans) 
		// all queries should be a specific index 
		// we also want to be able to look up the index entry from a template




		public Array_Data BuildArray(string value, VarType type)
		{
			if ((value == null) || (value == ""))
				return new Array_Data(0, 0, type);
			
			List<string> entries = ArrayPropertyParser.Parse(value);
			if (entries.Count == 0)
				return new Array_Data(0, 0, type);

			int start = IntArrayElems.Count;
			int count = entries.Count;

			switch (type)
			{
				case VarType.MeleeSwingInfo:
					start = MeleeSwingInfos.Count;
					foreach (var v in entries)
						AddMeleeSwingInfo(new MeleeSwingInfo_Data(v));
					count = MeleeSwingInfos.Count - start;
					break;				
				case VarType.Int:
					start = IntArrayElems.Count;
					foreach (var v in entries)
						IntArrayElems.Add(int.Parse((v[0] == '(') ? v[1..^1]:v));
					count = IntArrayElems.Count - start;
					break;
				case VarType.Float:
					start = FloatArrayElems.Count;
					foreach (var v in entries)
						FloatArrayElems.Add(float.Parse((v[0] == '(') ? v[1..^1] : v));
					count = FloatArrayElems.Count - start;
					break;
				case VarType.EG_StatMatchingString:
					start = EG_StatMatchingStrings.Count;
					foreach (var v in entries)
						AddEG_StatMatchingString(new EG_StatMatchingString_Data(v, this));
					count = EG_StatMatchingStrings.Count - start;
					break;
				case VarType.EG_StatRandomizer:
					start = EG_StatRandomizers.Count;
					foreach (var v in entries)
						AddEG_StatRandomizer(new EG_StatRandomizer_Data(v));
					count = EG_StatRandomizers.Count - start;
					break;
				case VarType.ULinearColor:
					start = ULinearColors.Count;
					foreach (var v in entries)
						AddULinearColor(new ULinearColor_Data(v));
					count = ULinearColors.Count - start;
					break;
				case VarType.DamageReduction:
					start = DamageReductions.Count;
					foreach (var v in entries)
						AddDamageReduction(new DamageReduction_Data(v, this));
					count = DamageReductions.Count - start;
					break;
				// object references
				case VarType.DunDefDamageType:
					start = IntArrayElems.Count;
					foreach (var v in entries)
						IntArrayElems.Add(GetDunDefProjectileIndex(v));
					count = IntArrayElems.Count - start;
					break;
				case VarType.DunDefProjectile:
					start = IntArrayElems.Count;
					foreach (var v in entries)
						IntArrayElems.Add(GetDunDefProjectileIndex(v));
					count = IntArrayElems.Count - start;
					break;
				default:
					int x = 0;
					break;
			}

			return new Array_Data(start, count, type);
		}

		public static class ArrayPropertyParser
		{
			private static readonly Regex EntryRegex = new Regex(
				@"^\s*(?<name>[A-Za-z_]\w*)\s*\[\s*(?<idx>\d+)\s*\]\s*=\s*(?<val>.*)\s*$",
				RegexOptions.Compiled);


			public static List<string> Parse(string input)
			{
				var itemsByIndex = new Dictionary<int, string>();
				int maxIndex = -1;
				string[] tokens = input.Split('\n');
				foreach (var token in tokens)
				{
					string part = token?.Trim() ?? "";
					if ((part == null) || (part == ""))
						continue;
					var m = EntryRegex.Match(part);
					if (!m.Success)
					{
						continue;
					}

					int idx = int.Parse(m.Groups["idx"].Value);
					string val = m.Groups["val"].Value;

					itemsByIndex[idx] = val;
					if (idx > maxIndex) maxIndex = idx;
				}

				var result = new List<string>(capacity: Math.Max(0, maxIndex + 1));
				for (int i = 0; i <= maxIndex; i++)
				{
					if (itemsByIndex.TryGetValue(i, out var obj))
						result.Add(obj);
					else
						result.Add("");
				}

				return result;
			}
		}

		public static class PropertyParser
		{
			public static Dictionary<string, string> Parse(string input)
			{
				if (string.IsNullOrWhiteSpace(input))
					throw new ArgumentException("Input string is null or empty.");

				// Remove parentheses
				input = input.Trim();
				if (input.StartsWith("(") && input.EndsWith(")"))
					input = input[1..^1];

				var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

				foreach (var pair in input.Split(','))
				{
					var kv = pair.Split('=', 2);

					if (kv.Length != 2)
						continue;

					dict[kv[0].Trim()] = kv[1].Trim();
				}

				return dict;
			}
		}

		public int GetDunDefDamageTypeIndex( string s)
		{
			if (DunDefDamageType_IndexMap.ContainsKey(s))
			{
				return DunDefDamageType_IndexMap[s];
			}
			else
				return -1;			
		}

		public int GetHeroEquipmentIndex(string s)
		{
			if (HeroEquipment_IndexMap.ContainsKey(s))
			{
				return HeroEquipment_IndexMap[s];
			}
			else
				return -1;
		}

		public int GetDunDefWeaponIndex(string s)
		{
			if (DunDefWeapon_IndexMap.ContainsKey(s))
			{
				return DunDefWeapon_IndexMap[s];
			}
			else
				return -1;
		}

		public int GetDunDefProjectileIndex(string s)
		{
			if (DunDefProjectile_IndexMap.ContainsKey(s))
			{
				return DunDefProjectile_IndexMap[s];
			}
			else
				return -1;
		}
		public int GetDunDefPlayerIndex(string s)
		{
			if (DunDefPlayer_IndexMap.ContainsKey(s))
			{
				return DunDefPlayer_IndexMap[s];
			}
			else
				return -1;
		}


		public int AddString( string s )
		{
			if (StringList.ContainsKey(s))
				return StringList[s];
			int idx = Strings.Count;
			StringList[s] = idx;
			Strings.Add(s);
			return idx;
		}

		// ============== NON-INDEXED TYPES (NO PATHS)
		
		// this is only the familiar part of hero equipment, so non indexed
		public int AddHeroEquipmentFamiliar(ref HeroEquipment_Familiar_Data familiarEquip)
		{
			int idx = HeroEquipment_Familiar_Datas.Count;
			HeroEquipment_Familiar_Datas.Add(familiarEquip);
			return idx;
		}

		public int AddULinearColor( ULinearColor_Data linearColor )
		{
			int idx = ULinearColors.Count;
			ULinearColors.Add(linearColor);
			return idx;
		}

		public int AddEG_StatRandomizer(EG_StatRandomizer_Data randomizerData)
		{
			int idx = EG_StatRandomizers.Count;
			EG_StatRandomizers.Add(randomizerData);
			return idx;
		}
		public int AddEG_StatMatchingString(EG_StatMatchingString_Data statStringData)
		{
			int idx = EG_StatMatchingStrings.Count;
			EG_StatMatchingStrings.Add(statStringData);
			return idx;
		}

		public int AddDamageReduction(DamageReduction_Data drData)
		{
			int idx = DamageReductions.Count;
			DamageReductions.Add(drData);
			return idx;
		}

		public int AddMeleeSwingInfo(MeleeSwingInfo_Data drData)
		{
			int idx = MeleeSwingInfos.Count;
			MeleeSwingInfos.Add(drData);
			return idx;
		}

		//============== INDEXED OBJECTS WITH PATHS
		public int AddIndexEntry(string path, string className, VarType type, int objIndex)
		{
			IndexEntry entry = new IndexEntry();
			entry.ObjIndex = objIndex;
			entry.TemplateName = AddString(path);
			entry.ClassName = AddString(className);
			entry.Type = type;

			int idx = IndexEntries.Count;
			IndexEntries.Add(entry);
			return idx;
		} 
			
		
		public int AddHeroEquipment(string path, string className, ref HeroEquipment_Data heroEquip )
		{
			if (HeroEquipment_IndexMap.ContainsKey(path))
				return IndexEntries[HeroEquipment_IndexMap[path]].ObjIndex;

			int objIdx = HeroEquipment_Datas.Count;
			int entryIdx = AddIndexEntry(path, className, VarType.HeroEquipment, objIdx);

			HeroEquipment_IndexMap.Add(path, entryIdx);			
			HeroEquipment_Datas.Add(heroEquip);

			return objIdx;
		}


		public int AddDunDefWeapon(string path, string className, ref DunDefWeapon_Data weaponData)
		{
			if (DunDefWeapon_IndexMap.ContainsKey(path))
				return IndexEntries[DunDefWeapon_IndexMap[path]].ObjIndex;

			int objIdx = DunDefWeapon_Datas.Count;
			int entryIdx = AddIndexEntry(path, className, VarType.DunDefWeapon, objIdx);

			DunDefWeapon_IndexMap.Add(path, entryIdx);
			DunDefWeapon_Datas.Add(weaponData);

			return objIdx;
		}

		public int AddDunDefProjectile(string path, string className, ref DunDefProjectile_Data projData)
		{
			if (DunDefProjectile_IndexMap.ContainsKey(path))
				return IndexEntries[DunDefProjectile_IndexMap[path]].ObjIndex;

			int objIdx = DunDefProjectile_Datas.Count;
			int entryIdx = AddIndexEntry(path, className, VarType.DunDefProjectile, objIdx);

			DunDefProjectile_IndexMap.Add(path, entryIdx);
			DunDefProjectile_Datas.Add(projData);

			return objIdx;
		}

		public int AddDunDefPlayer(string path, string className, ref DunDefPlayer_Data playerData)
		{
			if (DunDefPlayer_IndexMap.ContainsKey(path))
				return IndexEntries[DunDefPlayer_IndexMap[path]].ObjIndex;

			int objIdx = DunDefPlayer_Datas.Count;
			int entryIdx = AddIndexEntry(path, className, VarType.DunDefPlayer, objIdx);

			DunDefPlayer_IndexMap.Add(path, entryIdx);
			DunDefPlayer_Datas.Add(playerData);

			return objIdx;
		}
		public int AddDunDefDamageType(string path, string className, ref DunDefDamageType_Data damageTypeData)
		{
			if (DunDefDamageType_IndexMap.ContainsKey(path))
				return IndexEntries[DunDefDamageType_IndexMap[path]].ObjIndex;

			int objIdx = DunDefDamageType_Datas.Count;
			int entryIdx = AddIndexEntry(path, className, VarType.DunDefDamageType, objIdx);

			DunDefDamageType_IndexMap.Add(path, entryIdx);
			DunDefDamageType_Datas.Add(damageTypeData);

			return objIdx;
		}
	}
}
