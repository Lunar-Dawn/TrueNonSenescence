using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TrueNonSenescence
{
	[StaticConstructorOnStartup]
	public static class PatchBiosculpter
	{
		static PatchBiosculpter()
		{
			var harmony = new Harmony("LunarDawn.TrueNonSenescence");

			harmony.Patch(
				AccessTools.PropertyGetter(typeof(Pawn_AgeTracker), "AgeReversalDemandedDeadlineTicks"),
				postfix: new HarmonyMethod(typeof(PatchBiosculpter), nameof(AgeReversalDeadlinePatch))
			);
			harmony.Patch(
				AccessTools.Method(
					AccessTools.FirstInner(
						typeof(CompBiosculpterPod),
						t => t.Name.Contains("CompGetGizmosExtra")
					),
					"MoveNext"
				),
				transpiler: new HarmonyMethod(typeof(PatchBiosculpter), nameof(AutoAgeReversalPatch))
			);
		}

		private static void AgeReversalDeadlinePatch(ref long __result, Pawn ___pawn)
		{
			if (!Cache.PawnIsNonSenescent(___pawn))
				return;
			__result = long.MaxValue;
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
					CodeInstruction.Call(() => Cache.PawnIsNonSenescent(null)),
					// Skip the gizmo if they are
					new CodeInstruction(OpCodes.Brtrue, label)
				);

			return matcher.Instructions();
		}
	}
}