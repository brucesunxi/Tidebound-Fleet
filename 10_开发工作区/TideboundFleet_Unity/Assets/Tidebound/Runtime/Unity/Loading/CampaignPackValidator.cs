using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using Tidebound.Board;
using Tidebound.Boss;
using Tidebound.Core;
using Tidebound.LevelDesign;
using Tidebound.Ship;

namespace Tidebound.Config
{
    /// <summary>Offline content gate. Runtime still loads one layout at a time through PlayableLevelCatalog.</summary>
    public static class CampaignPackValidator
    {
        public const string Status = "AutomatedValidatedNeedsPlaytest";
        public static string Sha256(string text)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text))).Replace("-", "");
        }
        public static void Validate(string manifestJson, Func<string, string> read)
        {
            var manifest = JObject.Parse(manifestJson);
            var rows = manifest["levels"] as JArray;
            if ((int?)manifest["manifestVersion"] != 1 || (string)manifest["rulesVersion"] != LevelRules.Version ||
                (string)manifest["recipeVersion"] != CampaignLevelPlan.Version || (string)manifest["status"] != Status ||
                rows == null || (rows.Count != 30 && rows.Count != 100)) throw new ArgumentException("Invalid campaign manifest.");
            var keys = new HashSet<string>(StringComparer.Ordinal);
            var previous = new List<BoardModel>();
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i]; var design = CampaignLevelPlan.All[i]; var id = design.Recipe.LevelId;
                if ((string)row["levelId"] != id || (int?)row["number"] != i + 1 || (int?)row["contentRevision"] != 1 ||
                    (string)row["layoutFile"] != id + ".json" || (string)row["proofFile"] != id + ".solution.json" ||
                    (string)row["structure"] != design.Structure.ToString() || (string)row["pace"] != design.Pace.ToString())
                    throw new ArgumentException("Campaign order/design mismatch: " + id);
                var layout = read(id + ".json"); var proof = read(id + ".solution.json");
                if (Sha256(layout) != (string)row["layoutSha256"] || Sha256(proof) != (string)row["proofSha256"])
                    throw new ArgumentException("Content changed after pack certification: " + id);
                using (var session = Create(LevelJsonReader.Read(layout)))
                {
                    var board = session.InitialBoard;
                    if (session.LevelId != id || design.Recipe.Check(board).Count != 0 || design.CheckStructure(board).Count != 0 ||
                        !LevelProofJson.Read(proof).Replay(id, board).IsComplete || !keys.Add(LevelLayoutIdentity.CanonicalKey(board)))
                        throw new ArgumentException("Invalid proof, recipe or duplicate: " + id);
                    if (previous.Any(b => GeometryOverlap(b, board) > 0.65)) throw new ArgumentException("Near duplicate: " + id);
                    previous.Add(board);
                }
            }
        }
        public static GameSession Create(LevelData level) => LevelSessionFactory.Create(level,
            new[] { new ShipDefinition(FoundationLimits.BaseShipTypeId, FoundationLimits.BaseShipDamage) },
            new[] { new BossDefinition(level.BossId) });

        // Fraction of identical ship placements, including rectangular symmetries; independent of IDs and skins.
        public static double GeometryOverlap(BoardModel a, BoardModel b)
        {
            if (a.Width != b.Width || a.Height != b.Height || a.ShipCount != b.ShipCount) return 0;
            Func<BoardModel, int, HashSet<string>> placements = (board, mask) => new HashSet<string>(board.Ships.Select(s =>
            {
                var dx = GridFootprint.DirectionStep(s.Direction).X; var dy = GridFootprint.DirectionStep(s.Direction).Y;
                var x = s.Position.X; var y = s.Position.Y;
                if ((mask & 1) != 0) { x = board.Width - 1 - x; dx = -dx; }
                if ((mask & 2) != 0) { y = board.Height - 1 - y; dy = -dy; }
                return x + "," + y + "," + dx + "," + dy + "," + s.Length + "," + s.TypeId;
            }));
            var original = placements(a, 0);
            return Enumerable.Range(0, 4).Max(mask => placements(b, mask).Count(original.Contains) / (double)Math.Max(1, a.ShipCount));
        }
    }
}
