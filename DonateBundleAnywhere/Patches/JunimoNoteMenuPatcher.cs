using HarmonyLib;
using Pathoschild.Stardew.Common.Patching;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Fai0.StardewValleyMods.DonateBundleAnywhere.Patches;

/// <summary>
/// unused patcher, just for fun
/// </summary>
internal class JunimoNoteMenuPatcher : BasePatcher
{
    private static IMonitor Monitor = null!; // set when constructor is called
    public JunimoNoteMenuPatcher(IMonitor monitor)
    {
        JunimoNoteMenuPatcher.Monitor = monitor;
    }
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        //harmony.Patch(
        //    original: AccessTools.Method(typeof(JunimoNoteMenu), "setUpBundleSpecificPage"),
        //    transpiler: new HarmonyMethod(typeof(JunimoNoteMenuPatcher), nameof(JunimoNoteMenuPatcher.setUpBundleSpecificPage_Transpiler))
        //);
        //harmony.Patch(
        //    original: AccessTools.Method(typeof(JunimoNoteMenu), nameof(JunimoNoteMenu.receiveLeftClick)),
        //    transpiler: new HarmonyMethod(typeof(JunimoNoteMenuPatcher), nameof(JunimoNoteMenuPatcher.receiveLeftClick_Transpiler))
        //);
        //harmony.Patch(
        //    original: AccessTools.Method(typeof(JunimoNoteMenu), "restoreAreaOnExit"),
        //    postfix: new HarmonyMethod(typeof(JunimoNoteMenuPatcher), nameof(JunimoNoteMenuPatcher.restoreAreaOnExit_Postfix))
        //);
        
    }

    // This transpiler modifies the `setUpBundleSpecificPage` method to allow purchaseButton showing in the vault page
    public static IEnumerable<CodeInstruction> setUpBundleSpecificPage_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        CodeMatcher matcher = new(instructions);
        FieldInfo whichAreaInfo = AccessTools.Field(typeof(JunimoNoteMenu), nameof(JunimoNoteMenu.whichArea));
        FieldInfo fromGameMenuInfo = AccessTools.Field(typeof(JunimoNoteMenu), nameof(JunimoNoteMenu.fromGameMenu));

        //if (this.whichArea == 4)
        //{
        //    if (!this.fromGameMenu)
        //    { 
        //        this.purchaseButton = new ClickableTextureComponent(...
        //    }
        //}
        matcher.MatchStartForward(
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld, whichAreaInfo),
            new CodeMatch(OpCodes.Ldc_I4_4),
            new CodeMatch(OpCodes.Bne_Un),

            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld, fromGameMenuInfo),
            new CodeMatch(i => i.opcode == OpCodes.Brtrue || i.opcode == OpCodes.Brtrue_S) // replaced pos at 6
            )
            .ThrowIfNotMatch($"Could not find entry point for setUpBundleSpecificPage")
            .Advance(6)
            .Set(OpCodes.Pop, null); // Replace brtrue with pop, pop the bool from the top of the stack, and continue to execute if-block

        return matcher.InstructionEnumeration();
    }

    // This transpiler modifies the `receiveLeftClick` method to allow the rewards menu to be opened
    public static IEnumerable<CodeInstruction> receiveLeftClick_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        CodeMatcher matcher = new(instructions);
        FieldInfo presentButtonInfo = AccessTools.Field(typeof(JunimoNoteMenu), nameof(JunimoNoteMenu.presentButton));
        MethodInfo containsPointInfo = AccessTools.Method(typeof(ClickableComponent), nameof(ClickableComponent.containsPoint), [typeof(int), typeof(int)]);
        FieldInfo fromThisMenuInfo = AccessTools.Field(typeof(JunimoNoteMenu), nameof(JunimoNoteMenu.fromThisMenu));
        FieldInfo fromGameMenuInfo = AccessTools.Field(typeof(JunimoNoteMenu), nameof(JunimoNoteMenu.fromGameMenu));
        MethodInfo openRewardsMenuInfo = AccessTools.Method(typeof(JunimoNoteMenu), "openRewardsMenu");


        //if (this.presentButton != null && this.presentButton.containsPoint(x, y) && !this.fromGameMenu && !this.fromThisMenu)
        //{
        //    this.openRewardsMenu();
        //}
        matcher.MatchStartForward(
            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld, presentButtonInfo),
            new CodeMatch(i => i.opcode == OpCodes.Brfalse || i.opcode == OpCodes.Brfalse_S),

            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld, presentButtonInfo),
            new CodeMatch(OpCodes.Ldarg_1),
            new CodeMatch(OpCodes.Ldarg_2),
            new CodeMatch(OpCodes.Callvirt, containsPointInfo),
            new CodeMatch(i => i.opcode == OpCodes.Brfalse || i.opcode == OpCodes.Brfalse_S),

            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld, fromGameMenuInfo),
            new CodeMatch(i => i.opcode == OpCodes.Brtrue || i.opcode == OpCodes.Brtrue_S), // replaced pos at 11

            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Ldfld, fromThisMenuInfo),
            new CodeMatch(i => i.opcode == OpCodes.Brtrue || i.opcode == OpCodes.Brtrue_S), // replaced pos at 11 + 3

            new CodeMatch(OpCodes.Ldarg_0),
            new CodeMatch(OpCodes.Call, openRewardsMenuInfo)
        )
            .ThrowIfNotMatch($"Could not find entry point for {nameof(JunimoNoteMenu.receiveLeftClick)}")
            .Advance(11)
            .Set(OpCodes.Pop, null)
            .Advance(3)
            .Set(OpCodes.Pop, null); // fromThisMenu will be assigned true in SwapPage
        Monitor.Log($"[receiveLeftClick] matcher.Pos: 0x{matcher.Pos:X}", LogLevel.Debug);


        return matcher.InstructionEnumeration();
    }

    public static void restoreAreaOnExit_Postfix(JunimoNoteMenu __instance)
    {
        MethodInfo checkForMissedRewardsInfo = AccessTools.Method(typeof(CommunityCenter), "checkForMissedRewards");
        MethodInfo restoreAreaCutsceneInfo = AccessTools.Method(typeof(CommunityCenter), "restoreAreaCutscene");
        CommunityCenter cc = Game1.RequireLocation<CommunityCenter>("CommunityCenter");
        if (__instance.fromGameMenu)
        {
            if (Game1.player.currentLocation == cc)
            {
                // prevent returning to game menu
                if (Game1.activeClickableMenu is not null && Game1.activeClickableMenu is JunimoNoteMenu menu)
                {
                    menu.gameMenuTabToReturnTo = -1;
                }
                restoreAreaCutsceneInfo.Invoke(cc, [__instance.whichArea]);
            }
            else
            {
                // only check for missed rewards, but not to load restoration cutscene
                cc.areasComplete[__instance.whichArea] = true;
                checkForMissedRewardsInfo.Invoke(cc, null);
                //methodInfo.Invoke(cc, [__instance.whichArea]);
            }
        }
    }

}
