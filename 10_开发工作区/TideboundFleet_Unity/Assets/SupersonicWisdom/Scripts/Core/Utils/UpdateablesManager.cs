using System.Collections.Generic;
using UnityEngine;

namespace SupersonicWisdomSDK
{

    internal class SwUpdateableManager : MonoBehaviour
    {
        #region --- Members ---

        private List<ISwLateUpdateable> _lateUpdateables;
        private List<ISwUpdateable> _updateables;

        #endregion


        #region --- Mono Override ---

        private void Update()
        {
            if (_updateables.SwIsNullOrEmpty()) return;

            foreach (var updateable in _updateables)
            {
                updateable?.OnUpdate();
            }
        }

        private void LateUpdate()
        {
            if (_lateUpdateables.SwIsNullOrEmpty()) return;

            foreach (var lateUpdateable in _lateUpdateables)
            {
                lateUpdateable?.OnLateUpdate();
            }
        }

        #endregion


        #region --- Public Methods ---

        public void Add(params ISwLateUpdateable[] swLateUpdateables)
        {
            _lateUpdateables ??= new List<ISwLateUpdateable>();

            foreach (var lateUpdateables in swLateUpdateables)
            {
                _lateUpdateables.Add(lateUpdateables);
            }
        }
        
        public void Add(params ISwUpdateable[] swUpdateables)
        {
            _updateables ??= new List<ISwUpdateable>();

            foreach (var updateable in swUpdateables)
            {
                _updateables.Add(updateable);
            }
        }

        #endregion
    }
}