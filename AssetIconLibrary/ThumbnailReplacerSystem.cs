﻿using Game;
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
		internal static string ThumbnailPath { get; set; }

		protected override void OnCreate()
		{
			base.OnCreate();

			Enabled = false;
		}

		protected override void OnUpdate()
		{
			Enabled = false;

			var stopWatch = Stopwatch.StartNew();
			var loadedIcons = GetAvailableIcons();
			var prefabSystem = World.GetExistingSystemManaged<PrefabSystem>();
			var prefabs = typeof(PrefabSystem)
				.GetField("m_Prefabs", BindingFlags.NonPublic | BindingFlags.Instance)
				.GetValue(prefabSystem) as List<PrefabBase>;

#if DEBUG
			Mod.Log.Info("Loaded Thumbnails");
			foreach (var item in loadedIcons)
			{
				Mod.Log.InfoFormat("[{0}] = '{1}'", item.Key, item.Value);
			}
#endif

			for (var i = 0; i < prefabs.Count; i++)
			{
				var prefab = prefabs[i];

				if (!loadedIcons.TryGetValue(prefab.name, out var thumbnail))
				{
					continue;
				}

				if (prefab.TryGet<UIObject>(out var uIObject))
				{
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

			foreach (var item in Directory.EnumerateFiles(ThumbnailPath, "Brand_*.png"))
			{
				for (var i = 0; i < _brandGroups.Length; i++)
				{
					loadedIcons[_brandGroups[i] + Path.GetFileNameWithoutExtension(item).Substring(6)] = $"coui://ail/{Path.GetFileName(item)}";
				}
			}

			foreach (var item in Directory.EnumerateFiles(ThumbnailPath))
			{
				loadedIcons[Path.GetFileNameWithoutExtension(item)] = $"coui://ail/{Path.GetFileName(item)}";
			}

			if (Directory.Exists(FolderUtil.CustomThumbnailsFolder))
			{
				foreach (var item in Directory.EnumerateFiles(FolderUtil.CustomThumbnailsFolder, "*", SearchOption.AllDirectories))
				{
					loadedIcons[Path.GetFileNameWithoutExtension(item)] = $"coui://cail/{item.Substring(FolderUtil.CustomThumbnailsFolder.Length + 1)}";
				}
			}

			foreach (var folderKvp in FolderUtil.ModThumbnailsFolders)
			{
				foreach (var item in Directory.EnumerateFiles(folderKvp.Key, "*", SearchOption.AllDirectories))
				{
					loadedIcons[Path.GetFileNameWithoutExtension(item)] = $"coui://{folderKvp.Value}/{item.Substring(folderKvp.Key.Length + 1)}";
				}
			}

			return loadedIcons;
		}

		private static readonly string[] _brandGroups = new[]
		{
			"SignFrontwayLarge02 - ",
			"SignFrontwayMedium01 - ",
			"SignFrontwayMedium02 - ",
			"SignFrontwaySmall01 - ",
			"SignFrontwaySmall02 - ",
			"SignNeonLarge01 - ",
			"SignNeonLarge02 - ",
			"SignNeonMedium01 - ",
			"SignNeonMedium02 - ",
			"SignNeonSmall01 - ",
			"SignNeonSmall02 - ",
			"SignRoundLarge01 - ",
			"SignSidewayLarge01 - ",
			"SignSidewayLarge02 - ",
			"SignSidewayMedium01 - ",
			"SignSidewayMedium02 - ",
			"SignSidewaySmall01 - ",
			"SignSidewaySmall02 - ",
			"AStand01 - ",
			"AStand02 - ",
			"Stand01 - ",
			"Stand02 - ",
			"Screen02 - ",
			"SignFrontwayLarge01 - ",
			"BillboardHuge02 - ",
			"BillboardLarge02 - ",
			"BillboardMedium02 - ",
			"BillboardSmall01 - ",
			"BillboardSmall02 - ",
			"BillboardRoundLarge01 - ",
			"BillboardRoundMedium01 - ",
			"BillboardRoundSmall01 - ",
			"BillboardWallHuge02 - ",
			"BillboardWallLarge01 - ",
			"BillboardWallLarge03 - ",
			"BillboardWallMedium01 - ",
			"BillboardWallMedium02 - ",
			"BillboardWallSmall01 - ",
			"BillboardWallSmall02 - ",
			"BillboardWallSmall03 - ",
			"BillboardWallSmall04 - ",
			"GasStationPylon01 - ",
			"GasStationPylon02 - ",
			"GasStationPylon03 - ",
			"PosterHuge01 - ",
			"PosterHuge02 - ",
			"PosterLarge01 - ",
			"PosterLarge02 - ",
			"PosterMedium01 - ",
			"PosterMedium02 - ",
			"PosterSmall01 - ",
			"PosterSmall02 - ",
			"Screen01 - ",
			"BillboardLarge01 - ",
			"BillboardMedium01 - ",
			"BillboardWallHuge01 - ",
			"BillboardWallLarge02 - ",
			"BillboardWallLarge04 - ",
			"BillboardHuge01 - ",
		};
	}
}
