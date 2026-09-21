using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Tidebound.LevelDesign;

namespace Tidebound.Config
{
    public interface IPlayableLevelCatalog
    {
        int Count { get; }
        string GetLevelId(int index);
        bool IsAvailable(int index);
        LevelData Load(int index);
    }

    /// <summary>Ordered stable identities with lazy content loading. Does not certify candidate quality.</summary>
    public sealed class PlayableLevelCatalog : IPlayableLevelCatalog
    {
        private readonly string[] ids;
        private readonly Func<string, bool> available;
        private readonly Func<string, LevelData> load;
        public int Count => ids.Length;

        public PlayableLevelCatalog(IEnumerable<string> levelIds, Func<string, bool> available, Func<string, LevelData> load)
        {
            ids = levelIds?.ToArray() ?? throw new ArgumentNullException(nameof(levelIds));
            if (ids.Length == 0 || ids.Length > 10000 || ids.Any(string.IsNullOrWhiteSpace) ||
                ids.Distinct(StringComparer.Ordinal).Count() != ids.Length)
                throw new ArgumentException("Catalog requires unique ordered level identities.");
            this.available = available ?? throw new ArgumentNullException(nameof(available));
            this.load = load ?? throw new ArgumentNullException(nameof(load));
        }
        public string GetLevelId(int index) => ids[index];
        public bool IsAvailable(int index) => index >= 0 && index < ids.Length && available(ids[index]);
        public LevelData Load(int index)
        {
            if (!IsAvailable(index)) throw new InvalidOperationException("Level content is unavailable.");
            var level = load(ids[index]);
            if (level == null || level.LevelId != ids[index]) throw new InvalidOperationException("Level identity differs from catalog.");
            return level;
        }

        // Existing candidate metadata supplies order; proof replay remains in CandidateLevelCatalog/editor audits.
        public static PlayableLevelCatalog FromManifest(string json, IReadOnlyDictionary<string, Func<string>> assets)
        {
            var manifest = JObject.Parse(json);
            if ((int?)manifest["manifestVersion"] != 1 || (string)manifest["rulesVersion"] != LevelRules.Version ||
                !(manifest["levels"] is JArray rows)) throw new ArgumentException("Unsupported level manifest.");
            var files = rows.ToDictionary(r => (string)r["levelId"], r => (string)r["layoutFile"], StringComparer.Ordinal);
            return new PlayableLevelCatalog(rows.Select(r => (string)r["levelId"]),
                id => files[id] != null && assets.ContainsKey(files[id]),
                id => LevelJsonReader.Read(assets[files[id]]()));
        }
    }
}
