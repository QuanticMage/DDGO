using DDUP;
using Microsoft.VisualBasic.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Dynamic;
using System.Globalization;
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
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Xml.Linq;
using UELib;
using UELib.Branch.UE3.SFX.Tokens;
using UELib.Core;
using UELib.Engine;
using UELib.Services;
using UELib.Types;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace DungeonDefendersOfflinePreprocessor
{


	public struct IconReference
	{
		public string IconBase;
		public string IconMask1;
		public string IconMask2;
	}

	public class IconLocation
	{
		public int IconX;
		public int IconY;
		public byte[] TransparentMask = new byte[1];
	}

	public class UEPackageDatabase
	{

		// dev request
		List<string> SkipNames = new()
		{

		};


		private Dictionary<string, UnrealPackage> PackageCache = new(StringComparer.OrdinalIgnoreCase);
		private Dictionary<string, IconReference> IconRefs = new(StringComparer.OrdinalIgnoreCase);
		private Dictionary<string, IconLocation> IconLocs = new(StringComparer.OrdinalIgnoreCase);
		private string BaseDirectory = "";
		public void AddToDatabase(string tempPath, string upk)
		{
			var outputUpkPath = System.IO.Path.Combine(tempPath, upk);

			BaseDirectory = tempPath;
			var package = UnrealLoader.LoadPackage(outputUpkPath, System.IO.FileAccess.Read);
			package.CookerPlatform = BuildPlatform.PC;
			package.InitializePackage();
			PackageCache[package.PackageName] = package;

			MainWindow.Log($"Loaded {package.PackageName}");			
		}

		Dictionary<string, string> ParseDictionary(UDefaultProperty property)
		{
			if (property == null)
				return new Dictionary<string, string>();

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
					byte a = rgbValues[i + 3];
					if (r > a) r = a;
					if (g > a) g = a;

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
					pkg.CookerPlatform = BuildPlatform.PC;
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

		/// <summary>
		/// For an import-backed UObject, reconstruct the dotted import path (e.g. Engine.Actor or Package.Group.Obj)
		/// and return its outermost package name.
		/// </summary>
		string? FindPackageNameFromImport(UObject importObj)
		{
			var path = BuildImportPath(importObj);
			if (string.IsNullOrWhiteSpace(path))
				return null;
			return path.Split('.')[0];
		}

		/// <summary>
		/// Reconstruct the import path by following ImportTableItem.OuterIndex through the *owning* package's import/export tables.
		/// This is more reliable than walking UObject.Outer because imports are often just stubs.
		/// </summary>
		string? BuildImportPath(UObject importObj)
		{
			if (importObj == null) return null;
			if (importObj.ImportTable == null) return null;
			if (importObj.Package == null) return null;

			var ownerPkg = importObj.Package;
			var parts = new List<string>();

			// Start at the import entry for this object
			var currentImp = importObj.ImportTable;
			parts.Add(currentImp.ObjectName.Name);
			int outerIndex = currentImp.OuterIndex;

			while (outerIndex != 0)
			{
				if (outerIndex < 0)
				{
					// Import table indices are negative: -1 is Imports[0]
					var outerImp = ownerPkg.Imports[-outerIndex - 1];
					parts.Add(outerImp.ObjectName.Name);
					outerIndex = outerImp.OuterIndex;
				}
				else
				{
					// Export table indices are positive: 1 is Exports[0]
					var outerExp = ownerPkg.Exports[outerIndex - 1];
					parts.Add(outerExp.ObjectName.Name);
					outerIndex = outerExp.OuterIndex;
				}
			}

			parts.Reverse();
			return string.Join(".", parts);
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

		/// <summary>
		/// Resolves a UStruct (usually a UClass) that may be import-backed into an export-backed object
		/// from another already loaded package (or loaded on-demand via BaseDirectory).
		/// If resolution fails, returns the original struct.
		/// </summary>
		UStruct? ResolveStruct(UStruct? s)
		{
			if (s == null) return null;

			// Already export-backed.
			if (s.ExportTable != null)
			{
				s.Load();
				return s;
			}

			// Import-backed: resolve via PackageCache / BaseDirectory.
			if (s.ImportTable != null)
			{
				var importPath = BuildImportPath(s);
				var packageName = FindPackageNameFromImport(s);
				if (!string.IsNullOrWhiteSpace(packageName))
				{
					var pkg = LoadPackageByName(packageName);
					if (pkg != null)
					{
						// Prefer resolving by full path when available (avoids name collisions across groups).
						UStruct? resolved = null;
						if (!string.IsNullOrWhiteSpace(importPath))
							resolved = FindObjectByPath(pkg, importPath) as UStruct;

						// Fallback: name lookup (often sufficient for classes).
						resolved ??= pkg.FindObject<UStruct>(s.Name);
						if (resolved != null)
						{
							resolved.Load();
							return resolved;
						}
						else
						{
							if (!string.IsNullOrWhiteSpace(importPath))
								MainWindow.Log($"[ResolveStruct] Failed to resolve import '{importPath}' in loaded package '{packageName}'.");
							else
								MainWindow.Log($"[ResolveStruct] Failed to resolve import '{s.Name}' in loaded package '{packageName}'.");
						}
					}
				}
			}

			return s;
		}

		/// <summary>
		/// Resolves an archetype object that may be import-backed into an export-backed object
		/// from another already loaded package (or loaded on-demand via BaseDirectory).
		/// If resolution fails, returns the original archetype.
		/// </summary>
		UObject? ResolveArchetype(UObject? archetype)
		{
			if (archetype == null) return null;

			// Already export-backed.
			if (archetype.ExportTable != null)
			{
				archetype.Load();
				return archetype;
			}

			// Import-backed: resolve via PackageCache / BaseDirectory.
			if (archetype.ImportTable != null)
			{
				var importPath = BuildImportPath(archetype);
				var packageName = FindPackageNameFromImport(archetype);
				if (!string.IsNullOrWhiteSpace(packageName))
				{
					var pkg = LoadPackageByName(packageName);
					if (pkg != null)
					{
						// Prefer resolving by full path when available (avoids name collisions across groups).
						UObject? resolved = null;
						if (!string.IsNullOrWhiteSpace(importPath))
							resolved = FindObjectByPath(pkg, importPath) as UObject;

						// Fallback: name lookup.
						resolved ??= pkg.FindObject<UObject>(archetype.Name);
						if (resolved != null)
						{
							resolved.Load();
							return resolved;
						}
						else
						{
							if (!string.IsNullOrWhiteSpace(importPath))
								MainWindow.Log($"[ResolveArchetype] Failed to resolve import '{importPath}' in loaded package '{packageName}'.");
							else
								MainWindow.Log($"[ResolveArchetype] Failed to resolve import '{archetype.Name}' in loaded package '{packageName}'.");
						}
					}
				}
			}

			return archetype;
		}

		/// <summary>
		/// Gets the class default object (CDO). If UELib didn't hook up UClass.Default, attempt
		/// to find the Default__ export by name in the owning package.
		/// </summary>
		UObject? GetClassDefaultObject(UClass ucl)
		{
			if (ucl == null) return null;

			// Preferred: what UELib already linked.
			if (ucl.Default != null)
			{
				ucl.Default.Load();
				return ucl.Default;
			}

			// Fallback: find Default__<ClassName>.
			// If the class is import-backed, its Owner is the *referencing* package, not the defining one,
			// so hop to the imported package first.
			UnrealPackage? pkg = ucl.Package;
			if (ucl.ImportTable != null)
			{
				var pkgName = FindPackageNameFromImport(ucl);
				if (!string.IsNullOrWhiteSpace(pkgName))
					pkg = LoadPackageByName(pkgName);
			}
			if (pkg == null) return null;

			string defaultName = "Default__" + ucl.Name;
			var def = pkg.FindObject<UObject>(defaultName);
			if (def != null)
			{
				def.Load();
				return def;
			}

			return null;
		}


		public UDefaultProperty? GetProperty(UObject? obj, string propertyName)
		{
			if (obj == null || string.IsNullOrWhiteSpace(propertyName))
				return null;

			// Walk the Instance and Archetype chain
			var currentObj = obj;
			while (currentObj != null)
			{
				// Ensure the object has loaded its buffer
				currentObj.Load();

				var hit = currentObj.Properties?.Find(propertyName); // UELib's List has a Find helper
				if (hit != null) return hit;

				// Resolve archetype - handles both local exports and imports from other packages
				currentObj = ResolveArchetype(currentObj.Archetype as UObject);
			}

			// Walk the Class Hierarchy (resolving imports across loaded packages)
			// In UELib, UClass inherits from UState -> UStruct -> UField -> UObject
			var visitedClasses = new HashSet<UStruct>(ReferenceEqualityComparer.Instance);
			var currentClass = ResolveStruct(obj.Class as UStruct);
			while (currentClass != null && visitedClasses.Add(currentClass))
			{
				currentClass.Load();

				if (currentClass is UClass ucl)
				{
					var cdo = GetClassDefaultObject(ucl);
					if (cdo != null)
					{
						var hit = cdo.Properties?.Find(propertyName);
						if (hit != null) return hit;
					}
					else if (ucl.ImportTable != null)
					{
						// Helpful hint when the superclass lives in another package but wasn't linked.
						var pkgHint = FindPackageNameFromImport(ucl);
						//if (!string.IsNullOrWhiteSpace(pkgHint))
						//							MainWindow.Log($"[GetProperty] Could not locate CDO for imported class '{ucl.Name}'. Expected in package '{pkgHint}'.");
					}
				}

				// Move up the Super chain, resolving imports on each step.
				currentClass = ResolveStruct(currentClass.Super as UStruct);
			}

			return null;
		}
		public IReadOnlyList<UDefaultProperty> GetArrayPropertiesMerged(UObject? obj, string propertyName)
		{
			if (obj == null || string.IsNullOrWhiteSpace(propertyName))
				return Array.Empty<UDefaultProperty>();

			// 1) Collect the chain in the order we want to APPLY defaults:
			//    base -> derived (so derived overrides win when applied last)
			var chain = BuildDefaultChainBaseFirst(obj);

			// 2) Merge by ArrayIndex (derived overrides replace base values)
			var mergedByIndex = new SortedDictionary<int, UDefaultProperty>();

			foreach (var o in chain)
			{
				o.Load();

				var props = o.Properties;
				if (props == null) continue;

				foreach (var p in props)
				{
					if (p == null) continue;
					if (p.Name != propertyName) continue;

					// Unreal defaults are deltas: later layers override by index
					mergedByIndex[p.ArrayIndex] = p;
				}
			}

			return mergedByIndex.Values.ToList();
		}

		private List<UObject> BuildDefaultChainBaseFirst(UObject obj)
		{
			var visited = new HashSet<UObject>(ReferenceEqualityComparer.Instance);

			// We’ll collect “derived-first” then reverse at the end.
			var derivedFirst = new List<UObject>();

			// A) Instance -> Archetype chain (derived-first naturally)
			var currentObj = obj;
			while (currentObj != null && visited.Add(currentObj))
			{
				derivedFirst.Add(currentObj);
				// Resolve archetype - handles both local exports and imports from other packages
				currentObj = ResolveArchetype(currentObj.Archetype as UObject);
			}

			// B) Class hierarchy: CDOs from this class up to base (resolving imports across loaded packages)
			var visitedClasses = new HashSet<UStruct>(ReferenceEqualityComparer.Instance);
			var currentClass = ResolveStruct(obj.Class as UStruct);
			while (currentClass != null && visitedClasses.Add(currentClass))
			{
				if (currentClass is UClass ucl)
				{
					var cdo = GetClassDefaultObject(ucl);
					if (cdo != null && visited.Add(cdo))
						derivedFirst.Add(cdo);
					else if (cdo == null && ucl.ImportTable != null)
					{
						var pkgHint = FindPackageNameFromImport(ucl);
						//if (!string.IsNullOrWhiteSpace(pkgHint))
						//							MainWindow.Log($"[BuildDefaultChain] Could not locate CDO for imported class '{ucl.Name}'. Expected in package '{pkgHint}'.");
					}
				}

				currentClass = ResolveStruct(currentClass.Super as UStruct);
			}

			// Now reverse so base is applied first, derived last
			derivedFirst.Reverse();
			return derivedFirst;
		}
		/*	public string GetIntArrayMaterialized(UObject? obj, string propertyName)
			{
				var sparse = GetArrayPropertiesMerged(obj, propertyName);
				if (sparse.Count == 0) return Array.Empty<string>();

				// Array length in defaults is usually implied by the highest explicit index + 1
				var maxIndex = sparse.Max(p => p.ArrayIndex);
				var result = new string[maxIndex + 1]; // ints default to 0
				for (int i = 0; i <= maxIndex; i++)
					p.Value[i] = "0";
				foreach (var p in sparse)
					result[p.ArrayIndex] = p.Value;

				return result;
			}*/



		/// <summary>
		/// Reference equality comparer for cycle detection without relying on overridden Equals/GetHashCode.
		/// </summary>
		private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
		{
			public static readonly ReferenceEqualityComparer Instance = new();

			public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

			public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
		}


		int IconSize = 64;



		public void ExportAllHeroEquipmentToAtlas()
		{
			// 1. Storage for UNIQUE raw texture data
			// Key: Texture Name, Value: Raw pixel bytes from the original source file
			var uniqueIcons = new Dictionary<string, byte[]>();

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

						if (textureArrayProperty == null) continue;


						var parsedElements = ParseDictionary(textureArrayProperty);
						string[] targetKeys = { "EquipmentIcon", "EquipmentIconColorLayers" };

						foreach (var key in targetKeys)
						{
							if (!parsedElements.ContainsKey(key)) continue;

							string path = ExtractPath(parsedElements[key]);
							var iconImage = FindObjectByPath(item, path) as UTexture2D;
							if (iconImage == null) continue;

							// --- get png from file 
							string packageName = item.ToString();
							string referencePath = iconImage.GetReferencePath();
							string pathDir = BaseDirectory;
							int start = referencePath.IndexOf('\'') + 1;
							int end = referencePath.LastIndexOf('\'');

							string assetPath = referencePath.Substring(start, end - start);
							string relativePath = assetPath.Replace('.', Path.DirectorySeparatorChar) + ".png";

							string finalPath = Path.Combine(pathDir, packageName, relativePath);
							if (!File.Exists(finalPath))
							{
								MainWindow.Log("Can't find image at path " + finalPath);
								continue;
							}

							if (key == "EquipmentIconColorLayers")
							{
								// Special Handling: Split Red and Green channels into two distinct atlas entries
								string rKey = $"{iconImage.Name}_R";
								string gKey = $"{iconImage.Name}_G";

								ir.IconMask1 = rKey;
								ir.IconMask2 = gKey;
								if (!uniqueIcons.ContainsKey(rKey))
									uniqueIcons.Add(rKey, ExtractChannelAsGrayscale(finalPath, 'R'));

								if (!uniqueIcons.ContainsKey(gKey))
									uniqueIcons.Add(gKey, ExtractChannelAsGrayscale(finalPath, 'G'));
							}
							else
							{
								ir.IconBase = iconImage.Name;
								// Standard icon handling
								if (!uniqueIcons.ContainsKey(iconImage.Name))
								{
									uniqueIcons.Add(iconImage.Name, File.ReadAllBytes(finalPath));
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
			int atlasWidth = gridCount * IconSize;
			int atlasHeight = gridCount * IconSize;
			byte[] atlasBuffer = new byte[atlasWidth * atlasHeight * 4];

			// 3. Process and Stitch
			int index = 1;
			foreach (var kvp in uniqueIcons)
			{
				IconLocation il = new IconLocation();

				int row = index / gridCount;
				int col = index % gridCount;
				MainWindow.Log($"Queued unique icon: {kvp.Key} at row {row} and column {col}");

				il.IconX = col * IconSize;
				il.IconY = row * IconSize;
				// Use a MemoryStream to load the bytes we stored in the dictionary
				using (var ms = new MemoryStream(kvp.Value))
				using (var sourceBmp = new System.Drawing.Bitmap(ms))
				using (var resizedBmp = new System.Drawing.Bitmap(IconSize, IconSize))
				{
					// Resize logic
					using (var g = System.Drawing.Graphics.FromImage(resizedBmp))
					{
						g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
						g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
						g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
						g.DrawImage(sourceBmp, 0, 0, IconSize, IconSize);
					}

					// Extract Bits
					var rect = new System.Drawing.Rectangle(0, 0, IconSize, IconSize);
					var bmpData = resizedBmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

					il.TransparentMask = new byte[IconSize * IconSize];
					unsafe
					{
						byte* srcBase = (byte*)bmpData.Scan0;

						fixed (byte* destBase = atlasBuffer)
						{
							for (int y = 0; y < IconSize; y++)
							{
								byte* srcRow = srcBase + (y * bmpData.Stride);

								int destOffset = ((row * IconSize + y) * atlasWidth + (col * IconSize)) * 4;
								byte* destRow = destBase + destOffset;

								for (int x = 0; x < IconSize; x++)
								{
									byte* srcPixel = srcRow + (x * 4);
									byte* destPixel = destRow + (x * 4);

									// Copy BGRA to atlas buffer
									*((int*)destPixel) = *((int*)srcPixel);

									// if full color, use alpha- otherwise use red channel
									byte alpha = srcPixel[3];
									il.TransparentMask[y * IconSize + x] = alpha;
								}
							}
						}
					}

					resizedBmp.UnlockBits(bmpData);
				}

				IconLocs[kvp.Key] = il;
				index++;
			}

			// 4. Final Save
			DxtConverter.SaveToPng(atlasBuffer, atlasWidth, atlasHeight, atlasWidth, atlasHeight, BaseDirectory + @"\HeroEquipment_Atlas.png");
			MainWindow.Log($"Atlas complete! Saved {uniqueIcons.Count} unique icons to {atlasWidth}x{atlasHeight} texture.");
		}

		public static List<ULinearColor> ParseColorSetArray(string input)
		{
			var colors = new List<ULinearColor>();

			if (string.IsNullOrWhiteSpace(input))
				return colors;

			// Split safely on any newline style (\r\n, \n, etc.)
			var lines = input.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);

			foreach (var line in lines)
			{
				// Extract floats using regex
				var matches = Regex.Matches(line, @"[-+]?\d*\.\d+|\d+");

				// Expect at least 4 numbers (R,G,B,A). Skip the index at [0].
				if (matches.Count >= 5)
				{
					float r = float.Parse(matches[1].Value, CultureInfo.InvariantCulture);
					float g = float.Parse(matches[2].Value, CultureInfo.InvariantCulture);
					float b = float.Parse(matches[3].Value, CultureInfo.InvariantCulture);
					float a = float.Parse(matches[4].Value, CultureInfo.InvariantCulture);

					colors.Add(new ULinearColor(r, g, b, a));
				}
			}

			return colors;
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

				byte[]? colorAlpha = null;
				byte[]? maskRAlpha = null;
				byte[]? maskGAlpha = null;
				if (color != null && IconLocs.ContainsKey(color))
				{
					IconLocation loc = IconLocs[color];
					x = loc.IconX;
					y = loc.IconY;
					colorAlpha = loc.TransparentMask;
				}

				if (color1 != null && IconLocs.ContainsKey(color1))
				{
					IconLocation loc = IconLocs[color1];
					x1 = loc.IconX;
					y1 = loc.IconY;
					maskRAlpha = loc.TransparentMask;
				}

				if (color2 != null && IconLocs.ContainsKey(color2))
				{
					IconLocation loc = IconLocs[color2];
					x2 = loc.IconX;
					y2 = loc.IconY;
					maskGAlpha = loc.TransparentMask;
				}


				// verify the masks make sense
				if (colorAlpha != null && maskRAlpha != null && maskGAlpha != null)
				{
					int error = 0;
					for (int i = 0; i < IconSize * IconSize; i++)
					{
						if ((colorAlpha[i] < 2) && ((maskRAlpha[i] >= 32) || (maskGAlpha[i] >= 32)))
							error++;
					}

					// these are the only exceptions to our uncolorized detection metric - Navi is a false positive, the other two are false negatives
					if (((error > 0) && (!objPath.EndsWith("Equipment_familiar_NaviFairy'"))) ||
						(objPath.EndsWith("Spoon.HeroEquipment_Spoon'")) ||
						(objPath.EndsWith("Equipment_Familiar_AnimorphicEmber'")))
					{
						x1 = 0;
						y1 = 0;
						x2 = 0;
						y2 = 0;
					}
				}
			}

			var iconColorAddPrimary = GetProperty(obj, "IconColorAddPrimary");
			var iconColorAddSecondary = GetProperty(obj, "IconColorAddSecondary");
			var iconColorMulPrimary = GetProperty(obj, "IconColorMultPrimary");
			var iconColorMulSecondary = GetProperty(obj, "IconColorMultSecondary");
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



			var primaryColorSetArray = GetProperty(obj, "PrimaryColorSets");
			var secondaryColorSetArray = GetProperty(obj, "SecondaryColorSets");

			var primaryColorSet = (primaryColorSetArray != null ? ParseColorSetArray(primaryColorSetArray.Value) : null);
			var secondaryColorSet = (secondaryColorSetArray != null ? ParseColorSetArray(secondaryColorSetArray.Value) : null);



			if ((primaryColorSet != null) && (primaryColorSet.Count > 0))
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

			if ((secondaryColorSet != null) && (secondaryColorSet.Count > 0))
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

			foreach (var item in PackageCache.Values)
			{
				foreach (var obj in item.Objects)
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


		public async Task AddObjectsToDatabase(ExportedTemplateDatabase db)
		{
			foreach (var item in PackageCache.Values)
			{
				foreach (var obj in item.Objects)
				{
					if (obj.GetReferencePath().StartsWith("DunDefDamageType"))
					{
						MainWindow.Log($"Adding DunDefDamageType {obj.GetPath()}");
						AddDunDefDamageTypeToDB(obj, db);
						await Task.Yield();

					}
				}
			}
			foreach (var item in PackageCache.Values)
			{
				foreach (var obj in item.Objects)
				{
					if (obj.GetReferencePath().StartsWith("DunDefPlayer"))
					{
						MainWindow.Log($"Adding DunDefPlayer {obj.GetPath()}");
						AddPlayerToDB(obj, db);
						await Task.Yield();

					}
				}
			}
			foreach (var item in PackageCache.Values)
			{
				foreach (var obj in item.Objects)
				{
					if (obj.GetReferencePath().StartsWith("DunDefHero"))
					{
						MainWindow.Log($"Adding DunDefHero {obj.GetPath()}");
						AddDunDefHeroToDB(obj, db);
						await Task.Yield();

					}
				}
			}
			foreach (var item in PackageCache.Values)
			{
				foreach (var obj in item.Objects)
				{
					if (obj.GetReferencePath().StartsWith("DunDefProjectile"))
					{
						MainWindow.Log($"Adding DunDefProjectile {obj.GetPath()}");
						AddProjectileToDB(obj, db);
						await Task.Yield();
					}
				}

			}
			foreach (var item in PackageCache.Values)
			{
				foreach (var obj in item.Objects)
				{
					if (obj.GetReferencePath().StartsWith("DunDefWeapon"))
					{
						MainWindow.Log($"Adding DunDefWeapon {obj.GetPath()}");
						AddWeaponToDB(obj, db);
						await Task.Yield();
					}
				}
			}


			foreach (var item in PackageCache.Values)
			{
				foreach (var obj in item.Objects)
				{
					if (obj.GetReferencePath().StartsWith("HeroEquipment"))
					{
						MainWindow.Log($"Adding HeroEquipment {obj.GetPath()}");
						AddEquipmentToDB(obj, db);
						await Task.Yield();
					}
				}
			}
		}

		public bool DoesObjectInheritFromClass(UObject obj, string _class)
		{
			var currentClass = obj.Class as UStruct;
			while (currentClass != null)
			{
				if (currentClass.Name == _class)
					return true;

				// Move up the Super chain (SuperName is resolved to SuperField in UELib)
				currentClass = currentClass.Super as UStruct;
			}
			return false;
		}

		public void AddPropertyToMap(UObject obj, string property, Dictionary<string, string> map, string? def = null)
		{
			UDefaultProperty? prop = GetProperty(obj, property);
			if (prop != null)
			{
				map.Add(property, prop.Value);
			}
			else if (def != null)
			{
				map.Add(property, def);
			}
			else
			{
				MainWindow.Log($"Couldn't find property {property} on object {obj.Name}");
			}

		}
		public void AddArrayPropertyToMap(UObject obj, string propertyName, Dictionary<string, string> map, int forcedLength = -1, string defaultValue = "0")
		{
			var sparse = GetArrayPropertiesMerged(obj, propertyName); // IReadOnlyList<UDefaultProperty>

			if ((sparse.Count == 1) && (sparse[0].Value.Contains("[")))
			{
				map[propertyName] = sparse[0].Value;
				return;
			}

			// index -> raw string
			var byIndex = new Dictionary<int, string>(sparse.Count);
			var maxIndex = -1;

			foreach (var p in sparse)
			{
				int idx = p.ArrayIndex;
				if (idx > maxIndex) maxIndex = idx;

				// no Convert.ToInt32 here
				byIndex[idx] = p.Value?.ToString() ?? defaultValue;
			}

			int impliedLength = (maxIndex >= 0) ? (maxIndex + 1) : 0;
			int length = (forcedLength > 0) ? forcedLength : impliedLength;

			if (length <= 0)
			{
				map[propertyName] = "";
				return;
			}

			var sb = new StringBuilder();

			for (int i = 0; i < length; i++)
			{
				var v = byIndex.TryGetValue(i, out var val) ? val : defaultValue;

				sb.Append(propertyName)
				  .Append('[').Append(i).Append("]=")
				  .Append(v)
				  .Append("\r\n");
			}

			map[propertyName] = sb.ToString();

		}



		public int AddEquipmentToDB(UObject obj, ExportedTemplateDatabase db)
		{
			Dictionary<string, string> propertyMap = new Dictionary<string, string>();

			// Arrays (materialized)
			AddArrayPropertyToMap(obj, "StatModifiers", propertyMap, 10, "0");
			AddArrayPropertyToMap(obj, "DamageReductions", propertyMap);
			AddArrayPropertyToMap(obj, "DamageReductionRandomizers", propertyMap);
			AddArrayPropertyToMap(obj, "QualityDescriptorNames", propertyMap);
			AddArrayPropertyToMap(obj, "QualityDescriptorRealNames", propertyMap);
			AddArrayPropertyToMap(obj, "RandomBaseNames", propertyMap);
			AddArrayPropertyToMap(obj, "StatEquipmentIDs", propertyMap, 10, "0");
			AddArrayPropertyToMap(obj, "StatEquipmentTiers", propertyMap, 10, "0");
			AddArrayPropertyToMap(obj, "StatModifierRandomizers", propertyMap, 11, "0");
			AddArrayPropertyToMap(obj, "StatObjectArray", propertyMap);

			// Icon / color set arrays
			AddArrayPropertyToMap(obj, "PrimaryColorSets", propertyMap);
			AddArrayPropertyToMap(obj, "SecondaryColorSets", propertyMap);

			// Localized/string-ish fields
			AddPropertyToMap(obj, "AdditionalDescription", propertyMap, "0");
			AddPropertyToMap(obj, "BaseForgerName", propertyMap, "0");
			AddPropertyToMap(obj, "DamageDescription", propertyMap, "0");
			AddPropertyToMap(obj, "Description", propertyMap, "0");
			AddPropertyToMap(obj, "EquipmentName", propertyMap, "0");
			AddPropertyToMap(obj, "ExtraQualityUpgradeDamageNumberDescriptor", propertyMap, "0");
			AddPropertyToMap(obj, "ForgedByDescription", propertyMap, "0");
			AddPropertyToMap(obj, "LevelString", propertyMap, "0");
			AddPropertyToMap(obj, "Name", propertyMap, "0");
			AddPropertyToMap(obj, "RequiredClassString", propertyMap, "0");
			AddPropertyToMap(obj, "UserEquipmentName", propertyMap, "0");
			AddPropertyToMap(obj, "UserForgerName", propertyMap, "0");

			// Ints
			AddPropertyToMap(obj, "EquipmentID1", propertyMap, "0");
			AddPropertyToMap(obj, "EquipmentID2", propertyMap, "0");
			AddPropertyToMap(obj, "EquipmentSetID", propertyMap, "0");
			AddPropertyToMap(obj, "EquipmentTemplate", propertyMap, "0");
			AddPropertyToMap(obj, "EquipmentWeaponTemplate", propertyMap, "0");
			AddPropertyToMap(obj, "HeroStatUpgradeLimit", propertyMap, "0");
			AddPropertyToMap(obj, "Level", propertyMap, "0");
			AddPropertyToMap(obj, "LevelRequirementIndex", propertyMap, "0");
			AddPropertyToMap(obj, "MaxEquipmentLevel", propertyMap, "0");
			AddPropertyToMap(obj, "MaxEquipmentLevelRandomizer", propertyMap, "0");
			AddPropertyToMap(obj, "MaxHeroStatValue", propertyMap, "0");
			AddPropertyToMap(obj, "MaxLevel", propertyMap, "0");
			AddPropertyToMap(obj, "MaxNonTranscendentStatRollValue", propertyMap, "0");
			AddPropertyToMap(obj, "MaxUpgradeableSpeedOfProjectilesBonus", propertyMap, "0");
			AddPropertyToMap(obj, "MinDamageBonus", propertyMap, "0");
			AddPropertyToMap(obj, "MinLevel", propertyMap, "0");
			AddPropertyToMap(obj, "MinSupremeLevel", propertyMap, "0");
			AddPropertyToMap(obj, "MinTranscendentLevel", propertyMap, "0");
			AddPropertyToMap(obj, "MinUltimateLevel", propertyMap, "0");
			AddPropertyToMap(obj, "MinimumSellWorth", propertyMap, "0");
			AddPropertyToMap(obj, "StoredMana", propertyMap, "0");
			AddPropertyToMap(obj, "UltimateMaxHeroStatValue", propertyMap, "0");
			AddPropertyToMap(obj, "UltimatePlusMaxHeroStatValue", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponAdditionalDamageAmount", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponAdditionalDamageAmountRandomizer", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponAdditionalDamageType", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponAltDamageBonus", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponAltDamageBonusRandomizer", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponBlockingBonusRandomizer", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponChargeSpeedBonusRandomizer", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponClipAmmoBonus", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponClipAmmoBonusRandomizer", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponDamageBonus", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponDamageBonusRandomizer", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponDamageDisplayValueScale", propertyMap, "0");
			
			AddPropertyToMap(obj, "WeaponKnockbackBonusRandomizer", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponKnockbackMax", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponNumberOfProjectilesBonusRandomizer", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponNumberOfProjectilesQualityBaseline", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponReloadSpeedBonusRandomizer", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponShotsPerSecondBonusRandomizer", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponSpeedOfProjectilesBonus", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponSpeedOfProjectilesBonusRandomizer", propertyMap, "0");
			AddPropertyToMap(obj, "weaponType", propertyMap, "0");

			// Floats
			AddPropertyToMap(obj, "AdditionalWeaponDamageBonusRandomizerMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "AltDamageIncreasePerLevelMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "AltDamageRandomizerMult", propertyMap, "0.0");
			AddPropertyToMap(obj, "AltMaxDamageIncreasePerLevel", propertyMap, "0.0");
			AddPropertyToMap(obj, "DamageIncreasePerLevelMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "ElementalDamageIncreasePerLevelMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "ElementalDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "ExtraQualityDamageIncreasePerLevelMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "ExtraQualityMaxDamageIncreasePerLevel", propertyMap, "0.0");
			AddPropertyToMap(obj, "FullEquipmentSetStatMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "HighLevelManaCostPerLevelExponentialFactorAdditional", propertyMap, "0.0");
			AddPropertyToMap(obj, "HighLevelManaCostPerLevelMaxQualityMultiplierAdditional", propertyMap, "0.0");
			AddPropertyToMap(obj, "HighLevelRequirementRatingThreshold", propertyMap, "0.0");
			AddPropertyToMap(obj, "HighLevelThreshold", propertyMap, "0.0");
			AddPropertyToMap(obj, "MaxDamageIncreasePerLevel", propertyMap, "0.0");
			AddPropertyToMap(obj, "MaxRandomValue", propertyMap, "0.0");
			AddPropertyToMap(obj, "MaxRandomValueNegative", propertyMap, "0.0");
			AddPropertyToMap(obj, "MinElementalDamageIncreasePerLevel", propertyMap, "0");
			AddPropertyToMap(obj, "MinEquipmentLevels", propertyMap, "0.0");
			AddPropertyToMap(obj, "MinimumPercentageValue", propertyMap, "0.0");
			AddPropertyToMap(obj, "MythicalFullEquipmentSetStatMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "NegativeMinimumPercentageValue", propertyMap, "0.0");
			AddPropertyToMap(obj, "NegativeThresholdQualityPecentMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "PlayerSpeedMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "QualityThreshold", propertyMap, "0.0");
			AddPropertyToMap(obj, "RandomNegativeThreshold", propertyMap, "0.0");
			AddPropertyToMap(obj, "RandomPower", propertyMap, "0.0");
			AddPropertyToMap(obj, "RandomPowerOverrideIfNegative", propertyMap, "0.0");
			AddPropertyToMap(obj, "RandomizerQualityMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "RandomizerStatModifierGoNegativeChance", propertyMap, "0.0");
			AddPropertyToMap(obj, "RandomizerStatModifierGoNegativeMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "SecondExtraQualityDamageIncreasePerLevelMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "SecondExtraQualityMaxDamageIncreasePerLevel", propertyMap, "0.0");
			AddPropertyToMap(obj, "StackedStatModifier", propertyMap, "0.0");
			AddPropertyToMap(obj, "SupremeFullEquipmentSetStatMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "SupremeLevelBoostAmount", propertyMap, "0.0");
			AddPropertyToMap(obj, "SupremeLevelBoostRandomizerPower", propertyMap, "0.0");
			AddPropertyToMap(obj, "TotalRandomizerMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "TranscendentFullEquipmentSetStatMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "TranscendentLevelBoostAmount", propertyMap, "0.0");
			AddPropertyToMap(obj, "TranscendentLevelBoostRandomizerPower", propertyMap, "0.0");
			AddPropertyToMap(obj, "Ultimate93Chance", propertyMap, "0.0");
			AddPropertyToMap(obj, "UltimateDamageIncreasePerLevelMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "UltimateFullEquipmentSetStatMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "UltimateLevelBoostAmount", propertyMap, "0.0");
			AddPropertyToMap(obj, "UltimateLevelBoostRandomizerPower", propertyMap, "0.0");
			AddPropertyToMap(obj, "UltimateMaxDamageIncreasePerLevel", propertyMap, "0.0");
			AddPropertyToMap(obj, "UltimatePlusChance", propertyMap, "0.0");
			AddPropertyToMap(obj, "UltimatePlusPlusChance", propertyMap, "0.0");
			AddPropertyToMap(obj, "Values", propertyMap, "0.0");
			AddPropertyToMap(obj, "WeaponAltDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "WeaponDamageBonusRandomizerMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "WeaponDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "WeaponEquipmentRatingPercentBase", propertyMap, "0.0");
			AddPropertyToMap(obj, "WeaponSwingSpeedMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "RuthlessUltimate93Chance", propertyMap, "0.0");
			AddPropertyToMap(obj, "RuthlessUltimatePlusChance", propertyMap, "0.0");
			AddPropertyToMap(obj, "RuthlessUltimatePlusPlusChance", propertyMap, "0.0");

			// Bytes / bool-ish flags
			AddPropertyToMap(obj, "AllowNameRandomization", propertyMap, "0");
			AddPropertyToMap(obj, "CountsForAllArmorSets", propertyMap, "0");
			AddPropertyToMap(obj, "NameIndex_Base", propertyMap, "0");
			AddPropertyToMap(obj, "NameIndex_DamageReduction", propertyMap, "0");
			AddPropertyToMap(obj, "NameIndex_QualityDescriptor", propertyMap, "0");
			AddPropertyToMap(obj, "OnlyRandomizeBaseName", propertyMap, "0");

			AddPropertyToMap(obj, "WeaponAdditionalDamageTypeNotPoison", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponBlockingBonus", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponChargeSpeedBonus", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponKnockbackBonus", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponNumberOfProjectilesBonus", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponReloadSpeedBonus", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponShotsPerSecondBonus", propertyMap, "0");

			AddPropertyToMap(obj, "bCanBeEquipped", propertyMap, "0");
			AddPropertyToMap(obj, "bCantBeDropped", propertyMap, "0");
			AddPropertyToMap(obj, "bCantBeSold", propertyMap, "0");
			AddPropertyToMap(obj, "bDisableRandomization", propertyMap, "0");
			AddPropertyToMap(obj, "bEquipmentFeatureByte1", propertyMap, "0");
			AddPropertyToMap(obj, "bEquipmentFeatureByte2", propertyMap, "0");
			AddPropertyToMap(obj, "bForceAllowDropping", propertyMap, "0");
			AddPropertyToMap(obj, "bForceAllowSelling", propertyMap, "0");
			AddPropertyToMap(obj, "bForceRandomizerWithMinEquipmentLevel", propertyMap, "0");
			AddPropertyToMap(obj, "bHideQualityDescriptors", propertyMap, "0");
			AddPropertyToMap(obj, "bIsConsumable", propertyMap, "0");
			AddPropertyToMap(obj, "bIsSecondary", propertyMap, "0");
			AddPropertyToMap(obj, "bNoNegativeRandomizations", propertyMap, "0");
			AddPropertyToMap(obj, "bUseBonusStatsFromStacking", propertyMap, "0");
			AddPropertyToMap(obj, "bUseExtraQualityDamage", propertyMap, "0");
			AddPropertyToMap(obj, "bUseSecondExtraQualityDamage", propertyMap, "0");
			AddPropertyToMap(obj, "UseColorSets", propertyMap, "0");

			// Native-ish / icon section
			AddPropertyToMap(obj, "EquipmentDescription", propertyMap, "0");
			AddPropertyToMap(obj, "EquipmentType", propertyMap, "0");
			AddPropertyToMap(obj, "ForDamageType", propertyMap, "0");
			AddPropertyToMap(obj, "MaxRandomElementalDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "MyRating", propertyMap, "0.0");
			AddPropertyToMap(obj, "MyRatingPercent", propertyMap, "0.0");
			AddPropertyToMap(obj, "PercentageReduction", propertyMap, "0");
			AddPropertyToMap(obj, "UserID", propertyMap, "0");

			AddPropertyToMap(obj, "WeaponAltDamageBonusUse", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponBlockingBonusUse", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponChargeSpeedBonusUse", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponClipAmmoBonusUse", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponKnockbackBonusUse", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponReloadSpeedBonusUse", propertyMap, "0");
			AddPropertyToMap(obj, "WeaponShotsPerSecondBonusUse", propertyMap, "0");
			AddPropertyToMap(obj, "bDisableTheRandomization", propertyMap, "0");
			AddPropertyToMap(obj, "bForceUseParentTemplate", propertyMap, "0");
			AddPropertyToMap(obj, "UseWeaponCoreStats", propertyMap, "0");
			AddPropertyToMap(obj, "bForceToMinElementalScale", propertyMap, "0");
			AddPropertyToMap(obj, "bForceToMaxElementalScale", propertyMap, "0");

			AddPropertyToMap(obj, "IconColorAddPrimary", propertyMap, "0");
			AddPropertyToMap(obj, "IconColorAddSecondary", propertyMap, "0");
			AddPropertyToMap(obj, "IconColorMultPrimary", propertyMap, "0.0");
			AddPropertyToMap(obj, "IconColorMultSecondary", propertyMap, "0.0");



			// Calculate icon coordinates from the IconLocs/IconRefs dictionaries
			int iconX = 0, iconY = 0, iconX1 = 0, iconY1 = 0, iconX2 = 0, iconY2 = 0;
			string objPath = obj.GetReferencePath();

			if (IconRefs.ContainsKey(objPath))
			{
				string iconBase = IconRefs[objPath].IconBase;
				string iconMask1 = IconRefs[objPath].IconMask1;
				string iconMask2 = IconRefs[objPath].IconMask2;

				byte[]? colorAlpha = null;
				byte[]? maskRAlpha = null;
				byte[]? maskGAlpha = null;

				if (iconBase != null && IconLocs.ContainsKey(iconBase))
				{
					IconLocation loc = IconLocs[iconBase];
					iconX = loc.IconX;
					iconY = loc.IconY;
					colorAlpha = loc.TransparentMask;
				}

				if (iconMask1 != null && IconLocs.ContainsKey(iconMask1))
				{
					IconLocation loc = IconLocs[iconMask1];
					iconX1 = loc.IconX;
					iconY1 = loc.IconY;
					maskRAlpha = loc.TransparentMask;
				}

				if (iconMask2 != null && IconLocs.ContainsKey(iconMask2))
				{
					IconLocation loc = IconLocs[iconMask2];
					iconX2 = loc.IconX;
					iconY2 = loc.IconY;
					maskGAlpha = loc.TransparentMask;
				}

				// Verify the masks make sense (same logic as GetObjectCSLine)
				if (colorAlpha != null && maskRAlpha != null && maskGAlpha != null)
				{
					int error = 0;
					for (int i = 0; i < IconSize * IconSize; i++)
					{
						if ((colorAlpha[i] < 2) && ((maskRAlpha[i] >= 32) || (maskGAlpha[i] >= 32)))
							error++;
					}

					// Handle exceptions for false positives/negatives
					if (((error > 0) && (!objPath.EndsWith("Equipment_familiar_NaviFairy'"))) ||
						(objPath.EndsWith("Spoon.HeroEquipment_Spoon'")) ||
						(objPath.EndsWith("Equipment_Familiar_AnimorphicEmber'")))
					{
						iconX1 = 0;
						iconY1 = 0;
						iconX2 = 0;
						iconY2 = 0;
					}
				}
			}

			propertyMap["IconX"] = iconX.ToString();
			propertyMap["IconY"] = iconY.ToString();
			propertyMap["IconX1"] = iconX1.ToString();
			propertyMap["IconY1"] = iconY1.ToString();
			propertyMap["IconX2"] = iconX2.ToString();
			propertyMap["IconY2"] = iconY2.ToString();

			propertyMap["Template"] = obj.GetPath();
			propertyMap["Class"] = (obj.Class?.Name?.Name ?? "");
			// Finally store it in your DB (adapt to your actual DB API)
			HeroEquipment_Data hed = new HeroEquipment_Data(propertyMap, db);

			if (obj.GetReferencePath().StartsWith("HeroEquipment_Familiar"))
				propertyMap["FamiliarDataIndex"] = AddFamiliarToDB(obj, db).ToString();
			else
				propertyMap["FamiliarDataIndex"] = "-1";

			return db.AddHeroEquipment(obj.GetPath(), (obj.Class?.Name?.Name ?? ""), ref hed);
		}

		public int AddDunDefDamageTypeToDB(UObject obj, ExportedTemplateDatabase db)
		{
			Dictionary<string, string> propertyMap = new Dictionary<string, string>();
			AddPropertyToMap(obj, "AdjectiveName", propertyMap, "Default");
			AddPropertyToMap(obj, "FriendlyName", propertyMap, "Default");
			AddPropertyToMap(obj, "UseForNotPoisonElementalDamage", propertyMap, "false");
			AddPropertyToMap(obj, "UseForRandomElementalDamage", propertyMap, "false");
			AddPropertyToMap(obj, "DamageTypeArrayIndex", propertyMap, "-1");


			propertyMap["Template"] = obj.GetPath();
			propertyMap["Class"] = (obj.Class?.Name?.Name ?? "");           
			// Build the familiar data struct (parses arrays via db.BuildArray in the ctor)
			DunDefDamageType_Data dmgType = new DunDefDamageType_Data(propertyMap, db);

			// Finally store it in your DB (adapt to your actual DB API)
			// If your DB doesn’t have this exact method name/signature, mirror AddHeroEquipment’s pattern.
			return db.AddDunDefDamageType(obj.GetPath(), (obj.Class?.Name?.Name ?? ""), ref dmgType);
		}

		public int AddFamiliarToDB(UObject obj, ExportedTemplateDatabase db)
		{
			Dictionary<string, string> propertyMap = new Dictionary<string, string>();

			// Arrays (materialized)
			// HeroEquipment_Familiar_TowerDamageScaling
			AddArrayPropertyToMap(obj, "ProjectileDelays", propertyMap);
			AddArrayPropertyToMap(obj, "ProjectileTemplates", propertyMap);

			// Floats
			// HeroEquipment_Familiar_AoeBuffer
			AddPropertyToMap(obj, "BuffRange", propertyMap, "0.0");

			// HeroEquipment_Familiar_Corehealer
			AddPropertyToMap(obj, "HealAmountBase", propertyMap, "0.0");
			AddPropertyToMap(obj, "HealAmountExtraMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "HealAmountMaxPercent", propertyMap, "0.0");
			AddPropertyToMap(obj, "HealInterval", propertyMap, "0.0");
			AddPropertyToMap(obj, "HealRangeBase", propertyMap, "0.0");
			AddPropertyToMap(obj, "HealRangeStatBase", propertyMap, "0.0");
			AddPropertyToMap(obj, "HealRangeStatExponent", propertyMap, "0.0");
			AddPropertyToMap(obj, "HealRangeStatMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "MinimumCoreHealthPercent", propertyMap, "0.0");

			// HeroEquipment_Familiar_Melee_TowerScaling
			AddPropertyToMap(obj, "BaseDamageToHealRatio", propertyMap, "0.0");
			AddPropertyToMap(obj, "DamageHealMultiplierExponent", propertyMap, "0.0");
			AddPropertyToMap(obj, "ExtraNightmareMeleeDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "MaxHealMultiplierExponent", propertyMap, "0.0");
			AddPropertyToMap(obj, "MaxHealPerDamage", propertyMap, "0.0");
			AddPropertyToMap(obj, "MaxKnockbackMuliplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "MeleeDamageMomentum", propertyMap, "0.0");
			AddPropertyToMap(obj, "MeleeHitRadius", propertyMap, "0.0");
			AddPropertyToMap(obj, "MinHealPerDamage", propertyMap, "0.0");
			AddPropertyToMap(obj, "RandomizedDamageMultiplierDivisor", propertyMap, "0.0");

			// HeroEquipment_Familiar_PawnBooster
			AddPropertyToMap(obj, "BaseBoost", propertyMap, "0.0");
			AddPropertyToMap(obj, "BoostRangeStatBase", propertyMap, "0.0");
			AddPropertyToMap(obj, "BoostRangeStatExponent", propertyMap, "0.0");
			AddPropertyToMap(obj, "BoostRangeStatMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "BoostStatBase", propertyMap, "0.0");
			AddPropertyToMap(obj, "BoostStatExponent", propertyMap, "0.0");
			AddPropertyToMap(obj, "BoostStatMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "FirstBoostInterval", propertyMap, "0.0");
			AddPropertyToMap(obj, "MaxBoostStat", propertyMap, "0.0");

			// HeroEquipment_Familiar_PlayerHealer
			AddPropertyToMap(obj, "FalloffExponent", propertyMap, "0.0");
			AddPropertyToMap(obj, "HealRange", propertyMap, "0.0");
			AddPropertyToMap(obj, "MinimumHealDistancePercent", propertyMap, "0.0");

			// HeroEquipment_Familiar_TADPS
			AddPropertyToMap(obj, "dpsTreshold", propertyMap, "0.0");

			// HeroEquipment_Familiar_TowerBooster
			AddPropertyToMap(obj, "BaseBoostRange", propertyMap, "0.0");
			AddPropertyToMap(obj, "BoostAmountMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "BoostRangeExponent", propertyMap, "0.0");
			AddPropertyToMap(obj, "ETBAttackRangeExponent", propertyMap, "0.0");
			AddPropertyToMap(obj, "ETBAttackRateExponent", propertyMap, "0.0");
			AddPropertyToMap(obj, "ETBDamageExponent", propertyMap, "0.0");
			AddPropertyToMap(obj, "ETBResistanceExponent", propertyMap, "0.0");
			AddPropertyToMap(obj, "MaxRangeBoostStat", propertyMap, "0.0");

			// HeroEquipment_Familiar_TowerDamageScaling
			AddPropertyToMap(obj, "AbsoluteDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "AltProjectileMinimumRange", propertyMap, "0.0");
			AddPropertyToMap(obj, "BaseDamageToManaRatio", propertyMap, "0.0");
			AddPropertyToMap(obj, "BaseHealAmount", propertyMap, "0.0");
			AddPropertyToMap(obj, "Damage", propertyMap, "0.0");
			AddPropertyToMap(obj, "DamageManaMultiplierExponent", propertyMap, "0.0");
			AddPropertyToMap(obj, "ExtraNightmareDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "HealAmountMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "HealingPriorityHealthPercentage", propertyMap, "0.0");
			AddPropertyToMap(obj, "ManaMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "MaxManaMultiplierExponent", propertyMap, "0.0");
			AddPropertyToMap(obj, "MaxManaPerDamage", propertyMap, "0.0");
			AddPropertyToMap(obj, "MinManaPerDamage", propertyMap, "0.0");
			AddPropertyToMap(obj, "MinimumProjectileSpeed", propertyMap, "0.0");
			AddPropertyToMap(obj, "NightmareDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "NightmareHealingMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "ProjectileDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "ProjectileShootInterval", propertyMap, "0.0");
			AddPropertyToMap(obj, "ProjectileSpeedBonusMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "ShotsPerSecondExponent", propertyMap, "0.0");
			AddPropertyToMap(obj, "TargetRange", propertyMap, "0.0");
			AddPropertyToMap(obj, "WeakenEnemyTargetPercentage", propertyMap, "0.0");

			// HeroEquipment_Familiar_TowerHealer
			AddPropertyToMap(obj, "HealRadius", propertyMap, "0.0");

			// HeroEquipment_Familiar
			AddPropertyToMap(obj, "BarbStanceDamageMulti", propertyMap, "0.0");

			// Ints
			// HeroEquipment_Familiar_Corehealer
			AddPropertyToMap(obj, "StringHealAmount", propertyMap, "0");
			AddPropertyToMap(obj, "StringHealRange", propertyMap, "0");
			AddPropertyToMap(obj, "StringHealSpeed", propertyMap, "0");

			// HeroEquipment_Familiar_Melee_TowerScaling
			AddPropertyToMap(obj, "MeleeDamageType", propertyMap, "0");
			AddPropertyToMap(obj, "RandomizedDamageMultiplierMaximum", propertyMap, "0");

			// HeroEquipment_Familiar_PawnBooster
			AddPropertyToMap(obj, "BoostStatUpgradeInterval", propertyMap, "0");
			AddPropertyToMap(obj, "MaxNumberOfPawnsToBoost", propertyMap, "0");
			AddPropertyToMap(obj, "SoftMaxNumberOfPawnsToBoost", propertyMap, "0");

			// HeroEquipment_Familiar_TADPS
			AddPropertyToMap(obj, "AdditionalName", propertyMap, "0");
			AddPropertyToMap(obj, "fixedprojspeedbonus", propertyMap, "0");

			// HeroEquipment_Familiar_TowerBooster
			AddPropertyToMap(obj, "MaxBoostStatValue", propertyMap, "0");
			AddPropertyToMap(obj, "MaxNumberOfTowersToBoost", propertyMap, "0");
			AddPropertyToMap(obj, "MaxTowerBoostStat", propertyMap, "0");
			AddPropertyToMap(obj, "SoftMaxNumberOfTowersToBoost", propertyMap, "0");

			// HeroEquipment_Familiar_TowerDamageScaling
			AddPropertyToMap(obj, "Projectile", propertyMap, "0");
			AddPropertyToMap(obj, "ProjectileTemplate", propertyMap, "0");
			AddPropertyToMap(obj, "ProjectileTemplateAlt", propertyMap, "0");
			AddPropertyToMap(obj, "ShotsPerSecondBonusCap", propertyMap, "0");

			// Bytes / bool-ish flags
			// HeroEquipment_Familiar_Melee_TowerScaling
			AddPropertyToMap(obj, "bAlsoShootProjectile", propertyMap, "0");
			AddPropertyToMap(obj, "bDoMeleeHealing", propertyMap, "0");
			AddPropertyToMap(obj, "bUseRandomizedDamage", propertyMap, "0");

			// HeroEquipment_Familiar_PawnBooster
			AddPropertyToMap(obj, "ProModeFocused", propertyMap, "false");

			// HeroEquipment_Familiar_PlayerHealer
			AddPropertyToMap(obj, "bUseFixedHealSpeed", propertyMap, "false");

			// HeroEquipment_Familiar_TADPS
			AddPropertyToMap(obj, "bFixedProjSpeed", propertyMap, "0");

			// HeroEquipment_Familiar_TowerDamageScaling
			AddPropertyToMap(obj, "DoLineOfSightCheck", propertyMap, "false");
			AddPropertyToMap(obj, "bAddManaForDamage", propertyMap, "0");
			AddPropertyToMap(obj, "bChooseHealingTarget", propertyMap, "0");
			AddPropertyToMap(obj, "bDoShotsPerSecondBonusCap", propertyMap, "0");
			AddPropertyToMap(obj, "bUseAltProjectile", propertyMap, "0");
			AddPropertyToMap(obj, "bUseFixedShootSpeed", propertyMap, "0");
			AddPropertyToMap(obj, "bWeakenEnemyTarget", propertyMap, "0");

			// HeroEquipment_Familiar_TowerHealer
			AddPropertyToMap(obj, "bHealOverRadius", propertyMap, "0");

			// Build the familiar data struct (parses arrays via db.BuildArray in the ctor)
			HeroEquipment_Familiar_Data hef = new HeroEquipment_Familiar_Data(propertyMap, db);

			// Finally store it in your DB (adapt to your actual DB API)
			// If your DB doesn’t have this exact method name/signature, mirror AddHeroEquipment’s pattern.
			return db.AddHeroEquipmentFamiliar(ref hef);
		}



		public int AddPlayerToDB(UObject obj, ExportedTemplateDatabase db)
		{
			Dictionary<string, string> propertyMap = new Dictionary<string, string>();
			AddAnimationPropertiesToMap(obj, propertyMap);
			// Floats
			AddPropertyToMap(obj, "AdditionalSpeedMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "ExtraPlayerDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "HeroBonusPetDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "HeroBoostSpeedMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "NightmareModePlayerHealthMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "PlayerWeaponDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "StatExpFull_HeroCastingRate", propertyMap, "0.0");
			AddPropertyToMap(obj, "StatExpInitial_HeroCastingRate", propertyMap, "0.0");
			AddPropertyToMap(obj, "StatMultFull_HeroCastingRate", propertyMap, "0.0");
			AddPropertyToMap(obj, "StatMultInitial_HeroCastingRate", propertyMap, "0.0");
			AddPropertyToMap(obj, "AnimSpeedMultiplier", propertyMap, "0.0");

			// Arrays
			AddArrayPropertyToMap(obj, "MeleeSwingInfoMultipliers", propertyMap);
			AddArrayPropertyToMap(obj, "MainHandSwingInfoMultipliers", propertyMap);
			AddArrayPropertyToMap(obj, "OffHandSwingInfoMultipliers", propertyMap);

			// DunDefPawn float
			AddPropertyToMap(obj, "DamageMultiplierAdditional", propertyMap, "0.0");

			// Ints
			AddPropertyToMap(obj, "HeroBoostHealAmount", propertyMap, "0");

			propertyMap["Template"] = obj.GetPath();
			propertyMap["Class"] = (obj.Class?.Name?.Name ?? "");
			// Build data + store
			DunDefPlayer_Data ddp = new DunDefPlayer_Data(propertyMap, db);

			// Adapt this to your DB API (mirrors your AddHeroEquipment pattern)
			return db.AddDunDefPlayer(obj.GetPath(), (obj.Class?.Name?.Name ?? ""), ref ddp);
		}

		public int AddProjectileToDB(UObject obj, ExportedTemplateDatabase db)
		{
			Dictionary<string, string> propertyMap = new Dictionary<string, string>();

			// Arrays
			AddArrayPropertyToMap(obj, "RandomDamageTypes", propertyMap);


			AddPropertyToMap(obj, "bSecondScaleDamageStatOnAdditionalDamage", propertyMap, "0");
			AddPropertyToMap(obj, "bSecondScaleDamageStatType", propertyMap, "0");
			AddPropertyToMap(obj, "SecondScaleDamageStatType", propertyMap, "0");
		// Ints
			AddPropertyToMap(obj, "AdditionalDamageAmount", propertyMap, "0");
			AddPropertyToMap(obj, "AdditionalDamageType", propertyMap, "0");
			AddPropertyToMap(obj, "ScaleDamageStatType", propertyMap, "0");
			AddPropertyToMap(obj, "ProjDamageType", propertyMap, "0");
			AddPropertyToMap(obj, "NumAllowedPassThrough", propertyMap, "0");

			// Floats
			AddPropertyToMap(obj, "DamageRadiusFallOffExponent", propertyMap, "0.0");
			AddPropertyToMap(obj, "ScaleDamageStatExponent", propertyMap, "0.0");
			AddPropertyToMap(obj, "ProjDamage", propertyMap, "0.0");
			AddPropertyToMap(obj, "ProjDamageRadius", propertyMap, "0.0");
			AddPropertyToMap(obj, "ProjectileDamageByWeaponDamageDivider", propertyMap, "0.0");
			AddPropertyToMap(obj, "ProjectileDamagePerDistanceTravelled", propertyMap, "0.0");
			AddPropertyToMap(obj, "ProjectileLifespan", propertyMap, "0.0");
			AddPropertyToMap(obj, "ProjectileMaxSpeed", propertyMap, "0.0");
			AddPropertyToMap(obj, "ProjectileSpeed", propertyMap, "0.0");

			// Floats (homing / extra)
			AddPropertyToMap(obj, "TowerDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "HomingInterpSpeed", propertyMap, "0.0");

			// Bytes / bool-ish flags (stored as byte in struct)
			AddPropertyToMap(obj, "MultiplyProjectileDamageByPrimaryWeaponSwingSpeed", propertyMap, "0");
			AddPropertyToMap(obj, "MultiplyProjectileDamageByWeaponDamage", propertyMap, "0");
			AddPropertyToMap(obj, "OnlyCollideWithIgnoreClasses", propertyMap, "0");
			AddPropertyToMap(obj, "ScaleHeroDamage", propertyMap, "0");
			AddPropertyToMap(obj, "bAlwaysUseRandomDamageType", propertyMap, "0");
			AddPropertyToMap(obj, "bApplyBuffsOnAoe", propertyMap, "0");
			AddPropertyToMap(obj, "bReplicateWeaponProjectile", propertyMap, "0");
			AddPropertyToMap(obj, "bUseProjectilePerDistanceScaling", propertyMap, "0");
			AddPropertyToMap(obj, "bUseProjectilePerDistanceSizeScaling", propertyMap, "0");

			// Homing projectile flags
			AddPropertyToMap(obj, "bPierceEnemies", propertyMap, "0");
			AddPropertyToMap(obj, "bScaleDamagePerLevel", propertyMap, "0");			
			AddPropertyToMap(obj, "bDamageOnTouch", propertyMap, "0");

			AddPropertyToMap(obj, "FireDamageScale", propertyMap, "0.0");

			propertyMap["Template"] = obj.GetPath();
			propertyMap["Class"] = (obj.Class?.Name?.Name ?? "");
			// Build data + store
			DunDefProjectile_Data proj = new DunDefProjectile_Data(propertyMap, db);

			// Adapt this to your DB API (mirrors your AddHeroEquipment pattern)
			return db.AddDunDefProjectile(obj.GetPath(), (obj.Class?.Name?.Name ?? ""), ref proj);
		}

		public int AddWeaponToDB(UObject obj, ExportedTemplateDatabase db)
		{
			Dictionary<string, string> propertyMap = new Dictionary<string, string>();

			// Arrays
			AddArrayPropertyToMap(obj, "ExtraProjectileTemplates", propertyMap);
			AddArrayPropertyToMap(obj, "MeleeSwingInfos", propertyMap);
			AddArrayPropertyToMap(obj, "RainbowDamageTypeArrays", propertyMap);
			AddArrayPropertyToMap(obj, "RandomizedProjectileTemplate", propertyMap);
			
			// Ints (core)
			AddPropertyToMap(obj, "AdditionalDamageAmount", propertyMap, "0");
			AddPropertyToMap(obj, "AdditionalDamageType", propertyMap, "0");
			AddPropertyToMap(obj, "BaseAltDamage", propertyMap, "0");
			AddPropertyToMap(obj, "BaseDamage", propertyMap, "0");
			AddPropertyToMap(obj, "BaseShotsPerSecond", propertyMap, "0");
			AddPropertyToMap(obj, "BaseTotalAmmo", propertyMap, "0");			
			AddPropertyToMap(obj, "ProjectileTemplate", propertyMap, "0");

			// Floats (core)
			AddPropertyToMap(obj, "WeaponDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "WeaponSpeedMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "MinimumProjectileSpeed", propertyMap, "0.0");
			AddPropertyToMap(obj, "ProjectileSpeedAddition", propertyMap, "0.0");
			AddPropertyToMap(obj, "ProjectileSpeedBonusMultiplier", propertyMap, "0.0");

			// Bytes (core flags)
			AddPropertyToMap(obj, "bIsMeleeWeapon", propertyMap, "0");
			AddPropertyToMap(obj, "bRandomizeProjectileTemplate", propertyMap, "0");
			AddPropertyToMap(obj, "bUseAdditionalProjectileDamage", propertyMap, "0");
			AddPropertyToMap(obj, "bUseAltDamageForProjectileBaseDamage", propertyMap, "0");
			AddPropertyToMap(obj, "bUseDamageReductionForAbilities", propertyMap, "0");

			// DunDefWeapon_Crossbow
			AddPropertyToMap(obj, "BaseNumProjectiles", propertyMap, "0");
			AddPropertyToMap(obj, "BaseReloadSpeed", propertyMap, "0.0");
			AddPropertyToMap(obj, "ClipAmmo", propertyMap, "0");
			AddPropertyToMap(obj, "FireIntervalMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "bUseHighShotPerSecond", propertyMap, "0");

			// DunDefWeapon_MagicStaff
			AddPropertyToMap(obj, "AbilityCooldownTime", propertyMap, "0");
			AddPropertyToMap(obj, "BaseChargeSpeed", propertyMap, "0.0");
			AddPropertyToMap(obj, "BonusDamageMulti", propertyMap, "0.0");
			AddPropertyToMap(obj, "CooldownDuration", propertyMap, "0");
			AddPropertyToMap(obj, "ElementalDamageForRightClickScalar", propertyMap, "0.0");
			AddPropertyToMap(obj, "FullAltChargeTime", propertyMap, "0.0");
			AddPropertyToMap(obj, "FullChargeTime", propertyMap, "0.0");
			AddPropertyToMap(obj, "FullchargeRefireInterval", propertyMap, "0.0");
			AddPropertyToMap(obj, "MediumChargeFFThreshold", propertyMap, "0.0");
			AddPropertyToMap(obj, "NumProjectiles", propertyMap, "0");
			AddPropertyToMap(obj, "bIsRainMaker", propertyMap, "0");
			AddPropertyToMap(obj, "bEmberorMoon", propertyMap, "0");
			AddPropertyToMap(obj, "bUseAttackCD", propertyMap, "0");
			AddPropertyToMap(obj, "bUseElementalScallingForRightClick", propertyMap, "0");

			// DunDefWeapon_MagicStaff_Channeling
			AddPropertyToMap(obj, "ChannelingProjectileDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "ChannelingProjectileFireSpeed", propertyMap, "0.0");
			AddPropertyToMap(obj, "ChannelingProjectileTemplate", propertyMap, "0");
			AddPropertyToMap(obj, "ChannelingRangeMultiplier", propertyMap, "0.0");

			// DunDefWeapon_MeleeSword
			AddPropertyToMap(obj, "BaseMeleeDamageType", propertyMap, "0");
			AddPropertyToMap(obj, "DamageIncreaseForSwingSpeedFactor", propertyMap, "0.0");
			AddPropertyToMap(obj, "DamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "ExtraSpeedMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "IsSwingingWeapon", propertyMap, "0");
			AddPropertyToMap(obj, "MaxMomentumMultplierByDamage", propertyMap, "0.0");
			AddPropertyToMap(obj, "MaxTotalMomentumMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "MeleeDamageMomentum", propertyMap, "0.0");
			AddPropertyToMap(obj, "MinimumSwingDamageTime", propertyMap, "0.0");
			AddPropertyToMap(obj, "MinimumSwingTime", propertyMap, "0.0");
			AddPropertyToMap(obj, "MomentumMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "ProjectileDamageHeroStatExponentMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "SpeedMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "SpeedMultiplierDamageExponent", propertyMap, "0.0");
			AddPropertyToMap(obj, "WeakenEnemyTargetPercentage", propertyMap, "0.0");
			AddPropertyToMap(obj, "WeaponProjectileDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "bShootMeleeProjectile", propertyMap, "0");
			AddPropertyToMap(obj, "bUseRainbowDamageType", propertyMap, "0");
			AddPropertyToMap(obj, "bUseWeaponDamageForProjectileDamage", propertyMap, "0");
			AddPropertyToMap(obj, "BlockingMomentumExponent", propertyMap, "0.0");
			AddPropertyToMap(obj, "AdditionalMomentumExponent", propertyMap, "0.0");

			// DunDefWeapon_Minigun
			AddPropertyToMap(obj, "MinigunProjectileDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "SpeedPerDelta", propertyMap, "0.0");

			// DunDefWeapon_MonkSpear
			AddPropertyToMap(obj, "ShootInterval", propertyMap, "0.0");

			// DunDefWeapon_NessieLauncher
			AddPropertyToMap(obj, "Multiplier", propertyMap, "0.0");
			AddPropertyToMap(obj, "NessieCooldown", propertyMap, "0.0");

			propertyMap["Template"] = obj.GetPath();
			propertyMap["Class"] = (obj.Class?.Name?.Name ?? "");

			// Build data + store
			DunDefWeapon_Data weap = new DunDefWeapon_Data(propertyMap, db);

			// Adapt this to your DB API (mirrors your AddHeroEquipment pattern)
			return db.AddDunDefWeapon(obj.GetPath(), (obj.Class?.Name?.Name ?? ""), ref weap);
		}

		public int AddDunDefHeroToDB(UObject obj, ExportedTemplateDatabase db)
		{
			Dictionary<string, string> propertyMap = new Dictionary<string, string>();

			// Arrays
			AddArrayPropertyToMap(obj, "StatNames", propertyMap);
			AddArrayPropertyToMap(obj, "StatDescriptions", propertyMap);

			// Bytes (core flags)
			AddPropertyToMap(obj, "bIsMeleeHero", propertyMap, "0");

			// Ints (core)
			AddPropertyToMap(obj, "MyHeroType", propertyMap, "0");
			AddPropertyToMap(obj, "GivenCostumeString", propertyMap, "0");     // (string ref / id)

			
			AddArrayPropertyToMap(obj, "HeroCostumes", propertyMap);

			AddPropertyToMap(obj, "PlayerTemplate", propertyMap, "0");         // DunDefPlayer ref/id
			
			
			
			AddPropertyToMap(obj, "HeroClassDisplayName", propertyMap, "0");   // (string ref / id)
			AddPropertyToMap(obj, "HeroClassDescription", propertyMap, "0");   // (string ref / id)

			// Struct-ish / special (leave as string default; adjust if you store colors differently)
			AddPropertyToMap(obj, "ClassNameColor", propertyMap, "0");         // ULinear_Color

			// Floats (scaling)
			AddPropertyToMap(obj, "HeroDefenseAttackRateLinearFactor", propertyMap, "0.0");
			AddPropertyToMap(obj, "HeroDefenseAttackRateExponentialFactor", propertyMap, "0.0");
			AddPropertyToMap(obj, "HeroHealthExponentialFactor", propertyMap, "0.0");
			AddPropertyToMap(obj, "HeroHealthLinearFactor", propertyMap, "0.0");

			// HeroDamage
			AddPropertyToMap(obj, "StatExpFull_HeroDamage", propertyMap, "0.0");
			AddPropertyToMap(obj, "StatExpInitial_HeroDamage", propertyMap, "0.0");
			AddPropertyToMap(obj, "StatMultInitial_HeroDamage", propertyMap, "0.0");
			AddPropertyToMap(obj, "StatMultFull_HeroDamage", propertyMap, "0.0");

			// HeroSpeed
			AddPropertyToMap(obj, "StatMultInitial_HeroSpeed", propertyMap, "0.0");
			AddPropertyToMap(obj, "StatMultFull_HeroSpeed", propertyMap, "0.0");

			// HeroAbilityOne
			AddPropertyToMap(obj, "StatMultInitial_HeroAbilityOne", propertyMap, "0.0");
			AddPropertyToMap(obj, "StatExpInitial_HeroAbilityOne", propertyMap, "0.0");
			AddPropertyToMap(obj, "StatMultFull_HeroAbilityOne", propertyMap, "0.0");
			AddPropertyToMap(obj, "StatExptFull_HeroAbilityOne", propertyMap, "0.0");

			// HeroAbilityTwo
			AddPropertyToMap(obj, "StatMultInitial_HeroAbilityTwo", propertyMap, "0.0");
			AddPropertyToMap(obj, "StatExpInitial_HeroAbilityTwo", propertyMap, "0.0");
			AddPropertyToMap(obj, "StatMultFull_HeroAbilityTwo", propertyMap, "0.0");
			AddPropertyToMap(obj, "StatExptFull_HeroAbilityTwo", propertyMap, "0.0"); 

			// DefenseHealth
			AddPropertyToMap(obj, "StatMultInitial_DefenseHealth", propertyMap, "0.0");
			AddPropertyToMap(obj, "StatExpInitial_DefenseHealth", propertyMap, "0.0");
			AddPropertyToMap(obj, "StatMultFull_DefenseHealth", propertyMap, "0.0");
			AddPropertyToMap(obj, "StatExptFull_DefenseHealth", propertyMap, "0.0");

			// DefenseDamage
			AddPropertyToMap(obj, "StatMultInitial_DefenseDamage", propertyMap, "0.0");
			AddPropertyToMap(obj, "StatExpInitial_DefenseDamage", propertyMap, "0.0");
			AddPropertyToMap(obj, "StatMultFull_DefenseDamage", propertyMap, "0.0");
			AddPropertyToMap(obj, "StatExptFull_DefenseDamage", propertyMap, "0.0"); 

			// DefenseAttackRate
			AddPropertyToMap(obj, "StatMultInitial_DefenseAttackRate", propertyMap, "0.0");
			AddPropertyToMap(obj, "StatExpInitial_DefenseAttackRate", propertyMap, "0.0");

			// DefenseAOE
			AddPropertyToMap(obj, "StatMultInitial_DefenseAOE", propertyMap, "0.0");
			AddPropertyToMap(obj, "StatExpInitial_DefenseAOE", propertyMap, "0.0");
			AddPropertyToMap(obj, "StatMultFull_DefenseAOE", propertyMap, "0.0");
			AddPropertyToMap(obj, "StatExptFull_DefenseAOE", propertyMap, "0.0"); 
			AddPropertyToMap(obj, "StatBoostCapInitial_HeroDamage", propertyMap, "0.0");

			propertyMap["Template"] = obj.GetPath();
			propertyMap["Class"] = (obj.Class?.Name?.Name ?? "");
			// Build data + store
			DunDefHero_Data hero = new DunDefHero_Data(propertyMap, db, AnimationDurations);

			// Adapt this to your DB API (mirrors your AddHeroEquipment pattern)
			return db.AddDunDefHero(obj.GetPath(), (obj.Class?.Name?.Name ?? ""), ref hero);
		}

		// Cache for animation durations
		private Dictionary<string, Dictionary<string, float>> AnimationDurations = new(StringComparer.OrdinalIgnoreCase);

		public class AnimSequenceData
		{
			public string AnimName { get; set; }
			public float SequenceLength { get; set; }
			public int NumFrames { get; set; }
			public float RateScale { get; set; }

			public AnimSequenceData(string name, float length, int frames, float rate)
			{
				AnimName = name;
				SequenceLength = length;
				NumFrames = frames;
				RateScale = rate;
			}
		}
		string RemoveAfterLastDot(string input)
		{
			int lastDot = input.LastIndexOf('.');
			return lastDot >= 0 ? input.Substring(0, lastDot) : input;
		}

		public void LoadAnimationsFromPackage(string upkName)
		{
			if (!PackageCache.TryGetValue(upkName, out var package))
			{
				MainWindow.Log($"Package {upkName} not found in cache");
				return;
			}

			foreach (var export in package.Exports)
			{
				// Check if this export is an AnimSequence
				if (export.Class?.ObjectName.Name == "AnimSequence")
				{
					var animObj = export.Object;
					animObj?.Load();



					if (animObj != null)
					{
						var props = GetMergedProperties(animObj);

						//string animName = export.GetPath();
						float sequenceLength = 0.0f;
						int numFrames = 0;
						float rateScale = 30.0f; // Default FPS

						props.TryGetValue("SequenceName", out var sequenceName);
						string animName = sequenceName?.Value ?? "";

						// Extract properties
						if (props.TryGetValue("SequenceLength", out var lengthProp))
						{
							float.TryParse(lengthProp.Value?.ToString() ?? "0", out sequenceLength);
						}

						if (props.TryGetValue("NumFrames", out var framesProp))
						{
							int.TryParse(framesProp.Value?.ToString() ?? "0", out numFrames);
						}

						float rate = 1.0f;

						if (props.TryGetValue("RateScale", out var rateProp))
						{
							if (float.TryParse(rateProp.Value?.ToString() ?? "1.0", out rateScale))
							{
								rate = rateScale;
							}

						}
						
						string animObjName = RemoveAfterLastDot(export.GetPath());
						// Store in cache
						if (!AnimationDurations.ContainsKey(animObjName))
						{
							AnimationDurations.Add(animObjName, new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase));
						}
						AnimationDurations[animObjName][animName.Replace("\"", "")] = sequenceLength / rateScale;

						//MainWindow.Log($"{export.GetPath()}\t{animName}\t{sequenceLength}\t{numFrames}\t{rateScale}");
					}
				}
			}
		}
/*		public float GetAnimationDuration(string animName)
		{
			if (AnimationDurations.TryGetValue(animName, out float duration))
			{
				return duration;
			}

			// Fallback: return default duration
			MainWindow.Log($"Warning: Animation '{animName}' not found, using default 0.8s");
			return 0.8f;
		}*/
	

		public void AddAnimationPropertiesToMap(UObject playerTemplate, Dictionary<string,string> propertyMap)
		{
			playerTemplate.Load();
			float meleeAttack1_large = 1.0f;
			float meleeAttack2_large = 1.0f;
			float meleeAttack3_large = 0.5f;
			
			float meleeAttack1_medium = 0.7f;
			float meleeAttack2_medium = 0.533f;
			float meleeAttack3_medium = 1.033f;


			var meshProp = GetProperty(playerTemplate, "Mesh");

			if (meshProp != null)
			{

				var meshComp = FindObjectByPath(playerTemplate.Package, playerTemplate.GetPath() + "." + meshProp.Value);
				if (meshComp != null)
				{

					var props = GetMergedProperties(meshComp);

					if (props.ContainsKey("AnimSets"))
					{
						var animSet = props["AnimSets"];

						int start = animSet.Value.IndexOf('\'') + 1;
						int end = animSet.Value.IndexOf('\'', start);
						string path = animSet.Value.Substring(start, end - start);

						if (AnimationDurations[path].ContainsKey("meleeattack1_large")) meleeAttack1_large = AnimationDurations[path]["meleeattack1_large"];
						if (AnimationDurations[path].ContainsKey("meleeattack2_large")) meleeAttack2_large = AnimationDurations[path]["meleeattack2_large"];
						if (AnimationDurations[path].ContainsKey("meleeattack3_large")) meleeAttack3_large = AnimationDurations[path]["meleeattack3_large"];
						if (AnimationDurations[path].ContainsKey("meleeattack1_medium")) meleeAttack1_medium = AnimationDurations[path]["meleeattack1_medium"];
						if (AnimationDurations[path].ContainsKey("meleeattack2_medium")) meleeAttack2_medium = AnimationDurations[path]["meleeattack2_medium"];
						if (AnimationDurations[path].ContainsKey("meleeattack3_medium")) meleeAttack3_medium = AnimationDurations[path]["meleeattack3_medium"];
					}
				}
			}

			propertyMap.Add("MeleeAttack1LargeAnimDuration", meleeAttack1_large.ToString());
			propertyMap.Add("MeleeAttack2LargeAnimDuration", meleeAttack2_large.ToString());
			propertyMap.Add("MeleeAttack3LargeAnimDuration", meleeAttack3_large.ToString());
			propertyMap.Add("MeleeAttack1MediumAnimDuration", meleeAttack1_medium.ToString());
			propertyMap.Add("MeleeAttack2MediumAnimDuration", meleeAttack2_medium.ToString());
			propertyMap.Add("MeleeAttack3MediumAnimDuration", meleeAttack3_medium.ToString());

		}		

	}
}