using System.Linq;
using RimWorld;
using Verse;

namespace USAC
{
    // USAC 机兵商船到访事件
    public class IncidentWorker_USACTraderArrival : IncidentWorker
    {
        private const int MaxShips = 5;

        protected override bool CanFireNowSub(IncidentParms parms)
        {
            if (!base.CanFireNowSub(parms))
                return false;

            Map map = (Map)parms.target;
            if (map.passingShipManager.passingShips.Count >= MaxShips)
                return false;

            // 检查派系存续与关系
            Faction usacFaction = Find.FactionManager.FirstFactionOfDef(USAC_FactionDefOf.USAC_Faction);
            if (usacFaction == null || usacFaction.HostileTo(Faction.OfPlayer))
                return false;

            return true;
        }

        protected override bool TryExecuteWorker(IncidentParms parms)
        {
            Map map = (Map)parms.target;

            // 强制使用 USAC 机兵商人
            TraderKindDef traderKind = DefDatabase<TraderKindDef>.GetNamedSilentFail("USAC_Trader_MechSupplier");
            if (traderKind == null)
            {
                Log.Error("[USAC] TraderKindDef USAC_Trader_MechSupplier not found");
                return false;
            }

            // 获取 USAC 派系
            Faction usacFaction = Find.FactionManager.FirstFactionOfDef(USAC_FactionDefOf.USAC_Faction);

            TradeShip tradeShip = new TradeShip(traderKind, usacFaction);
            tradeShip.WasAnnounced = false;

            // 检查是否有通讯台
            if (map.listerBuildings.allBuildingsColonist.Any((Building b) =>
                b.def.IsCommsConsole && (b.GetComp<CompPowerTrader>() == null || b.GetComp<CompPowerTrader>().PowerOn)))
            {
                string factionPart = usacFaction != null
                    ? "TraderArrivalFromFaction".Translate(usacFaction.Named("FACTION"))
                    : "TraderArrivalNoFaction".Translate();

                SendStandardLetter(
                    tradeShip.def.LabelCap,
                    "TraderArrival".Translate(tradeShip.name, tradeShip.def.label, factionPart),
                    LetterDefOf.PositiveEvent,
                    parms,
                    LookTargets.Invalid);

                tradeShip.WasAnnounced = true;
            }

            map.passingShipManager.AddShip(tradeShip);
            tradeShip.GenerateThings();
            return true;
        }
    }
}
