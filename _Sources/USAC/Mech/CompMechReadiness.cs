using RimWorld;
using Verse;

namespace USAC
{
    // 定义机兵整备组件属性
    public class CompProperties_MechReadiness : CompProperties
    {
        // 记录机兵整备容量数值
        public float capacity = 100f;

        // 记录机兵整备日损耗值
        public float consumptionPerDay = 10f;

        // 记录整备补给物品定义
        public ThingDef supplyDef;

        // 记录低整备状态阈值
        public float lowThreshold = 0.3f;

        // 记录低整备状态异常定义
        public HediffDef lowReadinessHediff;

        public CompProperties_MechReadiness()
        {
            compClass = typeof(CompMechReadiness);
        }
    }

    // 定义机兵整备逻辑组件
    public class CompMechReadiness : ThingComp
    {
        private float readiness;
        private Need_Readiness cachedNeed;

        public CompProperties_MechReadiness Props => (CompProperties_MechReadiness)props;

        public float Readiness => readiness;
        public float ReadinessPercent => readiness / Props.capacity;
        public bool IsLowReadiness => readiness <= 0f;

        private Pawn Pawn => parent as Pawn;

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            if (!respawningAfterLoad)
            {
                readiness = Props.capacity;
            }
            SyncNeed();
            UpdateHediff();
        }

        public override void CompTick()
        {
            base.CompTick();

            // 执行机兵整备周期损耗
            if (parent.IsHashIntervalTick(2500))
            {
                ConsumeReadiness(Props.consumptionPerDay / 24f);
                UpdateHediff();
                SyncNeed();
            }
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref readiness, "readiness", Props.capacity);
        }

        public void ConsumeReadiness(float amount)
        {
            readiness -= amount;
            if (readiness < 0) readiness = 0;
            SyncNeed();
        }

        public void Resupply(float amount)
        {
            readiness += amount;
            if (readiness > Props.capacity) readiness = Props.capacity;
            SyncNeed();
            UpdateHediff();
        }

        public void Resupply(Thing supplyThing)
        {
            if (supplyThing.def != Props.supplyDef) return;

            float needed = Props.capacity - readiness;
            int toConsume = (int)System.Math.Min(needed, supplyThing.stackCount);

            if (toConsume > 0)
            {
                supplyThing.SplitOff(toConsume).Destroy();
                Resupply(toConsume);
            }
        }

        private void SyncNeed()
        {
            if (Pawn?.needs == null) return;

            if (cachedNeed == null)
            {
                cachedNeed = Pawn.needs.TryGetNeed<Need_Readiness>();
            }

            if (cachedNeed != null)
            {
                cachedNeed.CurLevel = readiness;
            }
        }

        private void UpdateHediff()
        {
            if (Pawn == null || Props.lowReadinessHediff == null) return;

            Hediff existing = Pawn.health.hediffSet.GetFirstHediffOfDef(Props.lowReadinessHediff);

            if (IsLowReadiness && existing == null)
            {
                Pawn.health.AddHediff(Props.lowReadinessHediff);
            }
            else if (!IsLowReadiness && existing != null)
            {
                Pawn.health.RemoveHediff(existing);
            }
        }

        public override string CompInspectStringExtra()
        {
            return "USAC_Readiness".Translate() + ": " + readiness.ToString("F0") + " / " + Props.capacity.ToString("F0");
        }
    }
}
