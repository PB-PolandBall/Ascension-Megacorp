using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace USAC
{
    // 定义物资箱建筑逻辑类
    public class Building_Crate : Building
    {
        public override IEnumerable<FloatMenuOption> GetFloatMenuOptions(Pawn selPawn)
        {
            foreach (var opt in base.GetFloatMenuOptions(selPawn)) yield return opt;
            yield return new FloatMenuOption("USAC_OpenCrate".Translate(), OpenCrate);
        }

        private void OpenCrate()
        {
            Map map = Map;
            IntVec3 pos = Position;
            Rot4 rot = Rotation;

            // 获取物资表数据扩展
            CrateExtension ext = def.GetModExtension<CrateExtension>();
            if (ext != null)
            {
                // 执行内容物资生成逻辑
                foreach (var group in ext.lootGroups)
                {
                    if (Rand.Value < group.chance)
                    {
                        CrateLootItem item = group.items.RandomElementByWeight(i => i.weight);
                        if (item != null) SpawnLoot(map, pos, item);
                    }
                }

                // 执行空箱建筑替换逻辑
                if (ext.emptyDef != null)
                {
                    Thing emptyCrate = ThingMaker.MakeThing(ext.emptyDef);
                    GenSpawn.Spawn(emptyCrate, pos, map, rot);
                }
            }

            Messages.Message("USAC_CrateOpened".Translate(), new TargetInfo(pos, map), MessageTypeDefOf.PositiveEvent);
            Destroy();
        }

        private void SpawnLoot(Map map, IntVec3 pos, CrateLootItem loot)
        {
            ThingDef lootDef = DefDatabase<ThingDef>.GetNamedSilentFail(loot.thingDef);
            if (lootDef == null) return;

            int count = Rand.RangeInclusive(loot.minCount, loot.maxCount);
            if (count <= 0) return;

            Thing thing = ThingMaker.MakeThing(lootDef);
            thing.stackCount = count;
            GenPlace.TryPlaceThing(thing, pos, map, ThingPlaceMode.Near);
        }
    }
}
