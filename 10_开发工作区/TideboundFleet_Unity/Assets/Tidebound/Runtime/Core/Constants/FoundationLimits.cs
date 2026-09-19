namespace Tidebound.Core
{
    public static class FoundationLimits
    {
        public const int LevelSchemaVersion = 2;
        public const string BaseShipTypeId = "TF_BASE_SHIP";
        public const string DefaultStandardSkinId = "TF_SKIN_DEFAULT";
        public const string DefaultLongSkinId = "TF_LONG_DEFAULT";
        public const int BaseShipDamage = 10;
        // Technical guards only. Product dimensions and density belong to a LevelProductionProfile.
        public const int MaxTechnicalBoardWidth = 24;
        public const int MaxTechnicalBoardHeight = 24;
        public const int MinShipLength = 2;
        public const int MaxShipLength = 3;
        public const int MaxTechnicalShipCount = 160;
    }
}
