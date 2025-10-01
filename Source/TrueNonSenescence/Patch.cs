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
            
            harmony.Patch(
                AccessTools.Method(
                    AccessTools.FirstInner(
                        typeof(CompBiosculpterPod),
                        t => t.Name.Contains("CompGetGizmosExtra")
                    ),
                    "MoveNext"
                ),
                transpiler: new HarmonyMethod(typeof(Patches),nameof(AutoAgeReversalPatch))
            );
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
            if (pawn == null || !PawnIsNonSenescent(pawn))
                return true;

            __result = __instance.curve.MaxY;
            return false;
        }
        
        private static float ReplaceQualityIfNonSenescent(float quality, RitualOutcomeComp_PawnAge instance, Pawn pawn, Precept_Ritual ritual)
        {
            if (ritual.outcomeEffect.def != ChildBirth)
                return quality;
            
            if (!PawnIsNonSenescent(pawn))
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
        
        // --- BioSculpter Patches ---
        [HarmonyPatch(typeof(Pawn_AgeTracker), "AgeReversalDemandedDeadlineTicks", MethodType.Getter)]
        [HarmonyPrefix]
        private static bool AgeReversalDeadlinePatch(Pawn ___pawn, ref long __result)
        {
            if (!PawnIsNonSenescent(___pawn))
                return true;

            __result = long.MaxValue;
            return false;
        }
        
        private static IEnumerable<CodeInstruction> AutoAgeReversalPatch(IEnumerable<CodeInstruction> instructions)
        {
            var getOrDefault = SymbolExtensions.GetMethodInfo((bool? n) => n.GetValueOrDefault());

            var matcher = new CodeMatcher(instructions);

            var pattern = new[]
            {
                new CodeMatch(OpCodes.Stloc_S),
                new CodeMatch(OpCodes.Ldloca_S),
                new CodeMatch(OpCodes.Call, getOrDefault),
                new CodeMatch(OpCodes.Brfalse)
            };

            matcher
                .MatchEndForward(pattern)
                .ThrowIfNotMatch("Could not find pattern in CompGetGizmosExtra");
            
            var label = matcher.Operand;
            
            matcher
                .Advance()
                .Insert(
                    //Load the Biosculpter
                    CodeInstruction.LoadLocal(2),
                    // Load the tuned pawn
                    CodeInstruction.LoadField(typeof(CompBiosculpterPod), "biotunedTo"),
                    // Check if they're Non-Senescent
                    CodeInstruction.Call(() => PawnIsNonSenescent(null)),
                    // Skip the gizmo if they are
                    new CodeInstruction(OpCodes.Brtrue, label)
                );

            return matcher.Instructions();
        }
    }
}