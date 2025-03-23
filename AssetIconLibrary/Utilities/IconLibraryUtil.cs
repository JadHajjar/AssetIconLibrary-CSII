using Colossal.IO.AssetDatabase;
using Colossal.UI;

using Game.SceneFlow;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AssetIconLibrary.Utilities
{
	internal class IconLibraryUtil
	{
		private static bool iconsLoaded;
		private static Dictionary<string, string> cache;

		internal static Dictionary<string, string> GetAvailableIcons()
		{
			if (iconsLoaded)
			{
				return cache;
			}

			iconsLoaded = true;

			IconAPIUtil.LoadModIcons();

			GameManager.instance.RunOnMainThread(SetupHostLocations);

			cache = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

			if (!AssetDatabase.global.TryGetDatabase("User", out var database) || database is not ILocalAssetDatabase localAssetDatabase)
			{
				Mod.Log.Error("Failed to get User database to import thumbnail");
				return cache;
			}

			foreach (var item in Directory.EnumerateFiles(FolderUtil.ThumbnailsFolder, "*", SearchOption.AllDirectories))
			{
				var folder = Path.GetFileName(Path.GetDirectoryName(item));

				if (folder == ".Thumbnails" || folder == Mod.Settings.IconsStyle)
				{
					addIcon(item, FolderUtil.ThumbnailsFolder);
				}
			}

			foreach (var folder in FolderUtil.ModThumbnailsFolders)
			{
				foreach (var item in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
				{
					addIcon(item, folder);
				}
			}

			foreach (var kvp in FolderUtil.ModIconMap)
			{
				addIconName(kvp.Key, kvp.Value.File, kvp.Value.Folder);
			}

			foreach (var kvp in FolderUtil.ModIconReferenceMap)
			{
				if (cache.TryGetValue(kvp.Value, out var reference))
				{
					cache[kvp.Key] = reference;
				}
			}

			if (Directory.Exists(FolderUtil.CustomThumbnailsFolder))
			{
				foreach (var item in Directory.EnumerateFiles(FolderUtil.CustomThumbnailsFolder, "*", SearchOption.AllDirectories))
				{
					addIcon(item, FolderUtil.CustomThumbnailsFolder);
				}
			}

			return cache;

			void addIcon(string path, string startingFolder) => addIconName(Path.GetFileNameWithoutExtension(path), path, startingFolder);

			void addIconName(string name, string path, string startingFolder)
			{
				if (Path.GetExtension(path).ToLower() is ".svg")
				{
					cache[name] = $"coui://ail/{path.Substring(startingFolder.Length + 1).Replace('\\', '/')}";
					return;
				}

				if (Path.GetExtension(path).ToLower() is not ".png" and not ".jpg" and not ".jpeg")
				{
					return;
				}

				cache[name] = "assetdb://global/" + localAssetDatabase.AddAsset(AssetDataPath.Create(Path.GetDirectoryName(path), Path.GetFileName(path), EscapeStrategy.None)).id.guid;
			}
		}

		private static void SetupHostLocations()
		{
			Mod.Log.Info("Setting up host locations");

			UIManager.defaultUISystem.AddHostLocation("ail", FolderUtil.ThumbnailsFolder, false, int.MaxValue);

			if (Directory.Exists(FolderUtil.CustomThumbnailsFolder))
			{
				UIManager.defaultUISystem.AddHostLocation("ail", FolderUtil.CustomThumbnailsFolder, false, -1);
			}

			var index = 0;
			foreach (var item in FolderUtil.ModThumbnailsFolders)
			{
				UIManager.defaultUISystem.AddHostLocation("ail", item, false, index++);
			}

			foreach (var item in FolderUtil.ModIconMap.Select(x => x.Value.Folder).Distinct())
			{
				UIManager.defaultUISystem.AddHostLocation("ail", item, false, index++);
			}
		}
	}
}
