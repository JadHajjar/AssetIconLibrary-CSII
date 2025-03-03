using Colossal.Logging;
using Colossal.PSI.Environment;

using System.Collections.Generic;
using System.IO;

namespace AssetIconLibrary
{
	internal class FolderUtil
	{
		internal static string ThumbnailsFolder { get; }
		internal static string CustomThumbnailsFolder { get; }
		internal static string ModPath { get; set; }
		internal static List<string> ModThumbnailsFolders { get; } = new();
		internal static Dictionary<string, (string Folder, string File)> ModIconMap { get; } = new();
		internal static Dictionary<string, string> ModIconReferenceMap { get; } = new();

		static FolderUtil()
		{
			var baseFolder = Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(AssetIconLibrary));

			ThumbnailsFolder = Path.Combine(baseFolder, ".Thumbnails");

			if (Directory.Exists(Path.Combine(baseFolder, ".CustomThumbnails")))
			{
				CustomThumbnailsFolder = Path.Combine(baseFolder, ".CustomThumbnails");
			}
			else
			{
				CustomThumbnailsFolder = Path.Combine(baseFolder, "CustomThumbnails");
			}

			var oldFolder = new DirectoryInfo(Path.Combine(baseFolder, "Thumbnails"));

			if (oldFolder.Exists)
			{
				try
				{
					oldFolder.Delete(true);
				}
				catch { }
			}
		}

		internal static void Output(ILog log)
		{
			log.Info("Folders:\r\n" +
				$"\tThumbnailsFolder: {ThumbnailsFolder}\r\n" +
				$"\tCustomThumbnailsFolder: {CustomThumbnailsFolder}\r\n" +
				$"\tModPath: {ModPath}\r\n" +
				$"\tModThumbnailsFolders: \r\n\t\t{string.Join("\r\n\t\t", ModThumbnailsFolders)}");
		}
	}
}
