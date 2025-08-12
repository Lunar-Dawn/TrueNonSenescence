using HarmonyLib;
using RimWorld;
using Verse;

namespace TrueNonSenescence
{
    [StaticConstructorOnStartup]
    public static class Patches
    {
        static Patches()
        {
            var harmony = new Harmony("LunarDawn.TrueNonSenescence");
            harmony.PatchAll();
        }

        private static readonly GeneDef NonSenescent = DefDatabase<GeneDef>.GetNamed("DiseaseFree");
        public static bool PawnIsNonSenescent(Pawn pawn)
        {
            return pawn?.genes?.HasActiveGene(NonSenescent) ?? false;
        }
    }
    
    [HarmonyPatch(typeof(StatPart_FertilityByGenderAge), "AgeFactor")]
    class PatchFertility
    {
        static bool Prefix(Pawn pawn, ref float __result)
        {
            if (!Patches.PawnIsNonSenescent(pawn))
                return true;

            __result = 1.0f;
            return false;
        }
    }
    
    [HarmonyPatch(typeof(StatPart_Age), "AgeMultiplier")]
    class PatchImmunity
    {
        private static readonly StatDef ImmunityGainSpeed = DefDatabase<StatDef>.GetNamed("ImmunityGainSpeed");
        
        static bool Prefix(StatPart_Age __instance, Pawn pawn, ref float __result)
        {
            // Check if we're calculating Immunity Gain Speed
            if(__instance.parentStat != ImmunityGainSpeed)
                return true;
            
            if (!Patches.PawnIsNonSenescent(pawn))
                return true;
            
            __result = 1.0f;
            return false;
        }
    }
    
    [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.LovinAgeFactor))]
    class PatchRomance
    {
        static bool Prefix(Pawn otherPawn, Pawn ___pawn, ref float __result)
        {
            if(!Patches.PawnIsNonSenescent(___pawn) || !Patches.PawnIsNonSenescent(otherPawn))
                return true;
            
            __result = 1.0f;
            return false;
        }
    }
}