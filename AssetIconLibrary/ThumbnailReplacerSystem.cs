using Colossal.Serialization.Entities;

using Game;
using Game.Prefabs;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

using Unity.Entities;

namespace AssetIconLibrary
{
	internal partial class ThumbnailReplacerSystem : GameSystemBase
	{
		protected override void OnCreate()
		{
			base.OnCreate();

			Enabled = false;
		}

		protected override void OnGamePreload(Purpose purpose, GameMode mode)
		{
			base.OnGamePreload(purpose, mode);

			if (mode is GameMode.Game or GameMode.Editor)
			{
				Enabled = true;
			}
		}

		protected override void OnUpdate()
		{
			Enabled = false;

			ReplaceThumbnails();
		}

		public void ReplaceThumbnails()
		{
			var stopWatch = Stopwatch.StartNew();
			var loadedIcons = GetAvailableIcons();
			var prefabSystem = World.GetExistingSystemManaged<PrefabSystem>();
			var prefabs = typeof(PrefabSystem)
				.GetField("m_Prefabs", BindingFlags.NonPublic | BindingFlags.Instance)
				.GetValue(prefabSystem) as List<PrefabBase>;

			for (var i = 0; i < prefabs.Count; i++)
			{
				var prefab = prefabs[i];

				if (!loadedIcons.TryGetValue(prefab.name, out var thumbnail))
				{
					continue;
				}

				if (prefab.TryGet<UIObject>(out var uIObject))
				{
#if DEBUG
					if (HasVanillaIcon(prefab, uIObject))
					{
						Mod.Log.Info("VANILLAICON: " + prefab.name);
					}
#endif
					if (!Mod.Settings.OverwriteIcons && HasVanillaIcon(prefab, uIObject))
					{
						continue;
					}

					uIObject.m_Icon = thumbnail;
				}
				else
				{
					uIObject = prefab.AddComponent<UIObject>();
					uIObject.m_Priority = 1;
					uIObject.m_Icon = thumbnail;
				}
			}

			stopWatch.Stop();

			Mod.Log.Info($"Prefab icon replacement completed in {stopWatch.Elapsed.TotalSeconds}s");
		}

		private static bool HasVanillaIcon(PrefabBase prefab, UIObject uIObject)
		{
			return uIObject.m_Group is not null
				&& prefab.builtin
				&& uIObject.m_Group.builtin;
		}

		private static Dictionary<string, string> GetAvailableIcons()
		{
			var loadedIcons = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

			foreach (var item in Directory.EnumerateFiles(FolderUtil.ThumbnailsFolder, "*", SearchOption.AllDirectories))
			{
				var folder = Path.GetFileName(Path.GetDirectoryName(item));

				if (folder == "Thumbnails" || folder == Mod.Settings.IconsStyle)
				{
					loadedIcons[Path.GetFileNameWithoutExtension(item)] = $"coui://ail/{item.Substring(FolderUtil.ThumbnailsFolder.Length + 1).Replace('\\', '/')}";
				}
			}

			foreach (var folderKvp in FolderUtil.ModThumbnailsFolders)
			{
				foreach (var item in Directory.EnumerateFiles(folderKvp.Key, "*", SearchOption.AllDirectories))
				{
					loadedIcons[Path.GetFileNameWithoutExtension(item)] = $"coui://cmail-{folderKvp.Value}/{item.Substring(folderKvp.Key.Length + 1).Replace('\\', '/')}";
				}
			}

			if (Directory.Exists(FolderUtil.CustomThumbnailsFolder))
			{
				foreach (var item in Directory.EnumerateFiles(FolderUtil.CustomThumbnailsFolder, "*", SearchOption.AllDirectories))
				{
					loadedIcons[Path.GetFileNameWithoutExtension(item)] = $"coui://cail/{item.Substring(FolderUtil.CustomThumbnailsFolder.Length + 1).Replace('\\', '/')}";
				}
			}

			return loadedIcons;
		}
	}
}
