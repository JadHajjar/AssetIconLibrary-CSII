using AssetIconLibrary.Utilities;
using Colossal;
using Colossal.IO.AssetDatabase;

using Game.Modding;
using Game.SceneFlow;
using Game.Settings;
using Game.UI.Widgets;

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace AssetIconLibrary
{
    [FileLocation(nameof(AssetIconLibrary))]
	[SettingsUIGroupOrder(SETTINGS_GROUP, CUSTOM_GROUP)]
	[SettingsUIShowGroupName(SETTINGS_GROUP, CUSTOM_GROUP)]
	public class Setting : ModSetting
	{
		public const string MAIN_SECTION = "Main";
		public const string SETTINGS_GROUP = "Settings";
		public const string CUSTOM_GROUP = "Custom";

		public Setting(IMod mod) : base(mod)
		{

		}

		[SettingsUISection(MAIN_SECTION, SETTINGS_GROUP)]
		[SettingsUIMultilineText]
		[SettingsUIHideByCondition(typeof(Setting), nameof(IsIngame))]
		public string RestartRequired => string.Empty;

		[SettingsUISection(MAIN_SECTION, SETTINGS_GROUP)]
		[SettingsUIDropdown(typeof(Setting), nameof(GetStyleDropdownItems))]
		public string IconsStyle { get; set; } = "ColoredPropless";

		[SettingsUISection(MAIN_SECTION, SETTINGS_GROUP)]
		public bool OverwriteIcons { get; set; } = true;

		[SettingsUISection(MAIN_SECTION, CUSTOM_GROUP)]
		[SettingsUIMultilineText]
		public string CustomIcons => string.Empty;

		[SettingsUIButton]
		[SettingsUISection(MAIN_SECTION, CUSTOM_GROUP)]
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

		public bool IsIngame()
		{
			return GameManager.instance.gameMode == Game.GameMode.MainMenu;
		}

		public DropdownItem<string>[] GetStyleDropdownItems()
		{
			var items = new DropdownItem<string>[]
			{
				new()
				{
					value = "ColoredPropless",
					displayName = GetOptionLabelLocaleID("ColoredPropless"),
				},
				new()
				{
					value = "White",
					displayName = GetOptionLabelLocaleID("White"),
				},
				new()
				{
					value = "Colored",
					displayName = GetOptionLabelLocaleID("Colored"),
				}
			};

			return items;
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
				{ m_Setting.GetOptionTabLocaleID(Setting.MAIN_SECTION), "Main" },

				{ m_Setting.GetOptionGroupLocaleID(Setting.SETTINGS_GROUP), "Settings" },
				{ m_Setting.GetOptionGroupLocaleID(Setting.CUSTOM_GROUP), "Custom" },

				{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.RestartRequired)), "⚠️  Changing these settings requires restarting your game to apply your changes." },

				{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.OverwriteIcons)), "Overwrite Existing Icons" },
				{ m_Setting.GetOptionDescLocaleID(nameof(Setting.OverwriteIcons)), $"While enabled, Asset Icon Library will overwrite vanilla assets' icons, even if the assets have an icon already." },

				{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.IconsStyle)), "Icon Style" },
				{ m_Setting.GetOptionDescLocaleID(nameof(Setting.IconsStyle)), $"Change what style to use for asset icons where applicable." },

				{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.CustomIcons)), "Use custom icons for assets by placing the image files with the assets' name inside the custom icons folder." },

				{ m_Setting.GetOptionLabelLocaleID(nameof(Setting.OpenCustomFolders)), "Open Custom Icons Folder" },
				{ m_Setting.GetOptionDescLocaleID(nameof(Setting.OpenCustomFolders)), $"Add personal custom icons to use over the vanilla or 'Asset Icon Library' icons." },

				{ m_Setting.GetOptionLabelLocaleID("ColoredPropless"), "Colored & No Props" },
				{ m_Setting.GetOptionLabelLocaleID("White"), "White & No Props" },
				{ m_Setting.GetOptionLabelLocaleID("Colored"), "Colored With Props" },
			};
		}

		public void Unload()
		{

		}
	}
}
