using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace USAC
{
    // USAC 机兵订单库存生成器
    public class StockGenerator_USAC_Mechs : StockGenerator
    {
        #region 字段

        // 生成的种类数量范围
        public IntRange kindCountRange = new IntRange(2, 4);

        // 每种机兵订单的数量范围
        public IntRange countPerKindRange = new IntRange(1, 2);

        #endregion

        #region 公共方法

        public override IEnumerable<Thing> GenerateThings(PlanetTile forTile, Faction faction = null)
        {
            // 获取所有机兵订单定义
            var orderDefs = GetMechOrderDefs().ToList();
            if (orderDefs.Count == 0)
                yield break;

            // 随机选择几种
            int kindCount = kindCountRange.RandomInRange;
            kindCount = System.Math.Min(kindCount, orderDefs.Count);

            var selectedDefs = new List<ThingDef>();
            for (int i = 0; i < kindCount && orderDefs.Count > 0; i++)
            {
                var def = orderDefs.RandomElement();
                selectedDefs.Add(def);
                orderDefs.Remove(def);
            }

            // 生成物品
            foreach (var def in selectedDefs)
            {
                int count = countPerKindRange.RandomInRange;
                for (int i = 0; i < count; i++)
                {
                    Thing order = ThingMaker.MakeThing(def);
                    yield return order;
                }
            }
        }

        public override bool HandlesThingDef(ThingDef thingDef)
        {
            return thingDef.tradeTags != null && thingDef.tradeTags.Contains("USAC_MechOrder");
        }

        #endregion

        #region 私有方法

        private IEnumerable<ThingDef> GetMechOrderDefs()
        {
            foreach (var def in DefDatabase<ThingDef>.AllDefs)
            {
                if (def.tradeTags != null && def.tradeTags.Contains("USAC_MechOrder"))
                {
                    yield return def;
                }
            }
        }

        #endregion
    }
}
