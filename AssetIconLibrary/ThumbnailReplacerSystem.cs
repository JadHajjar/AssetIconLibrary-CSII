using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

using Game;
using Game.Prefabs;
using Game.UI;

using Unity.Entities;

namespace AssetIconLibrary {
    internal partial class ThumbnailReplacerSystem : GameSystemBase {
        internal static string ThumbnailPath { get; set; }

        protected override void OnUpdate() {
            this.Enabled = false;

            Stopwatch stopWatch = Stopwatch.StartNew();
            Dictionary<string, string> loadedIcons = GetAvailableIcons();
            PrefabSystem prefabSystem = this.World.GetExistingSystemManaged<PrefabSystem>();
            IReadOnlyDictionary<PrefabBase, Entity> prefabEntityMapping = typeof(PrefabSystem)
                .GetField("m_Entities", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(prefabSystem) as Dictionary<PrefabBase, Entity>;


            // the whole image-magic
            ImageSystem imageSystem = this.World.GetExistingSystemManaged<ImageSystem>();


            List<PrefabBase> prefabs = prefabEntityMapping.Keys.ToList();

            for (int i = 0; i < prefabs.Count; i++) {
                PrefabBase prefab = prefabs[i];


                if (!loadedIcons.TryGetValue(prefab.name, out string newIcon)) {
                    continue;
                }


                if (prefab.TryGet<UIObject>(out UIObject uIObject)) {
                    if (Mod.Settings.OverwriteIcons
                        // almost none of the uiObjects have an icon
                        // imageSystem does the trick
                        // || String.IsNullOrWhiteSpace(uIObject.m_Icon)
                        ) {
                        uIObject.m_Icon = newIcon;
                    }
                } else {
                    // those without an icon?
                    uIObject = prefab.AddComponent<UIObject>();
                    uIObject.m_Priority = 1;
                    uIObject.m_Icon = newIcon;
                }
            }

            stopWatch.Stop();

            Mod.Log.Info($"Prefab icon replacement completed in {stopWatch.Elapsed.TotalSeconds}s");
        }

        private static Dictionary<string, string> GetAvailableIcons() {
            Dictionary<string, string> loadedIcons = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

            foreach (string item in Directory.EnumerateFiles(ThumbnailPath, "Brand_*.png")) {
                for (int i = 0; i < _brandGroups.Length; i++) {
                    loadedIcons[_brandGroups[i] + Path.GetFileNameWithoutExtension(item).Substring(6)] = $"coui://ail/{Path.GetFileName(item)}";
                }
            }

            foreach (string item in Directory.EnumerateFiles(ThumbnailPath)) {
                loadedIcons[Path.GetFileNameWithoutExtension(item)] = $"coui://ail/{Path.GetFileName(item)}";
            }

            if (Directory.Exists(FolderUtil.CustomContentFolder)) {
                foreach (string item in Directory.EnumerateFiles(FolderUtil.CustomContentFolder)) {
                    loadedIcons[Path.GetFileNameWithoutExtension(item)] = $"coui://cail/{Path.GetFileName(item)}";
                }
            }

            return loadedIcons;
        }

        private static readonly string[] _brandGroups = new[]
        {
            "SignFrontwayLarge02 - ",
            "SignFrontwayMedium01 - ",
            "SignFrontwayMedium02 - ",
            "SignFrontwaySmall01 - ",
            "SignFrontwaySmall02 - ",
            "SignNeonLarge01 - ",
            "SignNeonLarge02 - ",
            "SignNeonMedium01 - ",
            "SignNeonMedium02 - ",
            "SignNeonSmall01 - ",
            "SignNeonSmall02 - ",
            "SignRoundLarge01 - ",
            "SignSidewayLarge01 - ",
            "SignSidewayLarge02 - ",
            "SignSidewayMedium01 - ",
            "SignSidewayMedium02 - ",
            "SignSidewaySmall01 - ",
            "SignSidewaySmall02 - ",
            "AStand01 - ",
            "AStand02 - ",
            "Stand01 - ",
            "Stand02 - ",
            "Screen02 - ",
            "SignFrontwayLarge01 - ",
            "BillboardHuge02 - ",
            "BillboardLarge02 - ",
            "BillboardMedium02 - ",
            "BillboardSmall01 - ",
            "BillboardSmall02 - ",
            "BillboardRoundLarge01 - ",
            "BillboardRoundMedium01 - ",
            "BillboardRoundSmall01 - ",
            "BillboardWallHuge02 - ",
            "BillboardWallLarge01 - ",
            "BillboardWallLarge03 - ",
            "BillboardWallMedium01 - ",
            "BillboardWallMedium02 - ",
            "BillboardWallSmall01 - ",
            "BillboardWallSmall02 - ",
            "BillboardWallSmall03 - ",
            "BillboardWallSmall04 - ",
            "GasStationPylon01 - ",
            "GasStationPylon02 - ",
            "GasStationPylon03 - ",
            "PosterHuge01 - ",
            "PosterHuge02 - ",
            "PosterLarge01 - ",
            "PosterLarge02 - ",
            "PosterMedium01 - ",
            "PosterMedium02 - ",
            "PosterSmall01 - ",
            "PosterSmall02 - ",
            "Screen01 - ",
            "BillboardLarge01 - ",
            "BillboardMedium01 - ",
            "BillboardWallHuge01 - ",
            "BillboardWallLarge02 - ",
            "BillboardWallLarge04 - ",
            "BillboardHuge01 - ",
        };
    }
}
