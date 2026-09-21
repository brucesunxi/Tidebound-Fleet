using System;
using Tidebound.Save;
using UnityEngine;
using UnityEngine.UI;
using Tidebound.Unity.UI;

namespace Tidebound.Unity.LevelDesign
{
    public sealed partial class PortraitPuzzleGraybox
    {
        private RectTransform resultPanel;
        private VictoryResultPanel resultView;
        private CanvasGroup resultCanvas, defeatedBoss;
        private VictoryResult result;
        private bool resultStarted, resultSaveFailed, resultOverview, reducedResultMotion, continuingResult, resultDispatch;
        private float resultExitTime, resultRevealTime;
        private string resultNotice;
        private bool PracticeResult => campaign && IsCleared && saveService?.IsAvailable!=true;
        public bool IsResultOpen => resultPanel != null && resultPanel.gameObject.activeSelf;
        public bool IsResultReadable => result != null && IsResultOpen && resultRevealTime >= .18f;
        public bool IsResultSaveFailed => resultSaveFailed;
        public VictoryResult CurrentResult => result;
        public VictoryResultPanel ResultView => resultView;
        // I5-C may handle the level-2 collection choice here. False keeps the existing result/attempt.
        public Func<VictoryResult,bool> BeforeNextLevel { get; set; }
        private bool ResultOwnsInput => campaign && IsCleared;
        private void BuildResultControls(Transform parent)
        {
            resultPanel=Panel("VictoryResult",parent,new Rect(),HarborUI.Cream);
            resultPanel.GetComponent<Image>().raycastTarget=true;resultCanvas=resultPanel.gameObject.AddComponent<CanvasGroup>();
            resultView=resultPanel.gameObject.AddComponent<VictoryResultPanel>();
            resultView.Initialize(font,ContinueFromResult,()=>{if(homeNavigation)ReturnHome();else ToggleResultOverview();},RetryResultSave,()=>SetReducedResultMotion(!reducedResultMotion,true),homeNavigation);
            defeatedBoss=battlePanel.gameObject.AddComponent<CanvasGroup>();resultPanel.gameObject.SetActive(false);
        }
        public void SetReducedResultMotion(bool reduced,bool persist=false)
        {
            reducedResultMotion=reduced;
            if(persist){PlayerPrefs.SetInt("Tidebound.ReducedResultMotion",reduced?1:0);PlayerPrefs.Save();}
            if(reduced && resultStarted){resultExitTime=.6f;resultRevealTime=1;}
            PaintResult();
        }
        private void RestoreResultIfComplete()
        {
            if(!campaign || !IsCleared)return;
            TickResult(0);
            if(result!=null){resultExitTime=.6f;resultRevealTime=1;resultPanel.gameObject.SetActive(true);PaintResult();}
        }
        private bool TickResult(float seconds)
        {
            if(!ResultOwnsInput)return false;
            if(!resultStarted)
            {
                resultStarted=true;demo=null;input.CancelSelection();tools.CancelSelection();NotifyUserActivity();
                menuPanel.gameObject.SetActive(false);menuPauseOwned=false;
            }
            if(PracticeResult){resultPanel.gameObject.SetActive(true);resultCanvas.alpha=1;resultView.Practice();return true;}
            if(!backgrounded)resultExitTime+=seconds;
            if(result==null && !resultSaveFailed)
            {
                if(saveService?.IsAvailable==true && (saveService.CurrentAttemptSettled || SaveCheckpoint(true)))
                    result=VictoryResult.FromSaved(saveService.Snapshot,catalog.Count);
                else resultSaveFailed=true;
            }
            if(result==null)
            {resultPanel.gameObject.SetActive(true);resultCanvas.alpha=1;resultView.Pending(resultSaveFailed);return true;}
            if(reducedResultMotion)resultExitTime=.6f;
            defeatedBoss.alpha=1-Mathf.Clamp01(resultExitTime/.6f);
            if(resultExitTime<.6f)return true;
            if(!resultPanel.gameObject.activeSelf){resultPanel.gameObject.SetActive(true);resultRevealTime=0;}
            if(!backgrounded)resultRevealTime+=seconds;
            if(reducedResultMotion)resultRevealTime=1;
            PaintResult();return true;
        }
        private void PaintResult()
        {
            if(result==null || resultView==null || !IsResultOpen)return;
            var collectionLink=resultPanel.Find("OpenCollection");if(collectionLink!=null)collectionLink.gameObject.SetActive(!homeNavigation && IsResultReadable && saveService?.IsAvailable==true && saveService.CurrentLevel>=3);
            defeatedBoss.alpha=1-Mathf.Clamp01(resultExitTime/.6f);
            resultCanvas.alpha=Mathf.Clamp01(resultRevealTime/.18f);
            var t=reducedResultMotion ? 1 : Mathf.Clamp01(resultRevealTime/.55f);
            var progress=Mathf.Lerp(result.PreviousProgress,result.Progress,t);
            resultView.Present(result,resultOverview,IsResultReadable,progress,reducedResultMotion);
            if(resultNotice!=null)resultView.SetNotice(resultNotice);
        }
        public void RetryResultSave()
        {
            if(!ResultOwnsInput || IsEntrySaveBlocked || result!=null)return;
            resultSaveFailed=false;TickResult(0);UpdateLabels();
        }
        public void ToggleResultOverview()
        {
            if(!IsResultReadable || IsEntrySaveBlocked)return;
            resultOverview=!resultOverview;resultRevealTime=1;PaintResult();
        }
        public void ContinueFromResult()
        {
            if(PracticeResult && IsResultOpen)
            {resultDispatch=true;try{SelectLevel(LevelIndex);}finally{resultDispatch=false;}return;}
            if(IsHomeOpen || IsCollectionOpen || !IsResultReadable || (!homeNavigation && !result.HasNext) || IsEntrySaveBlocked || continuingResult)return;
            continuingResult=true;
            try
            {
                resultNotice=null;
                if(saveService.CanClaimFirstBlue){if(homeNavigation)ReturnHome();OpenCollection();return;}
                if(!catalog.IsAvailable(result.NextLevel-1))
                {resultNotice="The next level is not available yet. Your rewards are saved.";PaintResult();return;}
                if(BeforeNextLevel!=null && !BeforeNextLevel(result))
                {resultNotice="Complete your pending choice, then continue.";PaintResult();return;}
                resultDispatch=true;SelectLevel(result.NextLevel-1);
            }
            catch(Exception){resultNotice="Unable to load the next level. Your progress has been kept.";PaintResult();}
            finally{resultDispatch=false;continuingResult=false;}
        }
        private void ClearResult()
        {
            result=null;resultPanel=null;resultView=null;resultCanvas=null;defeatedBoss=null;
            resultStarted=resultSaveFailed=resultOverview=false;resultExitTime=resultRevealTime=0;resultNotice=null;
        }
    }
}
