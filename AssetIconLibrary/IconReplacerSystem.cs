using AssetIconLibrary.Utilities;

using Colossal.Serialization.Entities;

using Game;
using Game.Prefabs;

using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

using Unity.Entities;

namespace AssetIconLibrary
{
	internal partial class IconReplacerSystem : GameSystemBase
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
			var loadedIcons = IconLibraryUtil.GetAvailableIcons();
			var prefabSystem = World.GetExistingSystemManaged<PrefabSystem>();
			var prefabs = typeof(PrefabSystem)
				.GetField("m_Prefabs", BindingFlags.NonPublic | BindingFlags.Instance)
				.GetValue(prefabSystem) as List<PrefabBase>;

			for (var i = 0; i < prefabs.Count; i++)
			{
				var prefab = prefabs[i];

				if (!loadedIcons.TryGetValue(prefab.name, out var thumbnail)
					&& !loadedIcons.TryGetValue($"{prefab.GetType().Name}.{prefab.name}", out thumbnail))
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
	}
}
