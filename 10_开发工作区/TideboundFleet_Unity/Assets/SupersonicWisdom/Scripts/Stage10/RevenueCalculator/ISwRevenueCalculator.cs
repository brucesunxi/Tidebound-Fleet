#if SW_STAGE_STAGE10_OR_ABOVE
namespace SupersonicWisdomSDK
{
    internal interface ISwRevenueCalculator
    {
        public double Revenue { get; }
        public double GetRevenueByType(SwRevenueType type);
    }
}
#endif