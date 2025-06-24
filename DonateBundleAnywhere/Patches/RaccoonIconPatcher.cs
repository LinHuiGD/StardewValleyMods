using Fai0.StardewValleyMods.Common;
using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathoschild.Stardew.Common.Patching;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Characters;
using StardewValley.Locations;
using StardewValley.Menus;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Fai0.StardewValleyMods.DonateBundleAnywhere.Patches;

/// <summary>
/// Add the raccoon icon to the InventoryPage or GameMenu(Android)
/// </summary>
internal class RaccoonIconPatcher : BasePatcher
{
    private static IMonitor Monitor = null!;
    private static IModHelper Helper = null!;
    private static HUDMessageHelper MsgHelper = null!;

    private static readonly PerScreen<ClickableTextureComponent?> racconIcon = new();
    private static ClickableTextureComponent? RacconIcon
    {
        get => racconIcon.Value;
        set => racconIcon.Value = value;
    }

    public RaccoonIconPatcher(IMonitor monitor, IModHelper helper, HUDMessageHelper msgHelper)
    {
        Monitor = monitor;
        Helper = helper;
        MsgHelper = msgHelper;
    }

    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        if (Constants.TargetPlatform == GamePlatform.Android)
        {
            harmony.Patch(
                original: AccessTools.Method(typeof(GameMenu), "setupMenus"),
                postfix: new HarmonyMethod(typeof(RaccoonIconPatcher), nameof(RaccoonIconPatcher.SetupMenus_Postfix)),
                transpiler: new HarmonyMethod(typeof(RaccoonIconPatcher), nameof(RaccoonIconPatcher.SetupMenus_Transpiler))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(GameMenu), nameof(GameMenu.receiveLeftClick)),
                postfix: new HarmonyMethod(typeof(RaccoonIconPatcher), nameof(RaccoonIconPatcher.ReceiveLeftClick_Postfix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(GameMenu), nameof(GameMenu.performHoverAction)),
                postfix: new HarmonyMethod(typeof(RaccoonIconPatcher), nameof(RaccoonIconPatcher.PerformHoverAction_Postfix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(GameMenu), nameof(GameMenu.draw), [typeof(SpriteBatch)]),
                postfix: new HarmonyMethod(typeof(RaccoonIconPatcher), nameof(RaccoonIconPatcher.Draw_Postfix))
            );
        }
        else
        {
            harmony.Patch(
                original: AccessTools.Constructor(typeof(InventoryPage), [typeof(int), typeof(int), typeof(int), typeof(int)]),
                postfix: new HarmonyMethod(typeof(RaccoonIconPatcher), nameof(RaccoonIconPatcher.InventoryPageConstructor_Postfix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(InventoryPage), nameof(InventoryPage.receiveLeftClick)),
                postfix: new HarmonyMethod(typeof(RaccoonIconPatcher), nameof(RaccoonIconPatcher.ReceiveLeftClick_Postfix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(InventoryPage), nameof(InventoryPage.performHoverAction)),
                postfix: new HarmonyMethod(typeof(RaccoonIconPatcher), nameof(RaccoonIconPatcher.PerformHoverAction_Postfix))
            );
            harmony.Patch(
                original: AccessTools.Method(typeof(InventoryPage), nameof(InventoryPage.draw), [typeof(SpriteBatch)]),
                postfix: new HarmonyMethod(typeof(RaccoonIconPatcher), nameof(RaccoonIconPatcher.Draw_Postfix))
            );
        }

        harmony.Patch(
            original: AccessTools.Method(typeof(IClickableMenu), nameof(IClickableMenu.populateClickableComponentList)),
            postfix: new HarmonyMethod(typeof(RaccoonIconPatcher), nameof(RaccoonIconPatcher.PopulateClickableComponentList_Postfix))
        );
    }

    #region Add raccoon icon
    public static bool ShouldShowRaccoonIcon()
    {
        if (!Game1.MasterPlayer.mailReceived.Contains("raccoonMovedIn"))
            return false;

        bool interim = Game1.netWorldState.Value.Date.TotalDays - Game1.netWorldState.Value.DaysPlayedWhenLastRaccoonBundleWasFinished < 7;
        int whichDialogue = Game1.netWorldState.Value.TimesFedRaccoons;
        if (whichDialogue == 0)
            interim = false;
        if (interim)
            return false;

        Forest forest = Game1.RequireLocation<Forest>("Forest");
        if (forest.getCharacterFromName("Raccoon") == null)
            return false;

        return true;
    }

    public static void AddRaccoonIcon(int x, int y, ClickableTextureComponent? junimoNoteIcon = null)
    {
        // optional: replace junimo note icon with joja icon
        if (junimoNoteIcon != null && Game1.player.hasOrWillReceiveMail("hasSeenAbandonedJunimoNote"))
        {
            junimoNoteIcon.texture = Helper.ModContent.Load<Texture2D>("assets/icons.png");
            junimoNoteIcon.sourceRect = new Rectangle(0, 0, 15, 14);
            junimoNoteIcon.hoverText = CommunityCenter.getAreaDisplayNameFromNumber(6);
        }
        // remote donation entry for raccoon
        RacconIcon = null;
        if (RaccoonIconPatcher.ShouldShowRaccoonIcon())
        {
            int raccoonIconID = 19491001;
            int junimoNoteIconID = 898;
            RacconIcon = new ClickableTextureComponent("",
                new Rectangle(x, y, 64, 64),
                null, I18n.Npc_Name_MrRaccon(), Helper.ModContent.Load<Texture2D>("assets/icons.png"),
                new Rectangle(15, 0, 15, 14), 4f);

            if (junimoNoteIcon != null)
            {
                junimoNoteIcon.rightNeighborID = raccoonIconID;
                RacconIcon.leftNeighborID = junimoNoteIconID;
            }
            else
            {
                RacconIcon.myID = junimoNoteIconID;
                RacconIcon.leftNeighborID = 11;
                RacconIcon.downNeighborID = 106;
            }
        }
    }

    // add raccoon icon
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void InventoryPageConstructor_Postfix(InventoryPage __instance, int x, int y, int width, int height)
    {
        AddRaccoonIcon(__instance.xPositionOnScreen + width + (__instance.junimoNoteIcon != null ? (64 + 8) : 0), __instance.yPositionOnScreen + 96, __instance.junimoNoteIcon);
    }

    // add raccoon icon on Android
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void SetupMenus_Postfix(GameMenu __instance, bool standardTabs = true, bool optionsOnly = false)
    {
        int edgeX = Helper.Reflection.GetField<int>(__instance, "edgeX").GetValue();
        int tabWidth = Helper.Reflection.GetField<int>(__instance, "tabWidth").GetValue();
        int tabY = Helper.Reflection.GetField<int>(__instance, "tabY").GetValue();
        int edgeY = Helper.Reflection.GetField<int>(__instance, "edgeY").GetValue();

        ClickableTextureComponent junimoNoteIcon = Helper.Reflection.GetField<ClickableTextureComponent>(__instance, "junimoNoteIcon").GetValue();
        // position derived from GameMenu.setupMenus on Android
        RaccoonIconPatcher.AddRaccoonIcon(__instance.xPositionOnScreen + edgeX + 16 + tabWidth * 9 + (junimoNoteIcon != null ? (64 + 8) : 0), __instance.yPositionOnScreen + 4 + tabY + edgeY, junimoNoteIcon);
    }

    // derived from InventoryPage.ShouldShowJunimoNoteIcon()
    public static bool ShouldShowJunimoNoteIcon()
    {
        if (Game1.player.hasOrWillReceiveMail("canReadJunimoText") && !Game1.player.hasOrWillReceiveMail("JojaMember"))
        {
            if (Game1.MasterPlayer.hasCompletedCommunityCenter())
            {
                if (Game1.player.hasOrWillReceiveMail("hasSeenAbandonedJunimoNote"))
                {
                    return !Game1.MasterPlayer.hasOrWillReceiveMail("ccMovieTheater");
                }
                return false;
            }
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IEnumerable<CodeInstruction> SetupMenus_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        CodeMatcher matcher = new(instructions);
        #region Part 1. Unify JunimoNoteIcon display condition across Android and other platforms
        #region IL with C# of Part 1
        /*
            // bool flag = Game1.player.hasOrWillReceiveMail("canReadJunimoText") && !Game1.player.hasOrWillReceiveMail("JojaMember") && !Game1.player.hasCompletedCommunityCenter();
            IL_008e: call class StardewValley.Farmer StardewValley.Game1::get_player()
            IL_0093: ldstr "canReadJunimoText"
            IL_0098: callvirt instance bool StardewValley.Farmer::hasOrWillReceiveMail(string)
            IL_009d: brfalse.s IL_00bf

            IL_009f: call class StardewValley.Farmer StardewValley.Game1::get_player()
            IL_00a4: ldstr "JojaMember"
            IL_00a9: callvirt instance bool StardewValley.Farmer::hasOrWillReceiveMail(string)
            IL_00ae: brtrue.s IL_00bf

            // (no C# code)
            IL_00b0: call class StardewValley.Farmer StardewValley.Game1::get_player()
            IL_00b5: callvirt instance bool StardewValley.Farmer::hasCompletedCommunityCenter()
            IL_00ba: ldc.i4.0
            IL_00bb: ceq
            IL_00bd: br.s IL_00c0

            IL_00bf: ldc.i4.0
            IL_00c0: stloc.0
         */
        #endregion

        try
        {
            MethodInfo getPlayerInfo = AccessTools.PropertyGetter(typeof(Game1), nameof(Game1.player));
            MethodInfo hasOrWillReceiveMailInfo = AccessTools.Method(typeof(Farmer), nameof(Farmer.hasOrWillReceiveMail), [typeof(string)]);

            // the modified code acts like:
            // bool flag = RaccoonIconPatcher.ShouldShowJunimoNoteIcon()
            //  Without modding, it will not be shown when AbandonedJojaMart is available on Android
            matcher.MatchStartForward(
                new CodeMatch(OpCodes.Call, getPlayerInfo), // repalce with Call RaccoonIconPatcher.ShouldShowJunimoNoteIcon
            #region removed
                new CodeMatch(OpCodes.Ldstr, "canReadJunimoText"), // repalce with Stloc_0
                new CodeMatch(OpCodes.Callvirt, hasOrWillReceiveMailInfo),
                new CodeMatch(i => i.opcode == OpCodes.Brfalse_S || i.opcode == OpCodes.Brfalse),

                new CodeMatch(OpCodes.Call, getPlayerInfo),
                new CodeMatch(OpCodes.Ldstr, "JojaMember"),
                new CodeMatch(OpCodes.Callvirt, hasOrWillReceiveMailInfo),
                new CodeMatch(i => i.opcode == OpCodes.Brtrue_S || i.opcode == OpCodes.Brtrue),

                new CodeMatch(OpCodes.Call, getPlayerInfo),
                new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(Farmer), nameof(Farmer.hasCompletedCommunityCenter))),
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(OpCodes.Ceq),
                new CodeMatch(i => i.opcode == OpCodes.Br_S || i.opcode == OpCodes.Br),

                new CodeMatch(OpCodes.Ldc_I4_0),
            #endregion removed
                new CodeMatch(OpCodes.Stloc_0)

            )
                .ThrowIfNotMatch($"Could not find entry point for setupMenus")
                .SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(RaccoonIconPatcher), nameof(RaccoonIconPatcher.ShouldShowJunimoNoteIcon)));
            matcher.RemoveInstructions(13);
        }
        catch (Exception e)
        {
            Monitor.Log($"Failed in {nameof(SetupMenus_Transpiler)}:\n{e}", LogLevel.Error);
        }

        #endregion Part 1

        #region Part 2. Modify tabWidth to make room for RaccoonIcon
        #region IL with C# of Part 2
        /*
        	// this.tabWidth = (IClickableMenu.viewport.Width - Game1.xEdge * 2 - 80) / (9 + (flag ? 1 : 0));
            ...
            IL_0111: ldloc.0
            IL_0112: ldc.i4.0
            IL_0113: cgt.un
            IL_0115: add
            IL_0116: div
            IL_0117: stfld int32 StardewValley.Menus.GameMenu::tabWidth
        */
        #endregion 

        try
        {
            // the modified code acts like:
            // this.tabWidth = (IClickableMenu.viewport.Width - Game1.xEdge * 2 - 80) / (9 + (flag ? 1 : 0) + (InventoryPagePatcher.ShouldShowRaccoonIcon() ? 1 : 0));
            matcher.MatchStartForward(
                new CodeMatch(OpCodes.Ldloc_0),
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(i => i.opcode == OpCodes.Cgt_Un || i.opcode == OpCodes.Cgt),
                new CodeMatch(OpCodes.Add),
                // insert here
                new CodeMatch(OpCodes.Div),
                new CodeMatch(OpCodes.Stfld, AccessTools.Field(typeof(GameMenu), "tabWidth"))
            )
                .ThrowIfNotMatch($"Could not find entry point for setupMenus(modding tabWidth)")
                .Advance(4)
                .InsertAndAdvance(new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(RaccoonIconPatcher), nameof(RaccoonIconPatcher.ShouldShowRaccoonIcon))))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Ldc_I4_0))
                .InsertAndAdvance(new CodeInstruction(OpCodes.Cgt_Un))
                .Insert(new CodeInstruction(OpCodes.Add));
        }
        catch (Exception e)
        {
            Monitor.Log($"Failed in {nameof(SetupMenus_Transpiler)}(modding tabWidth):\n{e}", LogLevel.Error);
        }

        #endregion Part 2
        return matcher.InstructionEnumeration();
    }
    #endregion Add raccoon icon

    #region Handle input events or update
    // TO TEST
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ReceiveLeftClick_Postfix(int x, int y, bool playSound = true)
    {
        if (RaccoonIconPatcher.RacconIcon != null && RaccoonIconPatcher.RacconIcon.containsPoint(x, y))
        {
            Monitor.Log("raccoon icon clicked.", LogLevel.Trace);
            // another player complete this bundle?
            if (!RaccoonIconPatcher.ShouldShowRaccoonIcon())
            {
                Monitor.Log("the raccoon's bundle was just completed.", LogLevel.Trace);
                MsgHelper.AddHUDMessage(new HUDMessage(Game1.content.LoadString("Strings\\1_6_Strings:Raccoon_interim"), HUDMessage.error_type));
                return;
            }
            Forest forest = Game1.RequireLocation<Forest>("Forest");
            if (forest.getCharacterFromName("Raccoon") is Raccoon raccoon)
            {
                raccoon.mutex.RequestLock(
                    delegate
                    {
                        Monitor.Log("succced to acquire lock.", LogLevel.Trace);
                        Helper.Reflection.GetMethod(raccoon, "_activateMrRaccoon").Invoke();
                    },
                    delegate
                    {
                        Monitor.Log("failed to acquire lock.", LogLevel.Trace);
                        MsgHelper.AddHUDMessage(new HUDMessage(Game1.content.LoadString("Strings\\1_6_Strings:Raccoon_busy"), HUDMessage.error_type));
                    });
                Monitor.Log("request lock for raccoon", LogLevel.Trace);
            }
            else
            {
                Monitor.Log("can't find raccoon.", LogLevel.Trace);
                MsgHelper.AddHUDMessage(new HUDMessage("can't find raccoon(NPC) in forest", HUDMessage.error_type));
            }
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void PerformHoverAction_Postfix(IClickableMenu __instance, int x, int y)
    {
        if (RaccoonIconPatcher.RacconIcon != null)
        {
            RaccoonIconPatcher.RacconIcon.tryHover(x, y);
            if (RaccoonIconPatcher.RacconIcon.containsPoint(x, y))
            {
                if (__instance is GameMenu menu) menu.hoverText = RaccoonIconPatcher.RacconIcon.hoverText;
                if (__instance is InventoryPage page) page.hoverText = RaccoonIconPatcher.RacconIcon.hoverText;
            }
        }
    }

    // TEST Game controller
    // InventoryPage did not override ClickableMenu.PopulateClickableComponentsList, so we will patch this method in IClickableMenu.
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void PopulateClickableComponentList_Postfix(IClickableMenu __instance)
    {
        // used in controller mode
        if (RaccoonIconPatcher.RacconIcon != null)
        {
            __instance.allClickableComponents.Add(RaccoonIconPatcher.RacconIcon);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Draw_Postfix(SpriteBatch b)
    {
        RaccoonIconPatcher.RacconIcon?.draw(b);
    }
    #endregion Handle input events or update
}
