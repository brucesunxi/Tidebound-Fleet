using System;
using UnityEditor;
using System.Collections;
using UnityEngine;

internal class SwEditorCoroutines
{
    #region --- Members ---

    private readonly Action _callback;
    private readonly bool _delayed;
    private readonly IEnumerator _routine;
    private float _lastEditorUpdateTime;

    #endregion


    #region --- Construction ---

    public SwEditorCoroutines(IEnumerator routine, bool delayed, Action callback)
    {
        _routine = routine;
        _delayed = delayed;
        _callback = callback;
    }

    #endregion


    #region --- Mono Override ---

    private void Start ()
    {
        EditorApplication.update += Update;
    }

    private void Update ()
    {
        if (_delayed)
        {
            var floatValue = 0f;

            if (_routine.Current != null)
            {
                try
                {
                    floatValue = (float)_routine.Current;
                }
                catch (InvalidCastException)
                {
                    floatValue = 0f;
                }
            }

            if (_lastEditorUpdateTime == 0f)
            {
                if (floatValue > 0f)
                {
                    _lastEditorUpdateTime = Time.realtimeSinceStartup;

                    return;
                }
            }
            else
            {
                if (Time.realtimeSinceStartup - _lastEditorUpdateTime >= floatValue)
                {
                    _lastEditorUpdateTime = 0f;
                }
                else
                {
                    return;
                }
            }
        }

        if (!_routine.MoveNext())
        {
            StopEditorCoroutine();
            _callback?.Invoke();
        }
    }

    #endregion


    #region --- Public Methods ---

    public static SwEditorCoroutines StartDelayedEditorCoroutine(IEnumerator routine)
    {
        return StartEditorCoroutine(routine, true);
    }

    public static SwEditorCoroutines StartEditorCoroutine(IEnumerator routine, bool delayed = false, Action callback = null)
    {
        var coroutine = new SwEditorCoroutines(routine, delayed, callback);
        coroutine.Start();

        return coroutine;
    }

    #endregion


    #region --- Private Methods ---

    private void StopEditorCoroutine ()
    {
        EditorApplication.update -= Update;
    }

    #endregion
    
    
    #region --- New Delayed Coroutine Functionality ---
    
    private class DelayedCoroutineStarter
    {
        #region --- Members ---
        
        private readonly IEnumerator _coroutine;
        private readonly Action _callback;
        private readonly float _delay;
        private readonly double _startTime;
        
        #endregion
        
        
        #region --- Construction ---
        
        public DelayedCoroutineStarter(IEnumerator coroutine, float delay, Action callback)
        {
            _coroutine = coroutine;
            _delay = delay;
            _callback = callback;
            _startTime = EditorApplication.timeSinceStartup;
            EditorApplication.update += Update;
        }
        
        #endregion
        
        
        #region --- Mono Override ---
        
        private void Update()
        {
            if (!(EditorApplication.timeSinceStartup >= _startTime + _delay)) return;
            
            EditorApplication.update -= Update;
            StartEditorCoroutine(_coroutine, true, _callback);
        }
        
        #endregion
    }
    
    
    #region --- Public Methods ---
    
    public static void StartEditorCoroutineWithDelay(IEnumerator coroutine, float delay, Action callback = null)
    {
        new DelayedCoroutineStarter(coroutine, delay, callback);
    }
    
    #endregion
    
    #endregion
}