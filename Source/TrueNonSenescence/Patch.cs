using System;
using System.Collections.Generic;
using System.Reflection.Emit;
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

            // Fertility scale with lifespan replaces the entire StatPart for fertility age
            // with its own custom percentage-based one, so we patch it as well.
            var fertilityByGenderLifeSpanAgeFactor = AccessTools.Method(
                AccessTools.TypeByName("FertilityScaleWithLifeSpan.StatPart_FertilityByGenderLifeSpan"),
                "AgeFactor");
            if (fertilityByGenderLifeSpanAgeFactor != null)
            {
                harmony.Patch(
                    fertilityByGenderLifeSpanAgeFactor,
                    prefix: new HarmonyMethod(typeof(Patches), nameof(PatchFertility)));
            }
        }
        
        [HarmonyPatch(typeof(StatPart_FertilityByGenderAge), "AgeFactor")]
        [HarmonyPrefix]
        private static bool PatchFertility(Pawn pawn, ref float __result)
        {
            if (!Cache.PawnIsNonSenescent(pawn))
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
            
            if (!Cache.PawnIsNonSenescent(pawn))
                return true;
            
            __result = 1.0f;
            return false;
        }
        
        [HarmonyPatch(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.LovinAgeFactor))]
        [HarmonyPrefix]
        private static bool PatchRomance(Pawn otherPawn, Pawn ___pawn, ref float __result)
        {
            if(!Cache.PawnIsNonSenescent(___pawn) || !Cache.PawnIsNonSenescent(otherPawn))
                return true;
            
            float num1 = Mathf.InverseLerp(16f, 18f, ___pawn.ageTracker.AgeBiologicalYearsFloat);
            float num2 = Mathf.InverseLerp(16f, 18f, otherPawn.ageTracker.AgeBiologicalYearsFloat);
            __result = num1 * num2;
            return false;
        }
    }
}