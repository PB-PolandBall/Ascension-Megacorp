using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace USAC
{
    // 定义护卫前往矿机登机任务
    public class LordToil_BoardMiningRig : LordToil
    {
        private Building_HeavyMiningRig targetRig;

        // 记录已进入容器的护卫名单
        private HashSet<Pawn> boardedPawns = new HashSet<Pawn>();

        public override bool AllowSatisfyLongNeeds => false;

        public LordToil_BoardMiningRig()
        {
        }

        public LordToil_BoardMiningRig(Building_HeavyMiningRig rig)
        {
            targetRig = rig;
        }

        public void SetTargetRig(Building_HeavyMiningRig rig)
        {
            targetRig = rig;
        }

        public override void UpdateAllDuties()
        {
            if (targetRig == null || !targetRig.Spawned)
            {
                // 资源失效并启动护卫撤离指令
                for (int i = 0; i < lord.ownedPawns.Count; i++)
                {
                    Pawn pawn = lord.ownedPawns[i];
                    pawn.mindState.duty = new PawnDuty(DutyDefOf.ExitMapBest);
                    pawn.mindState.duty.locomotion = LocomotionUrgency.Jog;
                }
                return;
            }

            // 配置空闲职责配合逻辑控制
            for (int i = 0; i < lord.ownedPawns.Count; i++)
            {
                Pawn pawn = lord.ownedPawns[i];

                // 判定已登机对象不需要更新
                if (boardedPawns.Contains(pawn)) continue;

                pawn.mindState.duty = new PawnDuty(DutyDefOf.Idle);
            }
        }

        public override void LordToilTick()
        {
            if (Find.TickManager.TicksGame % 30 != 0) return;

            if (targetRig == null || !targetRig.Spawned)
            {
                USAC_Debug.Log("[USAC] BoardMiningRig: Rig is null or not spawned, sending RigDestroyed");
                lord.ReceiveMemo("RigDestroyed");
                return;
            }

            // 历遍并校验每个护卫状态
            for (int i = lord.ownedPawns.Count - 1; i >= 0; i--)
            {
                Pawn pawn = lord.ownedPawns[i];

                // 跳过已完成登机任务的对象
                if (boardedPawns.Contains(pawn)) continue;

                if (!pawn.Spawned || pawn.Dead || pawn.Downed)
                {
                    continue;
                }

                float dist = pawn.Position.DistanceTo(targetRig.Position);

                // 校验护卫是否位于矿机附近
                if (dist <= 5f)
                {
                    USAC_Debug.Log($"[USAC] BoardMiningRig: {pawn.LabelShort} trying to board (dist={dist})");
                    // 执行护卫进入矿机容器尝试
                    if (targetRig.TryAcceptPawn(pawn))
                    {
                        boardedPawns.Add(pawn);
                        USAC_Debug.Log($"[USAC] BoardMiningRig: {pawn.LabelShort} boarded successfully");
                    }
                }
                else
                {
                    // 为未达标护卫分配前往任务
                    if (pawn.CurJob == null || pawn.CurJob.def != JobDefOf.Goto ||
                        !pawn.CurJob.targetA.Cell.InHorDistOf(targetRig.Position, 3f))
                    {
                        // 检索矿机附近可用目标坐标
                        IntVec3 destCell = FindStandableCellNear(targetRig, pawn);
                        if (destCell.IsValid)
                        {
                            Job job = JobMaker.MakeJob(JobDefOf.Goto, destCell);
                            job.locomotionUrgency = LocomotionUrgency.Jog;
                            pawn.jobs.StartJob(job, JobCondition.InterruptForced);
                            USAC_Debug.Log($"[USAC] BoardMiningRig: {pawn.LabelShort} assigned Goto job to {destCell}");
                        }
                    }
                }
            }

            // 校验全员生还护卫登机状态
            bool allBoarded = true;
            int aliveCount = 0;
            int boardedCount = boardedPawns.Count;

            foreach (Pawn pawn in lord.ownedPawns)
            {
                // 判定伤亡对象脱离全员统计
                if (pawn.Dead || pawn.Downed) continue;

                aliveCount++;

                // 历遍并处理尚未登机护卫
                if (!boardedPawns.Contains(pawn))
                {
                    allBoarded = false;
                }
            }

            if (allBoarded && aliveCount > 0)
            {
                USAC_Debug.Log("[USAC] BoardMiningRig: All boarded, sending AllBoarded memo");
                lord.ReceiveMemo("AllBoarded");
            }
        }

        // 检索目标附近的可通行格点
        private IntVec3 FindStandableCellNear(Thing target, Pawn pawn)
        {
            // 检索目标邻近的首选停靠格
            foreach (IntVec3 cell in GenAdj.CellsAdjacent8Way(target))
            {
                if (cell.InBounds(lord.Map) && cell.Standable(lord.Map) && pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly))
                {
                    return cell;
                }
            }

            // 检索目标附近的可通达备选格
            for (int radius = 2; radius <= 5; radius++)
            {
                foreach (IntVec3 cell in GenRadial.RadialCellsAround(target.Position, radius, true))
                {
                    if (cell.InBounds(lord.Map) && cell.Standable(lord.Map) && pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly))
                    {
                        return cell;
                    }
                }
            }

            return IntVec3.Invalid;
        }
    }
}
