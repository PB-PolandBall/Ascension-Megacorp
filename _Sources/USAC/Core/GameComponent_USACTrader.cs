using RimWorld;
using Verse;

namespace USAC
{
    // 定义派系商船到访管理逻辑类
    // 执行年度固定频次商船到访
    public class GameComponent_USACTrader : GameComponent
    {
        #region 常量

        // 记录年度包含的总天数常数
        private const int DaysPerYear = 60;

        // 记录年度预设商船到访总次数
        private const int VisitsPerYear = 2;

        // 记录单次到访间的平均天数
        private const int DaysBetweenVisits = DaysPerYear / VisitsPerYear;

        // 记录开局首次到访最短天数
        private const int MinDaysForFirstVisit = 15;

        // 记录到访日期的随机偏移量
        private const int RandomOffsetDays = 5;

        #endregion

        #region 字段

        // 记录下次商船到访的理论时间
        private int nextVisitTick = -1;

        #endregion

        #region 构造函数

        public GameComponent_USACTrader(Game game)
        {
        }

        #endregion

        #region 生命周期

        public override void StartedNewGame()
        {
            base.StartedNewGame();
            ScheduleNextVisit(true);
        }

        public override void LoadedGame()
        {
            base.LoadedGame();

            // 判定缺失计划时执行计划补录
            if (nextVisitTick < 0)
            {
                ScheduleNextVisit(false);
            }
        }

        public override void GameComponentTick()
        {
            // 执行商船到访计划周期性校验
            if (Find.TickManager.TicksGame % 250 != 0)
                return;

            if (nextVisitTick > 0 && Find.TickManager.TicksGame >= nextVisitTick)
            {
                TryTriggerVisit();
                ScheduleNextVisit(false);
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref nextVisitTick, "nextVisitTick", -1);
        }

        #endregion

        #region 私有方法

        private void ScheduleNextVisit(bool isFirstVisit)
        {
            int baseDays = isFirstVisit ? MinDaysForFirstVisit : DaysBetweenVisits;
            int randomOffset = Rand.RangeInclusive(-RandomOffsetDays, RandomOffsetDays);
            int daysUntilVisit = baseDays + randomOffset;

            nextVisitTick = Find.TickManager.TicksGame + (daysUntilVisit * GenDate.TicksPerDay);
        }

        private void TryTriggerVisit()
        {
            // 校验该派系存续与其敌对状态
            Faction usacFaction = Find.FactionManager.FirstFactionOfDef(USAC_FactionDefOf.USAC_Faction);
            if (usacFaction == null || usacFaction.HostileTo(Faction.OfPlayer))
                return;

            // 检索玩家主基地所在地图实例
            Map map = Find.AnyPlayerHomeMap;
            if (map == null)
                return;

            // 校验当前地图动态商船总数量
            if (map.passingShipManager.passingShips.Count >= 5)
                return;

            // 触发指定派系商船到访事件
            IncidentDef incidentDef = DefDatabase<IncidentDef>.GetNamedSilentFail("USAC_MechSupplierArrival");
            if (incidentDef == null)
                return;

            IncidentParms parms = StorytellerUtility.DefaultParmsNow(incidentDef.category, map);
            incidentDef.Worker.TryExecute(parms);
        }

        #endregion
    }
}
