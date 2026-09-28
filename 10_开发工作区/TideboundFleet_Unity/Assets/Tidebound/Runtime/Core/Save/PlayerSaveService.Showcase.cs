using Tidebound.Collection;
using System.Linq;

namespace Tidebound.Save
{
    public enum ShowcaseSelectionStatus { Saved, AlreadySelected, Locked, InvalidShip, Busy, StorageUnavailable }
    public sealed partial class PlayerSaveService
    {
        public string SelectedShowcaseId=>data.ShowcaseId;
        public bool OwnsShowcase(string id)=>IsAvailable && (ShowcaseCatalog.IsOwned(id,data.HighestClearedLevel)||data.Appearance.UniqueReceipts.Any(r=>r.Pool==DrawPool.Showcase&&r.ItemId==id));
        public ShowcaseSelectionStatus SelectShowcase(string id)
        {
            if(!IsAvailable)return ShowcaseSelectionStatus.StorageUnavailable;
            if(ShowcaseCatalog.Find(id)==null)return ShowcaseSelectionStatus.InvalidShip;
            if(!OwnsShowcase(id))return ShowcaseSelectionStatus.Locked;
            if(data.ShowcaseId==id)return ShowcaseSelectionStatus.AlreadySelected;
            if(Runtime!=null && (data.Attempt?.AttemptId!=Runtime.Session.SessionId || Runtime.Movement.IsBusy || Runtime.PendingMoveId!=null ||
                (Runtime.Combat.IsVictorious && !CurrentAttemptSettled)))return ShowcaseSelectionStatus.Busy;
            var next=data.Copy();next.ShowcaseId=id;
            if(Runtime!=null)next.Attempt=Runtime.Capture();
            return Commit(next)?ShowcaseSelectionStatus.Saved:ShowcaseSelectionStatus.StorageUnavailable;
        }
    }
}
