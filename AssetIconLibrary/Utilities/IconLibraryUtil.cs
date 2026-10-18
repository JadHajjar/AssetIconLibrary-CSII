using Colossal.Core;
using Colossal.UI;

using Game.SceneFlow;

using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AssetIconLibrary.Utilities
{
	internal class IconLibraryUtil
	{
		private static Dictionary<string, string> cache;
		private static string packStyle;

		internal static bool IconsLoaded { get; private set; }

		internal static void LoadPackIcons()
		{
			var style = Mod.Settings.IconsStyle;
			var icons = new Dictionary<string, string>(new IconNameComparer());

			addFolder(FolderUtil.ThumbnailsFolder);
			addFolder(Path.Combine(FolderUtil.ThumbnailsFolder, style));

			cache = icons;
			packStyle = style;

			void addFolder(string folder)
			{
				if (!Directory.Exists(folder))
				{
					return;
				}

				foreach (var item in Directory.EnumerateFiles(folder))
				{
					AddIcon(icons, item, FolderUtil.ThumbnailsFolder);
				}
			}
		}

		internal static Dictionary<string, string> GetAvailableIcons()
		{
			if (IconsLoaded)
			{
				return cache;
			}

			if (packStyle != Mod.Settings.IconsStyle)
			{
				LoadPackIcons();
			}

			// Set before anything below can throw, so later loads reuse what a failed build listed.
			IconsLoaded = true;

			IconAPIUtil.LoadModIcons();

			// Before the walks that can throw, so the icons listed so far still get their host.
			MainThreadDispatcher.RunOnMainThread(SetupHostLocations);

			foreach (var folder in FolderUtil.ModThumbnailsFolders)
			{
				if (!Directory.Exists(folder))
				{
					continue;
				}

				foreach (var item in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
				{
					AddIcon(cache, item, folder);
				}
			}

			foreach (var kvp in FolderUtil.ModIconMap)
			{
				AddIconName(cache, kvp.Key, kvp.Value.File, kvp.Value.Folder);
			}

			var customIcons = new Dictionary<string, string>(new IconNameComparer());

			if (Directory.Exists(FolderUtil.CustomThumbnailsFolder))
			{
				foreach (var item in Directory.EnumerateFiles(FolderUtil.CustomThumbnailsFolder, "*", SearchOption.AllDirectories))
				{
					AddIcon(customIcons, item, FolderUtil.CustomThumbnailsFolder);
				}
			}

			foreach (var kvp in FolderUtil.ModIconReferenceMap)
			{
				if (customIcons.TryGetValue(kvp.Value, out var reference)
					|| cache.TryGetValue(kvp.Value, out reference))
				{
					cache[kvp.Key] = reference;
				}
			}

			// Merged last, so a custom icon wins over a reference with the same name.
			foreach (var kvp in customIcons)
			{
				cache[kvp.Key] = kvp.Value;
			}

			return cache;
		}

		private static void AddIcon(
			Dictionary<string, string> icons, string path, string startingFolder)
		{
			AddIconName(icons, Path.GetFileNameWithoutExtension(path), path, startingFolder);
		}

		private static void AddIconName(
			Dictionary<string, string> icons, string name, string path, string startingFolder)
		{
			var extension = Path.GetExtension(path).ToLower();

			if (extension is not ".svg" and not ".png" and not ".jpg" and not ".jpeg")
			{
				return;
			}

			icons[name] = $"coui://ail/{path.Substring(startingFolder.Length + 1).Replace('\\', '/')}";
		}

		private static void SetupHostLocations()
		{
			Mod.Log.Info("Setting up host locations");

			UIManager.defaultUISystem.AddHostLocation("ail", FolderUtil.ThumbnailsFolder, false, int.MaxValue);

			if (Directory.Exists(FolderUtil.CustomThumbnailsFolder))
			{
				UIManager.defaultUISystem.AddHostLocation("ail", FolderUtil.CustomThumbnailsFolder, false, -1);
			}

			// The host serves a shared file from the first folder that holds it.
			var index = 0;
			foreach (var item in FolderUtil.ModThumbnailsFolders.AsEnumerable().Reverse())
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
