using Colossal.PSI.Environment;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AssetIconLibrary
{
	internal class FolderUtil
	{
		public static string ThumbnailsFolder { get; }
		public static string CustomThumbnailsFolder { get; }
		public static string ModPath { get; set; }
		public static Dictionary<string, string> ModThumbnailsFolders { get; } = new();

		static FolderUtil()
		{
			ThumbnailsFolder =Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(AssetIconLibrary), "Thumbnails");
			CustomThumbnailsFolder = Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(AssetIconLibrary), "CustomThumbnails");
		}
	}
}
