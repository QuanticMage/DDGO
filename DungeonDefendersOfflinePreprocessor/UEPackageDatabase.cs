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
using System.Windows.Markup;
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

	/// <summary>
	/// Pre-builds the full default chain (base class → derived → archetype → instance) for a UObject
	/// once, then serves both scalar and array property lookups as O(1) dictionary hits.
	/// This eliminates repeated chain walks when extracting many properties from the same object.
	/// </summary>
	public class PreloadedProperties
	{
		private readonly Dictionary<string, UDefaultProperty> _scalarProps;
		private readonly List<UObject> _chain;

		public PreloadedProperties(List<UObject> chain)
		{
			_chain = chain;
			_scalarProps = new Dictionary<string, UDefaultProperty>();
			foreach (var o in chain)
			{
				o.Load();
				if (o.Properties == null) continue;
				foreach (var p in o.Properties)
					_scalarProps[p.Name] = p; // later (more derived) overrides earlier
			}
		}

		public UDefaultProperty? GetProperty(string name)
		{
			_scalarProps.TryGetValue(name, out var prop);
			return prop;
		}

		public IReadOnlyList<UDefaultProperty> GetArrayProperty(string name)
		{
			var mergedByIndex = new SortedDictionary<int, UDefaultProperty>();
			foreach (var o in _chain)
			{
				if (o.Properties == null) continue;
				foreach (var p in o.Properties)
				{
					if (p == null || p.Name != name) continue;
					mergedByIndex[p.ArrayIndex] = p;
				}
			}
			return mergedByIndex.Values.ToList();
		}
	}

	public class UEPackageDatabase
	{

		// dev request
		List<string> SkipNames = new()
		{

		};


		private Dictionary<string, UnrealPackage> PackageCache = new(StringComparer.OrdinalIgnoreCase);
		private Dictionary<string, Dictionary<string, UExportTableItem>> ExportDatabase = new();
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

			package.InitializeExportObjects();			

			var dict = new Dictionary<string, UExportTableItem>();
			ExportDatabase[package.PackageName] = dict;
			// load all objects
			foreach ( var v in package.Exports )
			{
				if (v.Owner == null) continue;

				var obj = v.Object;
				if (obj == null) continue;
				if (obj.ExportTable == null) continue;
				obj.Load();
				string s = obj.GetReferencePath().ToLowerInvariant();
				if (s.Contains("'")) s = s.Split('\'')[1]; // strip Type'...' prefix
				dict[s] = v;
			}


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
			string cleanPath = fullPath;
			if (cleanPath.Contains("'"))
				cleanPath = cleanPath.Split('\'')[1];
			cleanPath = cleanPath.ToLowerInvariant();

			if (ExportDatabase.TryGetValue(package.PackageName, out var dict) &&
				dict.TryGetValue(cleanPath, out var export))
			{
				export.Object?.Load();
				return export.Object;
			}

			return null;
		}

		private void DumpAllChildObjects( UObject obj )
		{
			foreach ( var v in obj.Package.Exports)
			{
				if (v.GetPath().Contains(obj.GetPath()))
				{
					MainWindow.Log("Found child: " + v.GetPath());
				}
			}
		}

		private UObject? FindChildSkeletalMeshComponent( UObject obj )
		{
			if (!ExportDatabase.TryGetValue(obj.Package.PackageName, out var dict))
			{
				MainWindow.Log($"[FindChildSkeletalMeshComponent] Package '{obj.Package.PackageName}' not in ExportDatabase for '{obj.GetPath()}'");
				return null;
			}

			string prefix = (obj.GetPath() + ".SkeletalMeshComponent").ToLowerInvariant();
			foreach (var kvp in dict)
			{
				if (kvp.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
					return kvp.Value.Object;
			}

			MainWindow.Log($"[FindChildSkeletalMeshComponent] No SkeletalMeshComponent found under '{obj.GetPath()}'");
			return null;
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

		public PreloadedProperties PreloadProperties(UObject obj)
		{
			return new PreloadedProperties(BuildDefaultChainBaseFirst(obj));
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
						if (matInst == null) continue;
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

							string iconImagePath = iconImage.GetReferencePath();

							if (key == "EquipmentIconColorLayers")
							{
								// Special Handling: Split Red and Green channels into two distinct atlas entries
								string rKey = $"{iconImagePath}_R";
								string gKey = $"{iconImagePath}_G";

								ir.IconMask1 = rKey;
								ir.IconMask2 = gKey;
								if (!uniqueIcons.ContainsKey(rKey))
									uniqueIcons.Add(rKey, ExtractChannelAsGrayscale(finalPath, 'R'));

								if (!uniqueIcons.ContainsKey(gKey))
									uniqueIcons.Add(gKey, ExtractChannelAsGrayscale(finalPath, 'G'));
							}
							else
							{
								ir.IconBase = iconImagePath;
								// Standard icon handling
								if (!uniqueIcons.ContainsKey(iconImagePath))
								{
									uniqueIcons.Add(iconImagePath, File.ReadAllBytes(finalPath));
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
			// Single pass: classify all objects into buckets instead of 8 separate full scans
			var damageTypes = new List<UObject>();
			var players = new List<UObject>();
			var heroes = new List<UObject>();
			var projectiles = new List<UObject>();
			var weapons = new List<UObject>();
			var enemies = new List<UObject>();
			var equipment = new List<UObject>();
			var giveEquipment = new List<UObject>();

			foreach (var item in PackageCache.Values)
			{
				foreach (var obj in item.Objects)
				{
					var refPath = obj.GetReferencePath();
					if (refPath.StartsWith("DunDefDamageType")) damageTypes.Add(obj);
					else if (refPath.StartsWith("DunDefPlayer")) players.Add(obj);
					else if (refPath.StartsWith("DunDefHero")) heroes.Add(obj);
					else if (refPath.StartsWith("DunDefProjectile")) projectiles.Add(obj);
					else if (refPath.StartsWith("DunDefWeapon")) weapons.Add(obj);
					else if (refPath.StartsWith("DunDefEnemy")) enemies.Add(obj);
					else if (refPath.StartsWith("HeroEquipment")) equipment.Add(obj);

					if (DoesObjectInheritFromClass(obj, "DunDef_SeqAct_GiveEquipmentToPlayers"))
						giveEquipment.Add(obj);
				}
			}

			// Process in dependency order
			foreach (var obj in damageTypes)
			{
				MainWindow.Log($"Adding DunDefDamageType {obj.GetPath()}");
				AddDunDefDamageTypeToDB(obj, db);
				await Task.Yield();
			}
			foreach (var obj in players)
			{
				MainWindow.Log($"Adding DunDefPlayer {obj.GetPath()}");
				AddPlayerToDB(obj, db);
				await Task.Yield();
			}
			foreach (var obj in heroes)
			{
				MainWindow.Log($"Adding DunDefHero {obj.GetPath()}");
				AddDunDefHeroToDB(obj, db);
				await Task.Yield();
			}
			foreach (var obj in projectiles)
			{
				MainWindow.Log($"Adding DunDefProjectile {obj.GetPath()}");
				AddProjectileToDB(obj, db);
				await Task.Yield();
			}
			foreach (var obj in weapons)
			{
				MainWindow.Log($"Adding DunDefWeapon {obj.GetPath()}");
				AddWeaponToDB(obj, db);
				await Task.Yield();
			}
			// DunDefEnemy pass (must run before HeroEquipment so enemy drop entry indices are valid)
			foreach (var obj in enemies)
			{
				MainWindow.Log($"Adding DunDefEnemy {obj.GetPath()}");
				AddEnemyToDB(obj, db);
				await Task.Yield();
			}
			foreach (var obj in equipment)
			{
				MainWindow.Log($"Adding HeroEquipment {obj.GetPath()}");
				AddEquipmentToDB(obj, db);
				await Task.Yield();
			}
			// DunDef_SeqAct_GiveEquipmentToPlayers pass (after HeroEquipment so indices resolve)
			foreach (var obj in giveEquipment)
			{
				MainWindow.Log($"Adding GiveEquipmentToPlayers {obj.GetPath()}");
				AddGiveEquipmentToPlayersToDB(obj, db);
				await Task.Yield();
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

		// Fast overload using pre-built property cache (no chain walk per call)
		public static void AddPropertyToMap(PreloadedProperties preloaded, string property, Dictionary<string, string> map, string? def = null)
		{
			UDefaultProperty? prop = preloaded.GetProperty(property);
			if (prop != null)
			{
				map[property] = prop.Value;
			}
			else if (def != null)
			{
				map[property] = def;
			}
		}

		// Fast overload using pre-built property cache (no chain walk per call)
		public static void AddArrayPropertyToMap(PreloadedProperties preloaded, string propertyName, Dictionary<string, string> map, int forcedLength = -1, string defaultValue = "0")
		{
			var sparse = preloaded.GetArrayProperty(propertyName);

			if ((sparse.Count == 1) && (sparse[0].Value.Contains("[")))
			{
				map[propertyName] = sparse[0].Value;
				return;
			}

			var byIndex = new Dictionary<int, string>(sparse.Count);
			var maxIndex = -1;

			foreach (var p in sparse)
			{
				int idx = p.ArrayIndex;
				if (idx > maxIndex) maxIndex = idx;
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
			var pl = PreloadProperties(obj);

			// Arrays (materialized)
			AddArrayPropertyToMap(pl, "StatModifiers", propertyMap, 10, "0");
			AddArrayPropertyToMap(pl, "DamageReductions", propertyMap);
			AddArrayPropertyToMap(pl, "DamageReductionRandomizers", propertyMap);
			AddArrayPropertyToMap(pl, "QualityDescriptorNames", propertyMap);
			AddArrayPropertyToMap(pl, "QualityDescriptorRealNames", propertyMap);
			AddArrayPropertyToMap(pl, "RandomBaseNames", propertyMap);
			AddArrayPropertyToMap(pl, "StatEquipmentIDs", propertyMap, 10, "0");
			AddArrayPropertyToMap(pl, "StatEquipmentTiers", propertyMap, 10, "0");
			AddArrayPropertyToMap(pl, "StatModifierRandomizers", propertyMap, 11, "0");
			AddArrayPropertyToMap(pl, "StatObjectArray", propertyMap);

			// Icon / color set arrays
			AddArrayPropertyToMap(pl, "PrimaryColorSets", propertyMap);
			AddArrayPropertyToMap(pl, "SecondaryColorSets", propertyMap);

			// Localized/string-ish fields
			AddPropertyToMap(pl, "AdditionalDescription", propertyMap, "0");
			AddPropertyToMap(pl, "BaseForgerName", propertyMap, "0");
			AddPropertyToMap(pl, "DamageDescription", propertyMap, "0");
			AddPropertyToMap(pl, "Description", propertyMap, "0");
			AddPropertyToMap(pl, "EquipmentName", propertyMap, "0");
			AddPropertyToMap(pl, "ExtraQualityUpgradeDamageNumberDescriptor", propertyMap, "0");
			AddPropertyToMap(pl, "ForgedByDescription", propertyMap, "0");
			AddPropertyToMap(pl, "LevelString", propertyMap, "0");
			AddPropertyToMap(pl, "Name", propertyMap, "0");
			AddPropertyToMap(pl, "RequiredClassString", propertyMap, "0");
			AddPropertyToMap(pl, "UserEquipmentName", propertyMap, "0");
			AddPropertyToMap(pl, "UserForgerName", propertyMap, "0");

			// Ints
			AddPropertyToMap(pl, "EquipmentID1", propertyMap, "0");
			AddPropertyToMap(pl, "EquipmentID2", propertyMap, "0");
			AddPropertyToMap(pl, "EquipmentSetID", propertyMap, "0");
			AddPropertyToMap(pl, "EquipmentTemplate", propertyMap, "0");
			AddPropertyToMap(pl, "EquipmentWeaponTemplate", propertyMap, "0");
			AddPropertyToMap(pl, "HeroStatUpgradeLimit", propertyMap, "0");
			AddPropertyToMap(pl, "Level", propertyMap, "0");
			AddPropertyToMap(pl, "LevelRequirementIndex", propertyMap, "0");
			AddPropertyToMap(pl, "MaxEquipmentLevel", propertyMap, "0");
			AddPropertyToMap(pl, "MaxEquipmentLevelRandomizer", propertyMap, "0");
			AddPropertyToMap(pl, "MaxHeroStatValue", propertyMap, "0");
			AddPropertyToMap(pl, "MaxLevel", propertyMap, "0");
			AddPropertyToMap(pl, "MaxNonTranscendentStatRollValue", propertyMap, "0");
			AddPropertyToMap(pl, "MaxUpgradeableSpeedOfProjectilesBonus", propertyMap, "0");
			AddPropertyToMap(pl, "MinDamageBonus", propertyMap, "0");
			AddPropertyToMap(pl, "MinLevel", propertyMap, "0");
			AddPropertyToMap(pl, "MinSupremeLevel", propertyMap, "0");
			AddPropertyToMap(pl, "MinTranscendentLevel", propertyMap, "0");
			AddPropertyToMap(pl, "MinUltimateLevel", propertyMap, "0");
			AddPropertyToMap(pl, "MinimumSellWorth", propertyMap, "0");
			AddPropertyToMap(pl, "StoredMana", propertyMap, "0");
			AddPropertyToMap(pl, "UltimateMaxHeroStatValue", propertyMap, "0");
			AddPropertyToMap(pl, "UltimatePlusMaxHeroStatValue", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponAdditionalDamageAmount", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponAdditionalDamageAmountRandomizer", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponAdditionalDamageType", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponAltDamageBonus", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponAltDamageBonusRandomizer", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponBlockingBonusRandomizer", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponChargeSpeedBonusRandomizer", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponClipAmmoBonus", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponClipAmmoBonusRandomizer", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponDamageBonus", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponDamageBonusRandomizer", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponDamageDisplayValueScale", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponAltDamageDisplayValueScale", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponKnockbackBonusRandomizer", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponKnockbackMax", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponNumberOfProjectilesBonusRandomizer", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponNumberOfProjectilesQualityBaseline", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponReloadSpeedBonusRandomizer", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponShotsPerSecondBonusRandomizer", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponSpeedOfProjectilesBonus", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponSpeedOfProjectilesBonusRandomizer", propertyMap, "0");
			AddPropertyToMap(pl, "weaponType", propertyMap, "0");


			// Floats
			AddPropertyToMap(pl, "AdditionalWeaponDamageBonusRandomizerMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "AltDamageIncreasePerLevelMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "AltDamageRandomizerMult", propertyMap, "0.0");
			AddPropertyToMap(pl, "AltMaxDamageIncreasePerLevel", propertyMap, "0.0");
			AddPropertyToMap(pl, "DamageIncreasePerLevelMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "ElementalDamageIncreasePerLevelMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "ElementalDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "ExtraQualityDamageIncreasePerLevelMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "ExtraQualityMaxDamageIncreasePerLevel", propertyMap, "0.0");
			AddPropertyToMap(pl, "FullEquipmentSetStatMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "HighLevelManaCostPerLevelExponentialFactorAdditional", propertyMap, "0.0");
			AddPropertyToMap(pl, "HighLevelManaCostPerLevelMaxQualityMultiplierAdditional", propertyMap, "0.0");
			AddPropertyToMap(pl, "HighLevelRequirementRatingThreshold", propertyMap, "0.0");
			AddPropertyToMap(pl, "HighLevelThreshold", propertyMap, "0.0");
			AddPropertyToMap(pl, "MaxDamageIncreasePerLevel", propertyMap, "0.0");
			AddPropertyToMap(pl, "MaxRandomValue", propertyMap, "0.0");
			AddPropertyToMap(pl, "MaxRandomValueNegative", propertyMap, "0.0");
			AddPropertyToMap(pl, "MinElementalDamageIncreasePerLevel", propertyMap, "0");
			AddPropertyToMap(pl, "MaxElementalDamageIncreasePerLevel", propertyMap, "400");
			AddPropertyToMap(pl, "MinEquipmentLevels", propertyMap, "0.0");
			AddPropertyToMap(pl, "MinimumPercentageValue", propertyMap, "0.0");
			AddPropertyToMap(pl, "MythicalFullEquipmentSetStatMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "NegativeMinimumPercentageValue", propertyMap, "0.0");
			AddPropertyToMap(pl, "NegativeThresholdQualityPecentMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "PlayerSpeedMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "QualityThreshold", propertyMap, "0.0");
			AddPropertyToMap(pl, "RandomNegativeThreshold", propertyMap, "0.0");
			AddPropertyToMap(pl, "RandomPower", propertyMap, "0.0");
			AddPropertyToMap(pl, "RandomPowerOverrideIfNegative", propertyMap, "0.0");
			AddPropertyToMap(pl, "RandomizerQualityMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "RandomizerStatModifierGoNegativeChance", propertyMap, "0.0");
			AddPropertyToMap(pl, "RandomizerStatModifierGoNegativeMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "SecondExtraQualityDamageIncreasePerLevelMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "SecondExtraQualityMaxDamageIncreasePerLevel", propertyMap, "0.0");
			AddPropertyToMap(pl, "StackedStatModifier", propertyMap, "0.0");
			AddPropertyToMap(pl, "SupremeFullEquipmentSetStatMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "SupremeLevelBoostAmount", propertyMap, "0.0");
			AddPropertyToMap(pl, "SupremeLevelBoostRandomizerPower", propertyMap, "0.0");
			AddPropertyToMap(pl, "TotalRandomizerMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "TranscendentFullEquipmentSetStatMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "TranscendentLevelBoostAmount", propertyMap, "0.0");
			AddPropertyToMap(pl, "TranscendentLevelBoostRandomizerPower", propertyMap, "0.0");
			AddPropertyToMap(pl, "Ultimate93Chance", propertyMap, "0.0");
			AddPropertyToMap(pl, "UltimateDamageIncreasePerLevelMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "UltimateFullEquipmentSetStatMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "UltimateLevelBoostAmount", propertyMap, "0.0");
			AddPropertyToMap(pl, "UltimateLevelBoostRandomizerPower", propertyMap, "0.0");
			AddPropertyToMap(pl, "UltimateMaxDamageIncreasePerLevel", propertyMap, "0.0");
			AddPropertyToMap(pl, "UltimatePlusChance", propertyMap, "0.0");
			AddPropertyToMap(pl, "UltimatePlusPlusChance", propertyMap, "0.0");
			AddPropertyToMap(pl, "Values", propertyMap, "0.0");
			AddPropertyToMap(pl, "WeaponAltDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "WeaponDamageBonusRandomizerMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "WeaponDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "WeaponEquipmentRatingPercentBase", propertyMap, "0.0");
			AddPropertyToMap(pl, "WeaponSwingSpeedMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "RuthlessUltimate93Chance", propertyMap, "0.0");
			AddPropertyToMap(pl, "RuthlessUltimatePlusChance", propertyMap, "0.0");
			AddPropertyToMap(pl, "RuthlessUltimatePlusPlusChance", propertyMap, "0.0");

			// Bytes / bool-ish flags
			AddPropertyToMap(pl, "AllowNameRandomization", propertyMap, "0");
			AddPropertyToMap(pl, "CountsForAllArmorSets", propertyMap, "0");
			AddPropertyToMap(pl, "NameIndex_Base", propertyMap, "0");
			AddPropertyToMap(pl, "NameIndex_DamageReduction", propertyMap, "0");
			AddPropertyToMap(pl, "NameIndex_QualityDescriptor", propertyMap, "0");
			AddPropertyToMap(pl, "OnlyRandomizeBaseName", propertyMap, "0");

			AddPropertyToMap(pl, "WeaponAdditionalDamageTypeNotPoison", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponBlockingBonus", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponChargeSpeedBonus", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponKnockbackBonus", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponNumberOfProjectilesBonus", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponReloadSpeedBonus", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponShotsPerSecondBonus", propertyMap, "0");

			AddPropertyToMap(pl, "bCanBeEquipped", propertyMap, "0");
			AddPropertyToMap(pl, "bCantBeDropped", propertyMap, "0");
			AddPropertyToMap(pl, "bCantBeSold", propertyMap, "0");
			AddPropertyToMap(pl, "bDisableRandomization", propertyMap, "0");
			AddPropertyToMap(pl, "bEquipmentFeatureByte1", propertyMap, "0");
			AddPropertyToMap(pl, "bEquipmentFeatureByte2", propertyMap, "0");
			AddPropertyToMap(pl, "bForceAllowDropping", propertyMap, "0");
			AddPropertyToMap(pl, "bForceAllowSelling", propertyMap, "0");
			AddPropertyToMap(pl, "bForceRandomizerWithMinEquipmentLevel", propertyMap, "0");
			AddPropertyToMap(pl, "bHideQualityDescriptors", propertyMap, "0");
			AddPropertyToMap(pl, "bIsConsumable", propertyMap, "0");
			AddPropertyToMap(pl, "bIsSecondary", propertyMap, "0");
			AddPropertyToMap(pl, "bNoNegativeRandomizations", propertyMap, "0");
			AddPropertyToMap(pl, "bUseBonusStatsFromStacking", propertyMap, "0");
			AddPropertyToMap(pl, "bUseExtraQualityDamage", propertyMap, "0");
			AddPropertyToMap(pl, "bUseSecondExtraQualityDamage", propertyMap, "0");
			AddPropertyToMap(pl, "UseColorSets", propertyMap, "0");

			// Native-ish / icon section
			AddPropertyToMap(pl, "EquipmentDescription", propertyMap, "0");
			AddPropertyToMap(pl, "EquipmentType", propertyMap, "0");
			AddPropertyToMap(pl, "ForDamageType", propertyMap, "0");
			AddPropertyToMap(pl, "MaxRandomElementalDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "MyRating", propertyMap, "0.0");
			AddPropertyToMap(pl, "MyRatingPercent", propertyMap, "0.0");
			AddPropertyToMap(pl, "PercentageReduction", propertyMap, "0");
			AddPropertyToMap(pl, "UserID", propertyMap, "0");

			AddPropertyToMap(pl, "WeaponAltDamageBonusUse", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponBlockingBonusUse", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponChargeSpeedBonusUse", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponClipAmmoBonusUse", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponKnockbackBonusUse", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponReloadSpeedBonusUse", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponShotsPerSecondBonusUse", propertyMap, "0");
			AddPropertyToMap(pl, "bDisableTheRandomization", propertyMap, "0");
			AddPropertyToMap(pl, "bForceUseParentTemplate", propertyMap, "0");
			AddPropertyToMap(pl, "UseWeaponCoreStats", propertyMap, "0");
			AddPropertyToMap(pl, "bForceToMinElementalScale", propertyMap, "0");
			AddPropertyToMap(pl, "bForceToMaxElementalScale", propertyMap, "0");

			// Sell worth
			AddPropertyToMap(pl, "SellWorthLinearFactor", propertyMap, "0.0");
			AddPropertyToMap(pl, "SellWorthExponentialFactor", propertyMap, "0.0");
			AddPropertyToMap(pl, "SellWorthMin", propertyMap, "0.0");
			AddPropertyToMap(pl, "SellWorthMax", propertyMap, "0.0");
			AddPropertyToMap(pl, "SellRatingExponent", propertyMap, "0.0");
			AddPropertyToMap(pl, "SellWorthEquipmentRatingBase", propertyMap, "0.0");
			AddPropertyToMap(pl, "SellWorthMultiplierLevelBase", propertyMap, "0.0");
			AddPropertyToMap(pl, "SellWorthMultiplierLevelMin", propertyMap, "0.0");
			AddPropertyToMap(pl, "SellWorthMultiplierLevelMax", propertyMap, "0.0");
			AddPropertyToMap(pl, "HighResaleWorthPower", propertyMap, "0.0");

			// Shop sell worth
			AddPropertyToMap(pl, "ShopSellWorthLinearFactor", propertyMap, "0.0");
			AddPropertyToMap(pl, "ShopSellWorthExponentialFactor", propertyMap, "0.0");
			AddPropertyToMap(pl, "ShopSellWorthMin", propertyMap, "0.0");
			AddPropertyToMap(pl, "ShopSellWorthMax", propertyMap, "0.0");
			AddPropertyToMap(pl, "ShopSellRatingExponent", propertyMap, "0.0");
			AddPropertyToMap(pl, "ShopSellWorthEquipmentRatingBase", propertyMap, "0.0");
			AddPropertyToMap(pl, "ShopSellWorthWeaponMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "ShopSellWorthWeaponExponentialFactorMult", propertyMap, "0.0");
			AddPropertyToMap(pl, "MaxShopSellWorth", propertyMap, "0.0");
			AddPropertyToMap(pl, "ShopSellWorthMinWeaponMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "ShopSellWorthMaxWeaponMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "ShopSellWorthRatingWeaponMultiplier", propertyMap, "0.0");

			// Mana cost per level
			AddPropertyToMap(pl, "ManaCostPerLevelLinearFactor", propertyMap, "0.0");
			AddPropertyToMap(pl, "ManaCostPerLevelExponentialFactor", propertyMap, "0.0");
			AddPropertyToMap(pl, "ManaCostPerLevelMinQualityMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "ManaCostPerLevelMaxQualityMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "ManaCostPerLevelExponentialFactorAdditional", propertyMap, "0.0");
			AddPropertyToMap(pl, "ManaCostPerLevelMaxQualityMultiplierAdditional", propertyMap, "0.0");

			// Other new floats
			AddPropertyToMap(pl, "RatingPercentForLevelUpCostExponent", propertyMap, "0.0");
			AddPropertyToMap(pl, "WeaponDrawScaleGlobalMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "WeaponDrawScaleRandomizerExtraMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "IconScaleMultiplier", propertyMap, "0.0");

			// New int/byte fields
			AddPropertyToMap(pl, "DamageReductionUpgradeInterval", propertyMap, "0");
			AddPropertyToMap(pl, "bUseAlternateThreshold", propertyMap, "0");
			AddPropertyToMap(pl, "WeaponDrawScaleMultiplierRandomizer", propertyMap, "0");

			// New array fields
			AddArrayPropertyToMap(pl, "QualityShopCostMultipliers", propertyMap);
			AddArrayPropertyToMap(pl, "QualityShopCostCaps", propertyMap);
			AddArrayPropertyToMap(pl, "EquipLevelRequirements", propertyMap);
			AddArrayPropertyToMap(pl, "AccessoryRequirements", propertyMap);

			AddPropertyToMap(pl, "IconColorAddPrimary", propertyMap, "0");
			AddPropertyToMap(pl, "IconColorAddSecondary", propertyMap, "0");
			AddPropertyToMap(pl, "IconColorMultPrimary", propertyMap, "0.0");
			AddPropertyToMap(pl, "IconColorMultSecondary", propertyMap, "0.0");



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
			// Compute FamiliarDataIndex before creating hed so the constructor can read it
			if (obj.GetReferencePath().StartsWith("HeroEquipment_Familiar"))
			{
				propertyMap["FamiliarDataIndex"] = AddFamiliarToDB(obj, db).ToString();				
			}
			else
				propertyMap["FamiliarDataIndex"] = "-1";

			HeroEquipment_Data hed = new HeroEquipment_Data(propertyMap, db);
			return db.AddHeroEquipment(obj.GetPath(), (obj.Class?.Name?.Name ?? ""), ref hed);
		}

		public int AddDunDefDamageTypeToDB(UObject obj, ExportedTemplateDatabase db)
		{
			Dictionary<string, string> propertyMap = new Dictionary<string, string>();
			var pl = PreloadProperties(obj);
			AddPropertyToMap(pl, "AdjectiveName", propertyMap, "Default");
			AddPropertyToMap(pl, "FriendlyName", propertyMap, "Default");
			AddPropertyToMap(pl, "UseForNotPoisonElementalDamage", propertyMap, "false");
			AddPropertyToMap(pl, "UseForRandomElementalDamage", propertyMap, "false");
			AddPropertyToMap(pl, "DamageTypeArrayIndex", propertyMap, "-1");


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
			var pl = PreloadProperties(obj);

			AddFamiliarAnimationPropertiesToMap(obj, propertyMap);

			AddPropertyToMap(pl, "bDoFamiliarAbilities", propertyMap, "true");

			// Arrays (materialized)
			// HeroEquipment_Familiar_TowerDamageScaling
			AddArrayPropertyToMap(pl, "ProjectileDelays", propertyMap);
			AddArrayPropertyToMap(pl, "ProjectileTemplates", propertyMap);

			// Floats
			// HeroEquipment_Familiar_AoeBuffer
			AddPropertyToMap(pl, "BuffRange", propertyMap, "0.0");

			// HeroEquipment_Familiar_Corehealer
			AddPropertyToMap(pl, "HealAmountBase", propertyMap, "0.0");
			AddPropertyToMap(pl, "HealAmountExtraMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "HealAmountMaxPercent", propertyMap, "0.0");
			AddPropertyToMap(pl, "HealInterval", propertyMap, "0.0");
			AddPropertyToMap(pl, "HealRangeBase", propertyMap, "0.0");
			AddPropertyToMap(pl, "HealRangeStatBase", propertyMap, "0.0");
			AddPropertyToMap(pl, "HealRangeStatExponent", propertyMap, "0.0");
			AddPropertyToMap(pl, "HealRangeStatMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "MinimumCoreHealthPercent", propertyMap, "0.0");

			// HeroEquipment_Familiar_Melee_TowerScaling
			AddPropertyToMap(pl, "BaseDamageToHealRatio", propertyMap, "0.0");
			AddPropertyToMap(pl, "DamageHealMultiplierExponent", propertyMap, "0.0");
			AddPropertyToMap(pl, "ExtraNightmareMeleeDamageMultiplier", propertyMap, "1.3");
			AddPropertyToMap(pl, "MaxHealMultiplierExponent", propertyMap, "0.0");
			AddPropertyToMap(pl, "MaxHealPerDamage", propertyMap, "0.0");
			AddPropertyToMap(pl, "MaxKnockbackMuliplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "MeleeDamageMomentum", propertyMap, "0.0");
			// MeleeHitRadius default is 110 for melee familiars, 0 for non-melee
			string meleeHitRadiusFallback = (obj.Class?.Name?.Name ?? "").Contains("Melee") ? "110.0" : "0.0";
			AddPropertyToMap(pl, "MeleeHitRadius", propertyMap, meleeHitRadiusFallback);
			AddPropertyToMap(pl, "MinHealPerDamage", propertyMap, "0.0");
			AddPropertyToMap(pl, "RandomizedDamageMultiplierDivisor", propertyMap, "0.0");
			AddPropertyToMap(pl, "MaxAttackAnimationSpeed", propertyMap, "2.4");

			// HeroEquipment_Familiar_PawnBooster
			AddPropertyToMap(pl, "BaseBoost", propertyMap, "0.0");
			AddPropertyToMap(pl, "BoostRangeStatBase", propertyMap, "0.0");
			AddPropertyToMap(pl, "BoostRangeStatExponent", propertyMap, "0.0");
			AddPropertyToMap(pl, "BoostRangeStatMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "BoostStatBase", propertyMap, "0.0");
			AddPropertyToMap(pl, "BoostStatExponent", propertyMap, "0.0");
			AddPropertyToMap(pl, "BoostStatMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "FirstBoostInterval", propertyMap, "0.0");
			AddPropertyToMap(pl, "MaxBoostStat", propertyMap, "0.0");

			// HeroEquipment_Familiar_PlayerHealer
			AddPropertyToMap(pl, "FalloffExponent", propertyMap, "0.0");
			AddPropertyToMap(pl, "HealRange", propertyMap, "0.0");
			AddPropertyToMap(pl, "MinimumHealDistancePercent", propertyMap, "0.0");

			// HeroEquipment_Familiar_TADPS
			AddPropertyToMap(pl, "dpsTreshold", propertyMap, "0.0");

			// HeroEquipment_Familiar_TowerBooster
			AddPropertyToMap(pl, "BaseBoostRange", propertyMap, "0.0");
			AddPropertyToMap(pl, "BoostAmountMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "BoostRangeExponent", propertyMap, "0.0");
			AddPropertyToMap(pl, "ETBAttackRangeExponent", propertyMap, "0.0");
			AddPropertyToMap(pl, "ETBAttackRateExponent", propertyMap, "0.0");
			AddPropertyToMap(pl, "ETBDamageExponent", propertyMap, "0.0");
			AddPropertyToMap(pl, "ETBResistanceExponent", propertyMap, "0.0");
			AddPropertyToMap(pl, "MaxRangeBoostStat", propertyMap, "0.0");

			// HeroEquipment_Familiar_TowerDamageScaling
			AddPropertyToMap(pl, "AbsoluteDamageMultiplier", propertyMap, "1.0");
			AddPropertyToMap(pl, "AltProjectileMinimumRange", propertyMap, "0.0");
			AddPropertyToMap(pl, "BaseDamageToManaRatio", propertyMap, "0.0");
			AddPropertyToMap(pl, "BaseHealAmount", propertyMap, "0.0");
			AddPropertyToMap(pl, "Damage", propertyMap, "0.0");
			AddPropertyToMap(pl, "DamageManaMultiplierExponent", propertyMap, "0.0");
			AddPropertyToMap(pl, "ExtraNightmareDamageMultiplier", propertyMap, "0.65");
			AddPropertyToMap(pl, "HealAmountMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "HealingPriorityHealthPercentage", propertyMap, "0.0");
			AddPropertyToMap(pl, "ManaMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "MaxManaMultiplierExponent", propertyMap, "0.0");
			AddPropertyToMap(pl, "MaxManaPerDamage", propertyMap, "0.0");
			AddPropertyToMap(pl, "MinManaPerDamage", propertyMap, "0.0");
			AddPropertyToMap(pl, "MinimumProjectileSpeed", propertyMap, "0.0");
			AddPropertyToMap(pl, "NightmareDamageMultiplier", propertyMap, "17.0");
			AddPropertyToMap(pl, "NightmareHealingMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "ProjectileDamageMultiplier", propertyMap, "1.0");
			AddPropertyToMap(pl, "ProjectileShootInterval", propertyMap, "3.0");
			AddPropertyToMap(pl, "ProjectileSpeedBonusMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "ShotsPerSecondExponent", propertyMap, "1.5");
			AddPropertyToMap(pl, "ShotsPerSecondAnimExponent", propertyMap, "0.75");
			AddPropertyToMap(pl, "TargetRange", propertyMap, "0.0");
			AddPropertyToMap(pl, "WeakenEnemyTargetPercentage", propertyMap, "0.0");

			// HeroEquipment_Familiar_TowerHealer
			AddPropertyToMap(pl, "HealRadius", propertyMap, "0.0");

			// HeroEquipment_Familiar
			AddPropertyToMap(pl, "BarbStanceDamageMulti", propertyMap, "0.0");

			// Ints
			// HeroEquipment_Familiar_Corehealer
			AddPropertyToMap(pl, "StringHealAmount", propertyMap, "0");
			AddPropertyToMap(pl, "StringHealRange", propertyMap, "0");
			AddPropertyToMap(pl, "StringHealSpeed", propertyMap, "0");

			// HeroEquipment_Familiar_Melee_TowerScaling
			AddPropertyToMap(pl, "MeleeDamageType", propertyMap, "0");
			AddPropertyToMap(pl, "RandomizedDamageMultiplierMaximum", propertyMap, "0");

			// HeroEquipment_Familiar_PawnBooster
			AddPropertyToMap(pl, "BoostStatUpgradeInterval", propertyMap, "0");
			AddPropertyToMap(pl, "MaxNumberOfPawnsToBoost", propertyMap, "0");
			AddPropertyToMap(pl, "SoftMaxNumberOfPawnsToBoost", propertyMap, "0");

			// HeroEquipment_Familiar_TADPS
			AddPropertyToMap(pl, "AdditionalName", propertyMap, "0");
			AddPropertyToMap(pl, "fixedprojspeedbonus", propertyMap, "0");

			// HeroEquipment_Familiar_TowerBooster
			AddPropertyToMap(pl, "MaxBoostStatValue", propertyMap, "0");
			AddPropertyToMap(pl, "MaxNumberOfTowersToBoost", propertyMap, "0");
			AddPropertyToMap(pl, "MaxTowerBoostStat", propertyMap, "0");
			AddPropertyToMap(pl, "SoftMaxNumberOfTowersToBoost", propertyMap, "0");

			// HeroEquipment_Familiar_TowerDamageScaling
			AddPropertyToMap(pl, "Projectile", propertyMap, "0");
			AddPropertyToMap(pl, "ProjectileTemplate", propertyMap, "0");
			AddPropertyToMap(pl, "ProjectileTemplateAlt", propertyMap, "0");
			AddPropertyToMap(pl, "ShotsPerSecondBonusCap", propertyMap, "0");

			// Bytes / bool-ish flags
			// HeroEquipment_Familiar_Melee_TowerScaling
			AddPropertyToMap(pl, "bAlsoShootProjectile", propertyMap, "0");
			AddPropertyToMap(pl, "bDoMeleeHealing", propertyMap, "0");
			AddPropertyToMap(pl, "bUseRandomizedDamage", propertyMap, "0");

			// HeroEquipment_Familiar_PawnBooster
			AddPropertyToMap(pl, "ProModeFocused", propertyMap, "false");

			// HeroEquipment_Familiar_PlayerHealer
			AddPropertyToMap(pl, "bUseFixedHealSpeed", propertyMap, "false");

			// HeroEquipment_Familiar_TADPS
			AddPropertyToMap(pl, "bFixedProjSpeed", propertyMap, "0");

			// HeroEquipment_Familiar_TowerDamageScaling
			AddPropertyToMap(pl, "DoLineOfSightCheck", propertyMap, "false");
			AddPropertyToMap(pl, "bAddManaForDamage", propertyMap, "0");
			AddPropertyToMap(pl, "bChooseHealingTarget", propertyMap, "0");
			AddPropertyToMap(pl, "bDoShotsPerSecondBonusCap", propertyMap, "0");
			AddPropertyToMap(pl, "bUseAltProjectile", propertyMap, "0");
			AddPropertyToMap(pl, "bUseFixedShootSpeed", propertyMap, "0");
			AddPropertyToMap(pl, "bWeakenEnemyTarget", propertyMap, "0");

			// HeroEquipment_Familiar_TowerHealer
			AddPropertyToMap(pl, "bHealOverRadius", propertyMap, "0");

			// HeroEquipment_Familiar size scaling
			AddPropertyToMap(pl, "SizeScalerMaximumLevel", propertyMap, "100.0");
			AddPropertyToMap(pl, "SizeScalerPower", propertyMap, "1.0");
			AddPropertyToMap(pl, "MaximumLevelScaleMultiplier", propertyMap, "1.0");
			AddPropertyToMap(pl, "DrawScaleOffsetExponent", propertyMap, "0.0");
			AddPropertyToMap(pl, "DrawScaleOffsetMult", propertyMap, "0.0");
			AddPropertyToMap(pl, "HeroExperienceInvestmentMultiplier", propertyMap, "0.0");

			// HeroEquipment_Familiar_CoreHealer mana cost
			AddPropertyToMap(pl, "ManaCostStatBase", propertyMap, "0.0");
			AddPropertyToMap(pl, "ManaCostMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "ManaCostExponent", propertyMap, "0.0");
			AddPropertyToMap(pl, "ManaCostMin", propertyMap, "0.0");
			AddPropertyToMap(pl, "ManaCostMax", propertyMap, "0.0");
			AddPropertyToMap(pl, "HealRangeMaxEffectiveStat", propertyMap, "0.0");
			AddPropertyToMap(pl, "bHealInCombatPhaseOnly", propertyMap, "0");

			// HeroEquipment_Familiar_TowerDamageScaling missing flags
			AddPropertyToMap(pl, "bSlowEnemyTarget", propertyMap, "0");
			AddPropertyToMap(pl, "SlowEnemyTargetPercentage", propertyMap, "0.0");
			AddPropertyToMap(pl, "EnemyClearSlowTime", propertyMap, "0.0");
			AddPropertyToMap(pl, "EnemyClearWeakenTime", propertyMap, "0.0");
			AddPropertyToMap(pl, "bShootProjectileWithoutTarget", propertyMap, "0");
			AddPropertyToMap(pl, "bMythicalScaleTowerDamage", propertyMap, "0");
			AddPropertyToMap(pl, "MythicalScaleDamageStatExponent", propertyMap, "0.0");
			AddPropertyToMap(pl, "MythicalScaleDamageStatType", propertyMap, "0");
			AddPropertyToMap(pl, "bIgnoreElementInTargeting", propertyMap, "0");
			AddPropertyToMap(pl, "bProjectilesCollideWithOwner", propertyMap, "0");
			AddPropertyToMap(pl, "AttackAnimationAlt", propertyMap, "0");

			// HeroEquipment_Familiar_AoeBuffer
			AddPropertyToMap(pl, "StaticBuffRange", propertyMap, "0.0");
			AddPropertyToMap(pl, "UseStaticBuffRange", propertyMap, "0");
			AddPropertyToMap(pl, "BoostAnimMinInterval", propertyMap, "0.0");
			AddPropertyToMap(pl, "BoostAnimMaxInterval", propertyMap, "0.0");

			// HeroEquipment_Familiar_Melee
			AddPropertyToMap(pl, "ScaleDamageStatExponent", propertyMap, "0.0");
			AddPropertyToMap(pl, "ScaleMeleeDamageForHeroStatType", propertyMap, "0");

			// Build the familiar data struct (parses arrays via db.BuildArray in the ctor)
			HeroEquipment_Familiar_Data hef = new HeroEquipment_Familiar_Data(propertyMap, db);

			// Finally store it in your DB (adapt to your actual DB API)
			// If your DB doesn’t have this exact method name/signature, mirror AddHeroEquipment’s pattern.
			return db.AddHeroEquipmentFamiliar(ref hef);
		}



		public int AddPlayerToDB(UObject obj, ExportedTemplateDatabase db)
		{
			Dictionary<string, string> propertyMap = new Dictionary<string, string>();
			var pl = PreloadProperties(obj);
			AddAnimationPropertiesToMap(obj, propertyMap);
			// Floats
			AddPropertyToMap(pl, "AdditionalSpeedMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "ExtraPlayerDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "HeroBonusPetDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "HeroBoostSpeedMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "NightmareModePlayerHealthMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "PlayerWeaponDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "StatExpFull_HeroCastingRate", propertyMap, "0.0");
			AddPropertyToMap(pl, "StatExpInitial_HeroCastingRate", propertyMap, "0.0");
			AddPropertyToMap(pl, "StatMultFull_HeroCastingRate", propertyMap, "0.0");
			AddPropertyToMap(pl, "StatMultInitial_HeroCastingRate", propertyMap, "0.0");
			AddPropertyToMap(pl, "AnimSpeedMultiplier", propertyMap, "0.0");

			// Arrays
			AddArrayPropertyToMap(pl, "MeleeSwingInfoMultipliers", propertyMap);
			AddArrayPropertyToMap(pl, "MainHandSwingInfoMultipliers", propertyMap);
			AddArrayPropertyToMap(pl, "OffHandSwingInfoMultipliers", propertyMap);

			// DunDefPawn float
			AddPropertyToMap(pl, "DamageMultiplierAdditional", propertyMap, "0.0");

			// Ints
			AddPropertyToMap(pl, "HeroBoostHealAmount", propertyMap, "0");

			propertyMap["Template"] = obj.GetPath();
			propertyMap["Class"] = (obj.Class?.Name?.Name ?? "");
			// Build data + store
			DunDefPlayer_Data ddp = new DunDefPlayer_Data(propertyMap, db);

			// Adapt this to your DB API (mirrors your AddHeroEquipment pattern)
			return db.AddDunDefPlayer(obj.GetPath(), (obj.Class?.Name?.Name ?? ""), ref ddp);
		}

		private void ResolveGasCloudProperties(UObject obj, string referencePropertyName, Dictionary<string, string> propertyMap,
			string damageAmountKey, string effectIntervalKey, string cloudLifeSpanKey)
		{
			// Defaults if we can't resolve
			propertyMap[damageAmountKey] = "0.0";
			propertyMap[effectIntervalKey] = "0.0";
			propertyMap[cloudLifeSpanKey] = "0.0";

			var refProp = GetProperty(obj, referencePropertyName);
			if (refProp == null || string.IsNullOrWhiteSpace(refProp.Value) || refProp.Value == "None" || refProp.Value == "0")
				return;

			// Resolve the object reference to get the GasCloud/EmitterSpawnable object
			var gasCloud = FindObjectByPath(obj.Package, refProp.Value);
			if (gasCloud == null)
			{
				MainWindow.Log($"[ResolveGasCloudProperties] Could not resolve '{referencePropertyName}' reference '{refProp.Value}' on '{obj.GetPath()}'");
				return;
			}

			gasCloud.Load();

			var damageAmount = GetProperty(gasCloud, "DamageAmount");
			if (damageAmount != null)
				propertyMap[damageAmountKey] = damageAmount.Value;

			var effectInterval = GetProperty(gasCloud, "EffectInterval");
			if (effectInterval != null)
				propertyMap[effectIntervalKey] = effectInterval.Value;

			var cloudLifeSpan = GetProperty(gasCloud, "CloudLifeSpan");
			if (cloudLifeSpan != null)
				propertyMap[cloudLifeSpanKey] = cloudLifeSpan.Value;
		}

		public int AddProjectileToDB(UObject obj, ExportedTemplateDatabase db)
		{
			Dictionary<string, string> propertyMap = new Dictionary<string, string>();
			var pl = PreloadProperties(obj);

			// Arrays
			AddArrayPropertyToMap(pl, "RandomDamageTypes", propertyMap);


			AddPropertyToMap(pl, "bSecondScaleDamageStatOnAdditionalDamage", propertyMap, "0");
			AddPropertyToMap(pl, "bSecondScaleDamageStatType", propertyMap, "0");
			AddPropertyToMap(pl, "SecondScaleDamageStatType", propertyMap, "0");
		// Ints
			AddPropertyToMap(pl, "AdditionalDamageAmount", propertyMap, "0");
			AddPropertyToMap(pl, "AdditionalDamageType", propertyMap, "0");
			AddPropertyToMap(pl, "ScaleDamageStatType", propertyMap, "0");
			AddPropertyToMap(pl, "ProjDamageType", propertyMap, "0");
			AddPropertyToMap(pl, "NumAllowedPassThrough", propertyMap, "0");

			// Floats
			AddPropertyToMap(pl, "DamageRadiusFallOffExponent", propertyMap, "0.0");
			AddPropertyToMap(pl, "ScaleDamageStatExponent", propertyMap, "0.0");
			AddPropertyToMap(pl, "ProjDamage", propertyMap, "0.0");
			AddPropertyToMap(pl, "ProjDamageRadius", propertyMap, "0.0");
			AddPropertyToMap(pl, "ProjectileDamageByWeaponDamageDivider", propertyMap, "0.0");
			AddPropertyToMap(pl, "ProjectileDamagePerDistanceTravelled", propertyMap, "0.0");
			AddPropertyToMap(pl, "ProjectileLifespan", propertyMap, "0.0");
			AddPropertyToMap(pl, "ProjectileMaxSpeed", propertyMap, "0.0");
			AddPropertyToMap(pl, "ProjectileSpeed", propertyMap, "0.0");

			// Floats (homing / extra)
			AddPropertyToMap(pl, "TowerDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "HomingInterpSpeed", propertyMap, "0.0");

			// Bytes / bool-ish flags (stored as byte in struct)
			AddPropertyToMap(pl, "MultiplyProjectileDamageByPrimaryWeaponSwingSpeed", propertyMap, "0");
			AddPropertyToMap(pl, "MultiplyProjectileDamageByWeaponDamage", propertyMap, "0");
			AddPropertyToMap(pl, "OnlyCollideWithIgnoreClasses", propertyMap, "0");
			AddPropertyToMap(pl, "ScaleHeroDamage", propertyMap, "0");
			AddPropertyToMap(pl, "bAlwaysUseRandomDamageType", propertyMap, "0");
			AddPropertyToMap(pl, "bApplyBuffsOnAoe", propertyMap, "0");
			AddPropertyToMap(pl, "bReplicateWeaponProjectile", propertyMap, "0");
			AddPropertyToMap(pl, "bUseProjectilePerDistanceScaling", propertyMap, "0");
			AddPropertyToMap(pl, "bUseProjectilePerDistanceSizeScaling", propertyMap, "0");

			// Homing projectile flags
			AddPropertyToMap(pl, "bPierceEnemies", propertyMap, "0");
			AddPropertyToMap(pl, "bScaleDamagePerLevel", propertyMap, "0");
			AddPropertyToMap(pl, "bDamageOnTouch", propertyMap, "0");

			AddPropertyToMap(pl, "FireDamageScale", propertyMap, "0.0");
			AddPropertyToMap(pl, "DotDamageScale", propertyMap, "0.0");

			AddPropertyToMap(pl, "TheDamageMinScale", propertyMap, "0.0");
			AddPropertyToMap(pl, "TheDamageMaxScale", propertyMap, "0.0");
			AddPropertyToMap(pl, "ExtraDamageMaxScale", propertyMap, "0.0");

			// Resolve GasCloud properties from DotTemplate reference (StaffDot)
			ResolveGasCloudProperties(obj, "DotTemplate", propertyMap,
				"DotCloudDamageAmount", "DotCloudEffectInterval", "DotCloudLifeSpan");

			// Resolve GasCloud properties from DamagingFireEmitters reference (Meteor)
			ResolveGasCloudProperties(obj, "DamagingFireEmitters", propertyMap,
				"FireCloudDamageAmount", "FireCloudEffectInterval", "FireCloudLifeSpan");

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
			var pl = PreloadProperties(obj);

			// Arrays
			AddArrayPropertyToMap(pl, "ExtraProjectileTemplates", propertyMap);
			AddArrayPropertyToMap(pl, "MeleeSwingInfos", propertyMap);
			AddArrayPropertyToMap(pl, "RainbowDamageTypeArrays", propertyMap);
			AddArrayPropertyToMap(pl, "RandomizedProjectileTemplate", propertyMap);
			AddArrayPropertyToMap(pl, "FireInterval", propertyMap);

			// Ints (core)
			AddPropertyToMap(pl, "AdditionalDamageAmount", propertyMap, "0");
			AddPropertyToMap(pl, "AdditionalDamageType", propertyMap, "0");
			AddPropertyToMap(pl, "BaseAltDamage", propertyMap, "0");
			AddPropertyToMap(pl, "BaseDamage", propertyMap, "0");
			AddPropertyToMap(pl, "BaseShotsPerSecond", propertyMap, "0");
			AddPropertyToMap(pl, "BaseTotalAmmo", propertyMap, "0");
			AddPropertyToMap(pl, "ProjectileTemplate", propertyMap, "0");

			// Floats (core)
			AddPropertyToMap(pl, "WeaponDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "WeaponSpeedMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "MinimumProjectileSpeed", propertyMap, "0.0");
			AddPropertyToMap(pl, "ProjectileSpeedAddition", propertyMap, "0.0");
			AddPropertyToMap(pl, "ProjectileSpeedBonusMultiplier", propertyMap, "0.0");

			// Bytes (core flags)
			AddPropertyToMap(pl, "bIsMeleeWeapon", propertyMap, "0");
			AddPropertyToMap(pl, "bRandomizeProjectileTemplate", propertyMap, "0");
			AddPropertyToMap(pl, "bUseAdditionalProjectileDamage", propertyMap, "0");
			AddPropertyToMap(pl, "bUseAltDamageForProjectileBaseDamage", propertyMap, "0");
			AddPropertyToMap(pl, "bUseDamageReductionForAbilities", propertyMap, "0");

			// DunDefWeapon_Crossbow
			AddPropertyToMap(pl, "BaseNumProjectiles", propertyMap, "0");
			AddPropertyToMap(pl, "BaseReloadSpeed", propertyMap, "0.0");
			AddPropertyToMap(pl, "ClipAmmo", propertyMap, "0");
			AddPropertyToMap(pl, "FireIntervalMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "bUseHighShotPerSecond", propertyMap, "0");

			// DunDefWeapon_MagicStaff
			AddPropertyToMap(pl, "AbilityCooldownTime", propertyMap, "0");
			AddPropertyToMap(pl, "BaseChargeSpeed", propertyMap, "0.0");
			AddPropertyToMap(pl, "BonusDamageMulti", propertyMap, "0.0");
			AddPropertyToMap(pl, "CooldownDuration", propertyMap, "0");
			AddPropertyToMap(pl, "ElementalDamageForRightClickScalar", propertyMap, "0.0");
			AddPropertyToMap(pl, "FullAltChargeTime", propertyMap, "0.0");
			AddPropertyToMap(pl, "FullChargeTime", propertyMap, "0.0");
			AddPropertyToMap(pl, "FullchargeRefireInterval", propertyMap, "0.0");
			AddPropertyToMap(pl, "MediumChargeFFThreshold", propertyMap, "0.0");
			AddPropertyToMap(pl, "NumProjectiles", propertyMap, "0");
			AddPropertyToMap(pl, "bIsRainMaker", propertyMap, "0");
			AddPropertyToMap(pl, "bEmberorMoon", propertyMap, "0");
			AddPropertyToMap(pl, "bUseAttackCD", propertyMap, "0");
			AddPropertyToMap(pl, "bUseElementalScallingForRightClick", propertyMap, "0");

			// DunDefWeapon_MagicStaff_Channeling
			AddPropertyToMap(pl, "ChannelingProjectileDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "ChannelingProjectileFireSpeed", propertyMap, "0.0");
			AddPropertyToMap(pl, "ChannelingProjectileTemplate", propertyMap, "0");
			AddPropertyToMap(pl, "ChannelingRangeMultiplier", propertyMap, "0.0");

			// DunDefWeapon_MeleeSword
			AddPropertyToMap(pl, "BaseMeleeDamageType", propertyMap, "0");
			AddPropertyToMap(pl, "DamageIncreaseForSwingSpeedFactor", propertyMap, "0.0");
			AddPropertyToMap(pl, "DamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "ExtraSpeedMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "IsSwingingWeapon", propertyMap, "0");
			AddPropertyToMap(pl, "MaxMomentumMultplierByDamage", propertyMap, "0.0");
			AddPropertyToMap(pl, "MaxTotalMomentumMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "MeleeDamageMomentum", propertyMap, "0.0");
			AddPropertyToMap(pl, "MinimumSwingDamageTime", propertyMap, "0.0");
			AddPropertyToMap(pl, "MinimumSwingTime", propertyMap, "0.0");
			AddPropertyToMap(pl, "MomentumMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "ProjectileDamageHeroStatExponentMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "SpeedMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "SpeedMultiplierDamageExponent", propertyMap, "0.0");
			AddPropertyToMap(pl, "WeakenEnemyTargetPercentage", propertyMap, "0.0");
			AddPropertyToMap(pl, "WeaponProjectileDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "bShootMeleeProjectile", propertyMap, "0");
			AddPropertyToMap(pl, "bUseRainbowDamageType", propertyMap, "0");
			AddPropertyToMap(pl, "bUseWeaponDamageForProjectileDamage", propertyMap, "0");
			AddPropertyToMap(pl, "BlockingMomentumExponent", propertyMap, "0.0");
			AddPropertyToMap(pl, "AdditionalMomentumExponent", propertyMap, "0.0");

			// DunDefWeapon_Minigun
			AddPropertyToMap(pl, "MinigunProjectileDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "SpeedPerDelta", propertyMap, "0.0");

			// DunDefWeapon_MonkSpear
			AddPropertyToMap(pl, "ShootInterval", propertyMap, "0.0");

			// DunDefWeapon_NessieLauncher
			AddPropertyToMap(pl, "Multiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "NessieCooldown", propertyMap, "0.0");

			AddPropertyToMap(pl, "ChargeSpeedBonusLinearScale", propertyMap, "0.0");
			AddPropertyToMap(pl, "ChargeSpeedBonusExpScale", propertyMap, "0.0");

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
			var pl = PreloadProperties(obj);

			// Arrays
			AddArrayPropertyToMap(pl, "StatNames", propertyMap);
			AddArrayPropertyToMap(pl, "StatDescriptions", propertyMap);

			// Bytes (core flags)
			AddPropertyToMap(pl, "bIsMeleeHero", propertyMap, "0");

			// Ints (core)
			AddPropertyToMap(pl, "MyHeroType", propertyMap, "0");
			AddPropertyToMap(pl, "GivenCostumeString", propertyMap, "0");     // (string ref / id)


			AddArrayPropertyToMap(pl, "HeroCostumes", propertyMap);

			AddPropertyToMap(pl, "PlayerTemplate", propertyMap, "0");         // DunDefPlayer ref/id



			AddPropertyToMap(pl, "HeroClassDisplayName", propertyMap, "0");   // (string ref / id)
			AddPropertyToMap(pl, "HeroClassDescription", propertyMap, "0");   // (string ref / id)

			// Struct-ish / special (leave as string default; adjust if you store colors differently)
			AddPropertyToMap(pl, "ClassNameColor", propertyMap, "0");         // ULinear_Color

			// Floats (scaling)
			AddPropertyToMap(pl, "HeroDefenseAttackRateLinearFactor", propertyMap, "0.0");
			AddPropertyToMap(pl, "HeroDefenseAttackRateExponentialFactor", propertyMap, "0.0");
			AddPropertyToMap(pl, "HeroHealthExponentialFactor", propertyMap, "0.0");
			AddPropertyToMap(pl, "HeroHealthLinearFactor", propertyMap, "0.0");

			// HeroDamage
			AddPropertyToMap(pl, "StatExpFull_HeroDamage", propertyMap, "0.0");
			AddPropertyToMap(pl, "StatExpInitial_HeroDamage", propertyMap, "0.0");
			AddPropertyToMap(pl, "StatMultInitial_HeroDamage", propertyMap, "0.0");
			AddPropertyToMap(pl, "StatMultFull_HeroDamage", propertyMap, "0.0");

			// HeroSpeed
			AddPropertyToMap(pl, "StatMultInitial_HeroSpeed", propertyMap, "0.0");
			AddPropertyToMap(pl, "StatMultFull_HeroSpeed", propertyMap, "0.0");

			// HeroAbilityOne
			AddPropertyToMap(pl, "StatMultInitial_HeroAbilityOne", propertyMap, "0.0");
			AddPropertyToMap(pl, "StatExpInitial_HeroAbilityOne", propertyMap, "0.0");
			AddPropertyToMap(pl, "StatMultFull_HeroAbilityOne", propertyMap, "0.0");
			AddPropertyToMap(pl, "StatExptFull_HeroAbilityOne", propertyMap, "0.0");

			// HeroAbilityTwo
			AddPropertyToMap(pl, "StatMultInitial_HeroAbilityTwo", propertyMap, "0.0");
			AddPropertyToMap(pl, "StatExpInitial_HeroAbilityTwo", propertyMap, "0.0");
			AddPropertyToMap(pl, "StatMultFull_HeroAbilityTwo", propertyMap, "0.0");
			AddPropertyToMap(pl, "StatExptFull_HeroAbilityTwo", propertyMap, "0.0");

			// DefenseHealth
			AddPropertyToMap(pl, "StatMultInitial_DefenseHealth", propertyMap, "0.0");
			AddPropertyToMap(pl, "StatExpInitial_DefenseHealth", propertyMap, "0.0");
			AddPropertyToMap(pl, "StatMultFull_DefenseHealth", propertyMap, "0.0");
			AddPropertyToMap(pl, "StatExptFull_DefenseHealth", propertyMap, "0.0");

			// DefenseDamage
			AddPropertyToMap(pl, "StatMultInitial_DefenseDamage", propertyMap, "0.0");
			AddPropertyToMap(pl, "StatExpInitial_DefenseDamage", propertyMap, "0.0");
			AddPropertyToMap(pl, "StatMultFull_DefenseDamage", propertyMap, "0.0");
			AddPropertyToMap(pl, "StatExptFull_DefenseDamage", propertyMap, "0.0");

			// DefenseAttackRate
			AddPropertyToMap(pl, "StatMultInitial_DefenseAttackRate", propertyMap, "0.0");
			AddPropertyToMap(pl, "StatExpInitial_DefenseAttackRate", propertyMap, "0.0");

			// DefenseAOE
			AddPropertyToMap(pl, "StatMultInitial_DefenseAOE", propertyMap, "0.0");
			AddPropertyToMap(pl, "StatExpInitial_DefenseAOE", propertyMap, "0.0");
			AddPropertyToMap(pl, "StatMultFull_DefenseAOE", propertyMap, "0.0");
			AddPropertyToMap(pl, "StatExptFull_DefenseAOE", propertyMap, "0.0");
			AddPropertyToMap(pl, "StatBoostCapInitial_HeroDamage", propertyMap, "0.0");

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

			// Pre-index AnimNotify_ScriptedEquipmentAttachment exports by parent path
			// to avoid O(n^2) inner scan for each AnimSequence
			var notifyByParent = new Dictionary<string, List<UExportTableItem>>(StringComparer.OrdinalIgnoreCase);
			foreach (var exp in package.Exports)
			{
				if (exp.Class?.ObjectName.Name != "AnimNotify_ScriptedEquipmentAttachment") continue;
				string parentPath = RemoveAfterLastDot(exp.GetPath());
				if (!notifyByParent.TryGetValue(parentPath, out var list))
				{
					list = new List<UExportTableItem>();
					notifyByParent[parentPath] = list;
				}
				list.Add(exp);
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

						float sequenceLength = 0.0f;
						int numFrames = 0;
						float rateScale = 1.0f;

						props.TryGetValue("SequenceName", out var sequenceName);
						string animName = sequenceName?.Value ?? "";

						if (props.TryGetValue("SequenceLength", out var lengthProp))
						{
							float.TryParse(lengthProp.Value?.ToString() ?? "0", out sequenceLength);
						}

						if (props.TryGetValue("NumFrames", out var framesProp))
						{
							int.TryParse(framesProp.Value?.ToString() ?? "0", out numFrames);
						}

						if (props.TryGetValue("RateScale", out var rateProp))
						{
							float.TryParse(rateProp.Value?.ToString() ?? "1.0", out rateScale);
						}

						float startweapondamageTime = -1.0f;
						int nFamiliarShots = 0;
						int nFamiliarShotsAlt = 0;
						if (props.TryGetValue("Notifies", out var notifyProp))
						{
							var pattern = @"Time=(\d+\.\d+).*?NotifyName=""AnimNotify_StartWeaponSwingDamage""";
							var matches = Regex.Matches(notifyProp.Value, pattern, RegexOptions.Singleline);
							if (matches.Count > 0)
							{
								startweapondamageTime = (float)(double.Parse(matches[0].Groups[1].Value, CultureInfo.InvariantCulture));
							}

							// Look up pre-indexed child notify exports (O(1) instead of O(n))
							string animSeqPath = export.GetPath();
							if (notifyByParent.TryGetValue(animSeqPath, out var childNotifies))
							{
								foreach (var childExport in childNotifies)
								{
									var notifyObj = childExport.Object;
									notifyObj?.Load();
									if (notifyObj == null) continue;

									var notifyObjProps = GetMergedProperties(notifyObj);
									if (notifyObjProps.TryGetValue("NotifyID", out var idProp) &&
										int.TryParse(idProp.Value?.ToString(), out int notifyId))
									{
										if (notifyId == 2000) nFamiliarShots++;
										else if (notifyId == 2005) nFamiliarShotsAlt++;
									}
								}
							}
						}

						string animObjName = RemoveAfterLastDot(export.GetPath());

						// Store in cache
						if (!AnimationDurations.ContainsKey(animObjName))
						{
							AnimationDurations.Add(animObjName, new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase));
						}
						AnimationDurations[animObjName][animName.Replace("\"", "")] = sequenceLength / rateScale;

						if (startweapondamageTime > 0.0f)
							AnimationDurations[animObjName][animName.Replace("\"", "") + "_StartWeaponSwingDamage"] = (float)startweapondamageTime / rateScale;

						if (nFamiliarShots > 0)
							AnimationDurations[animObjName][animName.Replace("\"", "") + "_NumFamiliarShots"] = (float)nFamiliarShots;

						if (nFamiliarShotsAlt > 0)
							AnimationDurations[animObjName][animName.Replace("\"", "") + "_NumFamiliarShotsAlt"] = (float)nFamiliarShotsAlt;

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

		public void AddFamiliarAnimationPropertiesToMap(UObject familiarTemplate, Dictionary<string, string> propertyMap)
		{
			float animLength = 1.0f;
			int numAnimNotifyAttacks = 0;
			int numAnimNotifyAttacksAlt = 0;
			var childSkeletalMeshComp = FindChildSkeletalMeshComponent(familiarTemplate);
			if (childSkeletalMeshComp != null)
			{
				childSkeletalMeshComp.Load();

				var animProp = GetProperty(familiarTemplate, "AttackAnimation");
				var props = GetMergedProperties(childSkeletalMeshComp);

				if (props.ContainsKey("AnimSets") && (animProp != null))
				{
					var animSet = props["AnimSets"];

					int start = animSet.Value.IndexOf('\'') + 1;
					int end = animSet.Value.IndexOf('\'', start);
					string path = animSet.Value.Substring(start, end - start);
					string key = animProp.Value;

					if (key.Length >= 2 &&
						key.StartsWith("\"") &&
						key.EndsWith("\""))
					{
						key = key.Substring(1, key.Length - 2);
					}

					if (AnimationDurations[path].ContainsKey(key))
					{
						animLength = AnimationDurations[path][key];			
					}

					if (AnimationDurations[path].ContainsKey(key + "_NumFamiliarShots"))
					{
						numAnimNotifyAttacks = (int)AnimationDurations[path][key + "_NumFamiliarShots"];
					}

					if (AnimationDurations[path].ContainsKey(key + "_NumFamiliarShotsAlt"))
					{
						numAnimNotifyAttacksAlt = (int)AnimationDurations[path][key + "_NumFamiliarShotsAlt"];
					}

				}
			}
			propertyMap["AttackAnimationLength"] = animLength.ToString();
			propertyMap["NumAnimNotifyAttacks"] = numAnimNotifyAttacks.ToString();
			propertyMap["NumAnimNotifyAttacksAlt"] = numAnimNotifyAttacksAlt.ToString();
		}

		public void AddAnimationPropertiesToMap(UObject playerTemplate, Dictionary<string,string> propertyMap)
		{
			playerTemplate.Load();
			float meleeAttack1_large = 1.0f;
			float meleeAttack2_large = 1.0f;
			float meleeAttack3_large = 0.5f;
			
			float meleeAttack1_medium = 0.7f;
			float meleeAttack2_medium = 0.533f;
			float meleeAttack3_medium = 1.033f;
			
			float meleeAttack1_large_startweaponswingdamage = 0.0f;
			float meleeAttack2_large_startweaponswingdamage = 0.0f;
			float meleeAttack3_large_startweaponswingdamage = 0.0f;
			float meleeAttack1_medium_startweaponswingdamage = 0.0f;
			float meleeAttack2_medium_startweaponswingdamage = 0.0f;
			float meleeAttack3_medium_startweaponswingdamage = 0.0f;

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
						if (AnimationDurations[path].ContainsKey("meleeattack1_large_StartWeaponSwingDamage")) meleeAttack1_large_startweaponswingdamage = AnimationDurations[path]["meleeattack1_large_StartWeaponSwingDamage"];
						if (AnimationDurations[path].ContainsKey("meleeattack2_large_StartWeaponSwingDamage")) meleeAttack2_large_startweaponswingdamage = AnimationDurations[path]["meleeattack2_large_StartWeaponSwingDamage"];
						if (AnimationDurations[path].ContainsKey("meleeattack3_large_StartWeaponSwingDamage")) meleeAttack3_large_startweaponswingdamage = AnimationDurations[path]["meleeattack3_large_StartWeaponSwingDamage"];
						if (AnimationDurations[path].ContainsKey("meleeattack1_medium_StartWeaponSwingDamage")) meleeAttack1_medium_startweaponswingdamage = AnimationDurations[path]["meleeattack1_medium_StartWeaponSwingDamage"];
						if (AnimationDurations[path].ContainsKey("meleeattack2_medium_StartWeaponSwingDamage")) meleeAttack2_medium_startweaponswingdamage = AnimationDurations[path]["meleeattack2_medium_StartWeaponSwingDamage"];
						if (AnimationDurations[path].ContainsKey("meleeattack3_medium_StartWeaponSwingDamage")) meleeAttack3_medium_startweaponswingdamage = AnimationDurations[path]["meleeattack3_medium_StartWeaponSwingDamage"];
					}
				}
			}

			propertyMap.Add("MeleeAttack1LargeAnimDuration", meleeAttack1_large.ToString());
			propertyMap.Add("MeleeAttack2LargeAnimDuration", meleeAttack2_large.ToString());
			propertyMap.Add("MeleeAttack3LargeAnimDuration", meleeAttack3_large.ToString());
			propertyMap.Add("MeleeAttack1MediumAnimDuration", meleeAttack1_medium.ToString());
			propertyMap.Add("MeleeAttack2MediumAnimDuration", meleeAttack2_medium.ToString());
			propertyMap.Add("MeleeAttack3MediumAnimDuration", meleeAttack3_medium.ToString());

			propertyMap.Add("MeleeAttack1LargeAnimDamageStart",  meleeAttack1_large_startweaponswingdamage.ToString());
			propertyMap.Add("MeleeAttack2LargeAnimDamageStart",  meleeAttack2_large_startweaponswingdamage.ToString());
			propertyMap.Add("MeleeAttack3LargeAnimDamageStart",  meleeAttack3_large_startweaponswingdamage.ToString());
			propertyMap.Add("MeleeAttack1MediumAnimDamageStart", meleeAttack1_medium_startweaponswingdamage.ToString());
			propertyMap.Add("MeleeAttack2MediumAnimDamageStart", meleeAttack2_medium_startweaponswingdamage.ToString());
			propertyMap.Add("MeleeAttack3MediumAnimDamageStart", meleeAttack3_medium_startweaponswingdamage.ToString());

		}		


		public int AddEnemyToDB(UObject obj, ExportedTemplateDatabase db)
		{
			Dictionary<string, string> propertyMap = new Dictionary<string, string>();
			var pl = PreloadProperties(obj);

			// Drop flags (byte)
			AddPropertyToMap(pl, "bDropEquipment", propertyMap, "0");
			AddPropertyToMap(pl, "bDropMana", propertyMap, "0");
			AddPropertyToMap(pl, "bForceDropEquipment", propertyMap, "0");
			AddPropertyToMap(pl, "bScaleDroppedEquipmentWithLevel", propertyMap, "0");
			AddPropertyToMap(pl, "bIgnoreGlobalEnemyDropQualityMultiplier", propertyMap, "0");
			AddPropertyToMap(pl, "bAffectWaveBonusDamageCauser", propertyMap, "0");

			// Global drop config (float)
			AddPropertyToMap(pl, "GlobalEquipmentDropChanceThreshold", propertyMap, "0.0");
			AddPropertyToMap(pl, "GlobalEquipmentDropValueMin", propertyMap, "0.0");
			AddPropertyToMap(pl, "GlobalEquipmentDropValueMax", propertyMap, "0.0");
			AddPropertyToMap(pl, "GlobalEquipmentDropQuality", propertyMap, "0.0");
			AddPropertyToMap(pl, "GlobalDropChanceThresholdMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "MaxWaveEquipmentQualityMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "AbsoluteMaxEquipmentDropQuality", propertyMap, "0.0");

			// Global drop config (int)
			AddPropertyToMap(pl, "NumGlobalEquipmentDropChances", propertyMap, "0");
			AddPropertyToMap(pl, "NumGlobalEquipmentDropChancesRuthless", propertyMap, "0");
			AddPropertyToMap(pl, "EquipmentQualityMultiplierMaxWave", propertyMap, "0");
			AddPropertyToMap(pl, "MinimumStartWaveDifferenceForEquipment", propertyMap, "0");

			// Custom drop config (float)
			AddPropertyToMap(pl, "CustomEquipmentDropChanceThreshold", propertyMap, "0.0");
			AddPropertyToMap(pl, "CustomEquipmentDropValueMin", propertyMap, "0.0");
			AddPropertyToMap(pl, "CustomEquipmentDropValueMax", propertyMap, "0.0");
			AddPropertyToMap(pl, "CustomEquipmentDropQuality", propertyMap, "0.0");

			// Custom drop config (int)
			AddPropertyToMap(pl, "NumCustomEquipmentDropChances", propertyMap, "0");

			// Custom drop entries array
			AddArrayPropertyToMap(pl, "CustomEquipmentDrops", propertyMap);

			// Difficulty scaling arrays
			AddArrayPropertyToMap(pl, "DifficultyEquipmentQualityMultipliers", propertyMap);
			AddArrayPropertyToMap(pl, "DifficultyEquipmentRarityWeightings", propertyMap);
			AddArrayPropertyToMap(pl, "DifficultyHealthMultipliers", propertyMap);
			AddArrayPropertyToMap(pl, "DifficultyDamageMultipliers", propertyMap);
			AddArrayPropertyToMap(pl, "DifficultySpeedMultipliers", propertyMap);
			AddArrayPropertyToMap(pl, "DifficultyManaMultipliers", propertyMap);
			AddArrayPropertyToMap(pl, "DifficultyScoreMultipliers", propertyMap);
			AddArrayPropertyToMap(pl, "NumPlayerHealthMultipliers", propertyMap);
			AddArrayPropertyToMap(pl, "GoldenEnemyDifficultyOffset", propertyMap);
			AddArrayPropertyToMap(pl, "MaxSimultaneousAllowedForPlayers", propertyMap);
			AddArrayPropertyToMap(pl, "DifficultySetWaveOffsetThresholds", propertyMap);

			// Elemental system
			AddArrayPropertyToMap(pl, "ElementalEntries", propertyMap);
			AddArrayPropertyToMap(pl, "ElementalDamageModifiers", propertyMap);
			AddPropertyToMap(pl, "ElementalChanceMultiplier", propertyMap, "0.0");

			// Ruthless modifiers (from RuthlessEnemyModifiers struct)
			AddPropertyToMap(pl, "RuthlessEnemyModifiers", propertyMap, "");

			// Nightmare / tower resistance
			AddPropertyToMap(pl, "NightmareDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "ExtraNightmareHealthMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "TowerDamageResistanceMultiplier", propertyMap, "0.0");

			// Spawn config
			AddPropertyToMap(pl, "SpawnClumpAbsoluteAmount", propertyMap, "0");
			AddPropertyToMap(pl, "SpawnClumpMaximumAmount", propertyMap, "0");
			AddPropertyToMap(pl, "SpawnClumpRelativePercent", propertyMap, "0.0");

			// Difficulty set
			AddPropertyToMap(pl, "DifficultySetOffset", propertyMap, "0");
			AddPropertyToMap(pl, "MaxDifficultySets", propertyMap, "0");
			AddPropertyToMap(pl, "DifficultySetWaveOffset", propertyMap, "0.0");

			// Speed / stats
			AddPropertyToMap(pl, "MaxGroundSpeed", propertyMap, "0.0");
			AddPropertyToMap(pl, "MaxDifficultySpeedMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "KillCountMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "EnemyLifeSpan", propertyMap, "0.0");
			AddPropertyToMap(pl, "EnemyPlayerFavoringMultiplier", propertyMap, "0.0");

			// Survival
			AddPropertyToMap(pl, "SurvivalPartOneWaveTreshold", propertyMap, "0");
			AddPropertyToMap(pl, "SurvivalPartTwoWaveTreshold", propertyMap, "0");
			AddPropertyToMap(pl, "SurvivalPartOneDifficultyMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "SurvivalPartTwoDifficultyMultiplier", propertyMap, "0.0");

			// Difficulty offset adders
			AddPropertyToMap(pl, "AdditionalDifficultyOffsetDamageMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "AdditionalDifficultyOffsetHealthMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "AdditionalDifficultyOffsetSpeedMultiplier", propertyMap, "0.0");

			// Combat flags (byte)
			AddPropertyToMap(pl, "bAllowDarkness", propertyMap, "0");
			AddPropertyToMap(pl, "bAllowCoughing", propertyMap, "0");
			AddPropertyToMap(pl, "bAllowShocking", propertyMap, "0");
			AddPropertyToMap(pl, "bAllowEnsnare", propertyMap, "0");
			AddPropertyToMap(pl, "bAllowEnrage", propertyMap, "0");
			AddPropertyToMap(pl, "bAllowOil", propertyMap, "0");
			AddPropertyToMap(pl, "bCanBeFrozen", propertyMap, "0");
			AddPropertyToMap(pl, "bIgnoreAllTowerDamage", propertyMap, "0");
			AddPropertyToMap(pl, "bInvincibleWhileSpawningIn", propertyMap, "0");
			AddPropertyToMap(pl, "bIgnoreDifficultyScaling", propertyMap, "0");
			AddPropertyToMap(pl, "IsPlayerAlly", propertyMap, "0");
			AddPropertyToMap(pl, "bAllowSlowByHero", propertyMap, "0");
			AddPropertyToMap(pl, "bAllowWeakenByHero", propertyMap, "0");
			AddPropertyToMap(pl, "bAddToEnemyCap", propertyMap, "0");
			AddPropertyToMap(pl, "bAllowInvincibility", propertyMap, "0");
			AddPropertyToMap(pl, "bAllowEnemyDrain", propertyMap, "0");
			AddPropertyToMap(pl, "bEvenlySpaceWaveSpawns", propertyMap, "0");
			AddPropertyToMap(pl, "bUseEnemyGlobalMultipliers", propertyMap, "0");
			AddPropertyToMap(pl, "UseDjinnSpawnClamping", propertyMap, "0");
			AddPropertyToMap(pl, "UseSharkenSpawnClamping", propertyMap, "0");
			AddPropertyToMap(pl, "UseCopterSpawnClamping", propertyMap, "0");
			AddPropertyToMap(pl, "UseRuthlessOgreSpawnClamping", propertyMap, "0");
			AddPropertyToMap(pl, "bDontUseStatsInSurvival", propertyMap, "0");
			AddPropertyToMap(pl, "bUseSurvivalExtraDifficulty", propertyMap, "0");
			AddPropertyToMap(pl, "bClampDifficultyToInsane", propertyMap, "0");
			AddPropertyToMap(pl, "bIgnoreStats", propertyMap, "0");
			AddPropertyToMap(pl, "bKillOnBuildPhase", propertyMap, "0");
			AddPropertyToMap(pl, "bUnclampDifficultyHealthMultiplier", propertyMap, "0");
			AddPropertyToMap(pl, "bUnclampDifficultySpeedMultiplier", propertyMap, "0");

			// Classification
			AddPropertyToMap(pl, "MyClassification", propertyMap, "0");
			AddPropertyToMap(pl, "MinimumDifficultyForRandomElementalEffect", propertyMap, "0");

			propertyMap["Template"] = obj.GetPath();
			propertyMap["Class"] = (obj.Class?.Name?.Name ?? "");
			propertyMap["DescriptiveName"] = "0";

			DunDefEnemy_Data enemy = new DunDefEnemy_Data(propertyMap, db);
			return db.AddDunDefEnemy(obj.GetPath(), (obj.Class?.Name?.Name ?? ""), ref enemy);
		}

		public int AddGiveEquipmentToPlayersToDB(UObject obj, ExportedTemplateDatabase db)
		{
			Dictionary<string, string> propertyMap = new Dictionary<string, string>();
			var pl = PreloadProperties(obj);

			// Give equipment entries array
			AddArrayPropertyToMap(pl, "GiveEquipmentEntries", propertyMap);

			// Flags (byte)
			AddPropertyToMap(pl, "bGiveToEveryone", propertyMap, "0");
			AddPropertyToMap(pl, "bNotifyUser", propertyMap, "0");
			AddPropertyToMap(pl, "bAutoLockEquipment", propertyMap, "0");
			AddPropertyToMap(pl, "bForceEquipmentIntoItemBox", propertyMap, "0");
			AddPropertyToMap(pl, "bOnlyGiveToUniqueProfile", propertyMap, "0");
			AddPropertyToMap(pl, "bOnlyGiveToPrimaryLocalPlayer", propertyMap, "0");
			AddPropertyToMap(pl, "bUseNightmareRandomizerMultiplier", propertyMap, "0");
			AddPropertyToMap(pl, "bAllowTranscendentGear", propertyMap, "0");
			AddPropertyToMap(pl, "bForceGiveEquipmentEvenOnFirstWave", propertyMap, "0");
			AddPropertyToMap(pl, "bChooseRandomRewardEntry", propertyMap, "0");
			AddPropertyToMap(pl, "bChooseTheBestReward", propertyMap, "0");
			AddPropertyToMap(pl, "FactorUpgradesForBestReward", propertyMap, "0");
			AddPropertyToMap(pl, "bIgnoreMapOfTheWeek", propertyMap, "0");

			// Floats
			AddPropertyToMap(pl, "NightmareRandomizerMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "HardcoreRandomizerMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "ExtraInsaneRandomizerMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "ExtraInsaneHardcoreRandomizerMultiplier", propertyMap, "0.0");
			AddPropertyToMap(pl, "HardcoreMinimumTranscendentRandomizerMultiplier", propertyMap, "0.0");

			// Ints
			AddPropertyToMap(pl, "NumberOfRewardsToChooseFrom", propertyMap, "0");

			propertyMap["Template"] = obj.GetPath();
			propertyMap["Class"] = (obj.Class?.Name?.Name ?? "");

			DunDef_SeqAct_GiveEquipmentToPlayers_Data data = new DunDef_SeqAct_GiveEquipmentToPlayers_Data(propertyMap, db);
			return db.AddGiveEquipmentToPlayers(obj.GetPath(), (obj.Class?.Name?.Name ?? ""), ref data);
		}
	}
}