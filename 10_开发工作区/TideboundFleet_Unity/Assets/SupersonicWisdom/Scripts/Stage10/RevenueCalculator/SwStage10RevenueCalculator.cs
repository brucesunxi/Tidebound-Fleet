#if SW_STAGE_STAGE10_OR_ABOVE
using System;
using System.Collections.Generic;
using System.Globalization;

namespace SupersonicWisdomSDK
{
    internal class SwStage10RevenueCalculator : ISwRevenueCalculator
    {
        #region --- Constants ---

        private const string REVENUE_STORE_KEY = "revenue";
        private const string REVENUE_BY_TYPE_STORE_KEY = "revenueByType";
        internal const string REVENUE_TYPE_EVENT_PARAM = "revenueType";
        internal const string REVENUE_AMOUNT_EVENT_PARAM = "revenueAmount";
        internal const string IS_DEV_REVENUE_REPORT_EVENT_PARAM = "isDevRevenueReport";
        internal const string TOTAL_REVENUE = "totalRevenue";
        
        #endregion


        #region --- Members ---
        
        private double _revenue;
        private Dictionary<SwRevenueType, double> _revenueByTypeDict;

        #endregion

        
        #region --- Events ---

        public event Action<double, object> RevenueChangedEvent;

        #endregion


        #region --- Properties ---

        public double Revenue
        {
            get { return _revenue; }
            private set
            {
                if (value <= _revenue)
                {
                    return;
                }

                _revenue = value;
                SwInfra.JsonKeyValueStore.SetFloat(REVENUE_STORE_KEY, (float)_revenue).Save();
            }
        }

        #endregion


        #region --- Construction ---

        public SwStage10RevenueCalculator()
        {			
            LoadPrefsData();
        }

        // Notice: Backward compatibility! 
        // This method take under consideration that before version 7.8.x wisdom saved the revenue data with SwPlayerPrefs store
        private void LoadPrefsData()
        {
            if (SwInfra.JsonKeyValueStore.HasKey(REVENUE_STORE_KEY))
            {
                _revenue = SwInfra.JsonKeyValueStore.GetFloat(REVENUE_STORE_KEY);
            }
            else
            {
                _revenue = SwInfra.KeyValueStore.GetFloat(REVENUE_STORE_KEY);
            }

            if (SwInfra.JsonKeyValueStore.HasKey(REVENUE_BY_TYPE_STORE_KEY))
            {
                _revenueByTypeDict =
                    SwInfra.JsonKeyValueStore.GetGenericSerializedData(
                        REVENUE_BY_TYPE_STORE_KEY,
                        new Dictionary<SwRevenueType, double>());
            }
            else
            {
                _revenueByTypeDict = 
                    SwInfra.KeyValueStore.GetGenericSerializedData(
                        REVENUE_BY_TYPE_STORE_KEY,
                        new Dictionary<SwRevenueType, double>());
            }
        }

        public double GetRevenueByType(SwRevenueType type)
        {
            return _revenueByTypeDict.SwSafelyGet(type, 0);
        }

        #endregion


        #region --- Private Methods --

        protected internal void TryAddRevenue(SwStage10RevenueData revenueData, object metaData = null)
        {
            if (!ValidateAmount(revenueData.Amount)) return;

            AddRevenue(revenueData);
            
            SwInfra.Logger.Log(EWisdomLogType.Revenue, Revenue.ToString("R", CultureInfo.InvariantCulture));
            SwInfra.JsonKeyValueStore.SetFloat(REVENUE_STORE_KEY, (float)Revenue).Save();
			SwInfra.JsonKeyValueStore.SetGenericSerializedData(REVENUE_BY_TYPE_STORE_KEY, _revenueByTypeDict).Save();

            RevenueChangedEvent?.Invoke(Revenue, metaData);
            
            TryReport(revenueData);
        }

        protected virtual void AddRevenue(SwStage10RevenueData revenueData)
        {
            if (revenueData == null) return;
            
            var revenueToAddNotNull = revenueData.Amount;
            
            Revenue += revenueToAddNotNull;
			
            var currentRevenueByType = _revenueByTypeDict.GetValueOrDefault(revenueData.Type, 0);
			_revenueByTypeDict.SwAddOrReplace(revenueData.Type, currentRevenueByType + revenueToAddNotNull);
        }

        protected virtual void TryReport(SwStage10RevenueData revenueData) { }
        
        private static bool ValidateAmount(double? amount)
        {
            var isValid = true;
            
            if (amount is null or <= 0)
            {
                SwInfra.Logger.LogError(EWisdomLogType.Revenue, $"Invalid amount: {amount}");

                isValid = false;
            }

            return isValid;
        }

        #endregion
    }
}
#endif