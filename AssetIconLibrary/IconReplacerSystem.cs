using AssetIconLibrary.Utilities;

using Colossal.Core;
using Colossal.Serialization.Entities;

using Game;
using Game.Prefabs;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

using Unity.Entities;

namespace AssetIconLibrary
{
	internal partial class IconReplacerSystem : GameSystemBase
	{
		// Prefabs keep their icons across loads in a session.
		private readonly HashSet<PrefabBase> handledPrefabs = new();
		private readonly HashSet<PrefabBase> handledMeshes = new();
		private bool isEditor;

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
				isEditor = mode is GameMode.Editor;
				Enabled = true;
			}
		}

		protected override void OnUpdate()
		{
			Enabled = false;

			if (Mod.SetupTask.IsCompleted)
			{
				ReplaceThumbnails();
				return;
			}

			// Waiting on the boot task would freeze this frame for the whole unpack.
			Mod.Log.Info("Icons are not ready yet, replacing them once they are");

			Mod.SetupTask.ContinueWith(_ => MainThreadDispatcher.RunOnMainThread(replaceLater));

			void replaceLater()
			{
				// The dispatcher retries a failing action on every frame.
				try
				{
					ReplaceThumbnails();
				}
				catch (Exception ex)
				{
					Mod.Log.Error(ex, "Failed to replace prefab icons");
				}
			}
		}

		public void ReplaceThumbnails()
		{
			var stopWatch = Stopwatch.StartNew();
			var loadedIcons = IconLibraryUtil.GetAvailableIcons();
			var prefabSystem = World.GetExistingSystemManaged<PrefabSystem>();
			var prefabs = typeof(PrefabSystem)
				.GetField("m_Prefabs", BindingFlags.NonPublic | BindingFlags.Instance)?
				.GetValue(prefabSystem) as List<PrefabBase>;

			if (prefabs is null)
			{
				Mod.Log.Error("Could not access PrefabSystem.m_Prefabs, icon replacement skipped");
				return;
			}

			var shouldSetMeshIcons = isEditor && Mod.Settings.RenderPrefabIcons;

			for (var i = 0; i < prefabs.Count; i++)
			{
				var prefab = prefabs[i];

				if (prefab is null)
				{
					continue;
				}

				var isNewPrefab = handledPrefabs.Add(prefab);
				var geometryPrefab = prefab as ObjectGeometryPrefab;
				var hasNewMeshes = shouldSetMeshIcons
					&& geometryPrefab is not null
					&& geometryPrefab.m_Meshes is not null
					&& handledMeshes.Add(prefab);

				if (!isNewPrefab && !hasNewMeshes)
				{
					continue;
				}

				var name = prefab.name;

				if (!loadedIcons.TryGetValue(name, out var thumbnail)
					&& !loadedIcons.TryGetValue($"{prefab.GetType().Name}.{name}", out thumbnail))
				{
					continue;
				}

				if (isNewPrefab)
				{
					UpdatePrefab(prefab, thumbnail);
				}

				if (hasNewMeshes)
				{
					foreach (var item in geometryPrefab.m_Meshes)
					{
						if (item?.m_Mesh is not null)
						{
							UpdatePrefab(item.m_Mesh, thumbnail);
						}
					}
				}
			}

			stopWatch.Stop();

			Mod.Log.Info($"Prefab icon replacement completed in {stopWatch.Elapsed.TotalSeconds}s");
		}

		private static void UpdatePrefab(PrefabBase prefab, string thumbnail)
		{
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
					return;
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

		private static bool HasVanillaIcon(PrefabBase prefab, UIObject uIObject)
		{
			return uIObject.m_Group is not null
				&& prefab.isBuiltin
				&& uIObject.m_Group.isBuiltin;
		}
	}
}
