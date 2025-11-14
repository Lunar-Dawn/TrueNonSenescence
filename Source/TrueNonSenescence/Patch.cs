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
        
        // --- Birth Ritual Patches ---
        // Curse you Ludeon for not putting this in a separate function
        
        private static readonly RitualOutcomeEffectDef ChildBirth =
            DefDatabase<RitualOutcomeEffectDef>.GetNamed("ChildBirth");
        
        [HarmonyPatch(typeof(RitualOutcomeComp_PawnAge), nameof(RitualOutcomeComp_PawnAge.QualityOffset))]
        [HarmonyPrefix]
        private static bool PatchQualityOffset(RitualOutcomeComp_PawnAge __instance, LordJob_Ritual ritual, RitualOutcomeComp_Data data, ref float __result)
        {
            if (ritual.Ritual.outcomeEffect.def != ChildBirth)
                return true;

            var pawn = ritual.PawnWithRole(__instance.roleId);
            if (pawn == null || !Cache.PawnIsNonSenescent(pawn))
                return true;

            __result = __instance.curve.MaxY;
            return false;
        }
        
        private static float ReplaceQualityIfNonSenescent(float quality, RitualOutcomeComp_PawnAge instance, Pawn pawn, Precept_Ritual ritual)
        {
            if (ritual.outcomeEffect.def != ChildBirth)
                return quality;
            
            if (!Cache.PawnIsNonSenescent(pawn))
                return quality;
            
            return instance.curve.MaxY;
        }
        
        [HarmonyPatch(typeof(RitualOutcomeComp_PawnAge), nameof(RitualOutcomeComp_PawnAge.GetDesc))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> PatchBirthDesc(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher
                .MatchEndForward(
                    CodeMatch.Calls(() => default(SimpleCurve).Evaluate(default))
                )
                .ThrowIfInvalid("Could not find pattern in RitualOutcomeComp_PawnAge.GetDesc")
                .Advance()
                .InsertAndAdvance(
                    CodeInstruction.LoadArgument(0),
                    CodeInstruction.LoadLocal(0),
                    CodeInstruction.LoadArgument(1),
                    CodeInstruction.Call(typeof(LordJob_Ritual), "get_Ritual"),
                    CodeInstruction.Call(() => ReplaceQualityIfNonSenescent(default, default, default, default)));

            return matcher.InstructionEnumeration();
        }
        
        [HarmonyPatch(typeof(RitualOutcomeComp_PawnAge), nameof(RitualOutcomeComp_PawnAge.GetQualityFactor))]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> PatchBirthQualityFactor(IEnumerable<CodeInstruction> instructions)
        {
            var matcher = new CodeMatcher(instructions);

            matcher
                .MatchEndForward(
                    CodeMatch.Calls(() => default(SimpleCurve).Evaluate(default))
                )
                .ThrowIfInvalid("Could not find pattern in RitualOutcomeComp_PawnAge.GetQualityFactor")
                .Advance()
                .InsertAndAdvance(
                    CodeInstruction.LoadArgument(0),
                    CodeInstruction.LoadLocal(0),
                    CodeInstruction.LoadArgument(1),
                    CodeInstruction.Call(() => ReplaceQualityIfNonSenescent(default, default, default, default)));

            return matcher.InstructionEnumeration();
        }
    }
}