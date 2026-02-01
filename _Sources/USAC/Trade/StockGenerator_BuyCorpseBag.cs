using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace USAC
{
    // 只购买尸体袋的库存生成器
    public class StockGenerator_BuyCorpseBag : StockGenerator
    {
        public string corpseBagDefName = "USAC_CorpseBag";

        private ThingDef cachedCorpseBagDef;

        private ThingDef CorpseBagDef
        {
            get
            {
                if (cachedCorpseBagDef == null)
                    cachedCorpseBagDef = DefDatabase<ThingDef>.GetNamedSilentFail(corpseBagDefName);
                return cachedCorpseBagDef;
            }
        }

        public override IEnumerable<Thing> GenerateThings(PlanetTile forTile, Faction faction = null)
        {
            return Enumerable.Empty<Thing>();
        }

        public override bool HandlesThingDef(ThingDef thingDef)
        {
            if (CorpseBagDef == null)
                return false;
            return thingDef == CorpseBagDef;
        }

        public override Tradeability TradeabilityFor(ThingDef thingDef)
        {
            if (!HandlesThingDef(thingDef))
                return Tradeability.None;
            return Tradeability.Sellable;
        }
    }
}
