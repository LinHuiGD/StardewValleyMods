using System.Runtime.CompilerServices;
using HarmonyLib;
using Pathoschild.Stardew.Common.Patching;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;

namespace Fai0.StardewValleyMods.DonateBundleAnywhere.Patches;

/// <summary>
/// This patch is mainly for multiplayer mode. <br/>
/// Let locations where the bundles are located keep theirs data synchronized, <br/>
/// then we can access NetFields of locations remotely.
/// </summary>
/// <remarks>
/// <see cref="GameLocation.CanBeRemotedlyViewed"/> implies that whether a location can be accessed remotely depends on whether it is always activated.<br/>
/// </remarks>
internal class MultiplayerPatcher : BasePatcher
{
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(Multiplayer), nameof(Multiplayer.isAlwaysActiveLocation)),
            postfix: new HarmonyMethod(typeof(MultiplayerPatcher), nameof(MultiplayerPatcher.IsAlwaysActiveLocation_Postfix))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(GameLocation), nameof(GameLocation.IsActiveLocation)),
            postfix: new HarmonyMethod(typeof(MultiplayerPatcher), nameof(MultiplayerPatcher.IsActiveLocation_Postfix))
        );
    }

    /// <summary>
    /// keep CommunityCenter, AbandonedJojaMart active for bundle mutex sync of Junimo.<br/>
    /// Forset for bundle mutex sync of Raccoon.
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void IsAlwaysActiveLocation_Postfix(GameLocation location, ref bool __result)
    {
        if (location != null && (location is CommunityCenter || location is AbandonedJojaMart || location is Forest))
        {
            __result = true;
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void IsActiveLocation_Postfix(GameLocation __instance, ref bool __result)
    {
        if (__instance != null && (__instance is CommunityCenter || __instance is AbandonedJojaMart || __instance is Forest))
        {
            __result = true;
        }
    }
}
