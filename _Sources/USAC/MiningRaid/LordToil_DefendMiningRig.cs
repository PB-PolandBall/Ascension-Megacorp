using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace USAC
{
    // 定义机兵采矿点防守任务
    // 记录攻击者为临时敌对派系
    public class LordToil_DefendMiningRig : LordToil
    {
        #region 字段

        private IntVec3 defendPoint;
        private float defendRadius;
        private float wanderRadius;

        #endregion

        #region 属性

        public override IntVec3 FlagLoc => defendPoint;
        public override bool AllowSatisfyLongNeeds => false;

        #endregion

        #region 构造函数

        public LordToil_DefendMiningRig(IntVec3 point, float defendRadius = 10f, float wanderRadius = 5f)
        {
            this.defendPoint = point;
            this.defendRadius = defendRadius;
            this.wanderRadius = wanderRadius;
        }

        #endregion

        #region 公共方法

        public override void UpdateAllDuties()
        {
            for (int i = 0; i < lord.ownedPawns.Count; i++)
            {
                Pawn pawn = lord.ownedPawns[i];
                if (pawn?.mindState == null) continue;

                pawn.mindState.duty = new PawnDuty(DutyDefOf.Defend, defendPoint);
                pawn.mindState.duty.focusSecond = defendPoint;
                pawn.mindState.duty.radius = defendRadius;
                pawn.mindState.duty.wanderRadius = wanderRadius;
            }
        }

        public override void Notify_PawnDamaged(Pawn victim, DamageInfo dinfo)
        {
            base.Notify_PawnDamaged(victim, dinfo);

            // 检索造成伤害的攻击者实例
            Thing instigator = dinfo.Instigator;
            if (instigator == null) return;

            USAC_Debug.Log($"[USAC] Notify_PawnDamaged: victim={victim?.LabelShort}, instigator={instigator}, faction={instigator.Faction}");

            Faction attackerFaction = instigator.Faction;

            // 避免对同级派系产生敌对
            if (attackerFaction != null && attackerFaction == lord.faction) return;

            // 记录攻击者派系至敌对库
            if (attackerFaction != null && lord.LordJob is LordJob_MiningGuard miningGuard)
            {
                miningGuard.AddHostileFaction(attackerFaction);
                USAC_Debug.Log($"[USAC] Added hostile faction: {attackerFaction.Name}");

                // 发送清理区域威胁执行信号
                lord.ReceiveMemo("StartKillThreats");
            }

            // 刷新全体护卫战斗目标缓存
            Map map = lord.Map;
            if (map != null)
            {
                foreach (Pawn guard in lord.ownedPawns)
                {
                    if (guard.Spawned)
                    {
                        map.attackTargetsCache.UpdateTarget(guard);
                    }
                }

                // 刷新攻击者战斗目标位置缓存
                if (instigator is Pawn attackerPawn && attackerPawn.Spawned)
                {
                    map.attackTargetsCache.UpdateTarget(attackerPawn);
                }
            }

            // 令受创护卫立即锁定并反击
            if (victim != null && !victim.Dead && !victim.Downed)
            {
                victim.mindState.enemyTarget = instigator;
                victim.mindState.lastEngageTargetTick = Find.TickManager.TicksGame;

                // 强制结束当前作业并战斗
                victim.jobs?.EndCurrentJob(JobCondition.InterruptForced);

                USAC_Debug.Log($"[USAC] Set enemy target for {victim.LabelShort}: {instigator}");
            }

            // 唤醒全员护卫进入战斗状态
            foreach (Pawn guard in lord.ownedPawns)
            {
                if (guard.Dead || guard.Downed) continue;

                // 统一设定全员当前仇恨目标
                guard.mindState.enemyTarget = instigator;
                guard.mindState.lastEngageTargetTick = Find.TickManager.TicksGame;

                // 强制全员结束非战斗类作业
                guard.jobs?.EndCurrentJob(JobCondition.InterruptForced);

                USAC_Debug.Log($"[USAC] Guard {guard.LabelShort} now targeting {instigator}");
            }
        }

        #endregion
    }
}
