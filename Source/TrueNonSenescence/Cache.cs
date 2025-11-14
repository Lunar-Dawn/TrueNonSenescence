using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace TrueNonSenescence
{
	[StaticConstructorOnStartup]
	public static class Cache
	{
		private static readonly Dictionary<Pawn_GeneTracker, bool> SenescenceCache =
			new Dictionary<Pawn_GeneTracker, bool>();

		private static readonly GeneDef NonSenescent = DefDatabase<GeneDef>.GetNamed("DiseaseFree");

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

			if (SenescenceCache.TryGetValue(pawn.genes, out var senescent))
				return senescent;

			senescent = pawn.genes.HasActiveGene(NonSenescent);
			SenescenceCache[pawn.genes] = senescent;
			return senescent;
		}

		private static void ClearCache(Pawn_GeneTracker __instance)
		{
			SenescenceCache.Remove(__instance);
		}
	}
}