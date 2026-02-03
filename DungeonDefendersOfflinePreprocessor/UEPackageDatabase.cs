using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Forms.Design;
using System.Windows.Forms.Integration;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using UELib;
using UELib.Branch.UE3.SFX.Tokens;
using UELib.Core;
using UELib.Engine;
using UELib.Services;
using UELib.Types;



namespace DungeonDefendersOfflinePreprocessor
{


	public struct IconReference
	{
		public string IconBase;
		public string IconMask1;
		public string IconMask2;
	}

	public struct IconLocation
	{
		public int IconX;
		public int IconY;
	}


	public class UEPackageDatabase
	{

		// dev request
		List<string> SkipNames = new() {
			
		};


		private Dictionary<string, UnrealPackage> PackageCache = new(StringComparer.OrdinalIgnoreCase);
		private Dictionary<string, IconReference> IconRefs = new(StringComparer.OrdinalIgnoreCase);
		private Dictionary<string, IconLocation> IconLocs = new(StringComparer.OrdinalIgnoreCase);
		private string BaseDirectory = "";
		private string TempPath = @"E:\Temp\";
		public async Task AddToDatabase( string baseDirectory, string upk )
		{
			var upkPath = System.IO.Path.Combine(baseDirectory, upk);
			var outputUpkPath = System.IO.Path.Combine(TempPath, upk);

			await MainWindow.RunDecompressAsync(TempPath, upkPath);

			BaseDirectory = baseDirectory;
			var package = UnrealLoader.LoadPackage(outputUpkPath, System.IO.FileAccess.Read);
			package.CookerPlatform = BuildPlatform.PC;
			package.InitializePackage();
			PackageCache[package.PackageName] = package;

			MainWindow.Log($"Loaded {package.PackageName}");
		}

		Dictionary<string, string> ParseArray(UDefaultProperty property)
		{
			string rawValue = property.Value.ToString();
			var result = new Dictionary<string, string>();

			// Matches the content inside the parentheses: (ParameterName="...",ParameterValue=...)
			var elementRegex = new Regex(@"\(([^)]+)\)");
			var matches = elementRegex.Matches(rawValue);

			foreach (Match match in matches)
			{
				string insideBrackets = match.Groups[1].Value;

				// Split by commas that are NOT inside nested parentheses (to skip ExpressionGUID internals)
				string[] pairs = Regex.Split(insideBrackets, @",(?![^(]*\))");

				string currentName = string.Empty;
				string currentValue = string.Empty;

				foreach (string pair in pairs)
				{
					int equalIndex = pair.IndexOf('=');
					if (equalIndex <= 0) continue;

					string key = pair.Substring(0, equalIndex).Trim();
					string val = pair.Substring(equalIndex + 1).Trim().Trim('"');

					if (key == "ParameterName")
					{
						currentName = val;
					}
					else if (key == "ParameterValue")
					{
						currentValue = val;
					}
				}

				// Only add to dictionary if we found both a name and a value
				if (!string.IsNullOrEmpty(currentName))
				{
					result[currentName] = currentValue;
				}
			}

			return result;
		}
		public UObject FindObjectByPath(UnrealPackage package, string fullPath)
		{
			// 1. Clean the path: Extract 'X.Y.Z' from T'X.Y.Z'
			string cleanPath = fullPath;
			if (cleanPath.Contains("'"))
			{
				cleanPath = cleanPath.Split('\'')[1];
			}

			// 2. Split into segments: ["X", "Y", "Z"]
			string[] parts = cleanPath.Split('.');

			// The last part is always the Object Name
			string targetName = parts[parts.Length - 1];

			// 3. Filter exports by name first (efficient)
			var potentialMatches = package.Exports.Where(e =>
				e.ObjectName.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase));

			foreach (var export in potentialMatches)
			{
				// 4. Verify the Outer chain
				if (VerifyOuterChain(export, parts))
				{
					export.Object?.Load();
					return export.Object;
				}
			}

			return null;
		}

		private bool VerifyOuterChain(UExportTableItem export, string[] pathParts)
		{
			// PathParts: ["Package", "Group", "Object"]
			// We walk backwards from the Object up to the Package
			int partIndex = pathParts.Length - 2; // Start with the immediate Outer (e.g., "Group")

			var currentOuter = export.Outer;

			while (partIndex >= 0)
			{
				string expectedOuterName = pathParts[partIndex];

				// If we ran out of Outers but still have path parts (or vice versa), it's a mismatch
				// Note: The top-level Outer in a UPK is usually null or represents the Package itself
				if (currentOuter == null)
				{
					// If the current part we're checking is the Package name (parts[0]), we're good
					return partIndex == 0 && export.Owner.PackageName.Equals(expectedOuterName, StringComparison.OrdinalIgnoreCase);
				}

				if (!currentOuter.ObjectName.Name.Equals(expectedOuterName, StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}

				currentOuter = currentOuter.Outer;
				partIndex--;
			}

			return true;
		}

		public Dictionary<string, UDefaultProperty> GetMergedProperties(UObject obj)
		{
			var mergedMap = new Dictionary<string, UDefaultProperty>();

			// 1. Base Layer: Class Defaults
			// Resolve the class and load its 'Default' object
			var objClass = obj.Class;
			if (objClass != null)
			{
				objClass.Load();
				if (objClass.Default != null)
				{
					objClass.Default.Load();
					foreach (var prop in objClass.Default.Properties)
					{
						mergedMap[prop.Name] = prop;
					}
				}
			}

			// 2. Middle Layer: Archetype
			// Overrides Class Defaults with Template data
			if (obj.Archetype != null && obj.Archetype != obj)
			{
				obj.Archetype.Load();
				if (obj.Archetype.Properties != null)
				{
					foreach (var prop in obj.Archetype.Properties)
					{
						mergedMap[prop.Name] = prop;
					}
				}
			}

			// 3. Top Layer: Instance
			// Overrides everything with the specific object's data
			if (obj.Properties != null)
			{
				foreach (var prop in obj.Properties)
				{
					mergedMap[prop.Name] = prop;
				}
			}

			return mergedMap;
		}

		string ExtractPath(string input)
		{
			// Matches everything between the first ' and the last '
			var match = Regex.Match(input, @"'([^']*)'");

			return match.Success ? match.Groups[1].Value : input;
		}

		private byte[] ExtractChannelAsGrayscale(string sourcePath, char channel)
		{
			using (var sourceBmp = new System.Drawing.Bitmap(sourcePath))
			using (var resultBmp = new System.Drawing.Bitmap(sourceBmp.Width, sourceBmp.Height, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
			{
				var rect = new System.Drawing.Rectangle(0, 0, sourceBmp.Width, sourceBmp.Height);
				var srcData = sourceBmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				var resData = resultBmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

				int bytes = Math.Abs(srcData.Stride) * sourceBmp.Height;
				byte[] rgbValues = new byte[bytes];
				byte[] outputValues = new byte[bytes];

				System.Runtime.InteropServices.Marshal.Copy(srcData.Scan0, rgbValues, 0, bytes);

				for (int i = 0; i < rgbValues.Length; i += 4)
				{
					// BGRA Format
					byte b = rgbValues[i];
					byte g = rgbValues[i + 1];
					byte r = rgbValues[i + 2];

					byte targetValue = (channel == 'R') ? r : g;

					outputValues[i] = targetValue;     // B
					outputValues[i + 1] = targetValue; // G
					outputValues[i + 2] = targetValue; // R
					outputValues[i + 3] = targetValue;       // A
				}

				System.Runtime.InteropServices.Marshal.Copy(outputValues, 0, resData.Scan0, bytes);

				sourceBmp.UnlockBits(srcData);
				resultBmp.UnlockBits(resData);

				using (var ms = new MemoryStream())
				{
					resultBmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
					return ms.ToArray();
				}
			}
		}

		private UnrealPackage? LoadPackageByName(string packageName)
		{ 
			if (PackageCache.ContainsKey(packageName))
			{
				return PackageCache[packageName];
			}

			// Try common extensions
			string ext = ".upk";
			string packagePath = Path.Combine(BaseDirectory, packageName + ext);
			if (File.Exists(packagePath))
			{
				try
				{
					var pkg = UnrealLoader.LoadPackage(packagePath, System.IO.FileAccess.Read);
					pkg.InitializePackage();
					PackageCache[packageName] = pkg;
					return pkg;
				}
				catch (Exception ex)
				{
				}
			}			

			return null;
		}

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

		UClass? ResolveClass(UObject obj)
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
					var pkg = LoadPackageByName(packageName);
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


		public UDefaultProperty? GetProperty(UObject? obj, string propertyName)
		{
			if (obj == null || string.IsNullOrWhiteSpace(propertyName))
				return null;

			// Use a single set to prevent infinite loops from circular archetypes
			var visited = new HashSet<UObject>(ReferenceEqualityComparer.Instance);

			// 1 & 2: Walk the Instance and Archetype chain
			var currentObj = obj;
			while (currentObj != null && visited.Add(currentObj))
			{
				// Ensure the object has loaded its buffer
				currentObj.Load();

				var hit = currentObj.Properties?.Find(propertyName); // UELib's List has a Find helper
				if (hit != null) return hit;

				currentObj = currentObj.Archetype as UObject;
			}

			// 3: Walk the Class Hierarchy
			// In UELib, UClass inherits from UState -> UStruct -> UField -> UObject
			var currentClass = obj.Class as UStruct;
			while (currentClass != null)
			{
				// Check the Class Default Object (CDO)
				// UELib often exposes this via UClass.DefaultObject
				if (currentClass is UClass ucl && ucl.Default != null)
				{
					if (visited.Add(ucl.Default))
					{
						ucl.Default.Load();
						var hit = ucl.Default.Properties?.Find(propertyName);
						if (hit != null) return hit;
					}
				}

				// Move up the Super chain (SuperName is resolved to SuperField in UELib)
				currentClass = currentClass.Super as UStruct;
			}

			return null;
		}
		/// <summary>
		/// Reference equality comparer for cycle detection without relying on overridden Equals/GetHashCode.
		/// </summary>
		private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
		{
			public static readonly ReferenceEqualityComparer Instance = new();

			public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

			public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
		}





		public void ExportAllHeroEquipmentToAtlas()
		{
			// 1. Storage for UNIQUE raw texture data
			// Key: Texture Name, Value: Raw pixel bytes from the original source file
			var uniqueIcons = new Dictionary<string, byte[]>();
			int iconSize = 64;

			foreach (var item in PackageCache.Values)
			{
				foreach (var exp in item.Exports)
				{
					if (exp?.Object == null) continue;
					var obj = exp.Object;

					if (!obj.GetReferencePath().StartsWith("HeroEquipment"))
						continue;

					IconReference ir = new(); // keep track 					
					string backupTexturePath = "";
					try
					{						
						obj.Load();
						var iconProperty = GetProperty(obj, "EquipmentIconMat");
						if (iconProperty == null) continue;
					
						var matInst = FindObjectByPath(item, iconProperty.Value);
						matInst.Load();

						var textureArrayProperty = GetProperty(matInst, "TextureParameterValues");
						if (textureArrayProperty == null)
						{
							var expressionsProperty = GetProperty(matInst, "Expressions");
							if (expressionsProperty != null)
							{
								string pattern = @"(?<=Texture=Texture2D').*?(?=')";
								Match match = Regex.Match(expressionsProperty.Value, pattern);
								if (match.Success)
								{
									backupTexturePath = match.Groups[0].Value;
									// if this is the case, we need to load this backupTexture explicitly.  Look later.
									break;
								}
							}
						}													

						// Logic for handling the Backup Texture found via Regex
						if (!string.IsNullOrEmpty(backupTexturePath))
						{
							// Extract name from path (e.g., "Folder/Sub/T_Icon" -> "T_Icon")
							string texName = Path.GetFileNameWithoutExtension(backupTexturePath);
							if (!uniqueIcons.ContainsKey(texName))
							{
								string localFilePath = Path.Combine(@"E:\DDGOTools\Texture2D\", $"{texName}.png");
								if (File.Exists(localFilePath))
								{
									ir.IconBase = texName;
									uniqueIcons.Add(texName, File.ReadAllBytes(localFilePath));
									MainWindow.Log($"Queued backup icon (Regex): {texName}");
								}
							}					
						}

						if (textureArrayProperty == null) continue;

						var parsedElements = ParseArray(textureArrayProperty);
						string[] targetKeys = { "EquipmentIcon", "EquipmentIconColorLayers" };

						foreach (var key in targetKeys)
						{
							if (!parsedElements.ContainsKey(key)) continue;

							string path = ExtractPath(parsedElements[key]);
							var iconImage = FindObjectByPath(item, path) as UTexture2D;
							if (iconImage == null) continue;

							string localFilePath = Path.Combine(@"E:\DDGOTools\Texture2D\", $"{iconImage.Name}.png");
							if (!File.Exists(localFilePath)) continue;

							if (key == "EquipmentIconColorLayers")
							{
								// Special Handling: Split Red and Green channels into two distinct atlas entries
								string rKey = $"{iconImage.Name}_R";
								string gKey = $"{iconImage.Name}_G";

								ir.IconMask1 = rKey;
								ir.IconMask2 = gKey;
								if (!uniqueIcons.ContainsKey(rKey))
									uniqueIcons.Add(rKey, ExtractChannelAsGrayscale(localFilePath, 'R'));

								if (!uniqueIcons.ContainsKey(gKey))
									uniqueIcons.Add(gKey, ExtractChannelAsGrayscale(localFilePath, 'G'));
							}
							else 
							{
								ir.IconBase = iconImage.Name;
								// Standard icon handling
								if (!uniqueIcons.ContainsKey(iconImage.Name))
								{
									uniqueIcons.Add(iconImage.Name, File.ReadAllBytes(localFilePath));								
								}
							}
						}
					}
					catch (Exception ex)
					{
						MainWindow.Log($"Error processing {obj.Name}: {ex.Message}");
					}
					IconRefs[obj.GetReferencePath()] = ir;					
				}
			}

			if (uniqueIcons.Count == 0) return;

			// 2. Prepare the Atlas
			int gridCount = (int)Math.Ceiling(Math.Sqrt(uniqueIcons.Count));
			int atlasWidth = gridCount * iconSize;
			int atlasHeight = gridCount * iconSize;
			byte[] atlasBuffer = new byte[atlasWidth * atlasHeight * 4];

			// 3. Process and Stitch
			int index = 1;
			foreach (var kvp in uniqueIcons)
			{
				IconLocation il = new IconLocation();
				
				int row = index / gridCount;
				int col = index % gridCount;
				MainWindow.Log($"Queued unique icon: {kvp.Key} at row {row} and column {col}");

				il.IconX = col * iconSize;
				il.IconY = row * iconSize;

				// Use a MemoryStream to load the bytes we stored in the dictionary
				using (var ms = new MemoryStream(kvp.Value))
				using (var sourceBmp = new System.Drawing.Bitmap(ms))
				using (var resizedBmp = new System.Drawing.Bitmap(iconSize, iconSize))
				{
					// Resize logic
					using (var g = System.Drawing.Graphics.FromImage(resizedBmp))
					{
						g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
						g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
						g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
						g.DrawImage(sourceBmp, 0, 0, iconSize, iconSize);
					}

					// Extract Bits
					var rect = new System.Drawing.Rectangle(0, 0, iconSize, iconSize);
					var bmpData = resizedBmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

					// Stitch directly into the Atlas Buffer
					for (int y = 0; y < iconSize; y++)
					{
						IntPtr sourceRowPtr = bmpData.Scan0 + (y * bmpData.Stride);
						int destOffset = ((row * iconSize + y) * atlasWidth + (col * iconSize)) * 4;
						System.Runtime.InteropServices.Marshal.Copy(sourceRowPtr, atlasBuffer, destOffset, iconSize * 4);
					}

					resizedBmp.UnlockBits(bmpData);
				}

				IconLocs[kvp.Key] = il;
				index++;
			}

			// 4. Final Save
			DxtConverter.SaveToPng(atlasBuffer, atlasWidth, atlasHeight, atlasWidth, atlasHeight, @"E:\temp\HeroEquipment_Atlas.png");
			MainWindow.Log($"Atlas complete! Saved {uniqueIcons.Count} unique icons to {atlasWidth}x{atlasHeight} texture.");
		}

		public List<ULinearColor>? GetColorSetProperty(UObject? obj, string propertyName)
		{
			if (obj == null || string.IsNullOrWhiteSpace(propertyName))
				return null;

			// Use a single set to prevent infinite loops from circular archetypes
			var visited = new HashSet<UObject>(ReferenceEqualityComparer.Instance);

			// 1 & 2: Walk the Instance and Archetype chain
			var currentObj = obj;
			while (currentObj != null && visited.Add(currentObj))
			{
				// Ensure the object has loaded its buffer
				currentObj.Load();
				if (currentObj.ExportTable == null) break;
				
				var hit = ReadRawColorSetProperty(currentObj.Package, currentObj.ExportTable, propertyName);
				if (hit != null) return hit;

				currentObj = currentObj.Archetype as UObject;
			}

			// 3: Walk the Class Hierarchy
			// In UELib, UClass inherits from UState -> UStruct -> UField -> UObject
			var currentClass = obj.Class as UStruct;
			while (currentClass != null)
			{
				// Check the Class Default Object (CDO)
				// UELib often exposes this via UClass.DefaultObject
				if (currentClass is UClass ucl && ucl.Default != null)
				{
					if (visited.Add(ucl.Default))
					{
						ucl.Default.Load();
						var hit = ReadRawColorSetProperty(ucl.Default.Package, ucl.Default.ExportTable, propertyName);						
						if (hit != null) return hit;
					}
				}

				// Move up the Super chain (SuperName is resolved to SuperField in UELib)
				currentClass = currentClass.Super as UStruct;
			}

			return null;
		}

		// PSEUDOCODE: adjust type names to your UELib version.
		public List<ULinearColor>? ReadRawColorSetProperty(UnrealPackage upk, UExportTableItem export, string propName)
		{
			var stream = upk.Stream; // or upk.GetStream()
			long currentOffset = stream.Position;
			stream.Seek(export.SerialOffset, SeekOrigin.Begin);

			int netIndex = stream.ReadInt32();
			while (true)							// properties
			{
				ulong nameIndex = stream.ReadUInt64();

				string name = upk.Names[(int)nameIndex];
				if (name == "None")
					break;				
				ulong typeIndex = stream.ReadUInt64();
				string type = upk.Names[(int)typeIndex];
				uint size = stream.ReadUInt32();
				uint array = stream.ReadUInt32();

				string StructType = "";
				string EnumType = "";
				if (type == "StructProperty")
				{
					ulong structNameIndex = stream.ReadUInt64();
					StructType = upk.Names[(int)structNameIndex];
				}

				if (type == "ByteProperty")
				{
					ulong enumNameIndex = stream.ReadUInt64();
					EnumType = upk.Names[(int)enumNameIndex];
				}


				if (type == "BoolProperty")
				{
					stream.ReadByte();
					continue;
				}

				long endOfSection = stream.Position;

				if ((type == "ArrayProperty") && (propName == name))
				{
					int ct = stream.ReadInt32();
					List<ULinearColor> rtn = new();

					for (int i = 0; i < ct; i++)
					{
						ULinearColor color = new();
						color.Deserialize(stream);						
						rtn.Add(color);

					}
					return rtn;
				}
				
				stream.Seek(endOfSection + size, SeekOrigin.Begin);

			}
			stream.Seek(currentOffset, SeekOrigin.Begin);
			return null;
		}



		public string GetObjectCSLine(UObject? obj)
		{
			if (obj == null) return "";

			string objPath = obj.GetReferencePath();
			
			var objTemplateMatch = Regex.Match(objPath, @"'([^']*)'");
			var randomNameMatch = new Regex(@"""([^""]*)""");
			var equipmentTypeMatch = new Regex(@"=(.*)");
			string csString = "[\"" + objTemplateMatch.Groups[1].Value + "\"] = new ItemEntry { ";
			bool bDontAllowNameRandomization = false;
			bDontAllowNameRandomization = GetProperty(obj, "AllowNameRandomization") != null ? (GetProperty(obj, "AllowNameRandomization").Value == "false") : false;
			bool bAllowNameRandomization = !bDontAllowNameRandomization;
			string[] randomizationArray = new string[100];


			string baseName = GetProperty(obj, "EquipmentName")?.Value?.Replace("\"", "");
			if (SkipNames.Contains(objPath))
				return "";
			if (bAllowNameRandomization)
			{
				if (GetProperty(obj, "RandomBaseNames") != null)
				{
					var value = GetProperty(obj, "RandomBaseNames");
					csString += "Names = new List<string> { ";
					List<string> values = randomNameMatch.Matches(value.Value).Select(m => m.Groups[1].Value).ToList();
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

			string description = GetProperty(obj, "EquipmentDescription")?.Value?.Replace("\"", "")?.Replace("\\", "") ?? ""; ;
			csString += ", Description = \"" + description + "\"";

			string equipmentType = GetProperty(obj, "EquipmentType")?.Value?.Replace("\"", "")?.Replace("\\", "") ?? "EEquipmentType.EQT_WEAPON";
			string weaponType = GetProperty(obj, "weaponType")?.Value ?? "";
			string equipmentSet = GetProperty(obj, "EquipmentSetID")?.Value ?? "0";

			if ((GetProperty(obj, "CountsForAllArmorSets") != null) && (GetProperty(obj, "CountsForAllArmorSets").Value == "CountsForAllArmorSets=true"))
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

			string userForgerName = GetProperty(obj, "BaseForgerName") != null ? equipmentTypeMatch.Match(GetProperty(obj, "BaseForgerName").Value).Groups[1].Value.Replace("\"", "") : "";
			if (userForgerName != "")
				csString += ", BaseForgerName = \"" + userForgerName + "\"";
			int x = 0;
			int y = 0;
			int x1 = 0;
			int y1 = 0;
			int x2 = 0;
			int y2 = 0;

			// icon!
			if (IconRefs.ContainsKey(objPath))
			{
				string color = IconRefs[objPath].IconBase;
				string color1 = IconRefs[objPath].IconMask1;
				string color2 = IconRefs[objPath].IconMask2;

				if (color != null && IconLocs.ContainsKey(color))
				{
					IconLocation loc = IconLocs[color];
					x = loc.IconX;
					y = loc.IconY;
				}

				if (color1 != null && IconLocs.ContainsKey(color1))
				{
					IconLocation loc = IconLocs[color1];
					x1 = loc.IconX;
					y1 = loc.IconY;
				}

				if (color2 != null && IconLocs.ContainsKey(color2))
				{
					IconLocation loc = IconLocs[color2];
					x2 = loc.IconX;
					y2 = loc.IconY;
				}
			}

			var iconColorAddPrimary = GetProperty(obj, "IconColorAddPrimary");
			var iconColorAddSecondary = GetProperty(obj, "IconColorAddSecondary");
			var iconColorMulPrimary = GetProperty(obj, "IconColorMulPrimary");
			var iconColorMulSecondary = GetProperty(obj, "IconColorMulSecondary");
			var useColorSets = GetProperty(obj, "UseColorSets");

			csString += $", IconX = {x}, IconY = {y}, IconX1 = {x1}, IconY1 = {y1}, IconX2 = {x2}, IconY2 = {y2}";

			if (iconColorAddPrimary != null)
			{
				string dblColor = iconColorAddPrimary.Value.Replace("(", "{").Replace(")", "}");
				string fltColor = Regex.Replace(dblColor, @"(?<![A-Za-z0-9_])(\d+\.\d+)(?![A-Za-z0-9_])", "$1f");
				csString += $", IconColorAddPrimary = new DDLinearColor() {fltColor:F2}";
			}

			if (iconColorAddSecondary != null)
			{
				string dblColor = iconColorAddSecondary.Value.Replace("(", "{").Replace(")", "}");
				string fltColor = Regex.Replace(dblColor, @"(?<![A-Za-z0-9_])(\d+\.\d+)(?![A-Za-z0-9_])", "$1f");
				csString += $", IconColorAddSecondary = new DDLinearColor() {fltColor:F2}";
			}


			if (iconColorMulPrimary != null)
			{
				csString += $", IconColorMulPrimary = {iconColorMulPrimary.Value}f";
			}

			if (iconColorMulSecondary != null)
			{
				csString += $", IconColorMulSecondary = {iconColorMulSecondary.Value}f";
			}

			var primaryColorSet = GetColorSetProperty(obj, "PrimaryColorSets");
			var secondaryColorSet = GetColorSetProperty(obj, "SecondaryColorSets");

			if (primaryColorSet != null)
			{
				csString += ", PrimaryColorSets = new List<DDLinearColor> { ";
				for (int i = 0; i < primaryColorSet.Count; i++)
				{
					csString += $" new DDLinearColor({primaryColorSet[i].R:F2}f, {primaryColorSet[i].G:F2}f, {primaryColorSet[i].B:F2}f, {primaryColorSet[i].A:F2}f)";
					if (i != primaryColorSet.Count - 1)
						csString += ", ";
				}
				csString += "}";
			}

			if (secondaryColorSet != null)
			{
				csString += ", SecondaryColorSets = new List<DDLinearColor> { ";
				for (int i = 0; i < secondaryColorSet.Count; i++)
				{
					csString += $"new DDLinearColor({secondaryColorSet[i].R:F2}f, {secondaryColorSet[i].G:F2}f, {secondaryColorSet[i].B:F2}f, {secondaryColorSet[i].A:F2}f)";
					if (i != secondaryColorSet.Count - 1)
						csString += ", ";
				}
				csString += "}";
			}
			if (IconRefs.ContainsKey(objPath))
				csString += " }, // " + IconRefs[objPath].IconBase + " " + IconRefs[objPath].IconMask1 + " " + IconRefs[objPath].IconMask2;
			else
				csString += " }, // Cant Find Icon Ref";
			return csString;

		}


		public void DumpObjectsToFile(string file)
		{
			List<string> lines = new();
			lines.Add("namespace DDUP");
			lines.Add("{");
			lines.Add("\tpublic static class ItemTemplateInfo");
			lines.Add("\t{");
			lines.Add("\t\tpublic static readonly Dictionary<string, ItemEntry> Map = new()");
			lines.Add("\t\t{");
			
			foreach ( var item in PackageCache.Values )
			{
				foreach ( var obj in item.Objects )
				{
					if (obj.GetReferencePath().StartsWith("HeroEquipment"))
					{
						lines.Add("\t\t\t" + GetObjectCSLine(obj));					
					}
				}
			}

			lines.Add("\t\t};");
			lines.Add("\t}");
			lines.Add("}");

			File.WriteAllLines(file, lines);
			MainWindow.Log("Finished writing new Item Database");
		}
	}
}
