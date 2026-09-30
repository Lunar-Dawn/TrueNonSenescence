using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TrueNonSenescence
{
	[StaticConstructorOnStartup]
	public static class Cache
	{
		private static readonly Dictionary<int, bool> SenescenceCache =
			new Dictionary<int, bool>();

		static Cache()
		{
			var harmony = new Harmony("LunarDawn.TrueNonSenescence");

			harmony.Patch(
				AccessTools.Method(typeof(Pawn_GeneTracker), "Notify_GenesChanged"),
				postfix: new HarmonyMethod(typeof(Cache), nameof(ClearCache))
			);
		}

		public static bool PawnIsNonSenescent(Pawn pawn)
		{
			if (pawn.genes is null)
				return false;

			if (SenescenceCache.TryGetValue(pawn.thingIDNumber, out var senescent))
				return senescent;

			senescent = pawn.genes.GenesListForReading.Any(gene =>
				gene.def.GetModExtension<GeneExtension>()?.givesNonSenescence ?? false
			);

			SenescenceCache[pawn.thingIDNumber] = senescent;
			return senescent;
		}

		private static void ClearCache(Pawn_GeneTracker __instance)
		{
			SenescenceCache.Remove(__instance.pawn.thingIDNumber);
		}
	}
}