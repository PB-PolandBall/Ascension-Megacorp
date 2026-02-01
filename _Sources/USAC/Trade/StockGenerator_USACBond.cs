using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using Verse;

namespace USAC
{
    // USAC债券库存生成器
    public class StockGenerator_USACBond : StockGenerator
    {
        public override IEnumerable<Thing> GenerateThings(PlanetTile forTile, Faction faction = null)
        {
            ThingDef bondDef = ThingDef.Named("USAC_Bond");
            int count = countRange.RandomInRange;

            Thing bond = ThingMaker.MakeThing(bondDef);
            bond.stackCount = count;
            yield return bond;
        }

        public override bool HandlesThingDef(ThingDef thingDef)
        {
            return thingDef.defName == "USAC_Bond";
        }
    }
}
