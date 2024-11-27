using Colossal;
using Colossal.IO.AssetDatabase;

using Game.Modding;
using Game.Settings;

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace AssetIconLibrary
{

	[FileLocation(nameof(AssetIconLibrary))]
	[SettingsUIGroupOrder("Settings")]
	[SettingsUIShowGroupName("Settings")]
	public class Setting : ModSetting
	{
		public const string kSection = "Main";

		public const string kButtonGroup = "Button";
		public const string kToggleGroup = "Toggle";
		public const string kSliderGroup = "Slider";
		public const string kDropdownGroup = "Dropdown";

		public Setting(IMod mod) : base(mod)
		{

		}

		[SettingsUIHidden]
		public bool DefaultBlock { get; set; }

		[SettingsUISection("Main", "Settings")]
		public bool OverwriteIcons { get; set; } = true;

		[SettingsUIButton]
		[SettingsUISection("Main", "Settings")]
		public bool OpenCustomFolders
		{
			set
			{
				Directory.CreateDirectory(FolderUtil.CustomThumbnailsFolder);
				Process.Start(FolderUtil.CustomThumbnailsFolder);
			}
		}

		public override void SetDefaults()
		{

		}
	}

	public class LocaleEN : IDictionarySource
	{
		private readonly Setting m_Setting;
		public LocaleEN(Setting setting)
		{
			m_Setting = setting;
		}

		public IEnumerable<KeyValuePair<string, string>> ReadEntries(IList<IDictionaryEntryError> errors, Dictionary<string, int> indexCounts)
		{
			return new Dictionary<string, string>
			{
				{ m_Setting.GetSettingsLocaleID(), "Asset Icon Library" },
				{ m_Setting.GetOptionTabLocaleID("Main"), "Main" },

				{ m_Setting.GetOptionGroupLocaleID("Settings"), "Settings" },

				{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverwriteIcons)), "Overwrite Existing Icons" },
				{ m_Setting.GetOptionDescLocaleID(nameof(Setting.OverwriteIcons)), $"While enabled, Asset Icon Library will overwrite vanilla icons and icons from other mods when available." },

				{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenCustomFolders)), "Open Custom Thumbnails Folder" },
				{ m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenCustomFolders)), $"Add personal custom thumbnails to use over the vanilla or 'Asset Icon Library' icons." },
			};
		}

		public void Unload()
		{

		}
	}
}
