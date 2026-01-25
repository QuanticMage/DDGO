using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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
using System.Windows.Shapes;
using System.Windows.Threading;
using UELib;
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
			UnrealConfig.VariableTypes["PrimaryColorSets"] = Tuple.Create("UDKGame.HeroEquipment.PrimaryColorSets", PropertyType.StructProperty);
			UnrealConfig.VariableTypes["SecondaryColorSets"] = Tuple.Create("UDKGame.HeroEquipment.SecondaryColorSets", PropertyType.StructProperty);
			UnrealConfig.VariableTypes["RandomBaseNames"] = Tuple.Create("UDKGame.HeroEquipment.RandomBaseNames", PropertyType.StructProperty);
			UnrealConfig.VariableTypes["MaxLevelRangeDifficultyArray"] = Tuple.Create("UDKGame.HeroEquipment.MaxLevelRangeDifficultyArray", PropertyType.StructProperty);
			UnrealConfig.VariableTypes["QualityDescriptorNames"] = Tuple.Create("UDKGame.HeroEquipment.QualityDescriptorNames", PropertyType.StructProperty);
			UnrealConfig.VariableTypes["LevelRequirementOverrides"] = Tuple.Create("UDKGame.HeroEquipment.LevelRequirementOverrides", PropertyType.StructProperty);

			UnrealConfig.VariableTypes["TextureParameterValues"] = Tuple.Create("Engine.MaterialInstanceConstant.TextureParameterValues", PropertyType.StructProperty);
			//};
			
			
		}

	

		public static async Task RunDecompressAsync(string outDir, string inputUpkPath)
		{
			var exePath = @"e:\ddgotools\decompress.exe";

			if (!System.IO.File.Exists(exePath))
				throw new System.IO.FileNotFoundException("decompress.exe not found", exePath);

			if (!System.IO.File.Exists(inputUpkPath))
				throw new System.IO.FileNotFoundException("Input .upk not found", inputUpkPath);

			System.IO.Directory.CreateDirectory(outDir);

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
			Log("Processing...");

			var upk = "Startup_INT.upk";
			var upkBase = "UDKGame.upk";
			var upkCore = "Core.upk";
			var upkEngine = "Core.upk";
			var packageDir = @"G:\SteamLibrary\steamapps\common\Dungeon Defenders\UDKGame\CookedPCConsole\";

			// Always build paths with Path.Combine			


			await db.AddToDatabase(packageDir, upkCore);
			await db.AddToDatabase(packageDir, upkEngine);
			
			await db.AddToDatabase(packageDir, upkBase);			
			await db.AddToDatabase(packageDir, upk);
			db.ExportAllHeroEquipmentToAtlas();
			db.DumpObjectsToFile(@"E:\DDGO\DungeonDefendersGearOptimizer\GeneratedItemTable.cs");
			//db.ExportAllHeroEquipmentToAtlas();
			//db.Explore();
		}

		public static void Log(string message)
		{
			if (Instance == null)
				return;

			string line = $"[{DateTime.Now:HH:mm:ss}] {message}";

			bool shouldAutoScroll = IsUserAtBottom(Instance.LogList);

			Instance.LogLines.Add(line);

			// Keep memory bounded
			while (Instance.LogLines.Count > MaxLines)
				Instance.LogLines.RemoveAt(0);

			if (shouldAutoScroll)
			{
				// Wait until UI has realized containers for the new item
				Instance.Dispatcher.BeginInvoke(new Action(() =>
				{
					Instance.LogList.ScrollIntoView(Instance.LogLines[^1]);
				}), DispatcherPriority.Background);
			}
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