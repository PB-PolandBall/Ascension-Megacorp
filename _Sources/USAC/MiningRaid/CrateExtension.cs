using System.Collections.Generic;
using RimWorld;
using Verse;

namespace USAC
{
    // 物资项定义
    public class CrateLootItem
    {
        public string thingDef; // 物项 DefName
        public int minCount = 1;
        public int maxCount = 1;
        public float weight = 1.0f; // 权重
    }

    // 物资组定义
    public class CrateLootGroup
    {
        public float chance = 1.0f; // 该组出现的概率
        public List<CrateLootItem> items = new List<CrateLootItem>();
    }

    // Crate 扩展数据
    public class CrateExtension : DefModExtension
    {
        public ThingDef emptyDef; // 开启后替换的 Def
        public List<CrateLootGroup> lootGroups = new List<CrateLootGroup>(); // 物资表
    }
}
