using System;
using System.Collections.Generic;
using System.Linq;
using Tidebound.Collection;

namespace Tidebound.Save
{
    public enum EquipmentStatus { Saved, AlreadySaved, Locked, InvalidRequest, InvalidEquipment, RequestConflict, Busy, StorageUnavailable }
    public sealed partial class PlayerSaveService
    {
        public bool CanClaimFirstBlue=>IsAvailable && data.Collection.CanClaimFirstBlue(data.HighestClearedLevel);
        public EquipmentStatus SetEquipment(string requestId,IReadOnlyList<string> slots)
        {
            if(!IsAvailable)return EquipmentStatus.StorageUnavailable;
            if(!Guid.TryParseExact(requestId,"N",out _))return EquipmentStatus.InvalidRequest;
            var prior=data.Collection.EquipmentReceipts.FirstOrDefault(r=>r.RequestId==requestId);
            if(prior!=null)return slots!=null && prior.Slots.SequenceEqual(slots) ? EquipmentStatus.AlreadySaved : EquipmentStatus.RequestConflict;
            if(data.Purchases.Any(r=>r.RequestId==requestId) || data.Collection.Receipts.Any(r=>r.RequestId==requestId))return EquipmentStatus.RequestConflict;
            if(data.CurrentLevel<3)return EquipmentStatus.Locked;
            try{CollectionData.ValidateSlots(slots?.ToArray(),data.Collection.OwnedIds);}catch(ArgumentException){return EquipmentStatus.InvalidEquipment;}
            if(Runtime!=null && (data.Attempt?.AttemptId!=Runtime.Session.SessionId || Runtime.Movement.IsBusy || Runtime.PendingMoveId!=null ||
                (Runtime.Combat.IsVictorious && !CurrentAttemptSettled)))return EquipmentStatus.Busy;
            var next=data.Copy();next.Collection.Equipment=slots.ToArray();
            next.Collection.EquipmentReceipts=next.Collection.EquipmentReceipts.Concat(new[]{new EquipmentReceipt{RequestId=requestId,AtLevel=data.CurrentLevel,Slots=slots.ToArray()}}).ToArray();
            if(Runtime!=null)next.Attempt=Runtime.Capture();
            return Commit(next) ? EquipmentStatus.Saved : EquipmentStatus.StorageUnavailable;
        }
    }
}
