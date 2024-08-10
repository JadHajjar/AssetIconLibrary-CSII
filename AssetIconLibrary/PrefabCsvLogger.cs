using System.Collections.Generic;
using System.Text;

using Colossal.Logging;

using Game.Prefabs;
using Game.UI;

using Unity.Entities;

namespace AssetIconLibrary {
    internal class PrefabCsvLogger {
        private readonly ILog logger;
        private readonly ImageSystem imageSystem;
        private readonly IReadOnlyDictionary<PrefabBase, Entity> prefabEntityMapping;
        private readonly StringBuilder builder = new StringBuilder();
        private bool started = false;
        public PrefabCsvLogger(ILog logger,
                               ImageSystem imageSystem,
                               IReadOnlyDictionary<PrefabBase, Entity> prefabEntityMapping) {
            this.logger = logger;
            this.imageSystem = imageSystem;
            this.prefabEntityMapping = prefabEntityMapping;
        }
        public void Start() {
            this.started = true;
            this.builder.AppendLine("prefab_name;instance_icon;group_icon;icon_or_group_icon;thumbnail;thumbnail_url");
        }
        public void Log(PrefabBase prefab) {
            if (!this.started) {
                return;
            }
            try {
                Entity prefabEntity = this.prefabEntityMapping[prefab];
                string prefabName = prefab.name;
                string instanceIcon = this.imageSystem.GetInstanceIcon(prefabEntity);
                string groupIcon = this.imageSystem.GetGroupIcon(prefabEntity);
                string iconOrGroupIcon = this.imageSystem.GetIconOrGroupIcon(prefabEntity);
                string thumbnail = this.imageSystem.GetThumbnail(prefabEntity);
                string thumbnailUrl = prefab.thumbnailUrl;
                this.builder.AppendLine($"{prefabName};{instanceIcon};{groupIcon};{iconOrGroupIcon};{thumbnail};{thumbnailUrl}");
            } catch {
                // some might not be available, so just skip logging
            }
        }
        public void Stop() {
            if (!this.started) {
                return;
            }
            this.logger.Info(this.builder);
        }
    }
}
