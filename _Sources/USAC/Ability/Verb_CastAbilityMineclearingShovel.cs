using RimWorld;
using Verse;

namespace USAC
{
    // 定义排雷铲冲刺动作类
    public class Verb_CastAbilityMineclearingShovel : Verb_CastAbilityJump
    {
        public override ThingDef JumpFlyerDef => ThingDef.Named("USAC_PawnFlyer_Shovel");
    }
}
