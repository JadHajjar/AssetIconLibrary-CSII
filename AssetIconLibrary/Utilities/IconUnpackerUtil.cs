using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace AssetIconLibrary.Utilities
{
	internal class IconUnpackerUtil
	{
		internal static async Task UnpackIcons()
		{
			Mod.Log.Info("Unpacking icons");

			var directoryInfo = new DirectoryInfo(FolderUtil.ThumbnailsFolder);
			var versionFilePath = Path.Combine(FolderUtil.ThumbnailsFolder, ".version");
			var thumbnailFile = Path.Combine(Path.GetDirectoryName(FolderUtil.ModPath), ".Thumbnails", "Pack.zip");

			var stagingFolder = $"{FolderUtil.ThumbnailsFolder}.new";
			var oldFolder = $"{FolderUtil.ThumbnailsFolder}.old";

			if (Directory.Exists(oldFolder))
			{
				Directory.Delete(oldFolder, true);
			}

			if (Directory.Exists(stagingFolder))
			{
				Directory.Delete(stagingFolder, true);
			}

			if (!File.Exists(thumbnailFile))
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

			// With no icons to keep, it unpacks in place, so a failure leaves what it extracted.
			var isStaged = directoryInfo.Exists;
			var targetFolder = isStaged ? stagingFolder : FolderUtil.ThumbnailsFolder;
			var tempFolder = Path.Combine(targetFolder, ".temp");

			Directory.CreateDirectory(tempFolder);

			using var stream = File.OpenRead(thumbnailFile);
			using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read, false);

			zipArchive.ExtractToDirectory(tempFolder);

			var tasks = Directory.GetFiles(tempFolder, "*.zip").Select(zip => Task.Run(() => UnpackZip(zip, targetFolder)));

			await Task.WhenAll(tasks);

			Directory.Delete(tempFolder, true);

			File.WriteAllText(Path.Combine(targetFolder, ".version"), typeof(Mod).Assembly.GetName().Version.ToString());

			if (isStaged)
			{
				// The old icons move aside first: a delete can stop halfway, and a move cannot.
				Directory.Move(FolderUtil.ThumbnailsFolder, oldFolder);

				// An antivirus can hold a just-written file, which fails the move for a moment.
				for (var attempt = 1; ; attempt++)
				{
					try
					{
						Directory.Move(stagingFolder, FolderUtil.ThumbnailsFolder);

						break;
					}
					catch when (attempt < 4)
					{
						await Task.Delay(1000);
					}
					catch
					{
						Directory.Move(oldFolder, FolderUtil.ThumbnailsFolder);

						throw;
					}
				}

				Directory.Delete(oldFolder, true);
			}

			stopwatch.Stop();

			Mod.Log.Info($"{directoryInfo.GetFiles("*.*", SearchOption.AllDirectories).Length} icons finished unpacking in {stopwatch.Elapsed.TotalSeconds}s");
		}

		private static void UnpackZip(string item, string targetFolder)
		{
			using var stream = File.OpenRead(item);
			using var zipArchive = new ZipArchive(stream, ZipArchiveMode.Read, false);

			var rootPath = Path.GetFullPath(targetFolder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

			// Extract entry by entry with overwrite, so files shared between chunks don't throw
			foreach (var entry in zipArchive.Entries)
			{
				var destination = Path.GetFullPath(Path.Combine(rootPath, entry.FullName));

				if (!destination.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
				{
					Mod.Log.Warn($"Skipping zip entry outside of target folder: {entry.FullName}");
					continue;
				}

				// Directory entries have an empty name
				if (string.IsNullOrEmpty(entry.Name))
				{
					Directory.CreateDirectory(destination);
					continue;
				}

				Directory.CreateDirectory(Path.GetDirectoryName(destination));
				entry.ExtractToFile(destination, true);
			}
		}
	}
}
