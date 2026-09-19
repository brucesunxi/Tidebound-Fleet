using Tidebound.Core;

namespace Tidebound.LevelDesign
{
    public readonly struct GenerationArea
    {
        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }
        public GenerationArea(int x, int y, int width, int height)
        { X = x; Y = y; Width = width; Height = height; }
        public bool Contains(int x, int y) => x >= X && y >= Y && x < (long)X + Width && y < (long)Y + Height;
    }

    public sealed class ReverseGenerationProfile
    {
        public string Id { get; }
        public int Width { get; }
        public int Height { get; }
        public int ShipCount { get; }
        public int LongShipCount { get; }
        public int MinInitialExits { get; }
        public int MaxInitialExits { get; }
        public int MinDependencyDepth { get; }
        public int MaxDependencyDepth { get; }
        public int MaxIndependentSeeds { get; }
        public double MinDirectionEntropy { get; }
        public double MaxDirectionClustering { get; }
        public double MaxDirectionShare { get; }
        public int MinPartialMoves { get; }
        public GenerationArea Area { get; }

        public ReverseGenerationProfile(string id, int width, int height, int shipCount, int longShipCount,
            int minInitialExits, int maxInitialExits, int minDependencyDepth, int maxDependencyDepth,
            int maxIndependentSeeds, double minDirectionEntropy = 0.95, double maxDirectionClustering = 0.60,
            double maxDirectionShare = 0.35, int minPartialMoves = 1, GenerationArea? area = null)
        {
            Id = id; Width = width; Height = height; ShipCount = shipCount; LongShipCount = longShipCount;
            MinInitialExits = minInitialExits; MaxInitialExits = maxInitialExits;
            MinDependencyDepth = minDependencyDepth; MaxDependencyDepth = maxDependencyDepth;
            MaxIndependentSeeds = maxIndependentSeeds; MinDirectionEntropy = minDirectionEntropy;
            MaxDirectionClustering = maxDirectionClustering; MaxDirectionShare = maxDirectionShare;
            MinPartialMoves = minPartialMoves; Area = area ?? new GenerationArea(0, 0, width, height);
        }

        internal bool IsValid => !string.IsNullOrWhiteSpace(Id) && Width > 0 && Height > 0 &&
            Width <= FoundationLimits.MaxTechnicalBoardWidth && Height <= FoundationLimits.MaxTechnicalBoardHeight &&
            ShipCount > 0 && ShipCount <= FoundationLimits.MaxTechnicalShipCount &&
            LongShipCount >= 0 && LongShipCount <= ShipCount &&
            MinInitialExits >= 1 && MaxInitialExits >= MinInitialExits && MaxInitialExits <= ShipCount &&
            MinDependencyDepth >= 1 && MaxDependencyDepth >= MinDependencyDepth && MaxDependencyDepth <= ShipCount &&
            MaxIndependentSeeds >= 1 && MaxIndependentSeeds <= ShipCount &&
            MinDirectionEntropy >= 0 && MinDirectionEntropy <= 1 &&
            MaxDirectionClustering >= 0 && MaxDirectionClustering <= 1 &&
            MaxDirectionShare >= 0.25 && MaxDirectionShare <= 1 &&
            MinPartialMoves >= 0 && MinPartialMoves <= ShipCount &&
            Area.X >= 0 && Area.Y >= 0 && Area.Width > 0 && Area.Height > 0 &&
            (long)Area.X + Area.Width <= Width && (long)Area.Y + Area.Height <= Height;
    }

    public sealed class ReverseGenerationBudget
    {
        public int MaxCandidateEvaluations { get; }
        public int MaxBacktracks { get; }
        public int BranchWidth { get; }
        public int TimeLimitMilliseconds { get; }
        public ReverseGenerationBudget(int maxCandidateEvaluations = 2000000, int maxBacktracks = 1000,
            int branchWidth = 16, int timeLimitMilliseconds = 15000)
        {
            MaxCandidateEvaluations = maxCandidateEvaluations; MaxBacktracks = maxBacktracks;
            BranchWidth = branchWidth; TimeLimitMilliseconds = timeLimitMilliseconds;
        }
        internal bool IsValid => MaxCandidateEvaluations > 0 && MaxBacktracks >= 0 &&
            BranchWidth > 0 && BranchWidth <= 256 && TimeLimitMilliseconds > 0;
    }

    public static class ReverseGenerationProfiles
    {
        // Candidate dimensions, not a frozen mobile product specification.
        public static readonly ReverseGenerationProfile Tutorial = new ReverseGenerationProfile(
            "I1_Tutorial_7", 14, 18, 7, 0, 2, 3, 3, 4, 4,
            0.90, 0.60, 0.43, 1, new GenerationArea(4, 5, 6, 8));
        public static readonly ReverseGenerationProfile FullBoard = new ReverseGenerationProfile(
            "I1_FullBoard_80", 14, 18, 80, 0, 12, 16, 5, 8, 26);
    }
}
