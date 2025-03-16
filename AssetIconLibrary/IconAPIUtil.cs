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

namespace AssetIconLibrary
{
	internal static class IconAPIUtil
	{
		internal static async Task ImportModIcons()
		{
			foreach (var item in GameManager.instance.modManager)
			{
				AddModFolderIfExists(Path.GetDirectoryName(item.asset.path), item.asset?.assembly);
			}

			var mods = await PlatformManager.instance.GetPSI<PdxSdkPlatform>("PdxSdk").GetModsInActivePlayset();

			if (mods != null)
			{
				foreach (var item in mods)
				{
					AddModFolderIfExists(item.LocalData?.FolderAbsolutePath);
				}
			}
		}

		private static void AddModFolderIfExists(string folder, Assembly assembly = null)
		{
			if (folder is null)
			{
				return;
			}

			Mod.Log.Debug($"Checking folder: {folder}");

			var dotAilFolder = Path.Combine(folder, ".ail");
			var ailFolder = Path.Combine(folder, "ail");
			var ailConfig = Directory.GetFiles(folder, "*.ail", SearchOption.AllDirectories);

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

			if (getMethod("GetIconsMap") is MethodInfo getIconsMapMethod)
			{
				Mod.Log.Info($"Invoking GetIconsMap from assembly: {assembly.FullName}");

				var map = InvokeMethod(getIconsMapMethod, out var root);

				ImportIconMap(root ?? folder, map);
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

						FolderUtil.ModThumbnailsFolders.Add(item);
					}
				}
				catch (Exception ex)
				{
					Mod.Log.Error(ex, "API call failed");
				}
			}

			MethodInfo getMethod(string name) => assembly?.GetTypesDerivedFrom<IMod>().FirstOrDefault()?.GetMethod(name, BindingFlags.Static | BindingFlags.Public);
		}

		private static void ImportIconMap(string folder, Dictionary<string, string> dictionary)
		{
			if (dictionary is null)
			{
				return;
			}

			Mod.Log.Info("Importing icon map");

			foreach (var kvp in dictionary)
			{
				if (File.Exists(Path.Combine(folder, kvp.Value)))
				{
					Mod.Log.DebugFormat("Mapping icon file: {0} -> {1}", kvp.Key, kvp.Value);
					FolderUtil.ModIconMap[kvp.Key] = (folder, kvp.Value);
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
					result = methodInfo.Invoke(null, new object[] { Mod.Settings.IconsStyle, root });
				}
				else if (parameters.Length == 1 && parameters[0].ParameterType == typeof(string))
				{
					// Case 2: method(string style)
					result = methodInfo.Invoke(null, new object[] { Mod.Settings.IconsStyle });
				}
				else if (parameters.Length == 1 && parameters[0].IsOut)
				{
					// Case 3: method(out string root)
					result = methodInfo.Invoke(null, new object[] { root });
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
