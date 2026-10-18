using Colossal.Json;
using Colossal.PSI.Common;
using Colossal.PSI.PdxSdk;
using Colossal.Reflection;

using Game.Modding;
using Game.SceneFlow;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace AssetIconLibrary.Utilities
{
	internal static class IconAPIUtil
	{
		private static readonly HashSet<string> scannedFolders =
			new(StringComparer.OrdinalIgnoreCase);

		internal static async Task GetModIcons()
		{
			var mods = await PlatformManager.instance.GetPSI<PdxSdkPlatform>("PdxSdk").GetModsInActivePlayset();

			if (mods != null)
			{
				foreach (var item in mods)
				{
					AddModFolderIfExists(item.path);
				}
			}
		}

		// At the load, as local mods are not in the playset and code mods can fill their maps late.
		internal static void LoadModIcons()
		{
			foreach (var item in GameManager.instance.modManager)
			{
				if (!File.Exists(item?.asset?.path))
				{
					continue;
				}

				var folder = Path.GetDirectoryName(item.asset.path);

				try
				{
					AddModFolderIfExists(folder);

					// A mod that failed to load has no map to give, and its types may not load.
					if (item.state is ModManager.ModInfo.State.Loaded)
					{
						LoadModAPI(folder, item.asset.assembly);
					}
				}
				catch (Exception ex)
				{
					Mod.Log.Warn(ex, $"Failed to load mod icons: {folder}");
				}
			}
		}

		private static void AddModFolderIfExists(string folder)
		{
			if (folder is null || !Directory.Exists(folder))
			{
				return;
			}

			if (!scannedFolders.Add(Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar)))
			{
				return;
			}

			Mod.Log.Debug($"Checking folder: {folder}");

			var dotAilFolder = Path.Combine(folder, ".ail");
			var ailFolder = Path.Combine(folder, "ail");
			string[] ailConfig;

			try
			{
				ailConfig = Directory.GetFiles(folder, "*.ail", SearchOption.AllDirectories);
			}
			catch (Exception ex)
			{
				Mod.Log.Warn(ex, $"Failed to scan mod folder: {folder}");

				return;
			}

			if (Directory.Exists(dotAilFolder) && !FolderUtil.ModThumbnailsFolders.Contains(dotAilFolder))
			{
				Mod.Log.Info($"Adding mod folder to thumbnails list: {dotAilFolder}");
				FolderUtil.ModThumbnailsFolders.Add(dotAilFolder);
			}

			if (Directory.Exists(ailFolder) && !FolderUtil.ModThumbnailsFolders.Contains(ailFolder))
			{
				Mod.Log.Info($"Adding mod folder to thumbnails list: {ailFolder}");
				FolderUtil.ModThumbnailsFolders.Add(ailFolder);
			}

			foreach (var file in ailConfig)
			{
				try
				{
					var dictionary = JSON.MakeInto<Dictionary<string, string>>(JSON.Load(File.ReadAllText(file)));

					ImportIconMap(folder, dictionary);
				}
				catch (Exception ex)
				{
					Mod.Log.Warn(ex, $"Failed to load ail config: {file}");
				}
			}
		}

		private static void LoadModAPI(string folder, Assembly assembly)
		{
			if (getMethod("GetIconsMap") is MethodInfo getIconsMapMethod)
			{
				Mod.Log.Info($"Invoking GetIconsMap from assembly: {assembly.FullName}");

				var map = InvokeMethod(getIconsMapMethod, out var root);

				try
				{
					ImportIconMap(root ?? folder, map);
				}
				catch (Exception ex)
				{
					Mod.Log.Warn(ex, $"Failed to load icon map: {assembly.FullName}");
				}
			}

			if (getMethod("GetIconsRootFolders") is MethodInfo getIconsRootFoldersMethod)
			{
				Mod.Log.Info($"Invoking GetIconsRootFolders from assembly: {assembly.FullName}");

				try
				{
					var parameters = getIconsRootFoldersMethod.GetParameters();
					var folders = getIconsRootFoldersMethod.Invoke(null, parameters.Length == 1 ? new object[] { Mod.Settings.IconsStyle } : new object[0]) as IEnumerable<string> ?? Enumerable.Empty<string>();

					foreach (var item in folders)
					{
						if (!Directory.Exists(item))
						{
							continue;
						}

						Mod.Log.Info($"Mapping custom mod folder to thumbnails list: {item}");

						FolderUtil.ModThumbnailsFolders.Add(
							Path.GetFullPath(item).TrimEnd(Path.DirectorySeparatorChar));
					}
				}
				catch (Exception ex)
				{
					Mod.Log.Error(ex, "API call failed");
				}
			}

			// GetMethod would throw on a mod that declares two overloads of the same method.
			MethodInfo getMethod(string name) => assembly?.GetTypesDerivedFrom<IMod>().FirstOrDefault()?.GetMethods(BindingFlags.Static | BindingFlags.Public).FirstOrDefault(x => x.Name == name);
		}

		private static void ImportIconMap(string folder, Dictionary<string, string> dictionary)
		{
			if (dictionary is null)
			{
				return;
			}

			Mod.Log.Info("Importing icon map");

			// An icon's URL cuts the folder and one separator off its path, so both are full paths.
			folder = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar);

			foreach (var kvp in dictionary)
			{
				var path = Path.Combine(folder, kvp.Value);

				if (File.Exists(path))
				{
					path = Path.GetFullPath(path);

					// No host serves a file outside the folder, and a shorter path breaks its URL.
					if (!path.StartsWith(
						folder + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
					{
						Mod.Log.WarnFormat(
							"Skipping icon outside its folder: {0} -> {1}", kvp.Key, kvp.Value);

						continue;
					}

					Mod.Log.DebugFormat("Mapping icon file: {0} -> {1}", kvp.Key, kvp.Value);
					FolderUtil.ModIconMap[kvp.Key] = (folder, path);
				}
				else
				{
					Mod.Log.DebugFormat("Mapping icon reference: {0} -> {1}", kvp.Key, kvp.Value);
					FolderUtil.ModIconReferenceMap[kvp.Key] = kvp.Value;
				}
			}
		}

		private static Dictionary<string, string> InvokeMethod(MethodInfo methodInfo, out string root)
		{
			var parameters = methodInfo.GetParameters();
			root = null;
			object result;

			try
			{
				if (parameters.Length == 2 && parameters[0].ParameterType == typeof(string) && parameters[1].IsOut)
				{
					// Case 1: method(string style, out string root)
					// Invoke writes an out argument into the array it was given.
					var args = new object[] { Mod.Settings.IconsStyle, null };

					result = methodInfo.Invoke(null, args);
					root = args[1] as string;
				}
				else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
				{
					// Case 2: method(string style)
					result = methodInfo.Invoke(null, new object[] { Mod.Settings.IconsStyle });
				}
				else if (parameters.Length == 1 && parameters[0].IsOut)
				{
					// Case 3: method(out string root)
					var args = new object[] { null };

					result = methodInfo.Invoke(null, args);
					root = args[0] as string;
				}
				else if (parameters.Length == 0)
				{
					// Case 4: method()
					result = methodInfo.Invoke(null, null);
				}
				else
				{
					result = null;
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Error(ex, "API call failed");

				return null;
			}

			if (result is Dictionary<string, string> dictionary)
			{
				return dictionary;
			}

			Mod.Log.Warn("Method signature does not match expected cases.");

			return null;
		}
	}
}
