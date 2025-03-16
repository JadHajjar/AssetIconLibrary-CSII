using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.UI;

using Game;
using Game.Modding;
using Game.SceneFlow;

using System;
using System.IO;
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
			Log.Info("Initialize started");

			Task.Run(UnpackAndSetup);
		}

		private async void UnpackAndSetup()
		{
			try
			{
				await IconUnpackerUtil.UnpackIcons();

				await IconAPIUtil.ImportModIcons();

				GameManager.instance.RunOnMainThread(SetupHostLocations);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to unpack icons");
			}

			FolderUtil.Output(Log);
		}

		private void SetupHostLocations()
		{
			Log.Info("Setting up host locations");

			UIManager.defaultUISystem.AddHostLocation("ail", FolderUtil.ThumbnailsFolder, true, int.MaxValue);

			if (Directory.Exists(FolderUtil.CustomThumbnailsFolder))
			{
				UIManager.defaultUISystem.AddHostLocation("ail", FolderUtil.CustomThumbnailsFolder, true, -1);
			}

			var index = 0;
			foreach (var item in FolderUtil.ModThumbnailsFolders)
			{
				UIManager.defaultUISystem.AddHostLocation("ail", item, true, index++);
			}

			foreach (var item in FolderUtil.ModIconMap.Select(x => x.Value.Folder).Distinct())
			{
				UIManager.defaultUISystem.AddHostLocation("ail", item, true, index++);
			}
		}

		public void OnDispose()
		{
			Log.Info(nameof(OnDispose));

			if (Settings != null)
			{
				Settings.UnregisterInOptionsUI();
				Settings = null;
			}

			UIManager.defaultUISystem.RemoveHostLocation("ail");
		}
	}
}
