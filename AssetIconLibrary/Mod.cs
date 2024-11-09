using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.UI;

using Game;
using Game.Modding;
using Game.SceneFlow;

using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

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
			AssetDatabase.global.LoadSettings(nameof(AssetIconLibrary), Settings, new Setting(this) { DefaultBlock = true });

			if (GameManager.instance.modManager.TryGetExecutableAsset(this, out var asset))
			{
				FolderUtil.ModPath = asset.path;

				Task.Run(UnpackIcons);

				if (Directory.Exists(FolderUtil.CustomContentFolder))
				{
					UIManager.defaultUISystem.AddHostLocation($"cail", FolderUtil.CustomContentFolder, false);
				}

				updateSystem.UpdateAt<ThumbnailReplacerSystem>(SystemUpdatePhase.PrefabReferences);
			}
			else
			{
				Log.Error("Load Failed, could not get executable path");
			}
		}

		private async void UnpackIcons()
		{
			var targetFoler = Path.Combine(FolderUtil.ContentFolder, "Thumbnails");
			var directoryInfo = new DirectoryInfo(targetFoler);

			if (directoryInfo.Exists && directoryInfo.LastWriteTime > File.GetLastWriteTime(FolderUtil.ModPath))
			{
				SetThumbnailFolder(targetFoler);

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

			var tasks = Directory.GetFiles(thumbnailFolder, "*.zip").Select(zip => Task.Run(() => UnpackZip(zip, targetFoler)));

			await Task.WhenAll(tasks);

			stopwatch.Stop();

			Log.Info($"{directoryInfo.GetFiles().Length} icons finished unpacking in {stopwatch.Elapsed.TotalSeconds}s");

			SetThumbnailFolder(targetFoler);
		}

		private static void SetThumbnailFolder(string targetFoler)
		{
			UIManager.defaultUISystem.AddHostLocation($"ail", targetFoler, false);

			ThumbnailReplacerSystem.ThumbnailPath = targetFoler;
		}

		private void UnpackZip(string item, string targetFoler)
		{
			using var stream = File.OpenRead(item);
			using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read, false);

			zipArchive.ExtractToDirectory(targetFoler);
		}

		public void OnDispose()
		{
			Log.Info(nameof(OnDispose));

			if (Settings != null)
			{
				Settings.UnregisterInOptionsUI();
				Settings = null;
			}
		}
	}
}
