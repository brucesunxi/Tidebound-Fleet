using System;
using System.Collections;
using System.Collections.Generic;

namespace SupersonicWisdomSDK
{
    internal interface ISwTacSystem : ISwCoreConfigListener
    {
        public IEnumerator FireTriggers(params string[] newTriggers);
        public IEnumerator FireTriggers(Dictionary<string, object> metaData, params string[] newTriggers);
        public IEnumerator InternalFireTriggersRoutine(params string[] triggers);
        public void InternalFireTriggers(params string[] triggers);
        public void AddListeners(params ISwTacSystemListener[] listeners);
    }
}