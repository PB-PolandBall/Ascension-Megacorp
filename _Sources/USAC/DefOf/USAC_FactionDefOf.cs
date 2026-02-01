using RimWorld;
using Verse;

namespace USAC
{
    [DefOf]
    public static class USAC_FactionDefOf
    {
        public static FactionDef USAC_Faction;

        static USAC_FactionDefOf()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(USAC_FactionDefOf));
        }
    }
}
