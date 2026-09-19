using System;
using System.Collections.Generic;

namespace Tidebound.LevelDesign
{
    /// <summary>Editor-facing product gate. Runtime safety limits remain in FoundationLimits.</summary>
    public sealed class LevelProductionProfile
    {
        public string Id { get; }
        public int Width { get; }
        public int Height { get; }
        public int MinShipCount { get; }
        public int MaxShipCount { get; }
        public double MaxLongShipRatio { get; }
        public double MinDirectionEntropy { get; }
        public double MaxDirectionClustering { get; }
        public double MaxDirectionShare { get; }
        public int MinInitialExitCount { get; }

        public LevelProductionProfile(string id, int width, int height, int minShipCount, int maxShipCount,
            double maxLongShipRatio, double minDirectionEntropy, double maxDirectionClustering,
            double maxDirectionShare, int minInitialExitCount)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A profile id is required.", nameof(id));
            if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
            if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));
            if (minShipCount < 0 || maxShipCount < minShipCount) throw new ArgumentOutOfRangeException(nameof(minShipCount));
            if (maxLongShipRatio < 0d || maxLongShipRatio > 1d) throw new ArgumentOutOfRangeException(nameof(maxLongShipRatio));
            if (minDirectionEntropy < 0d || minDirectionEntropy > 1d) throw new ArgumentOutOfRangeException(nameof(minDirectionEntropy));
            if (maxDirectionClustering < 0d || maxDirectionClustering > 1d) throw new ArgumentOutOfRangeException(nameof(maxDirectionClustering));
            if (maxDirectionShare < 0.25d || maxDirectionShare > 1d) throw new ArgumentOutOfRangeException(nameof(maxDirectionShare));
            if (minInitialExitCount < 0) throw new ArgumentOutOfRangeException(nameof(minInitialExitCount));

            Id = id;
            Width = width;
            Height = height;
            MinShipCount = minShipCount;
            MaxShipCount = maxShipCount;
            MaxLongShipRatio = maxLongShipRatio;
            MinDirectionEntropy = minDirectionEntropy;
            MaxDirectionClustering = maxDirectionClustering;
            MaxDirectionShare = maxDirectionShare;
            MinInitialExitCount = minInitialExitCount;
        }
    }

    public static class LevelProductionProfiles
    {
        public static readonly LevelProductionProfile Tutorial =
            new LevelProductionProfile("Tutorial_4x6", 4, 6, 7, 7, 0d, 0d, 1d, 1d, 1);

        private static readonly LevelProductionProfile[] candidateValues =
        {
            Candidate("Candidate_18x18", 18, 18, 80, 90),
            Candidate("Candidate_18x22", 18, 22, 90, 100),
            Candidate("Candidate_20x20", 20, 20, 90, 100),
            Candidate("Candidate_22x22", 22, 22, 100, 110)
        };

        public static IReadOnlyList<LevelProductionProfile> Candidates => Array.AsReadOnly(candidateValues);

        public static bool TryGetCandidate(int width, int height, out LevelProductionProfile profile)
        {
            for (var i = 0; i < candidateValues.Length; i++)
            {
                var item = candidateValues[i];
                if (item.Width == width && item.Height == height)
                {
                    profile = item;
                    return true;
                }
            }
            profile = null;
            return false;
        }

        private static LevelProductionProfile Candidate(string id, int width, int height, int minShips, int maxShips) =>
            new LevelProductionProfile(id, width, height, minShips, maxShips,
                0.10d, 0.95d, 0.70d, 0.32d, 4);
    }
}
