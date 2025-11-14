using System;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace TrueNonSenescence
{
	[StaticConstructorOnStartup]
	public static class PatchNumericalFactors
	{
		private static readonly StatDef ImmunityGainSpeed = DefDatabase<StatDef>.GetNamed("ImmunityGainSpeed");

		static PatchNumericalFactors()
		{
			var harmony = new Harmony("LunarDawn.TrueNonSenescence");

			harmony.Patch(
				AccessTools.Method(typeof(StatPart_Age), "AgeMultiplier"),
				postfix: new HarmonyMethod(typeof(PatchNumericalFactors), nameof(PatchImmunity))
			);
			harmony.Patch(
				AccessTools.Method(typeof(Pawn_RelationsTracker), nameof(Pawn_RelationsTracker.LovinAgeFactor)),
				postfix: new HarmonyMethod(typeof(PatchNumericalFactors), nameof(PatchRomance))
			);

			harmony.Patch(
				AccessTools.Method(typeof(StatPart_FertilityByGenderAge), "AgeFactor"),
				postfix: new HarmonyMethod(typeof(PatchNumericalFactors), nameof(PatchFertility))
			);
			// "Fertility scale with lifespan" replaces the entire StatPart for fertility age
			// with its own custom percentage-based one, so we patch it if it exists.
			var fertilityByGenderLifeSpanAgeFactor = AccessTools.Method(
				AccessTools.TypeByName("FertilityScaleWithLifeSpan.StatPart_FertilityByGenderLifeSpan"),
				"AgeFactor");
			if (fertilityByGenderLifeSpanAgeFactor != null)
			{
				harmony.Patch(
					fertilityByGenderLifeSpanAgeFactor,
					postfix: new HarmonyMethod(typeof(PatchNumericalFactors), nameof(PatchFertility))
				);
			}
		}

		private static void PatchImmunity(ref float __result, StatPart_Age __instance, Pawn pawn)
		{
			// Check if we're calculating Immunity Gain Speed
			if (__instance.parentStat != ImmunityGainSpeed)
				return;

			if (!Cache.PawnIsNonSenescent(pawn))
				return;

			__result = Math.Max(1.0f, __result);
		}

		private static void PatchRomance(ref float __result, Pawn otherPawn, Pawn ___pawn)
		{
			if (!Cache.PawnIsNonSenescent(___pawn) || !Cache.PawnIsNonSenescent(otherPawn))
				return;

			float num1 = Mathf.InverseLerp(16f, 18f, ___pawn.ageTracker.AgeBiologicalYearsFloat);
			float num2 = Mathf.InverseLerp(16f, 18f, otherPawn.ageTracker.AgeBiologicalYearsFloat);
			__result = Math.Max(num1 * num2, __result);
		}

		private static void PatchFertility(ref float __result, Pawn pawn)
		{
			if (!Cache.PawnIsNonSenescent(pawn))
				return;

			__result = Math.Max(1.0f, __result);
		}
	}
}