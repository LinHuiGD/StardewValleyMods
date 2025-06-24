using HarmonyLib;
using Pathoschild.Stardew.Common.Patching;
using StardewModdingAPI;
using StardewValley.Characters;

namespace Fai0.StardewValleyMods.DonateBundleAnywhere.Patches;

internal class RaccoonPatcher: BasePatcher
{
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: AccessTools.DeclaredMethod(typeof(Raccoon), nameof(Raccoon.update)),
            transpiler: new HarmonyMethod(typeof(ModUtilities), nameof(ModUtilities.Update_Transpiler))
        );
    }
}
