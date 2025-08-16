using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TrueNonSenescence
{
    [StaticConstructorOnStartup]
    [HarmonyPatch]
    public static class Patches
    {
        static Patches()
        {
            var harmony = new Harmony("LunarDawn.TrueNonSenescence");
            harmony.PatchAll();
        }

        private static readonly Dictionary<Pawn_GeneTracker, bool> SenescenceCache = new Dictionary<Pawn_GeneTracker, bool>();
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
        [HarmonyPatch(typeof(Pawn_GeneTracker), "Notify_GenesChanged")]
        [HarmonyPostfix]
        private static void ClearCache(Pawn_GeneTracker __instance)
        {
            SenescenceCache.Remove(__instance);
        }
        
        [HarmonyPatch(typeof(StatPart_FertilityByGenderAge), "AgeFactor")]
        [HarmonyPrefix]
        private static bool PatchFertility(Pawn pawn, ref float __result)
        {
            if (!PawnIsNonSenescent(pawn))
                return true;

            __result = 1.0f;
            return false;
        }
        
        private static readonly StatDef ImmunityGainSpeed = DefDatabase<StatDef>.GetNamed("ImmunityGainSpeed");
        [HarmonyPatch(typeof(StatPart_Age), "AgeMultiplier")]
        [HarmonyPrefix]
        private static bool PatchImmunity(StatPart_Age __instance, Pawn pawn, ref float __result)
        {
            // Check if we're calculating Immunity Gain Speed
            if(__instance.parentStat != ImmunityGainSpeed)
                return true;
            
            if (!PawnIsNonSenescent(pawn))
                return true;
            
            __result = 1.0f;
            return false;
        }
        
        [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.LovinAgeFactor))]
        [HarmonyPrefix]
        private static bool PatchRomance(Pawn otherPawn, Pawn ___pawn, ref float __result)
        {
            if(!PawnIsNonSenescent(___pawn) || !PawnIsNonSenescent(otherPawn))
                return true;
            
            float num1 = Mathf.InverseLerp(16f, 18f, ___pawn.ageTracker.AgeBiologicalYearsFloat);
            float num2 = Mathf.InverseLerp(16f, 18f, otherPawn.ageTracker.AgeBiologicalYearsFloat);
            __result = num1 * num2;
            return false;
        }
    }
}