using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Tidebound.LevelDesign;
using Tidebound.Save;

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
        private readonly Dictionary<int, Dictionary<string, Func<LevelData>>> previous =
            new Dictionary<int, Dictionary<string, Func<LevelData>>>();
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

        /// <summary>Only the installed revision or an explicitly shipped archive can resume an attempt.</summary>
        public static LevelData LoadForAttempt(IPlayableLevelCatalog catalog, int index, string levelId)
        {
            if (catalog == null || index < 0 || index >= catalog.Count)
                throw new InvalidOperationException("Saved level number is outside the catalog.");
            if (catalog.GetLevelId(index) == levelId) return catalog.Load(index);
            if (catalog is PlayableLevelCatalog revisions && revisions.previous.TryGetValue(index, out var versions) &&
                versions.TryGetValue(levelId, out var loader))
            {
                var level = loader();
                if (level.LevelId == levelId) return level;
            }
            throw new InvalidOperationException("Saved level revision is not installed for this level number.");
        }

        public static bool CanReplaceAttempt(IPlayableLevelCatalog catalog, AttemptSaveData previousAttempt, SavedGameRuntime replacement)
        {
            if (previousAttempt == null || replacement == null || catalog == null ||
                previousAttempt.LevelNumber != replacement.LevelNumber) return false;
            var index = replacement.LevelNumber - 1;
            if (index < 0 || index >= catalog.Count || catalog.GetLevelId(index) != replacement.Session.LevelId) return false;
            try
            {
                using (var old = CampaignPackValidator.Create(LoadForAttempt(catalog, index, previousAttempt.LevelId)))
                using (var next = CampaignPackValidator.Create(catalog.Load(index)))
                    return LevelStateIdentity.Fingerprint(old.InitialBoard) == previousAttempt.LayoutFingerprint &&
                        LevelStateIdentity.Fingerprint(next.InitialBoard) == LevelStateIdentity.Fingerprint(replacement.Session.InitialBoard);
            }
            catch (Exception e) when (e is ArgumentException || e is InvalidOperationException || e is LevelFormatException)
            { return false; }
        }

        // Existing candidate metadata supplies order; proof replay remains in CandidateLevelCatalog/editor audits.
        public static PlayableLevelCatalog FromManifest(string json, IReadOnlyDictionary<string, Func<string>> assets)
        {
            var manifest = JObject.Parse(json);
            if ((int?)manifest["manifestVersion"] != 1 || (string)manifest["rulesVersion"] != LevelRules.Version ||
                !(manifest["levels"] is JArray rows)) throw new ArgumentException("Unsupported level manifest.");
            var files = rows.ToDictionary(r => (string)r["levelId"], r => (string)r["layoutFile"], StringComparer.Ordinal);
            var catalog = new PlayableLevelCatalog(rows.Select(r => (string)r["levelId"]),
                id => files[id] != null && assets.ContainsKey(files[id]),
                id => LevelJsonReader.Read(assets[files[id]]()));
            if (manifest["previousRevisions"] is JArray archives)
                foreach (var row in archives)
                {
                    var index = (int)row["number"] - 1;
                    var id = (string)row["levelId"]; var file = (string)row["layoutFile"];
                    var hash = (string)row["layoutSha256"];
                    if (index < 0 || index >= catalog.Count || string.IsNullOrWhiteSpace(id) ||
                        string.IsNullOrWhiteSpace(file) || hash == null || hash.Length != 64 || id == catalog.GetLevelId(index))
                        throw new ArgumentException("Invalid archived level revision.");
                    if (!catalog.previous.TryGetValue(index, out var versions))
                        catalog.previous.Add(index, versions = new Dictionary<string, Func<LevelData>>(StringComparer.Ordinal));
                    versions.Add(id, () =>
                    {
                        if (!assets.TryGetValue(file, out var read)) throw new InvalidOperationException("Archived level is unavailable.");
                        var text = read();
                        if (CampaignPackValidator.Sha256(text) != hash) throw new InvalidOperationException("Archived level content changed.");
                        return LevelJsonReader.Read(text);
                    });
                }
            return catalog;
        }
    }
}
