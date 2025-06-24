using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Pathoschild.Stardew.Common.Patching;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;

namespace Fai0.StardewValleyMods.DonateBundleAnywhere.Patches;

internal class GameLocationPatcher : BasePatcher
{
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(GameLocation), "updateCharacters"),
            transpiler: new HarmonyMethod(typeof(GameLocationPatcher), nameof(GameLocationPatcher.UpdateCharacters_Transpiler))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(CommunityCenter), nameof(CommunityCenter.updateEvenIfFarmerIsntHere)),
            transpiler: new HarmonyMethod(typeof(ModUtilities), nameof(ModUtilities.Update_Transpiler))
        );
        // harmony.Patch(
        //     original: AccessTools.Method(typeof(CommunityCenter), nameof(CommunityCenter.markAreaAsComplete)),
        //     postfix: new HarmonyMethod(typeof(GameLocationPatcher), nameof(GameLocationPatcher.MarkAreaAsComplete_Postfix))
        // );
        harmony.Patch(
            original: AccessTools.Method(typeof(AbandonedJojaMart), nameof(AbandonedJojaMart.updateEvenIfFarmerIsntHere)),
            transpiler: new HarmonyMethod(typeof(ModUtilities), nameof(ModUtilities.Update_Transpiler))
        );
        harmony.Patch(
            original: AccessTools.Method(typeof(AbandonedJojaMart), "doRestoreAreaCutscene"),
            prefix: new HarmonyMethod(typeof(GameLocationPatcher), nameof(GameLocationPatcher.DoRestoreAreaCutscene_Prefix))
        );
    }

    // keep raccoon updated anytime, otherwise its mutex will be out of sync.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IEnumerable<CodeInstruction> UpdateCharacters_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        /*
            // if (nPC != null && (flag || nPC is Horse || nPC.forceUpdateTimer > 0))
            ...

            IL_0027: ldloc.0
            IL_0028: brtrue.s IL_003b

            IL_002a: ldloc.2
            IL_002b: isinst StardewValley.Characters.Horse
            IL_0030: brtrue.s IL_003b
        
        */
        CodeMatcher matcher = new(instructions);
        matcher.MatchStartForward(
            new CodeMatch(OpCodes.Isinst, typeof(Horse)),
            new CodeMatch(i => i.opcode == OpCodes.Brtrue_S || i.opcode == OpCodes.Brtrue)
        ).ThrowIfNotMatch($"Could not find entry point for {nameof(GameLocationPatcher.UpdateCharacters_Transpiler)}");
        CodeInstruction ldloc_x = matcher.InstructionAt(-1).Clone(); // copy opcode (load a npc)
        CodeInstruction isinst = new(OpCodes.Isinst, typeof(Raccoon)); // npc is Raccoon
        CodeInstruction brtrue = matcher.InstructionAt(1).Clone(); // copy opcode and operand(a Label indicating the destination to jump into)
        matcher.Advance(2)
            .InsertAndAdvance(ldloc_x)
            .InsertAndAdvance(isinst)
            .Insert(brtrue);
        return matcher.InstructionEnumeration();
    }

    // restore area if player is not there
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void DoRestoreAreaCutscene_Prefix(AbandonedJojaMart __instance)
    {
        // it is safe for both master farmer and farm helpers
        if (Game1.currentLocation != __instance)
        {
            // Game1.player.freezePause = 1000;
            DelayedAction.removeTileAfterDelay(8, 8, 100, __instance, "Buildings");
        }
    }

    // MarkAreaAsComplete even if lock owner is not there
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void MarkAreaAsComplete_Postfix(CommunityCenter __instance, int area)
    {
        if (Game1.currentLocation != __instance && __instance.bundleMutexes[area].IsLockHeld())
        {
            __instance.areasComplete[area] = true;
        }
    }
}
