using Colossal.IO.AssetDatabase;
using Colossal.Logging;
using Colossal.UI;

using Game;
using Game.Modding;
using Game.SceneFlow;
using Game.Settings;

using System.IO;

using UnityEngine;

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
				UIManager.defaultUISystem.AddHostLocation($"ail", ThumbnailReplacerSystem.ThumbnailPath = Path.Combine(Path.GetDirectoryName(asset.path), "Thumbnails"), false);

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
