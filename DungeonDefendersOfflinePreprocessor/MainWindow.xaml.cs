using DDUP;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.NetworkInformation;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shell;
using System.Windows.Threading;
using UELib;
using UELib.Core;
using UELib.Types;

namespace DungeonDefendersOfflinePreprocessor
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public ObservableCollection<string> LogLines { get; } = new();

		private const int MaxLines = 10_000; // adjust		
		public static MainWindow? Instance = null;

		public UEPackageDatabase db = new();

		public MainWindow()
		{
			InitializeComponent();
			DataContext = this;
			Instance = this;

			Log("App started.");

			UnrealConfig.VariableTypes = new Dictionary<string, Tuple<string, PropertyType>>();
			//{
			// Common UE3 arrays - add more as you discover them
			UnrealConfig.VariableTypes["Components"] = Tuple.Create("Engine.Actor.Components", PropertyType.ObjectProperty);
			UnrealConfig.VariableTypes["Skins"] = Tuple.Create("Engine.Actor.Skins", PropertyType.ObjectProperty);
			UnrealConfig.VariableTypes["AnimSets"] = Tuple.Create("Engine.SkeletalMeshComponent.AnimSets", PropertyType.ObjectProperty);
			UnrealConfig.VariableTypes["InputLinks"] = Tuple.Create("Engine.SequenceOp.InputLinks", PropertyType.StructProperty);
			UnrealConfig.VariableTypes["OutputLinks"] = Tuple.Create("Engine.SequenceOp.OutputLinks", PropertyType.StructProperty);
			UnrealConfig.VariableTypes["VariableLinks"] = Tuple.Create("Engine.SequenceOp.VariableLinks", PropertyType.StructProperty);
			UnrealConfig.VariableTypes["Targets"] = Tuple.Create("Engine.SequenceAction.Targets", PropertyType.ObjectProperty);
			UnrealConfig.VariableTypes["Controls"] = Tuple.Create("XInterface.GUIComponent.Controls", PropertyType.ObjectProperty);
			UnrealConfig.VariableTypes["Expressions"] = Tuple.Create("Engine.Material.Expressions", PropertyType.ObjectProperty);
			UnrealConfig.VariableTypes["Modules"] = Tuple.Create("Engine.ParticleEmitter.Modules", PropertyType.ObjectProperty);
			UnrealConfig.VariableTypes["Emitters"] = Tuple.Create("Engine.ParticleSystem.Emitters", PropertyType.ObjectProperty);
			UnrealConfig.VariableTypes["InstanceParameters"] = Tuple.Create("Engine.ParticleSystemComponent.InstanceParameters", PropertyType.StructProperty);
			UnrealConfig.VariableTypes["PrimaryColorSets"] = Tuple.Create("UDKGame.HeroEquipment.PrimaryColorSets", PropertyType.LinearColor);
			UnrealConfig.VariableTypes["SecondaryColorSets"] = Tuple.Create("UDKGame.HeroEquipment.SecondaryColorSets", PropertyType.LinearColor);
			UnrealConfig.VariableTypes["RandomBaseNames"] = Tuple.Create("UDKGame.HeroEquipment.RandomBaseNames", PropertyType.StructProperty);
			UnrealConfig.VariableTypes["MaxLevelRangeDifficultyArray"] = Tuple.Create("UDKGame.HeroEquipment.MaxLevelRangeDifficultyArray", PropertyType.StructProperty);
			UnrealConfig.VariableTypes["QualityDescriptorNames"] = Tuple.Create("UDKGame.HeroEquipment.QualityDescriptorNames", PropertyType.StructProperty);
			UnrealConfig.VariableTypes["LevelRequirementOverrides"] = Tuple.Create("UDKGame.HeroEquipment.LevelRequirementOverrides", PropertyType.StructProperty);


			UnrealConfig.VariableTypes["MeleeSwingInfoMultipliers"] = Tuple.Create("UDKGame.DunDefPlayer.MeleeSwingInfoMultipliers", PropertyType.StructProperty);
			UnrealConfig.VariableTypes["MainHandSwingInfoMultipliers"] = Tuple.Create("UDKGame.DunDefPlayer.MainHandSwingInfoMultipliers", PropertyType.StructProperty);
			UnrealConfig.VariableTypes["OffHandSwingInfoMultipliers"] = Tuple.Create("UDKGame.DunDefPlayer.OffHandSwingInfoMultipliers", PropertyType.StructProperty);


			UnrealConfig.VariableTypes["ScalarParameterValues"] = Tuple.Create("Engine.MaterialInstanceConstant.ScalarParameterValues", PropertyType.FloatProperty);
			UnrealConfig.VariableTypes["VectorParameterValues"] = Tuple.Create("Engine.MaterialInstanceConstant.VectorParameterValues", PropertyType.Vector4);
			UnrealConfig.VariableTypes["TextureParameterValues"] = Tuple.Create("Engine.MaterialInstanceConstant.TextureParameterValues", PropertyType.StructProperty);
		}

		private readonly StringBuilder _logBuilder = new();

		public string LogText
		{
			get => _logBuilder.ToString();
		}


		public static async Task RunExtractorAsync(string outDir, string inputUpk)
		{
			var exePath = @"e:\ddgotools\umodel.exe";

			if (!System.IO.File.Exists(exePath))
				throw new System.IO.FileNotFoundException("umodel.exe not found", exePath);
			

			var psi = new ProcessStartInfo
			{
				FileName = exePath,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};

			psi.ArgumentList.Add($"-export");
			psi.ArgumentList.Add($"-path={outDir}");
			psi.ArgumentList.Add($"-out={outDir}");
			psi.ArgumentList.Add($"-png");
			psi.ArgumentList.Add($"-groups");
			psi.ArgumentList.Add($"-noanim");
			psi.ArgumentList.Add($"-nomesh");
			psi.ArgumentList.Add($"-nostat");
			psi.ArgumentList.Add($"-novert");
			psi.ArgumentList.Add($"-nomorph");
			psi.ArgumentList.Add($"-nolightmap");
			psi.ArgumentList.Add(inputUpk);

			Log(psi.Arguments);


			using var process = new Process { StartInfo = psi };

			process.OutputDataReceived += (_, e) =>
			{
				if (e.Data != null)
					MainWindow.Instance?.Dispatcher.BeginInvoke(new Action(() => Log(e.Data)));
			};

			process.ErrorDataReceived += (_, e) =>
			{
				if (e.Data != null)
					MainWindow.Instance?.Dispatcher.BeginInvoke(new Action(() => Log("[ERR] " + e.Data)));
			};

			Log($"Running: {psi.FileName} {psi.Arguments}");
			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			await process.WaitForExitAsync();

			Log($"Process exited with code {process.ExitCode}");

		}



		public static async Task RunDecompressAsync(string outDir, string inputUpkPath)
		{
			var exePath = @"e:\ddgotools\decompress.exe";


			if (!System.IO.File.Exists(exePath))
				throw new System.IO.FileNotFoundException("decompress.exe not found", exePath);

			if (!System.IO.File.Exists(inputUpkPath))
				throw new System.IO.FileNotFoundException("Input .upk not found", inputUpkPath);

			
			var psi = new ProcessStartInfo
			{
				FileName = exePath,
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true
			};
			psi.ArgumentList.Add($"-out={outDir}");
			psi.ArgumentList.Add(inputUpkPath);

			Log(psi.Arguments);


			using var process = new Process { StartInfo = psi };

			process.OutputDataReceived += (_, e) =>
			{
				if (e.Data != null)
					MainWindow.Instance?.Dispatcher.BeginInvoke(new Action(() => Log(e.Data)));
			};

			process.ErrorDataReceived += (_, e) =>
			{
				if (e.Data != null)
					MainWindow.Instance?.Dispatcher.BeginInvoke(new Action(() => Log("[ERR] " + e.Data)));
			};

			Log($"Running: {psi.FileName} {psi.Arguments}");
			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();

			await process.WaitForExitAsync();

			Log($"Process exited with code {process.ExitCode}");

		}


		private async void Process_Click(object sender, RoutedEventArgs e)
		{
			var workingDir = @"E:\Temp\DunDef";
			var packageDir = @"g:\SteamLibrary\steamapps\common\Dungeon Defenders\UDKGame\CookedPCConsole\";

			string[] files = Directory.GetFiles(packageDir);
			System.IO.Directory.CreateDirectory(workingDir);

			// all the templates we need are in these two, as well as the icons
			string[] upkFiles = { "Startup_INT.upk", "UDKGame.upk", "Core.upk" };

			Log("Processing...");
			await Task.Run(async () =>
			{
				// 1) Synchronous heavy work -> background thread
				foreach (var fileName in upkFiles)
				{					
					db.AddToDatabase(workingDir, fileName);
				}

				// export the texture atlas
				db.ExportAllHeroEquipmentToAtlas();

				// 2) Your async method can run here too
				var tdb = new DDUP.ExportedTemplateDatabase();
				await db.AddObjectsToDatabase(tdb).ConfigureAwait(false);
				foreach (var kvp in Parse.UniqueFailedKeys)
				{
					MainWindow.Log($"Parse Error on [{kvp.Key}]: {kvp.Value}");
				}
				try
				{
					MainWindow.Log("Saving template database...");
					byte[] rawData = tdb.SaveToRaw();
					string dbPath = Path.Combine(workingDir, "DunDefTemplates.dbz");
					using (FileStream fileStream = File.Create(dbPath))
					using (GZipStream compressionStream = new GZipStream(fileStream, CompressionMode.Compress))
					{
						compressionStream.Write(rawData, 0, rawData.Length);
					}
					
					MainWindow.Log($"Template database saved to {dbPath} ({rawData.Length:N0} bytes)");
				}
				catch (Exception ex)
				{
					MainWindow.Log($"Error saving template database: {ex.Message}");
				}

			}).ConfigureAwait(true);			
		}

		private const int MaxChars = 2_000_000; // ~2MB of text

		public static void Log(string message)
		{
			if (Instance == null)
				return;
			System.Diagnostics.Debug.WriteLine(message);

			Instance.Dispatcher.Invoke(() =>
			{
				string line = $"[{DateTime.Now:HH:mm:ss}] {message}";

				Instance._logBuilder.AppendLine(line);

				// Trim if too large
				if (Instance._logBuilder.Length > MaxChars)
				{
					Instance._logBuilder.Remove(0, Instance._logBuilder.Length / 4);
				}

				Instance.LogTextBox.Text = Instance._logBuilder.ToString();
				Instance.LogTextBox.CaretIndex = Instance.LogTextBox.Text.Length;
				Instance.LogTextBox.ScrollToEnd();
			});
		}

		// Detect whether the user is at (or very near) the bottom.
		// If they scrolled up, do NOT yank them back down.
		private static bool IsUserAtBottom(ListBox listBox)
		{
			var sv = FindDescendantScrollViewer(listBox);
			if (sv == null) return true; 

			return sv.VerticalOffset >= sv.ScrollableHeight - 1;
		}

		private static ScrollViewer? FindDescendantScrollViewer(DependencyObject root)
		{
			if (root is ScrollViewer sv) return sv;

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
			{
				var child = VisualTreeHelper.GetChild(root, i);
				var result = FindDescendantScrollViewer(child);
				if (result != null) return result;
			}

			return null;
		}		

	}
}