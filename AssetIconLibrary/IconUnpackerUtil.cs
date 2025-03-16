using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace AssetIconLibrary
{
	internal class IconUnpackerUtil
	{
		internal static async Task UnpackIcons()
		{
			Mod.Log.Info("Unpacking icons");

			var directoryInfo = new DirectoryInfo(FolderUtil.ThumbnailsFolder);
			var versionFilePath = Path.Combine(FolderUtil.ThumbnailsFolder, ".version");
			var thumbnailFolder = Path.Combine(Path.GetDirectoryName(FolderUtil.ModPath), ".Thumbnails");

			if (!Directory.Exists(thumbnailFolder))
			{
				Mod.Log.Warn("Thumbnails content not available");

				return;
			}

			if (File.Exists(versionFilePath) && File.ReadAllText(versionFilePath) == typeof(Mod).Assembly.GetName().Version.ToString())
			{
				Mod.Log.Info("Thumbnails up to date");

				return;
			}

			var stopwatch = Stopwatch.StartNew();

			if (directoryInfo.Exists)
			{
				directoryInfo.Delete(true);
			}

			directoryInfo.Create();

			var tasks = Directory.GetFiles(thumbnailFolder, "*.zip").Select(zip => Task.Run(() => UnpackZip(zip, FolderUtil.ThumbnailsFolder)));

			await Task.WhenAll(tasks);

			File.WriteAllText(versionFilePath, typeof(Mod).Assembly.GetName().Version.ToString());

			stopwatch.Stop();

			Mod.Log.Info($"{directoryInfo.GetFiles("*.*", SearchOption.AllDirectories).Length} icons finished unpacking in {stopwatch.Elapsed.TotalSeconds}s");
		}

		private static void UnpackZip(string item, string targetFoler)
		{
			using var stream = File.OpenRead(item);
			using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read, false);

			zipArchive.ExtractToDirectory(targetFoler);
		}
	}
}
