using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace USAC
{
    // 定义携带并使用物品作业逻辑
    public class JobDriver_UseItemOnTarget : JobDriver
    {
        private Thing carriedItem;
        private int useDuration;

        protected Thing Item => job.GetTarget(TargetIndex.A).Thing;
        protected Thing Target => job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Item, job, 1, -1, null, errorOnFailed)
                && pawn.Reserve(Target, job, 1, -1, null, errorOnFailed);
        }

        public override void Notify_Starting()
        {
            base.Notify_Starting();
            useDuration = Item?.TryGetComp<CompUsable>()?.Props.useDuration ?? 100;
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnIncapable(PawnCapacityDefOf.Manipulation);
            this.FailOnDestroyedNullOrForbidden(TargetIndex.B);

            // 引导小人前往物品坐标
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch).FailOnDespawnedNullOrForbidden(TargetIndex.A);

            // 拾取物品并缓存引用数据
            Toil pickUp = ToilMaker.MakeToil("PickUpItem");
            pickUp.initAction = delegate
            {
                carriedItem = Item;
                pawn.carryTracker.TryStartCarry(carriedItem, 1);
            };
            pickUp.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return pickUp;

            // 引导小人前往目标物体坐标
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch).FailOnDespawnedNullOrForbidden(TargetIndex.B);

            // 执行使用物品等待动作
            Toil useToil = Toils_General.Wait(useDuration, TargetIndex.B);
            useToil.WithProgressBarToilDelay(TargetIndex.B);
            useToil.handlingFacing = true;
            useToil.tickAction = delegate
            {
                pawn.rotationTracker.FaceTarget(Target);
            };
            yield return useToil;

            // 执行物品效果应用逻辑
            Toil applyEffect = ToilMaker.MakeToil("ApplyEffect");
            applyEffect.initAction = delegate
            {
                if (carriedItem == null) return;
                carriedItem.TryGetComp<CompUsable>()?.UsedBy(pawn);
            };
            applyEffect.defaultCompleteMode = ToilCompleteMode.Instant;
            yield return applyEffect;
        }
    }
}
