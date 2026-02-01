using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace USAC
{
    // 定义采矿袭击派系关系补丁
    // 护卫受击不改派系关系逻辑
    // 护卫对攻击者切换对抗模式
    public static class Patch_MiningRaidFaction
    {
        // 判定对象是否为采矿护卫机
        public static bool IsMiningGuard(Pawn pawn)
        {
            if (pawn == null) return false;
            Lord lord = pawn.GetLord();
            return lord?.LordJob is LordJob_MiningGuard;
        }

        // 检索对象关联护卫逻辑任务
        public static LordJob_MiningGuard GetMiningGuardJob(Pawn pawn)
        {
            if (pawn == null) return null;
            Lord lord = pawn.GetLord();
            return lord?.LordJob as LordJob_MiningGuard;
        }

        // 拦截受创诱发的派系关系变动
        // 仅锁定第三方触发的关系变动
        [HarmonyPatch(typeof(Faction), nameof(Faction.Notify_MemberTookDamage))]
        public static class Patch_Notify_MemberTookDamage
        {
            [HarmonyPrefix]
            public static bool Prefix(Pawn member, DamageInfo dinfo)
            {
                // 非采矿护卫则执行常规逻辑
                if (!IsMiningGuard(member)) return true;

                // 检索造成伤害的攻击者派系
                Faction attackerFaction = dinfo.Instigator?.Faction;

                // 玩家攻击则执行常规关系逻辑
                if (attackerFaction == Faction.OfPlayer)
                {
                    USAC_Debug.Log($"[USAC] Notify_MemberTookDamage: {member?.LabelShort} attacked by player, allowing relation change");
                    return true;
                }

                // 第三方攻击则阻止关系变动
                USAC_Debug.Log($"[USAC] Notify_MemberTookDamage: {member?.LabelShort} attacked by {attackerFaction?.Name}, blocking relation change");
                return false;
            }
        }

        // 拦截致死诱发的派系关系变动
        // 仅锁定第三方致死的关系变动
        [HarmonyPatch(typeof(Faction), nameof(Faction.Notify_MemberDied))]
        public static class Patch_Notify_MemberDied
        {
            [HarmonyPrefix]
            public static bool Prefix(Pawn member, DamageInfo? dinfo, bool wasWorldPawn, bool wasGuilty, Map map)
            {
                // 非采矿护卫则执行常规逻辑
                if (!IsMiningGuard(member)) return true;

                // 检索造成伤害的攻击者派系
                Faction attackerFaction = dinfo?.Instigator?.Faction;

                // 玩家致死则执行常规关系逻辑
                if (attackerFaction == Faction.OfPlayer)
                {
                    USAC_Debug.Log($"[USAC] Notify_MemberDied: {member?.LabelShort} killed by player, allowing relation change");
                    return true;
                }

                // 第三方致死则阻止关系变动
                USAC_Debug.Log($"[USAC] Notify_MemberDied: {member?.LabelShort} killed by {attackerFaction?.Name}, blocking relation change");
                return false;
            }
        }
    }
}
