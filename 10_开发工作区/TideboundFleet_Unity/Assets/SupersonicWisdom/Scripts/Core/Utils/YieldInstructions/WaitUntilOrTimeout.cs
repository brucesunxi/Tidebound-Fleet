using System;
using UnityEngine;

namespace SupersonicWisdomSDK
{
    public class WaitUntilOrTimeout : CustomYieldInstruction
    {
        #region --- Members ---

        private readonly DateTime _startTime;
        private readonly float _timeoutInSeconds;
        private readonly Func<bool> _predicate;

        #endregion


        #region --- Properties ---

        public override bool keepWaiting
        {
            get { return !_predicate() && (DateTime.UtcNow - _startTime).TotalSeconds < _timeoutInSeconds; }
        }

        #endregion


        #region --- Construction ---

        public WaitUntilOrTimeout(Func<bool> predicate, float timeoutInSeconds)
        {
            _predicate = predicate;
            _timeoutInSeconds = timeoutInSeconds;

            _startTime = DateTime.UtcNow;
        }

        #endregion
    }
}