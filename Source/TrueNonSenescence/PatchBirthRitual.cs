using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TrueNonSenescence
{
	[StaticConstructorOnStartup]
	public static class PatchBirthRitual
	{
		private static readonly RitualOutcomeEffectDef ChildBirth =
			DefDatabase<RitualOutcomeEffectDef>.GetNamed("ChildBirth");

		static PatchBirthRitual()
		{
			var harmony = new Harmony("LunarDawn.TrueNonSenescence");

			harmony.Patch(
				AccessTools.Method(typeof(RitualOutcomeComp_PawnAge), nameof(RitualOutcomeComp_PawnAge.QualityOffset)),
				postfix: new HarmonyMethod(typeof(PatchBirthRitual), nameof(PatchQualityOffset))
			);
			harmony.Patch(
				AccessTools.Method(typeof(RitualOutcomeComp_PawnAge), nameof(RitualOutcomeComp_PawnAge.GetDesc)),
				transpiler: new HarmonyMethod(typeof(PatchBirthRitual), nameof(PatchBirthDesc))
			);
			harmony.Patch(
				AccessTools.Method(typeof(RitualOutcomeComp_PawnAge),
					nameof(RitualOutcomeComp_PawnAge.GetQualityFactor)),
				transpiler: new HarmonyMethod(typeof(PatchBirthRitual), nameof(PatchBirthQualityFactor))
			);
		}

		private static void PatchQualityOffset(ref float __result, RitualOutcomeComp_PawnAge __instance,
			LordJob_Ritual ritual)
		{
			if (ritual.Ritual.outcomeEffect.def != ChildBirth)
				return;

			var pawn = ritual.PawnWithRole(__instance.roleId);
			if (pawn == null || !Cache.PawnIsNonSenescent(pawn))
				return;

			__result = Math.Max(__instance.curve.MaxY, __result);
		}

		private static float ReplaceQualityIfNonSenescent(float quality, RitualOutcomeComp_PawnAge instance, Pawn pawn,
			Precept_Ritual ritual)
		{
			if (ritual.outcomeEffect.def != ChildBirth || !Cache.PawnIsNonSenescent(pawn))
				return quality;

			return Math.Max(instance.curve.MaxY, quality);
		}

		private static IEnumerable<CodeInstruction> PatchBirthDesc(IEnumerable<CodeInstruction> instructions)
		{
			var matcher = new CodeMatcher(instructions);

			matcher
				.MatchEndForward(
					CodeMatch.Calls(AccessTools.Method(typeof(SimpleCurve), nameof(SimpleCurve.Evaluate)))
				)
				.ThrowIfInvalid("Could not find pattern in RitualOutcomeComp_PawnAge.GetDesc")
				.Advance()
				.InsertAndAdvance(
					CodeInstruction.LoadArgument(0),
					CodeInstruction.LoadLocal(0),
					CodeInstruction.LoadArgument(1),
					CodeInstruction.Call(typeof(LordJob_Ritual), "get_Ritual"),
					CodeInstruction.Call(typeof(PatchBirthRitual), nameof(ReplaceQualityIfNonSenescent)));

			return matcher.InstructionEnumeration();
		}

		private static IEnumerable<CodeInstruction> PatchBirthQualityFactor(IEnumerable<CodeInstruction> instructions)
		{
			var matcher = new CodeMatcher(instructions);

			matcher
				.MatchEndForward(
					CodeMatch.Calls(AccessTools.Method(typeof(SimpleCurve), nameof(SimpleCurve.Evaluate)))
				)
				.ThrowIfInvalid("Could not find pattern in RitualOutcomeComp_PawnAge.GetQualityFactor")
				.Advance()
				.InsertAndAdvance(
					CodeInstruction.LoadArgument(0),
					CodeInstruction.LoadLocal(0),
					CodeInstruction.LoadArgument(1),
					CodeInstruction.Call(typeof(PatchBirthRitual), nameof(ReplaceQualityIfNonSenescent)));

			return matcher.InstructionEnumeration();
		}
	}
}