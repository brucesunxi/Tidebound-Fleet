using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace SupersonicWisdomSDK
{
    internal class SwTacModel
    {
        #region --- Members ---

        private Dictionary<string, List<SwActionData>> _actionsByTriggerDict;
        private Dictionary<ESwActionType, List<SwActionData>> _actionsByActionTypeDict;
        private Dictionary<ESwActionType, List<SwActionData>> _actionsAfterActionDict;

        #endregion


        #region --- Properties ---
        
        public int MaximumUiActions { get; private set; }

        #endregion

        
        #region --- Construction ---

        public SwTacModel(SwTacConfig triggerData, float allSessionsPlaytimeNeto)
        {
            SwInfra.Logger.Log(EWisdomLogType.TAC, SwUtils.JsonHandler.SerializeObject(triggerData));
            
            triggerData.Actions = EditTriggerData(triggerData, allSessionsPlaytimeNeto);
            
            InitDataStructures();
            UpdateTriggersData(triggerData);
        }

        private static SwActionData[] EditTriggerData(SwTacConfig triggerData, float allSessionsPlaytimeNeto)
        {
            var sortedActionData = triggerData.Actions.OrderBy(t => -t.Priority).ToArray();

            foreach (var actionData in sortedActionData)
            {
                var singleUseAboveSeconds = actionData?.Conditions?.SingleUseAboveSeconds?.ToList();

                if (singleUseAboveSeconds != null)
                {
                    singleUseAboveSeconds.RemoveAll((value) => value <= allSessionsPlaytimeNeto);

                    actionData.Conditions.SingleUseAboveSeconds = singleUseAboveSeconds.ToArray();
                }
            }

            return sortedActionData;
        }

        #endregion


        #region --- Public Methods ---

        public List<SwActionData> GetActionsByTrigger(string keyTrigger)
        {
            return keyTrigger.SwIsNullOrEmpty() || !_actionsByTriggerDict.TryGetValue(keyTrigger, out var value) ? null : value;
        }

        public List<SwActionData> GetActionAfterAction(string currentTrigger, params ESwActionType[] performedActions)
        {
            var actionsAllowed = _actionsByTriggerDict.SwSafelyGet(currentTrigger, new List<SwActionData>());

            foreach (var performedAction in performedActions)
            {
                var actionAfterAction = _actionsAfterActionDict.SwSafelyGet(performedAction, new List<SwActionData>());
                actionsAllowed = actionsAllowed.Intersect(actionAfterAction).ToList();
            }

            return actionsAllowed;
        }
        
        #endregion


        #region --- Private Methods ---

        private void InitDataStructures()
        {
            _actionsByTriggerDict = new Dictionary<string, List<SwActionData>>();
            _actionsAfterActionDict = new Dictionary<ESwActionType, List<SwActionData>>();
            _actionsByActionTypeDict = new Dictionary<ESwActionType, List<SwActionData>>();
        }

        private void UpdateTriggersData([NotNull] SwTacConfig tacConfig)
        {
            _actionsByTriggerDict.Clear();
            _actionsAfterActionDict.Clear();
            _actionsByActionTypeDict.Clear();

            MaximumUiActions = tacConfig.MaximumUiActions;

            var actionsData = tacConfig.Actions;

            if (actionsData == null || !actionsData.Any())
            {
                return;
            }

            // Create empty list for each action type
            foreach (var type in Enum.GetValues(typeof(ESwActionType)).Cast<ESwActionType>())
            {
                if (!_actionsAfterActionDict.ContainsKey(type))
                {
                    _actionsAfterActionDict.Add(type, new List<SwActionData>());
                }
                
                if (!_actionsByActionTypeDict.ContainsKey(type))
                {
                    _actionsByActionTypeDict.Add(type, new List<SwActionData>());
                }
            }

            // Fill both list
            foreach (var actionData in actionsData.Where(ad => !ad.Triggers.SwIsNullOrEmpty()))
            {
                foreach (var keyTrigger in actionData.Triggers)
                {
                    if (!_actionsByTriggerDict.ContainsKey(keyTrigger))
                    {
                        _actionsByTriggerDict.Add(keyTrigger, new List<SwActionData>());
                    }

                    _actionsByTriggerDict[keyTrigger].Add(actionData);
                    _actionsByActionTypeDict[actionData.ActionType].Add(actionData);
                }

                if (actionData.Conditions != null && !actionData.Conditions.AllowAfter.SwIsNullOrEmpty())
                {
                    foreach (var allowAfter in actionData.Conditions.AllowAfter)
                    {
                        _actionsAfterActionDict[allowAfter].Add(actionData);
                    }
                }
            }
        }

        #endregion
    }
}