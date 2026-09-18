using System.Collections.Generic;
using System.Linq;
using Tidebound.Board;
using Tidebound.Boss;
using Tidebound.Config;
using Tidebound.Ship;

namespace Tidebound.Core
{
    public static class LevelSessionFactory
    {
        public static GameSession Create(LevelData level, IReadOnlyList<ShipDefinition> shipCatalog,
            IReadOnlyList<BossDefinition> bossCatalog)
        {
            var validation = BoardValidator.Validate(level, shipCatalog, bossCatalog);
            if (!validation.IsValid) throw new LevelValidationException(validation);
            var byType = shipCatalog.ToDictionary(x => x.TypeId);
            var runtime = new ShipRuntimeData[level.Ships.Length];
            var initialHp = 0;
            for (var i = 0; i < level.Ships.Length; i++)
            {
                var placement = level.Ships[i]; var definition = byType[placement.TypeId];
                var skinId = placement.Length == FoundationLimits.MaxShipLength
                    ? FoundationLimits.DefaultLongSkinId
                    : FoundationLimits.DefaultStandardSkinId;
                runtime[i] = new ShipRuntimeData(placement.Id, placement.TypeId, skinId, placement.Position,
                    placement.Direction, placement.Length, definition.DamageLv1);
                initialHp = checked(initialHp + definition.DamageLv1);
            }
            // Compute exactly once from all initial Lv1 instances. No callbacks or live list binding.
            return new GameSession(level.LevelId, level.Width, level.Height, runtime, new BossRuntimeData(level.BossId, initialHp));
        }
    }
}
