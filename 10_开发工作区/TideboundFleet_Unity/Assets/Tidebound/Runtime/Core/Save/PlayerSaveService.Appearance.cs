using System;
using System.Linq;
using Tidebound.Collection;

namespace Tidebound.Save
{
    public enum AppearanceSelectionStatus { Saved, AlreadySelected, Locked, InvalidSelection, SlotsFull, Busy, StorageUnavailable }
    public sealed partial class PlayerSaveService
    {
        public string SelectedSceneId=>data.Appearance.SceneId;
        public string SelectedTrailId=>data.Appearance.TrailId;
        public string[] AppearanceEquipment=>data.Appearance.EffectiveSlots(data.Collection);
        public bool OwnsAppearance(string id)=>IsAvailable&&data.Appearance.Owns(id,data.HighestClearedLevel,data.Collection);
        public AppearanceSelectionStatus SelectAppearance(string id)
        {
            if(!IsAvailable)return AppearanceSelectionStatus.StorageUnavailable;
            var item=AppearanceCatalog.Find(id);if(item==null)return AppearanceSelectionStatus.InvalidSelection;
            if(!OwnsAppearance(id))return AppearanceSelectionStatus.Locked;
            if(Runtime!=null&&(Runtime.Movement.IsBusy||Runtime.PendingMoveId!=null||Runtime.Combat.IsVictorious&&!CurrentAttemptSettled))return AppearanceSelectionStatus.Busy;
            var next=data.Copy();
            if(item.Kind==AppearanceKind.Skin)
            {
                var slots=AppearanceEquipment;var index=Array.IndexOf(slots,id);
                if(index>=0)slots[index]=null;
                else{index=Array.IndexOf(slots,null);if(index<0)return AppearanceSelectionStatus.SlotsFull;slots[index]=id;}
                next.Appearance.Equipment=slots;
            }
            else if(item.Kind==AppearanceKind.Scene)
            {if(next.Appearance.SceneId==id)return AppearanceSelectionStatus.AlreadySelected;next.Appearance.SceneId=id;}
            else{if(next.Appearance.TrailId==id)return AppearanceSelectionStatus.AlreadySelected;next.Appearance.TrailId=id;}
            if(Runtime!=null)next.Attempt=Runtime.Capture();
            return Commit(next)?AppearanceSelectionStatus.Saved:AppearanceSelectionStatus.StorageUnavailable;
        }
    }
}
