using RimWorld;
using Verse;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace USAC
{
    // 限制目标选择为机械体
    public class CompTargetable_Mechanoid : CompTargetable
    {
        protected override bool PlayerChoosesTarget => true;

        protected override TargetingParameters GetTargetingParameters()
        {
            return new TargetingParameters
            {
                canTargetPawns = true,
                canTargetBuildings = false,
                canTargetMechs = true,
                validator = (TargetInfo target) =>
                {
                    if (target.Thing is not Pawn pawn) return false;
                    return pawn.RaceProps.IsMechanoid && pawn.Faction == Faction.OfPlayer;
                }
            };
        }

        public override IEnumerable<Thing> GetTargets(Thing targetChosenByPlayer = null)
        {
            yield return targetChosenByPlayer;
        }

        public override bool ValidateTarget(LocalTargetInfo target, bool showMessages = true)
        {
            if (target.Thing is not Pawn pawn || !pawn.RaceProps.IsMechanoid)
            {
                if (showMessages) Messages.Message("MessageTargetMustBeMechanoid".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            if (pawn.Faction != Faction.OfPlayer)
            {
                if (showMessages) Messages.Message("MessageTargetMustBePlayerMech".Translate(), MessageTypeDefOf.RejectInput, false);
                return false;
            }
            return true;
        }
    }

    // 执行机械体能量充满逻辑
    public class CompTargetEffect_DisposableBattery : CompTargetEffect
    {
        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (target is not Pawn targetPawn) return;

            // 刷新生物技术能量数值
            if (targetPawn.needs?.energy != null)
            {
                targetPawn.needs.energy.CurLevel = targetPawn.needs.energy.MaxLevel;
            }

            // 生成伴随视觉特效
            FleckMaker.Static(targetPawn.Position, targetPawn.Map, FleckDefOf.MicroSparks);
            Messages.Message("USAC_MessageMechCharged".Translate(targetPawn.LabelShort, targetPawn), targetPawn, MessageTypeDefOf.PositiveEvent);
        }
    }

    // 执行机械体自动修复逻辑
    public class CompTargetEffect_DisposableRepairTool : CompTargetEffect
    {
        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (target is not Pawn targetPawn) return;

            // 搜寻并移除所有伤口描述
            List<Hediff> injuries = targetPawn.health.hediffSet.hediffs
                .Where(h => h is Hediff_Injury || h is Hediff_MissingPart).ToList();

            foreach (var h in injuries)
            {
                targetPawn.health.RemoveHediff(h);
            }

            // 生成伴随修复特效
            EffecterDefOf.Deflect_Metal.Spawn(targetPawn.Position, targetPawn.Map).Cleanup();
            Messages.Message("USAC_MessageMechRepaired".Translate(targetPawn.LabelShort, targetPawn), targetPawn, MessageTypeDefOf.PositiveEvent);
        }
    }
}
