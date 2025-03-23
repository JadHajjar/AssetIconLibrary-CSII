using AssetIconLibrary.Utilities;
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

			updateSystem.UpdateAt<IconReplacerSystem>(SystemUpdatePhase.PrefabReferences);

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

				await IconAPIUtil.GetModIcons();
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Failed to unpack icons");
			}

			FolderUtil.Output(Log);
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
