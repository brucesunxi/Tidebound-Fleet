using System;
using System.Collections.Generic;
using System.Linq;
using Tidebound.Board;

namespace Tidebound.Tools
{
    public sealed class ToolInventoryData
    {
        public int Version = 1;
        public int Rescue;
        public int Shuffle;
        public int Reverse;
        public string[] Receipts = Array.Empty<string>();
        public ToolInventoryData Copy() => new ToolInventoryData { Version=Version, Rescue=Rescue, Shuffle=Shuffle, Reverse=Reverse, Receipts=Receipts.ToArray() };
        public void Validate()
        {
            if(Version!=1 || Rescue<0 || Shuffle<0 || Reverse<0 || Receipts==null ||
                Receipts.Any(string.IsNullOrWhiteSpace) || Receipts.Distinct(StringComparer.Ordinal).Count()!=Receipts.Length)
                throw new ArgumentException("Invalid tool inventory.");
        }
    }
    public interface IToolInventoryStore
    {
        ToolInventoryData Load();
        void Save(ToolInventoryData data);
    }
    public sealed class ToolMutation
    {
        public BoardModel BoardAfter { get; }
        public ForwardPathResult[] RescuePaths { get; }
        public ToolMutation(BoardModel boardAfter,ForwardPathResult[] rescuePaths=null) {BoardAfter=boardAfter;RescuePaths=rescuePaths;}
    }
    public interface IToolMutationStore { void SaveToolMutation(ToolInventoryData data,ToolMutation mutation); }
    public sealed class MemoryToolInventoryStore : IToolInventoryStore
    {
        private ToolInventoryData data;
        public ToolInventoryData Load() => data?.Copy();
        public void Save(ToolInventoryData value) { data=value.Copy(); }
    }
    /// <summary>Cross-level stock. Write first, then expose the change; failed storage never grants or spends.</summary>
    public sealed class ToolInventory
    {
        private readonly IToolInventoryStore store;
        private ToolInventoryData data;
        public bool IsAvailable { get; private set; } = true;
        public string LastError { get; private set; }
        public ToolInventory(IToolInventoryStore store=null)
        {
            this.store=store ?? new MemoryToolInventoryStore();
            try { data=this.store.Load() ?? new ToolInventoryData();data.Validate(); }
            catch(Exception e) { data=new ToolInventoryData();IsAvailable=false;LastError=e.GetType().Name; }
        }
        // The profile is authoritative after a transaction that updates stock outside this facade.
        internal void AcceptCommitted(ToolInventoryData value) {value.Validate();data=value.Copy();LastError=null;}
        public int Count(ShipTool tool) => tool==ShipTool.Rescue ? data.Rescue : tool==ShipTool.Shuffle ? data.Shuffle : tool==ShipTool.Reverse ? data.Reverse : 0;
        // Called only by a trusted milestone / confirmed reward / verified purchase adapter, never by an ad-open button.
        public bool Grant(string receipt,int rescue,int shuffle,int reverse)
        {
            if(string.IsNullOrWhiteSpace(receipt) || receipt.StartsWith("coin:",StringComparison.Ordinal) || rescue<0 || shuffle<0 || reverse<0 || (rescue==0 && shuffle==0 && reverse==0))throw new ArgumentException("Invalid inventory grant.");
            if(!IsAvailable || data.Receipts.Contains(receipt,StringComparer.Ordinal))return false;
            var next=data.Copy();
            next.Rescue=checked(next.Rescue+rescue);next.Shuffle=checked(next.Shuffle+shuffle);next.Reverse=checked(next.Reverse+reverse);
            next.Receipts=next.Receipts.Concat(new[]{receipt}).ToArray();return Commit(next);
        }
        public bool TrySpend(ShipTool tool,ToolMutation mutation=null)
        {
            if(!IsAvailable || Count(tool)<=0)return false;
            var next=data.Copy();
            switch(tool) { case ShipTool.Rescue:next.Rescue--;break;case ShipTool.Shuffle:next.Shuffle--;break;case ShipTool.Reverse:next.Reverse--;break;default:return false; }
            return Commit(next,mutation);
        }
        public void ReachLevel(int level,ToolGiftPolicy policy=null)
        {
            if(level<1 || level>10000)throw new ArgumentOutOfRangeException(nameof(level));
            policy=policy ?? new ToolGiftPolicy();
            if(level>=policy.UnlockLevel)Grant("milestone:tools:unlock",1,1,1);
            for(var n=policy.Interval;n<=level;n+=policy.Interval)
            {
                if(n<=policy.UnlockLevel)continue;
                var receipt="milestone:tools:"+n;
                if(data.Receipts.Contains(receipt,StringComparer.Ordinal))continue;
                var tool=new Random().Next(3);
                Grant(receipt,tool==0?1:0,tool==1?1:0,tool==2?1:0);
            }
        }
        private bool Commit(ToolInventoryData next,ToolMutation mutation=null)
        {
            try { next.Validate();if(mutation!=null && store is IToolMutationStore transaction)transaction.SaveToolMutation(next.Copy(),mutation);else store.Save(next.Copy());data=next;LastError=null;return true; }
            catch(Exception e) { LastError=e.GetType().Name;return false; }
        }
    }
    public sealed class ToolGiftPolicy
    {
        public int UnlockLevel { get; }
        public int Interval { get; }
        public ToolGiftPolicy(int unlockLevel=3,int interval=10)
        {
            if(unlockLevel<1 || interval<1)throw new ArgumentOutOfRangeException(nameof(unlockLevel));
            UnlockLevel=unlockLevel;Interval=interval;
        }
    }
}
