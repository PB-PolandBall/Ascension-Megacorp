using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace USAC
{
    // 定义采矿护卫群体逻辑任务
    // 守卫矿机并执行登机撤离程序
    // 切换对抗模式但不改变全局关系
    public class LordJob_MiningGuard : LordJob
    {
        #region 字段

        private IntVec3 defendPoint;
        private Building_HeavyMiningRig targetRig;

        // 记录对抗模式临时敌对派系
        private HashSet<Faction> hostileFactions = new HashSet<Faction>();

        #endregion

        #region 属性

        public override bool AddFleeToil => false;

        // 检索当前临时敌对派系列表
        public IReadOnlyCollection<Faction> HostileFactions => hostileFactions;

        #endregion

        #region 构造函数

        public LordJob_MiningGuard()
        {
        }

        public LordJob_MiningGuard(IntVec3 point, Building_HeavyMiningRig rig = null)
        {
            defendPoint = point;
            targetRig = rig;
        }

        #endregion

        #region 状态图

        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();

            // 状态定义为防守矿机任务
            LordToil_DefendMiningRig toilDefend = new LordToil_DefendMiningRig(defendPoint, defendRadius: 10f, wanderRadius: 5f);
            stateGraph.AddToil(toilDefend);

            // 状态定义为清理区域威胁
            LordToil_KillThreats toilKill = new LordToil_KillThreats(defendPoint, maxChaseRadius: 25f);
            stateGraph.AddToil(toilKill);

            // 状态定义为执行登机撤离
            LordToil_BoardMiningRig toilBoard = new LordToil_BoardMiningRig(targetRig);
            stateGraph.AddToil(toilBoard);

            // 状态定义为矿机被毁强制撤离
            LordToil_ExitMap toilExit = new LordToil_ExitMap(LocomotionUrgency.Jog);
            stateGraph.AddToil(toilExit);

            // 定义被攻击转化清理威胁逻辑
            Transition transToKill = new Transition(toilDefend, toilKill);
            transToKill.AddTrigger(new Trigger_Memo("StartKillThreats"));
            stateGraph.AddTransition(transToKill);

            // 定义威胁清除转化防守逻辑
            Transition transBackToDefend = new Transition(toilKill, toilDefend);
            transBackToDefend.AddTrigger(new Trigger_Memo("ThreatsCleared"));
            stateGraph.AddTransition(transBackToDefend);

            // 定义防守态转化登机逻辑
            Transition transToBoard = new Transition(toilDefend, toilBoard);
            transToBoard.AddTrigger(new Trigger_Memo("StartBoarding"));
            stateGraph.AddTransition(transToBoard);

            // 定义追猎态转化登机逻辑
            Transition transKillToBoard = new Transition(toilKill, toilBoard);
            transKillToBoard.AddTrigger(new Trigger_Memo("StartBoarding"));
            stateGraph.AddTransition(transKillToBoard);

            // 定义被毁态转化撤离逻辑
            Transition transToExit = new Transition(toilBoard, toilExit);
            transToExit.AddTrigger(new Trigger_Memo("RigDestroyed"));
            stateGraph.AddTransition(transToExit);

            // 定义全员登机完成转化结束
            Transition transAllBoarded = new Transition(toilBoard, toilExit);
            transAllBoarded.AddTrigger(new Trigger_Memo("AllBoarded"));
            stateGraph.AddTransition(transAllBoarded);

            return stateGraph;
        }

        #endregion

        #region 存档序列化

        public override void ExposeData()
        {
            Scribe_Values.Look(ref defendPoint, "defendPoint");
            Scribe_References.Look(ref targetRig, "targetRig");
            Scribe_Collections.Look(ref hostileFactions, "hostileFactions", LookMode.Reference);

            if (hostileFactions == null)
                hostileFactions = new HashSet<Faction>();
        }

        #endregion

        #region 公共方法

        // 配置关联目标矿机建筑实例
        public void SetTargetRig(Building_HeavyMiningRig rig)
        {
            targetRig = rig;
        }

        // 发送全员开始登机执行信号
        public void NotifyStartBoarding()
        {
            // 同步更新全体登机任务目标
            foreach (LordToil toil in lord.Graph.lordToils)
            {
                if (toil is LordToil_BoardMiningRig boardToil)
                {
                    boardToil.SetTargetRig(targetRig);
                }
            }

            lord.ReceiveMemo("StartBoarding");
        }

        // 记录并添加临时敌对派系
        public void AddHostileFaction(Faction faction)
        {
            if (faction == null || faction == lord.faction) return;

            if (hostileFactions.Add(faction))
            {
                USAC_Debug.Log($"[USAC] AddHostileFaction: {faction.Name}, total hostile factions: {hostileFactions.Count}");

                Map map = lord.Map;
                if (map != null)
                {
                    // 刷新双方战斗目标位置缓存
                    foreach (Pawn guard in lord.ownedPawns)
                    {
                        if (guard.Spawned && !guard.Dead)
                        {
                            map.attackTargetsCache.UpdateTarget(guard);
                            // 强制全员重评估当前战斗目标
                            if (!guard.Downed)
                                guard.jobs?.EndCurrentJob(JobCondition.InterruptForced);
                        }
                    }

                    foreach (Pawn enemy in map.mapPawns.SpawnedPawnsInFaction(faction))
                    {
                        if (!enemy.Dead)
                            map.attackTargetsCache.UpdateTarget(enemy);
                    }
                }
            }
        }

        // 校验派系是否存在临时敌对表
        public bool IsHostileFaction(Faction faction)
        {
            return faction != null && hostileFactions.Contains(faction);
        }

        #endregion
    }
}
