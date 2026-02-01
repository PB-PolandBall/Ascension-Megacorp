using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace USAC
{
    // 定义装载尸体袋作业逻辑
    public class JobDriver_PackCorpse : JobDriver
    {
        private Corpse Corpse => (Corpse)job.GetTarget(TargetIndex.A).Thing;
        private Building_CorpseBag Bag => (Building_CorpseBag)job.GetTarget(TargetIndex.B).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Corpse, job, errorOnFailed: errorOnFailed)
                && pawn.Reserve(Bag, job, errorOnFailed: errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDestroyedOrNull(TargetIndex.A);
            this.FailOnDestroyedOrNull(TargetIndex.B);
            this.FailOn(() => Bag.HasCorpse);

            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            yield return Toils_Haul.StartCarryThing(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);

            Toil packToil = ToilMaker.MakeToil("PackCorpse");
            packToil.initAction = delegate
            {
                Thing carriedThing = pawn.carryTracker.CarriedThing;
                if (carriedThing is Corpse && Bag != null && !Bag.HasCorpse)
                {
                    pawn.carryTracker.innerContainer.TryTransferToContainer(carriedThing, Bag.GetDirectlyHeldThings());
                    Bag.NotifyContentsChanged();
                }
            };
            packToil.defaultCompleteMode = ToilCompleteMode.Delay;
            packToil.defaultDuration = 60;
            packToil.WithProgressBarToilDelay(TargetIndex.B);
            yield return packToil;
        }
    }
}
