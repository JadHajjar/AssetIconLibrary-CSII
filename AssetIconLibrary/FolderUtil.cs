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
		public static string ContentFolder { get; }
		public static string CustomContentFolder { get; }
		public static string SettingsFolder { get; }
		public static string ModPath { get; set; }

		static FolderUtil()
		{
			ContentFolder = Path.Combine(EnvPath.kUserDataPath, "ModsData", nameof(AssetIconLibrary));
			CustomContentFolder = Path.Combine(ContentFolder, "CustomThumbnails");
			//SettingsFolder = Path.Combine(EnvPath.kUserDataPath, "ModsSettings", nameof(AssetIconLibrary));
		}
	}
}
