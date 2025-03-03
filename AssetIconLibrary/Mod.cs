﻿using Colossal.IO.AssetDatabase;
using Colossal.Json;
using Colossal.Logging;
using Colossal.PSI.Common;
using Colossal.PSI.PdxSdk;
using Colossal.Reflection;
using Colossal.UI;

using Game;
using Game.Modding;
using Game.SceneFlow;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Unity.Entities;

namespace AssetIconLibrary
{
	public class Mod : IMod
	{
		public static readonly ILog Log = LogManager.GetLogger(nameof(AssetIconLibrary)).SetShowsErrorsInUI(false);

		public static Setting Settings { get; private set; }

		public void OnLoad(UpdateSystem updateSystem)
		{
			Log.Info(nameof(OnLoad));

			Settings = new Setting(this);
			Settings.RegisterInOptionsUI();
			GameManager.instance.localizationManager.AddSource("en-US", new LocaleEN(Settings));
			AssetDatabase.global.LoadSettings(nameof(AssetIconLibrary), Settings, new Setting(this));

			updateSystem.UpdateAt<ThumbnailReplacerSystem>(SystemUpdatePhase.PrefabReferences);

			if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
			{
				FolderUtil.ModPath = asset.path;

				GameManager.instance.RegisterUpdater(Initialize);
			}
			else
			{
				Log.Error("Load Failed, could not get executable path");
			}

			FolderUtil.Output(Log);
		}

		private void Initialize()
		{
			Task.Run(UnpackAndSetup);
		}

		private async void UnpackAndSetup()
		{
			try
			{
				await UnpackIcons();

				foreach (var item in GameManager.instance.modManager)
				{
					AddModFolderIfExists(Path.GetDirectoryName(item.asset.path), item.asset?.assembly);
				}

				var mods = await PlatformManager.instance.GetPSI<PdxSdkPlatform>("PdxSdk").GetModsInActivePlayset();

				if (mods != null)
				{
					foreach (var item in mods)
					{
						AddModFolderIfExists(item.LocalData?.FolderAbsolutePath);
					}
				}

				GameManager.instance.RegisterUpdater(StartReplacement);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to unpack icons");
			}
		}

		private void AddModFolderIfExists(string folder, Assembly assembly = null)
		{
			if (folder is null)
			{
				return;
			}

			var dotAilFolder = Path.Combine(folder, ".ail");
			var ailFolder = Path.Combine(folder, "ail");
			var ailConfig = Directory.GetFiles(folder, "*.ail");

			if (Directory.Exists(dotAilFolder) && !FolderUtil.ModThumbnailsFolders.Contains(dotAilFolder))
			{
				FolderUtil.ModThumbnailsFolders.Add(dotAilFolder);
			}

			if (Directory.Exists(ailFolder) && !FolderUtil.ModThumbnailsFolders.Contains(ailFolder))
			{
				FolderUtil.ModThumbnailsFolders.Add(ailFolder);
			}

			foreach (var file in ailConfig)
			{
				try
				{
					var dictionary = JSON.MakeInto<Dictionary<string, string>>(JSON.Load(File.ReadAllText(file)));

					ImportIconMap(folder, dictionary);
				}
				catch { }
			}

			if (assembly?.GetTypesDerivedFrom<IMod>().FirstOrDefault()?.GetMethod("GetIconsMap", BindingFlags.Static | BindingFlags.Public) is MethodInfo method)
			{
				ImportIconMap(folder, method.Invoke(null, new object[0]) as Dictionary<string, string>);
			}
		}

		private static void ImportIconMap(string folder, Dictionary<string, string> dictionary)
		{
			if (dictionary is null)
			{
				return;
			}

			foreach (var kvp in dictionary)
			{
				if (File.Exists(Path.Combine(folder, kvp.Value)))
				{
					FolderUtil.ModIconMap[kvp.Key] = (folder, kvp.Value);
				}
				else
				{
					FolderUtil.ModIconReferenceMap[kvp.Key] = kvp.Value;
				}
			}
		}

		private async Task UnpackIcons()
		{
			var directoryInfo = new DirectoryInfo(FolderUtil.ThumbnailsFolder);
			var versionFilePath = Path.Combine(FolderUtil.ThumbnailsFolder, ".version");
			var thumbnailFolder = Path.Combine(Path.GetDirectoryName(FolderUtil.ModPath), ".Thumbnails");

			if (!Directory.Exists(thumbnailFolder))
			{
				Log.Warn("Thumbnails content not available");

				return;
			}

			if (File.Exists(versionFilePath) && File.ReadAllText(versionFilePath) == typeof(Mod).Assembly.GetName().Version.ToString())
			{
				Log.Info("Thumbnails up to date");

				return;
			}

			var stopwatch = Stopwatch.StartNew();

			if (directoryInfo.Exists)
			{
				directoryInfo.Delete(true);
			}

			directoryInfo.Create();

			var tasks = Directory.GetFiles(thumbnailFolder, "*.zip").Select(zip => Task.Run(() => UnpackZip(zip, FolderUtil.ThumbnailsFolder)));

			await Task.WhenAll(tasks);

			File.WriteAllText(versionFilePath, typeof(Mod).Assembly.GetName().Version.ToString());

			stopwatch.Stop();

			Log.Info($"{directoryInfo.GetFiles().Length} icons finished unpacking in {stopwatch.Elapsed.TotalSeconds}s");
		}

		private void UnpackZip(string item, string targetFoler)
		{
			using var stream = File.OpenRead(item);
			using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read, false);

			zipArchive.ExtractToDirectory(targetFoler);
		}

		private void StartReplacement()
		{
			UIManager.defaultUISystem.AddHostLocation($"ail", FolderUtil.ThumbnailsFolder, false, int.MaxValue);

			if (Directory.Exists(FolderUtil.CustomThumbnailsFolder))
			{
				UIManager.defaultUISystem.AddHostLocation($"ail", FolderUtil.CustomThumbnailsFolder, true, -1);
			}

			var index = 0;
			foreach (var item in FolderUtil.ModThumbnailsFolders)
			{
				UIManager.defaultUISystem.AddHostLocation($"ail", item, false, index++);
			}

			foreach (var item in FolderUtil.ModIconMap.Select(x => x.Value.Folder).Distinct())
			{
				UIManager.defaultUISystem.AddHostLocation($"ail", item, false, index++);
			}

			World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<ThumbnailReplacerSystem>().Enabled = true;
		}

		public void OnDispose()
		{
			Log.Info(nameof(OnDispose));

			if (Settings != null)
			{
				Settings.UnregisterInOptionsUI();
				Settings = null;
			}

			if (Directory.Exists(FolderUtil.CustomThumbnailsFolder))
			{
				UIManager.defaultUISystem.RemoveHostLocation($"ail");
			}
		}
	}
}
