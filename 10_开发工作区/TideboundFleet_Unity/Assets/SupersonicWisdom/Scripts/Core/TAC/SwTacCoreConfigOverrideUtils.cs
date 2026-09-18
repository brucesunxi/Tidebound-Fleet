using System;
using System.Collections.Generic;
using System.Linq;

namespace SupersonicWisdomSDK
{
    public static class SwTacCoreConfigOverrideUtils
    {
        #region --- Private Methods ---

        
        internal static SwTacConfig OverrideTacConfig(ISwCoreInternalConfig swConfigAccessor)
        {
            var tacConfig = swConfigAccessor.Tac;

            if (tacConfig?.Actions == null)
            {
                return tacConfig;
            }

            var overridersPairs = swConfigAccessor.DynamicConfig.GetValueWithPrefix(SwTacConfigOverrideConstants.CONFIG_PREFIX);

            foreach (var overridePair in overridersPairs)
            {
                var keyWithoutPrefix = overridePair.Key.Replace(SwTacConfigOverrideConstants.CONFIG_PREFIX, string.Empty);
                var id = keyWithoutPrefix.SubstringUntilUpperLetter();
                var keyWithoutPrefixAndId = keyWithoutPrefix.Replace(id, string.Empty);
                var split = keyWithoutPrefixAndId.Split(SwTacConfigOverrideConstants.CONFIG_SEPARATOR);
                var overrideJsonSection = split?.First();
                var suffix = split?.Last();

                if (overrideJsonSection == null)
                {
                    continue;
                }

                foreach (var action in tacConfig.Actions.Where(action => action.Id == id))
                {
                    try
                    {
                        switch (overrideJsonSection)
                        {
                            case SwTacConfigOverrideConstants.CONFIG_TRIGGERS_SCHEME_NAME:
                                SwTacUtils.ParseTriggers(action, overridePair);

                                break;
                            case SwTacConfigOverrideConstants.CONFIG_CONDITIONS_SCHEME_NAME:
                                action.Conditions ??= new SwCoreTacConditions();
                                OverrideConditions(action, overridePair, suffix);

                                break;
                            case SwTacConfigOverrideConstants.CONFIG_PRIORITY_SCHEME_NAME:
                                action.Priority = Convert.ToInt32(overridePair.Value);

                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        SwInfra.Logger.LogException(e, EWisdomLogType.TAC, $"Couldn't parse {overridePair.Key}");
                    }
                }
            }

            return tacConfig;
        }

        // This function made to override the TAC config using the config dynamic dictionary
        private static void OverrideConditions(SwActionData action, KeyValuePair<string, object> overridePair, string suffix)
        {
            var value = overridePair.Value;

            try
            {
                switch (suffix)
                {
                    case SwTacConfigOverrideConstants.IS_DURING_LEVEL_SUFFIX_NAME:
                    {
                        action.Conditions.IsDuringLevel = (bool)value;

                        break;
                    }
                    case SwTacConfigOverrideConstants.CONFIG_STATUS_SUFFIX_NAME:
                    {
                        action.Conditions.ConfigStatus = SwUtils.JsonHandler.DeserializeObject<EConfigStatus[]>((string)value);

                        break;
                    }
                    case SwTacConfigOverrideConstants.CURRENT_STATE_SUFFIX_NAME:
                    {
                        action.Conditions.CurrentState = SwUtils.JsonHandler.DeserializeObject<SwSystemState.EGameState[]>((string)value);

                        break;
                    }
                    case SwTacConfigOverrideConstants.PREVIOUS_STATE_SUFFIX_NAME:
                    {
                        action.Conditions.PreviousState = SwUtils.JsonHandler.DeserializeObject<SwSystemState.EGameState[]>((string)value);

                        break;
                    }
                    case SwTacConfigOverrideConstants.MIN_LEVEL_SUFFIX_NAME:
                    {
                        action.Conditions.MinLevel = (int?)((long)value);

                        break;
                    }
                    case SwTacConfigOverrideConstants.MIN_MINUTE_SUFFIX_NAME:
                    {
                        action.Conditions.MinMinute = (int?)((long)value);

                        break;
                    }
                    case SwTacConfigOverrideConstants.MIN_AGE_SUFFIX_NAME:
                    {
                        action.Conditions.MinAge = (int?)((long)value);

                        break;
                    }
                    case SwTacConfigOverrideConstants.MAX_LEVEL_SUFFIX_NAME:
                    {
                        action.Conditions.MaxLevel = (int?)((long)value);

                        break;
                    }
                    case SwTacConfigOverrideConstants.MAX_MINUTE_SUFFIX_NAME:
                    {
                        action.Conditions.MaxMinute = (int?)((long)value);

                        break;
                    }
                    case SwTacConfigOverrideConstants.ALLOW_AFTER_SUFFIX_NAME:
                    {
                        action.Conditions.AllowAfter = SwUtils.JsonHandler.DeserializeObject<ESwActionType[]>((string)value);

                        break;
                    }
                    case SwTacConfigOverrideConstants.MIN_LEVEL_ATTEMPTS_SUFFIX_NAME:
                    {
                        action.Conditions.MinLevelAttempts = (int?)((long)value);

                        break;
                    }
                    case SwTacConfigOverrideConstants.MIN_LEVEL_REVIVES_SUFFIX_NAME:
                    {
                        action.Conditions.MinLevelRevives = (int?)((long)value);

                        break;
                    }
                    case SwTacConfigOverrideConstants.MIN_COMPLETED_LEVELS_SUFFIX_NAME:
                    {
                        action.Conditions.MinCompletedLevels = (int?)((long)value);

                        break;
                    }
                    case SwTacConfigOverrideConstants.MIN_COMPLETED_BONUS_LEVELS_SUFFIX_NAME:
                    {
                        action.Conditions.MinCompletedBonusLevels = (int?)((long)value);

                        break;
                    }
                    case SwTacConfigOverrideConstants.MIN_COMPLETED_TUTORIAL_LEVELS_SUFFIX_NAME:
                    {
                        action.Conditions.MinCompletedTutorialLevels = (int?)((long)value);

                        break;
                    }
                    case SwTacConfigOverrideConstants.SINGLE_USER_ABOVE_SECONDS_SUFFIX_NAME:
                    {
                        action.Conditions.SingleUseAboveSeconds = SwUtils.JsonHandler.DeserializeObject<int[]>((string)value);

                        break;
                    }
                    case SwTacConfigOverrideConstants.EXACT_LEVELS_SUFFIX_NAME:
                    {
                        action.Conditions.ExactLevels = SwUtils.JsonHandler.DeserializeObject<int[]>((string)value);

                        break;
                    }
                    case SwTacConfigOverrideConstants.MIN_ACTIVE_DAYS_SUFFIX_NAME:
                    {
                        action.Conditions.MinActiveDays = (int)value;

                        break;
                    }
                    case SwTacConfigOverrideConstants.MIN_REVENUE_SUFFIX_NAME:
                    {
                        action.Conditions.MinRevenue = (float)value;

                        break;
                    }
                    case SwTacConfigOverrideConstants.FREE_ZONE_SUFFIX_NAME:
                    {
                        action.Conditions.AdFreeZone = (bool)value;

                        break;
                    }
                    case SwTacConfigOverrideConstants.IS_NO_ADS_SUFFIX_NAME:
                    {
                        action.Conditions.IsNoAds = (bool)value;

                        break;
                    }
                }
            }
            catch (Exception e)
            {
                SwInfra.Logger.LogException(e, EWisdomLogType.TAC, $"Couldn't parse {overridePair.Key}");

                throw;
            }
        }

        #endregion
    }
}