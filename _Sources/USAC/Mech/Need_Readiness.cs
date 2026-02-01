using System.Text;
using RimWorld;
using UnityEngine;
using Verse;

namespace USAC
{
    // 定义机兵整备需求逻辑类
    public class Need_Readiness : Need
    {
        public Need_Readiness(Pawn pawn) : base(pawn)
        {
            threshPercents = new System.Collections.Generic.List<float> { 0.01f };
        }

        private CompMechReadiness Comp => pawn.TryGetComp<CompMechReadiness>();

        public override float MaxLevel => Comp?.Props.capacity ?? 100f;

        public override int GUIChangeArrow => -1;

        // 判定整备需求列表可见性
        public override bool ShowOnNeedList => Comp != null;

        public override void NeedInterval()
        {
            // 记录需求值同步来源说明
        }

        public override string GetTipString()
        {
            StringBuilder sb = new StringBuilder(base.GetTipString());
            var comp = Comp;
            if (comp != null)
            {
                float percent = comp.Props.consumptionPerDay / comp.Props.capacity * 100f;
                sb.AppendInNewLine("USAC_ReadinessConsumption".Translate(percent.ToString("F1")));
            }
            return sb.ToString();
        }
    }
}
