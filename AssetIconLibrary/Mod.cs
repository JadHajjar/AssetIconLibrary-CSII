using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.UI;

using Game;
using Game.Modding;
using Game.SceneFlow;

using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
		}

		private void Initialize()
		{
			Task.Run(SetupAndUnpack);
		}

		private async void SetupAndUnpack()
		{
			await UnpackIcons();

			foreach (var item in GameManager.instance.modManager)
			{
				var customFolder = Path.Combine(Path.GetDirectoryName(item.asset.path), "ail");

				if (Directory.Exists(customFolder))
				{
					FolderUtil.ModThumbnailsFolders[customFolder] = Guid.NewGuid().ToString();
				}
			}

			GameManager.instance.RegisterUpdater(StartReplacement);
		}

		private async Task UnpackIcons()
		{
			var directoryInfo = new DirectoryInfo(FolderUtil.ThumbnailsFolder);

			if (directoryInfo.Exists && directoryInfo.LastWriteTime > File.GetLastWriteTime(FolderUtil.ModPath))
			{
				Log.Info("Thumbnails up to date");

				return;
			}

			var stopwatch = Stopwatch.StartNew();
			var thumbnailFolder = Path.Combine(Path.GetDirectoryName(FolderUtil.ModPath), ".Thumbnails");

			if (directoryInfo.Exists)
			{
				directoryInfo.Delete(true);
			}

			directoryInfo.Create();

			var tasks = Directory.GetFiles(thumbnailFolder, "*.zip").Select(zip => Task.Run(() => UnpackZip(zip, FolderUtil.ThumbnailsFolder)));

			await Task.WhenAll(tasks);

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
			UIManager.defaultUISystem.AddHostLocation($"ail", FolderUtil.ThumbnailsFolder, false);

			if (Directory.Exists(FolderUtil.CustomThumbnailsFolder))
			{
				UIManager.defaultUISystem.AddHostLocation($"cail", FolderUtil.CustomThumbnailsFolder, true);
			}

			foreach (var item in FolderUtil.ModThumbnailsFolders)
			{
				UIManager.defaultUISystem.AddHostLocation($"cmail-{item.Value}", item.Key, false);
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
				UIManager.defaultUISystem.RemoveHostLocation($"cail");
			}
		}
	}
}
