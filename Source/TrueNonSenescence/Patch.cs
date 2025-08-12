using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
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

        private static readonly Dictionary<Pawn_GeneTracker, bool> SenescenceCache = new Dictionary<Pawn_GeneTracker, bool>();
        public static void ClearCache(Pawn_GeneTracker tracker)
        {
            SenescenceCache.Remove(tracker);
        }
        
        private static readonly GeneDef NonSenescent = DefDatabase<GeneDef>.GetNamed("DiseaseFree");
        public static bool PawnIsNonSenescent(Pawn pawn)
        {
            if (pawn.genes is null)
                return false;
            
            if(SenescenceCache.TryGetValue(pawn.genes, out var senescent))
                return senescent;

            senescent = pawn.genes.HasActiveGene(NonSenescent);
            SenescenceCache[pawn.genes] = senescent;
            return senescent;
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
            
            float num1 = Mathf.InverseLerp(16f, 18f, ___pawn.ageTracker.AgeBiologicalYearsFloat);
            float num2 = Mathf.InverseLerp(16f, 18f, otherPawn.ageTracker.AgeBiologicalYearsFloat);
            __result = num1 * num2;
            return false;
        }
    }

    [HarmonyPatch(typeof(Pawn_GeneTracker), "Notify_GenesChanged")]
    class ClearCache
    {
        static void Postfix(Pawn_GeneTracker __instance)
        {
            Patches.ClearCache(__instance);
        }
    }
}