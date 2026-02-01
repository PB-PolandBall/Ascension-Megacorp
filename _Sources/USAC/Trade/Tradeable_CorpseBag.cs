using RimWorld;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace USAC
{
    // 尸体袋货币交易类
    public class Tradeable_CorpseBag : Tradeable
    {
        #region 属性

        public override bool IsCurrency => true;
        public override bool TraderWillTrade => true;
        public override string Label => "USAC_CorpseBag_Currency".Translate();
        public override string TipDescription => "USAC_CorpseBag_CurrencyDesc".Translate();
        public override float BaseMarketValue => 1f;

        #endregion

        #region 公共方法

        public override int CostToInt(float cost)
        {
            return Mathf.CeilToInt(cost);
        }

        public override int CountHeldBy(Transactor trans)
        {
            List<Thing> things = (trans == Transactor.Colony) ? thingsColony : thingsTrader;
            float totalValue = 0f;

            for (int i = 0; i < things.Count; i++)
            {
                if (things[i] is Building_CorpseBag bag && bag.HasCorpse)
                {
                    totalValue += Building_CorpseBag.CalculateCorpseValue(bag.ContainedCorpse);
                }
            }

            return Mathf.RoundToInt(totalValue);
        }

        public override void ResolveTrade()
        {
            if (ActionToDo == TradeAction.PlayerSells)
            {
                float valueToTransfer = CountToTransferToDestination;
                TransferFilledBags(thingsColony, valueToTransfer);
            }
        }

        private void TransferFilledBags(List<Thing> sourceThings, float valueToTransfer)
        {
            float remainingValue = valueToTransfer;
            List<Building_CorpseBag> toTransfer = new();

            List<Building_CorpseBag> filledBags = new();
            foreach (var thing in sourceThings)
            {
                if (thing is Building_CorpseBag bag && bag.HasCorpse)
                    filledBags.Add(bag);
            }
            // 按价值排序优先使用低价袋
            filledBags.SortBy(b => Building_CorpseBag.CalculateCorpseValue(b.ContainedCorpse));

            foreach (var bag in filledBags)
            {
                if (remainingValue <= 0)
                    break;

                float bagValue = Building_CorpseBag.CalculateCorpseValue(bag.ContainedCorpse);
                toTransfer.Add(bag);
                remainingValue -= bagValue;
            }

            // 转移尸体袋给商人不找零
            foreach (var bag in toTransfer)
            {
                if (bag.Spawned)
                    bag.DeSpawn();
                TradeSession.trader.GiveSoldThingToTrader(bag, 1, TradeSession.playerNegotiator);
            }
        }

        public override int GetHashCode()
        {
            return "USACCorpseBagCurrency".GetHashCode();
        }

        #endregion
    }
}
