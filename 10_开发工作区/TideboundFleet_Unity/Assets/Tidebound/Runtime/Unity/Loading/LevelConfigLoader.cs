using System;
using System.Linq;
using Tidebound.Core;

namespace Tidebound.Config
{
    public static class LevelConfigLoader
    {
        public static GameSession Load(LevelConfigSO config)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (config.LevelJson == null) throw new LevelFormatException("LevelConfigSO must reference a JSON TextAsset.");
            var level = LevelJsonReader.Read(config.LevelJson.text);
            var ships = config.GetShipConfigs()?.Select(x => x == null ? null : x.CreateDefinition()).ToArray();
            var bosses = config.GetBossConfigs()?.Select(x => x == null ? null : x.CreateDefinition()).ToArray();
            return LevelSessionFactory.Create(level, ships, bosses);
        }
    }
}
