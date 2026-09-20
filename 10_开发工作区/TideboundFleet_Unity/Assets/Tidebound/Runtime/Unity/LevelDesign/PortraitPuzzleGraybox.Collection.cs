using Tidebound.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Tidebound.Unity.LevelDesign
{
    public sealed partial class PortraitPuzzleGraybox
    {
        private CollectionPanel collectionView;private RectTransform collectionPanel;private bool collectionPauseOwned;
        public CollectionPanel CollectionView=>collectionView;
        public bool IsCollectionOpen=>collectionPanel!=null && collectionPanel.gameObject.activeSelf;
        private void BuildCollectionControls(Transform parent)
        {
            Button("OpenCollection",resultPanel,"Collection",OpenCollection).gameObject.SetActive(false);
            Button("Collection",menuPanel,"Collection / equipment",OpenCollection);
            collectionPanel=Panel("Collection",parent,new Rect(),new Color(.025f,.055f,.08f,1));collectionPanel.GetComponent<Image>().raycastTarget=true;
            collectionView=collectionPanel.gameObject.AddComponent<CollectionPanel>();collectionView.Initialize(saveService,font,CloseCollection);collectionPanel.gameObject.SetActive(false);
        }
        public void OpenCollection()
        {
            if(saveService?.IsAvailable!=true || saveService.CurrentLevel<3 || !IsEntryReady || IsEntrySaveBlocked || IsBusy || IsAutoPlaying || IsAcquisitionOpen || IsDeadlockOpen || IsCollectionOpen || (ResultOwnsInput && !IsResultReadable))return;
            NotifyUserActivity();input.CancelSelection();tools.CancelSelection();
            collectionPauseOwned=session.State==GameState.Playing || IsMenuOpen && menuPauseOwned;
            if(IsMenuOpen){menuPanel.gameObject.SetActive(false);menuPauseOwned=false;}
            if(collectionPauseOwned){world.PresentationPause=true;if(session.State==GameState.Playing)movement.Pause();}
            collectionView.Open();SaveCheckpoint(true);UpdateLabels();
        }
        public void CloseCollection()
        {
            if(!IsCollectionOpen)return;collectionPanel.gameObject.SetActive(false);
            if(collectionPauseOwned && IsPaused && SaveCheckpoint(true))movement.Resume();
            collectionPauseOwned=false;world.PresentationPause=false;NotifyUserActivity();SaveCheckpoint(true);UpdateLabels();
        }
    }
}
