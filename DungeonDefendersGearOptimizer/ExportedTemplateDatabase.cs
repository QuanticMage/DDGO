using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
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

		// These variables are for managing the db once loaded
		// Offsets into AllData for each section
		private int IndexEntriesOffset;
		private int IntArrayElemsOffset;
		private int FloatArrayElemsOffset;
		private int StringsOffset;
		private int ULinearColorsOffset;
		private int EG_StatRandomizersOffset;
		private int EG_StatMatchingStringsOffset;
		private int DamageReductionsOffset;
		private int MeleeSwingInfosOffset;
		private int DunDefDamageType_DatasOffset;
		private int DunDefPlayer_DatasOffset;
		private int DunDefWeapon_DatasOffset;
		private int DunDefProjectile_DatasOffset;
		private int HeroEquipment_DatasOffset;
		private int HeroEquipment_Familiar_DatasOffset;

		// Counts for loaded data
		private int IndexEntriesCount;
		private int IntArrayElemsCount;
		private int FloatArrayElemsCount;
		private int StringsCount;
		private int ULinearColorsCount;
		private int EG_StatRandomizersCount;
		private int EG_StatMatchingStringsCount;
		private int DamageReductionsCount;
		private int MeleeSwingInfosCount;
		private int DunDefDamageType_DatasCount;
		private int DunDefPlayer_DatasCount;
		private int DunDefWeapon_DatasCount;
		private int DunDefProjectile_DatasCount;
		private int HeroEquipment_DatasCount;
		private int HeroEquipment_Familiar_DatasCount;

		// String data for loaded database
		private List<string> LoadedStrings = new();

		// Index lookup for loaded database
		private Dictionary<string, int> LoadedTemplateIndexMap = new Dictionary<string, int>();

		public byte[] SaveToRaw()
		{
			using (MemoryStream ms = new MemoryStream())
			using (BinaryWriter writer = new BinaryWriter(ms))
			{
				// Write FileHeader (ID = 0xe47a46e, Version = 1)
				writer.Write((uint)0xe47a46e);
				writer.Write((uint)1);

				// Write IndexEntries array, prepended by count
				writer.Write(IndexEntries.Count);
				foreach (var entry in IndexEntries)
				{
					WriteStruct(writer, entry);
				}

				// Write IntArrayElems
				writer.Write(IntArrayElems.Count);
				foreach (var elem in IntArrayElems)
				{
					writer.Write(elem);
				}

				// Write FloatArrayElems
				writer.Write(FloatArrayElems.Count);
				foreach (var elem in FloatArrayElems)
				{
					writer.Write(elem);
				}

				// Write Strings
				writer.Write(Strings.Count);
				foreach (var str in Strings)
				{
					writer.Write(str ?? "");
				}

				// Write ULinearColors
				writer.Write(ULinearColors.Count);
				foreach (var elem in ULinearColors)
				{
					WriteStruct(writer, elem);
				}

				// Write EG_StatRandomizers
				writer.Write(EG_StatRandomizers.Count);
				foreach (var elem in EG_StatRandomizers)
				{
					WriteStruct(writer, elem);
				}

				// Write EG_StatMatchingStrings
				writer.Write(EG_StatMatchingStrings.Count);
				foreach (var elem in EG_StatMatchingStrings)
				{
					WriteStruct(writer, elem);
				}

				// Write DamageReductions
				writer.Write(DamageReductions.Count);
				foreach (var elem in DamageReductions)
				{
					WriteStruct(writer, elem);
				}

				// Write MeleeSwingInfos
				writer.Write(MeleeSwingInfos.Count);
				foreach (var elem in MeleeSwingInfos)
				{
					WriteStruct(writer, elem);
				}

				// Write DunDefDamageType_Datas
				writer.Write(DunDefDamageType_Datas.Count);
				foreach (var elem in DunDefDamageType_Datas)
				{
					WriteStruct(writer, elem);
				}

				// Write DunDefPlayer_Datas
				writer.Write(DunDefPlayer_Datas.Count);
				foreach (var elem in DunDefPlayer_Datas)
				{
					WriteStruct(writer, elem);
				}

				// Write DunDefWeapon_Datas
				writer.Write(DunDefWeapon_Datas.Count);
				foreach (var elem in DunDefWeapon_Datas)
				{
					WriteStruct(writer, elem);
				}

				// Write DunDefProjectile_Datas
				writer.Write(DunDefProjectile_Datas.Count);
				foreach (var elem in DunDefProjectile_Datas)
				{
					WriteStruct(writer, elem);
				}

				// Write HeroEquipment_Datas
				writer.Write(HeroEquipment_Datas.Count);
				foreach (var elem in HeroEquipment_Datas)
				{
					WriteStruct(writer, elem);
				}

				// Write HeroEquipment_Familiar_Datas
				writer.Write(HeroEquipment_Familiar_Datas.Count);
				foreach (var elem in HeroEquipment_Familiar_Datas)
				{
					WriteStruct(writer, elem);
				}

				return ms.ToArray();
			}
		}

		private void WriteStruct<T>(BinaryWriter writer, T data) where T : struct
		{
			int size = Marshal.SizeOf(typeof(T));
			byte[] buffer = new byte[size];
			IntPtr ptr = Marshal.AllocHGlobal(size);
			try
			{
				Marshal.StructureToPtr(data, ptr, false);
				Marshal.Copy(ptr, buffer, 0, size);
				writer.Write(buffer);
			}
			finally
			{
				Marshal.FreeHGlobal(ptr);
			}
		}

		public void LoadFromRaw(byte[] bytes)
		{
			AllData = bytes;
			Span<byte> span = new Span<byte>(AllData);
			int offset = 0;

			// Read FileHeader
			FileHeader = ReadStruct<FileHeader>(span, ref offset);

			// Validate header
			if (FileHeader.ID != 0xe47a46e || FileHeader.Version != 1)
			{
				throw new InvalidDataException($"Invalid file header. ID: 0x{FileHeader.ID:X}, Version: {FileHeader.Version}");
			}

			// Read IndexEntries
			IndexEntriesCount = ReadInt(span, ref offset);
			IndexEntriesOffset = offset;
			offset += IndexEntriesCount * Marshal.SizeOf<IndexEntry>();

			// Read IntArrayElems
			IntArrayElemsCount = ReadInt(span, ref offset);
			IntArrayElemsOffset = offset;
			offset += IntArrayElemsCount * sizeof(int);

			// Read FloatArrayElems
			FloatArrayElemsCount = ReadInt(span, ref offset);
			FloatArrayElemsOffset = offset;
			offset += FloatArrayElemsCount * sizeof(float);

			// Read Strings
			StringsCount = ReadInt(span, ref offset);
			StringsOffset = offset;
			LoadedStrings.Clear();
			for (int i = 0; i < StringsCount; i++)
			{
				string str = ReadString(span, ref offset);
				LoadedStrings.Add(str);
			}

			// Read ULinearColors
			ULinearColorsCount = ReadInt(span, ref offset);
			ULinearColorsOffset = offset;
			offset += ULinearColorsCount * Marshal.SizeOf<ULinearColor_Data>();

			// Read EG_StatRandomizers
			EG_StatRandomizersCount = ReadInt(span, ref offset);
			EG_StatRandomizersOffset = offset;
			offset += EG_StatRandomizersCount * Marshal.SizeOf<EG_StatRandomizer_Data>();

			// Read EG_StatMatchingStrings
			EG_StatMatchingStringsCount = ReadInt(span, ref offset);
			EG_StatMatchingStringsOffset = offset;
			offset += EG_StatMatchingStringsCount * Marshal.SizeOf<EG_StatMatchingString_Data>();

			// Read DamageReductions
			DamageReductionsCount = ReadInt(span, ref offset);
			DamageReductionsOffset = offset;
			offset += DamageReductionsCount * Marshal.SizeOf<DamageReduction_Data>();

			// Read MeleeSwingInfos
			MeleeSwingInfosCount = ReadInt(span, ref offset);
			MeleeSwingInfosOffset = offset;
			offset += MeleeSwingInfosCount * Marshal.SizeOf<MeleeSwingInfo_Data>();

			// Read DunDefDamageType_Datas
			DunDefDamageType_DatasCount = ReadInt(span, ref offset);
			DunDefDamageType_DatasOffset = offset;
			offset += DunDefDamageType_DatasCount * Marshal.SizeOf<DunDefDamageType_Data>();

			// Read DunDefPlayer_Datas
			DunDefPlayer_DatasCount = ReadInt(span, ref offset);
			DunDefPlayer_DatasOffset = offset;
			offset += DunDefPlayer_DatasCount * Marshal.SizeOf<DunDefPlayer_Data>();

			// Read DunDefWeapon_Datas
			DunDefWeapon_DatasCount = ReadInt(span, ref offset);
			DunDefWeapon_DatasOffset = offset;
			offset += DunDefWeapon_DatasCount * Marshal.SizeOf<DunDefWeapon_Data>();

			// Read DunDefProjectile_Datas
			DunDefProjectile_DatasCount = ReadInt(span, ref offset);
			DunDefProjectile_DatasOffset = offset;
			offset += DunDefProjectile_DatasCount * Marshal.SizeOf<DunDefProjectile_Data>();

			// Read HeroEquipment_Datas
			HeroEquipment_DatasCount = ReadInt(span, ref offset);
			HeroEquipment_DatasOffset = offset;
			offset += HeroEquipment_DatasCount * Marshal.SizeOf<HeroEquipment_Data>();

			// Read HeroEquipment_Familiar_Datas
			HeroEquipment_Familiar_DatasCount = ReadInt(span, ref offset);
			HeroEquipment_Familiar_DatasOffset = offset;
			offset += HeroEquipment_Familiar_DatasCount * Marshal.SizeOf<HeroEquipment_Familiar_Data>();

			// Build index map for loaded templates
			LoadedTemplateIndexMap.Clear();
			for (int i = 0; i < IndexEntriesCount; i++)
			{
				ref readonly var entry = ref GetIndexEntry(i);
				if (entry.TemplateName >= 0 && entry.TemplateName < LoadedStrings.Count)
				{
					string templateName = LoadedStrings[entry.TemplateName];
					LoadedTemplateIndexMap[templateName] = i;
				}
			}
		}

		// Helper methods for reading from byte array
		private static T ReadStruct<T>(Span<byte> span, ref int offset) where T : struct
		{
			int size = Marshal.SizeOf<T>();
			T result = MemoryMarshal.Read<T>(span.Slice(offset, size));
			offset += size;
			return result;
		}

		private static int ReadInt(Span<byte> span, ref int offset)
		{
			int result = MemoryMarshal.Read<int>(span.Slice(offset, sizeof(int)));
			offset += sizeof(int);
			return result;
		}

		private static string ReadString(Span<byte> span, ref int offset)
		{
			// Read 7-bit encoded length
			int length = 0;
			int shift = 0;
			byte b;
			do
			{
				b = span[offset++];
				length |= (b & 0x7F) << shift;
				shift += 7;
			} while ((b & 0x80) != 0);

			if (length == 0)
				return string.Empty;

			string result = Encoding.UTF8.GetString(span.Slice(offset, length));
			offset += length;
			return result;
		}

		// ============== READONLY REF GETTERS FOR LOADED DATA ==============
		// These methods return ref readonly to avoid copying large structs

		public ref readonly IndexEntry GetIndexEntry(int index)
		{
			if (index < 0 || index >= IndexEntriesCount)
				throw new ArgumentOutOfRangeException(nameof(index));

			return ref MemoryMarshal.Cast<byte, IndexEntry>(
				new ReadOnlySpan<byte>(AllData, IndexEntriesOffset, IndexEntriesCount * Marshal.SizeOf<IndexEntry>())
			)[index];
		}

		public ref readonly ULinearColor_Data GetULinearColor(int index)
		{
			if (index < 0 || index >= ULinearColorsCount)
				throw new ArgumentOutOfRangeException(nameof(index));

			return ref MemoryMarshal.Cast<byte, ULinearColor_Data>(
				new ReadOnlySpan<byte>(AllData, ULinearColorsOffset, ULinearColorsCount * Marshal.SizeOf<ULinearColor_Data>())
			)[index];
		}

		public ref readonly EG_StatRandomizer_Data GetEG_StatRandomizer(int index)
		{
			if (index < 0 || index >= EG_StatRandomizersCount)
				throw new ArgumentOutOfRangeException(nameof(index));

			return ref MemoryMarshal.Cast<byte, EG_StatRandomizer_Data>(
				new ReadOnlySpan<byte>(AllData, EG_StatRandomizersOffset, EG_StatRandomizersCount * Marshal.SizeOf<EG_StatRandomizer_Data>())
			)[index];
		}

		public ref readonly EG_StatMatchingString_Data GetEG_StatMatchingString(int index)
		{
			if (index < 0 || index >= EG_StatMatchingStringsCount)
				throw new ArgumentOutOfRangeException(nameof(index));

			return ref MemoryMarshal.Cast<byte, EG_StatMatchingString_Data>(
				new ReadOnlySpan<byte>(AllData, EG_StatMatchingStringsOffset, EG_StatMatchingStringsCount * Marshal.SizeOf<EG_StatMatchingString_Data>())
			)[index];
		}

		public ref readonly DamageReduction_Data GetDamageReduction(int index)
		{
			if (index < 0 || index >= DamageReductionsCount)
				throw new ArgumentOutOfRangeException(nameof(index));

			return ref MemoryMarshal.Cast<byte, DamageReduction_Data>(
				new ReadOnlySpan<byte>(AllData, DamageReductionsOffset, DamageReductionsCount * Marshal.SizeOf<DamageReduction_Data>())
			)[index];
		}

		public ref readonly MeleeSwingInfo_Data GetMeleeSwingInfo(int index)
		{
			if (index < 0 || index >= MeleeSwingInfosCount)
				throw new ArgumentOutOfRangeException(nameof(index));

			return ref MemoryMarshal.Cast<byte, MeleeSwingInfo_Data>(
				new ReadOnlySpan<byte>(AllData, MeleeSwingInfosOffset, MeleeSwingInfosCount * Marshal.SizeOf<MeleeSwingInfo_Data>())
			)[index];
		}

		public ref readonly DunDefDamageType_Data GetDunDefDamageType(int index)
		{
			if (index < 0 || index >= DunDefDamageType_DatasCount)
				throw new ArgumentOutOfRangeException(nameof(index));

			return ref MemoryMarshal.Cast<byte, DunDefDamageType_Data>(
				new ReadOnlySpan<byte>(AllData, DunDefDamageType_DatasOffset, DunDefDamageType_DatasCount * Marshal.SizeOf<DunDefDamageType_Data>())
			)[index];
		}

		public ref readonly DunDefPlayer_Data GetDunDefPlayer(int index)
		{
			if (index < 0 || index >= DunDefPlayer_DatasCount)
				throw new ArgumentOutOfRangeException(nameof(index));

			return ref MemoryMarshal.Cast<byte, DunDefPlayer_Data>(
				new ReadOnlySpan<byte>(AllData, DunDefPlayer_DatasOffset, DunDefPlayer_DatasCount * Marshal.SizeOf<DunDefPlayer_Data>())
			)[index];
		}

		public ref readonly DunDefWeapon_Data GetDunDefWeapon(int index)
		{
			if (index < 0 || index >= DunDefWeapon_DatasCount)
				throw new ArgumentOutOfRangeException(nameof(index));

			return ref MemoryMarshal.Cast<byte, DunDefWeapon_Data>(
				new ReadOnlySpan<byte>(AllData, DunDefWeapon_DatasOffset, DunDefWeapon_DatasCount * Marshal.SizeOf<DunDefWeapon_Data>())
			)[index];
		}

		public ref readonly DunDefProjectile_Data GetDunDefProjectile(int index)
		{
			if (index < 0 || index >= DunDefProjectile_DatasCount)
				throw new ArgumentOutOfRangeException(nameof(index));

			return ref MemoryMarshal.Cast<byte, DunDefProjectile_Data>(
				new ReadOnlySpan<byte>(AllData, DunDefProjectile_DatasOffset, DunDefProjectile_DatasCount * Marshal.SizeOf<DunDefProjectile_Data>())
			)[index];
		}

		public ref readonly HeroEquipment_Data GetHeroEquipment(int index)
		{
			if (index < 0 || index >= HeroEquipment_DatasCount)
				throw new ArgumentOutOfRangeException(nameof(index));

			return ref MemoryMarshal.Cast<byte, HeroEquipment_Data>(
				new ReadOnlySpan<byte>(AllData, HeroEquipment_DatasOffset, HeroEquipment_DatasCount * Marshal.SizeOf<HeroEquipment_Data>())
			)[index];
		}

		public ref readonly HeroEquipment_Familiar_Data GetHeroEquipment_Familiar(int index)
		{
			if (index < 0 || index >= HeroEquipment_Familiar_DatasCount)
				throw new ArgumentOutOfRangeException(nameof(index));

			return ref MemoryMarshal.Cast<byte, HeroEquipment_Familiar_Data>(
				new ReadOnlySpan<byte>(AllData, HeroEquipment_Familiar_DatasOffset, HeroEquipment_Familiar_DatasCount * Marshal.SizeOf<HeroEquipment_Familiar_Data>())
			)[index];
		}

		// Array element accessors with bounds checking
		public int GetIntArrayElem(int index)
		{
			if (index < 0 || index >= IntArrayElemsCount)
				throw new ArgumentOutOfRangeException(nameof(index));

			return MemoryMarshal.Read<int>(new ReadOnlySpan<byte>(AllData, IntArrayElemsOffset + index * sizeof(int), sizeof(int)));
		}

		public float GetFloatArrayElem(int index)
		{
			if (index < 0 || index >= FloatArrayElemsCount)
				throw new ArgumentOutOfRangeException(nameof(index));

			return MemoryMarshal.Read<float>(new ReadOnlySpan<byte>(AllData, FloatArrayElemsOffset + index * sizeof(float), sizeof(float)));
		}

		// Get array elements as ReadOnlySpan for efficient iteration
		public ReadOnlySpan<int> GetIntArrayElemsSpan(int start, int count)
		{
			if (start < 0 || start >= IntArrayElemsCount)
				throw new ArgumentOutOfRangeException(nameof(start));
			if (count < 0 || start + count > IntArrayElemsCount)
				throw new ArgumentOutOfRangeException(nameof(count));

			return MemoryMarshal.Cast<byte, int>(
				new ReadOnlySpan<byte>(AllData, IntArrayElemsOffset + start * sizeof(int), count * sizeof(int))
			);
		}

		public ReadOnlySpan<float> GetFloatArrayElemsSpan(int start, int count)
		{
			if (start < 0 || start >= FloatArrayElemsCount)
				throw new ArgumentOutOfRangeException(nameof(start));
			if (count < 0 || start + count > FloatArrayElemsCount)
				throw new ArgumentOutOfRangeException(nameof(count));

			return MemoryMarshal.Cast<byte, float>(
				new ReadOnlySpan<byte>(AllData, FloatArrayElemsOffset + start * sizeof(float), count * sizeof(float))
			);
		}

		/// <summary>
		/// Helper method to get int array data from Array_Data
		/// </summary>
		public Span<int> GetIntArray(Array_Data arrayData)
		{
			if (arrayData.Type != VarType.Int || arrayData.Count == 0)
				return Span<int>.Empty;

			Span<byte> span = new Span<byte>(AllData);
			int offset = IntArrayElemsOffset + arrayData.Start * sizeof(int);
			int byteCount = arrayData.Count * sizeof(int);
			return MemoryMarshal.Cast<byte, int>(span.Slice(offset, byteCount));
		}

		/// <summary>
		/// Helper method to get float array data from Array_Data
		/// </summary>
		public Span<float> GetFloatArray(Array_Data arrayData)
		{
			if (arrayData.Type != VarType.Float || arrayData.Count == 0)
				return Span<float>.Empty;

			Span<byte> span = new Span<byte>(AllData);
			int offset = FloatArrayElemsOffset + arrayData.Start * sizeof(float);
			int byteCount = arrayData.Count * sizeof(float);
			return MemoryMarshal.Cast<byte, float>(span.Slice(offset, byteCount));
		}

		/// <summary>
		/// Gets the total number of index entries
		/// </summary>
		public int GetIndexEntryCount()
		{
			return IndexEntriesCount;
		}

		// String accessors
		public string GetString(int index)
		{
			if (index < 0 || index >= StringsCount)
				return string.Empty;

			return LoadedStrings[index];
		}

		// Template lookup methods
		public bool TryGetTemplateIndex(string templateName, out int index)
		{
			return LoadedTemplateIndexMap.TryGetValue(templateName, out index);
		}

		public ref readonly IndexEntry GetTemplateByName(string templateName)
		{
			if (!LoadedTemplateIndexMap.TryGetValue(templateName, out int index))
				throw new KeyNotFoundException($"Template '{templateName}' not found");

			return ref GetIndexEntry(index);
		}

		// Array helper - returns ref readonly to avoid copying Array_Data struct
		public ref readonly Array_Data GetArray(int arrayIndex, VarType type)
		{
			// This assumes Array_Data is stored somewhere - implementation depends on your data structure
			// Placeholder - adjust based on actual storage
			throw new NotImplementedException("Implement based on your Array_Data storage");
		}

		// ============== METHODS FOR BUILDING DATABASE ==============

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
						IntArrayElems.Add(int.Parse((v[0] == '(') ? v[1..^1] : v));
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
					break;
			}

			return new Array_Data(start, count, type);
		}

		public Array_Data AddFloatArray(List<float> entries)
		{
			int start = FloatArrayElems.Count;
			FloatArrayElems.AddRange(entries);
			int count = FloatArrayElems.Count - start;
			return new Array_Data(start, count, VarType.Float);
		}

		public Array_Data AddIntArray(List<int> entries)
		{
			int start = IntArrayElems.Count;
			IntArrayElems.AddRange(entries);
			int count = IntArrayElems.Count - start;
			return new Array_Data(start, count, VarType.Int);
		}

		public Array_Data AddDunDefPlayerArray(List<string> entries)
		{
			int start = IntArrayElems.Count;
			foreach (var v in entries)
				IntArrayElems.Add(GetDunDefPlayerIndex(v));
			int count = IntArrayElems.Count - start;
			return new Array_Data(start, count, VarType.DunDefPlayer);
		}

		public Array_Data AddDunDefWeaponArray(List<string> entries)
		{
			int start = IntArrayElems.Count;
			foreach (var v in entries)
				IntArrayElems.Add(GetDunDefWeaponIndex(v));
			int count = IntArrayElems.Count - start;
			return new Array_Data(start, count, VarType.DunDefWeapon);
		}

		public Array_Data AddHeroEquipmentArray(List<string> entries)
		{
			int start = IntArrayElems.Count;
			foreach (var v in entries)
				IntArrayElems.Add(GetHeroEquipmentIndex(v));
			int count = IntArrayElems.Count - start;
			return new Array_Data(start, count, VarType.HeroEquipment);
		}

		public Array_Data AddDunDefDamageTypeArray(List<string> entries)
		{
			int start = IntArrayElems.Count;
			foreach (var v in entries)
				IntArrayElems.Add(GetDunDefDamageTypeIndex(v));
			int count = IntArrayElems.Count - start;
			return new Array_Data(start, count, VarType.DunDefDamageType);
		}

		public Array_Data AddObjArray(List<string> entries, VarType type)
		{
			int start = IntArrayElems.Count;
			int count = 0;

			switch (type)
			{
				case VarType.Float:
					foreach (var v in entries)
						FloatArrayElems.Add(float.Parse(v));
					count = FloatArrayElems.Count - start;
					break;
				case VarType.Int:
					foreach (var v in entries)
						IntArrayElems.Add(int.Parse(v));
					count = IntArrayElems.Count - start;
					break;
				case VarType.DunDefPlayer:
					foreach (var v in entries)
						IntArrayElems.Add(GetDunDefPlayerIndex(v));
					count = IntArrayElems.Count - start;
					break;
				case VarType.DunDefWeapon:
					foreach (var v in entries)
						IntArrayElems.Add(GetDunDefWeaponIndex(v));
					count = IntArrayElems.Count - start;
					break;
				case VarType.HeroEquipment:
					foreach (var v in entries)
						IntArrayElems.Add(GetHeroEquipmentIndex(v));
					count = IntArrayElems.Count - start;
					break;
				case VarType.DunDefDamageType:
					foreach (var v in entries)
						IntArrayElems.Add(GetDunDefDamageTypeIndex(v));
					count = IntArrayElems.Count - start;
					break;
				case VarType.DunDefProjectile:
					foreach (var v in entries)
						IntArrayElems.Add(GetDunDefProjectileIndex(v));
					count = IntArrayElems.Count - start;
					break;
				default:
					break;
			}

			return new Array_Data(start, count, type);
		}

		public static class ArrayPropertyParser
		{
			private static readonly Regex EntryRegex = new Regex(
				@"^\s*(?<n>[A-Za-z_]\w*)\s*\[\s*(?<idx>\d+)\s*\]\s*=\s*(?<val>.*)\s*$",
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

		public int GetDunDefDamageTypeIndex(string s)
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


		public int AddString(string s)
		{
			if ((s == null) || (s == ""))
				return -1;
			if (s[0] == '"' && s[^1] == '"')
				s = s[1..^1];

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

		public int AddULinearColor(ULinearColor_Data linearColor)
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


		public int AddHeroEquipment(string path, string className, ref HeroEquipment_Data heroEquip)
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
