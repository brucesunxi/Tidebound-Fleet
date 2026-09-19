namespace Tidebound.Boss
{
    public sealed class BossRuntimeData
    {
        public string BossId { get; }
        public int InitialHp { get; }
        public int Hp { get; internal set; }
        public BossRuntimeData(string bossId, int initialHp)
        { BossId = bossId; InitialHp = initialHp; Hp = initialHp; }
    }
}
