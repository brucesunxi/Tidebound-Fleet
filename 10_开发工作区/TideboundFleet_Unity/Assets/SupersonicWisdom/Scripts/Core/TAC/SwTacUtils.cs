using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace SupersonicWisdomSDK
{
    public static class SwTacUtils
    {
        internal static string ModifyInternalTrigger(string trigger)
        {
            return SwTacSystem.INTERNAL_TRIGGER_FORMAT.Format(trigger.ToLower());
        }

        internal static void LogNewTriggerReached(string triggerReached)
        {
            SwInfra.Logger.Log(EWisdomLogType.TAC, "New triggers reached - {0}".Format(triggerReached));
        }

        internal static void LogAction(ESwActionType actionToPerform)
        {
            if (actionToPerform == ESwActionType.None) return;
            
            SwInfra.Logger.Log(EWisdomLogType.TAC, "Trying to perform action - {0}".Format(actionToPerform.SwToString()));
        }
        
        internal static void ParseTriggers(SwActionData action, KeyValuePair<string, object> overridePair)
        {
            action.Triggers = SwUtils.JsonHandler.DeserializeObject<string[]>(overridePair.Value.ToString());
        }
        
        internal static void ValidateListeners(List<ISwTacSystemListener> listeners)
        {
            var listenerTypes = new HashSet<ESwActionType>();

            foreach (var listener in listeners)
            {
                if (listenerTypes.Contains(listener.ActionType))
                {
                    SwInfra.Logger.LogError(EWisdomLogType.TAC, $"We have two listeners responding to the same ActionType : {listener.ActionType}");
                    Application.Quit();
                }

                listenerTypes.Add(listener.ActionType);
            }
        }

        internal static void ModifyConditionsOnActionPerformed(SwActionData performedAction, bool isTimeBased)
        {
            if (performedAction.Conditions == null) return;

            var listSingleUseAboveSeconds = performedAction.Conditions.SingleUseAboveSeconds?.ToList();

            if (isTimeBased && listSingleUseAboveSeconds != null)
            {
                listSingleUseAboveSeconds.RemoveAt(0);
                performedAction.Conditions.SingleUseAboveSeconds = listSingleUseAboveSeconds.ToArray();
            }
        }
    }
}