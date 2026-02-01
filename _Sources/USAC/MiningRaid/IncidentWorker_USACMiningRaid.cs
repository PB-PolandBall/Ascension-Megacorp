using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace USAC
{
    // 定义盗米采矿突袭事件逻辑
    public class IncidentWorker_USACMiningRaid : IncidentWorker
    {
        #region 常量

        private const float MinPoints = 300f;
        private const float GuardPointsFactor = 0.6f;

        #endregion

        #region 配置

        private static readonly List<MechGenOption> MechOptions = new List<MechGenOption>
        {
            new MechGenOption("USAC_Mech_Rocky", 80f, 10f),
            new MechGenOption("USAC_Mech_Paraman", 160f, 6f),
            new MechGenOption("USAC_Mech_Cobalt", 100f, 6f),
            new MechGenOption("USAC_Mech_Gonk", 420f, 2f),
            new MechGenOption("USAC_Mech_Omaha", 500f, 2f),
        };

        private class MechGenOption
        {
            public string KindDefName;
            public float CombatPower;
            public float SelectionWeight;

            public MechGenOption(string kindDefName, float combatPower, float selectionWeight)
            {
                KindDefName = kindDefName;
                CombatPower = combatPower;
                SelectionWeight = selectionWeight;
            }
        }

        #endregion

        #region 事件检查

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms)) return false;

            Map map = (Map)parms.target;
            Faction usacFaction = Find.FactionManager.FirstFactionOfDef(USAC_FactionDefOf.USAC_Faction);
            if (usacFaction == null) return false;

            // 校验地图深层资源存续情况
            if (!HasDeepResources(map)) return false;

            // 搜寻合理的采矿突袭落点
            return TryFindMiningLocation(map, out _);
        }

        #endregion

        #region 执行逻辑

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;
            Faction usacFaction = Find.FactionManager.FirstFactionOfDef(USAC_FactionDefOf.USAC_Faction);
            if (usacFaction == null) return false;

            // 锁定采矿机空投目标位置
            if (!TryFindMiningLocation(map, out IntVec3 miningLocation)) return false;

            // 计算派系好感度权重系数
            int goodwill = usacFaction.PlayerGoodwill;
            float goodwillFactor = Mathf.Clamp01(goodwill / 100f);

            // 生成初始突袭护卫名单
            List<Pawn> originalGuards = GenerateGuards(usacFaction, parms.points * GuardPointsFactor);

            // 执行机兵战斗力层级分类
            List<Pawn> lightMechs = new List<Pawn>();
            List<Pawn> mediumMechs = new List<Pawn>();
            List<Pawn> ultraMechs = new List<Pawn>();

            foreach (var p in originalGuards)
            {
                float cp = p.kindDef.combatPower;
                if (cp >= 400) ultraMechs.Add(p);
                else if (cp >= 150) mediumMechs.Add(p);
                else lightMechs.Add(p);
            }

            int ultraToConvert = Mathf.RoundToInt(ultraMechs.Count * goodwillFactor);
            int largeCrates = ultraToConvert;


            int mediumToConvert = Mathf.RoundToInt(mediumMechs.Count * goodwillFactor);
            int mediumCrates = Mathf.RoundToInt(mediumToConvert * Rand.Range(0.5f, 1.0f));


            int lightToConvert = Mathf.RoundToInt(lightMechs.Count * goodwillFactor * 0.333f);
            int smallCrates = lightToConvert;

            // 构建最终实战护卫名单
            List<Pawn> finalGuards = new List<Pawn>();
            finalGuards.AddRange(lightMechs.Skip(lightToConvert));
            finalGuards.AddRange(mediumMechs.Skip(mediumToConvert));
            finalGuards.AddRange(ultraMechs.Skip(ultraToConvert));

            // 执行最终名单保底校验
            if (finalGuards.Count == 0 && originalGuards.Count > 0 && goodwill < 90)
            {
                finalGuards.Add(originalGuards.RandomElement());
            }

            // 执行突袭机组全球部署
            SpawnMiningRig(map, miningLocation, finalGuards, usacFaction);
            SpawnSupplyCrates(map, miningLocation, smallCrates, mediumCrates, largeCrates, usacFaction);

            SendStandardLetter(parms, new TargetInfo(miningLocation, map));
            USAC_Debug.Log($"[USAC] Mining raid triggered. Goodwill: {goodwill}, Factor: {goodwillFactor:F2}, Converted: {ultraToConvert}U/{mediumToConvert}M/{lightToConvert}L -> Crates: L{largeCrates}/M{mediumCrates}/S{smallCrates}");

            return true;
        }

        private void SpawnSupplyCrates(Map map, IntVec3 center, int small, int medium, int large, Faction faction)
        {
            int totalSent = 0;
            // 确定物资箱发送优先级
            SendCrates(map, center, "USAC_Crate_Large", large, faction, ref totalSent);
            SendCrates(map, center, "USAC_Crate_Medium", medium, faction, ref totalSent);
            SendCrates(map, center, "USAC_Crate_Small", small, faction, ref totalSent);
        }

        private void SendCrates(Map map, IntVec3 center, string defName, int count, Faction faction, ref int totalSent)
        {
            if (count <= 0) return;
            ThingDef crateDef = DefDatabase<ThingDef>.GetNamed(defName, false);
            if (crateDef == null) return;

            for (int i = 0; i < count; i++)
            {
                if (totalSent >= 20) break; // 硬性总额限制

                if (CellFinder.TryFindRandomCellNear(center, map, 6, c => c.Walkable(map) && !c.Roofed(map) && c.GetEdifice(map) == null, out IntVec3 cratePos))
                {
                    Thing crate = ThingMaker.MakeThing(crateDef);
                    crate.SetFaction(faction);

                    ThingDef skyfallerDef = DefDatabase<ThingDef>.GetNamed("USAC_CrateIncoming", false);
                    if (skyfallerDef != null)
                    {
                        Skyfaller skyfaller = (Skyfaller)ThingMaker.MakeThing(skyfallerDef);
                        skyfaller.innerContainer.TryAdd(crate);
                        GenSpawn.Spawn(skyfaller, cratePos, map);
                    }
                    else
                    {
                        ActiveTransporterInfo info = new ActiveTransporterInfo();
                        info.innerContainer.TryAdd(crate);
                        info.leaveSlag = true;
                        DropPodUtility.MakeDropPodAt(cratePos, map, info);
                    }
                    totalSent++;
                }
            }
        }

        #endregion

        #region 逻辑支持

        private bool HasDeepResources(Map map)
        {
            DeepResourceGrid grid = map.deepResourceGrid;
            if (grid == null) return false;

            // 快速检索可用矿床的存在
            foreach (IntVec3 cell in map.AllCells)
            {
                if (grid.ThingDefAt(cell) != null && grid.CountAt(cell) > 0)
                {
                    if (cell.GetEdifice(map) == null) return true;
                }
            }
            return false;
        }

        private bool TryFindMiningLocation(Map map, out IntVec3 location)
        {
            DeepResourceGrid grid = map.deepResourceGrid;

            // 收集所有潜在矿床坐标点
            List<IntVec3> potentialCells = new List<IntVec3>();
            foreach (IntVec3 cell in map.AllCells)
            {
                if (grid.ThingDefAt(cell) != null && grid.CountAt(cell) > 0)
                {
                    // 执行矿床落点初步预过滤
                    if (cell.Walkable(map) && !cell.Roofed(map) && cell.GetEdifice(map) == null)
                    {
                        potentialCells.Add(cell);
                    }
                }
            }

            if (potentialCells.Count == 0)
            {
                location = IntVec3.Invalid;
                return false;
            }

            // 执行多轮落点有效性验证
            // 预期此次验证具有高成功率
            for (int i = 0; i < 50; i++)
            {
                IntVec3 target = potentialCells.RandomElement();
                if (CanPlaceMiningRig(map, target))
                {
                    location = target;
                    return true;
                }
            }

            location = IntVec3.Invalid;
            return false;
        }

        private bool CanPlaceMiningRig(Map map, IntVec3 center)
        {
            // 校验目标核心区平坦度
            CellRect rect = CellRect.CenteredOn(center, 2);
            foreach (IntVec3 cell in rect)
            {
                // 执行核心区物理冲突校验
                // 忽略植被与杂物放置干扰
                if (!cell.InBounds(map) || !cell.Walkable(map) || cell.Roofed(map)) return false;
                if (cell.GetEdifice(map) != null) return false;
            }
            return true;
        }

        private void SpawnMiningRig(Map map, IntVec3 location, List<Pawn> guards, Faction faction)
        {
            ThingDef skyfallerDef = DefDatabase<ThingDef>.GetNamed("USAC_MiningRigIncoming", false);
            if (skyfallerDef == null) return;

            Skyfaller_MiningRigIncoming skyfaller = (Skyfaller_MiningRigIncoming)ThingMaker.MakeThing(skyfallerDef);
            skyfaller.SetGuards(guards, faction);
            GenSpawn.Spawn(skyfaller, location, map);
        }

        private List<Pawn> GenerateGuards(Faction faction, float points)
        {
            List<Pawn> guards = new List<Pawn>();
            float pointsLeft = points;

            var availableOptions = MechOptions.Where(opt => DefDatabase<PawnKindDef>.GetNamedSilentFail(opt.KindDefName) != null).ToList();
            if (availableOptions.Count == 0) return guards;

            while (pointsLeft > 0)
            {
                var validOptions = availableOptions.Where(opt => opt.CombatPower <= pointsLeft).ToList();
                if (validOptions.Count == 0) break;

                MechGenOption chosen = validOptions.RandomElementByWeight(opt => opt.SelectionWeight);
                PawnKindDef kindDef = DefDatabase<PawnKindDef>.GetNamed(chosen.KindDefName);
                Pawn mech = PawnGenerator.GeneratePawn(kindDef, faction);
                if (mech != null)
                {
                    guards.Add(mech);
                    pointsLeft -= chosen.CombatPower;
                }
                else break;
            }
            return guards;
        }

        #endregion
    }
}
