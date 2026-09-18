using System;
using System.Collections.Generic;

namespace SupersonicWisdomSDK
{
    public static class SwEconomyUtils
    {
        #region --- Constants ---
        
        internal const string USD_CURRENCY_ISO = "USD";
        
        #endregion
        
        
        #region --- Public Methods ---

        public static double ConvertToUSD(double price, string currencyIso, Dictionary<string, float> conversionRates)
        {
            if (currencyIso == null || currencyIso.Equals(USD_CURRENCY_ISO, StringComparison.OrdinalIgnoreCase))
            {
                return price;
            }

            try
            {
                var toUsdConversionRate = conversionRates[currencyIso];
                var priceInUsd = price / toUsdConversionRate;

                SwInfra.Logger.Log(EWisdomLogType.Revenue, "Price {0} ({1}) = {2} (USD)".Format(price, currencyIso, priceInUsd));

                return priceInUsd;
            }
            catch (Exception e)
            {
                SwInfra.Logger.LogException(e, EWisdomLogType.Revenue, $"Failed to {nameof(ConvertToUSD)}");

                return 0;
            }
        }

        #endregion
    }
}