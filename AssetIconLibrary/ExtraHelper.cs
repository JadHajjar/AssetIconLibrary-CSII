using System.Collections.Generic;

using Game.Prefabs;
using Game.UI;

using Unity.Entities;

namespace AssetIconLibrary {
    internal class ExtraHelper {
        public static string ExtraDetailingToolsUri { get; } = "://extra";
        private readonly ImageSystem imageSystem;
        private readonly IReadOnlyDictionary<PrefabBase, Entity> prefabEntityMapping;
        public ExtraHelper(ImageSystem imageSystem, IReadOnlyDictionary<PrefabBase, Entity> prefabEntityMapping) {
            this.imageSystem = imageSystem;
            this.prefabEntityMapping = prefabEntityMapping;
        }
        public bool IsExtra(PrefabBase prefab) {
            try {
                Entity prefabEntity = this.prefabEntityMapping[prefab];
                string groupIcon = this.imageSystem.GetGroupIcon(prefabEntity);
                return groupIcon.Contains(ExtraHelper.ExtraDetailingToolsUri);
            } catch {
                // some might not be available, so just skip logging
            }
            return false;
        }
    }
}