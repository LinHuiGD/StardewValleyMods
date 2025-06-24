using HarmonyLib;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Pathoschild.Stardew.Common.Patching;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Fai0.StardewValleyMods.DonateBundleAnywhere.Patches;

internal class JunimoNoteMenuPatcher : BasePatcher
{
	private static IMonitor Monitor = null!;
	private static IModHelper Helper = null!;
	private static IManifest ModManifest = null!;
	public static readonly PerScreen<bool> canClick = new(() => true);
	public static bool CanClick
	{
		get => canClick.Value;
		set => canClick.Value = value;
	}

	public JunimoNoteMenuPatcher(IMonitor monitor, IModHelper helper, IManifest modManifest)
	{
		Monitor = monitor;
		Helper = helper;
		ModManifest = modManifest;
	}

	public override void Apply(Harmony harmony, IMonitor monitor)
	{
		harmony.Patch(
			original: AccessTools.Method(typeof(JunimoNoteMenu), "setUpBundleSpecificPage"),
			transpiler: new HarmonyMethod(typeof(JunimoNoteMenuPatcher), nameof(JunimoNoteMenuPatcher.SetUpBundleSpecificPage_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.Method(typeof(JunimoNoteMenu), nameof(JunimoNoteMenu.receiveLeftClick)),
			prefix: new HarmonyMethod(typeof(JunimoNoteMenuPatcher), nameof(JunimoNoteMenuPatcher.ReceiveLeftClick_Prefix))
		);
		harmony.Patch(
			original: AccessTools.Method(typeof(JunimoNoteMenu), Constants.TargetPlatform == GamePlatform.Android ? "releaseLeftClick" : "receiveLeftClick"),
			transpiler: new HarmonyMethod(typeof(JunimoNoteMenuPatcher), nameof(JunimoNoteMenuPatcher.HandleLeftClick_Transpiler))
		);
		if (Constants.TargetPlatform == GamePlatform.Android)
		{
			harmony.Patch(
				original: AccessTools.Method(typeof(JunimoNoteMenu), nameof(JunimoNoteMenu.releaseLeftClick)),
				prefix: new HarmonyMethod(typeof(JunimoNoteMenuPatcher), nameof(JunimoNoteMenuPatcher.ReleaseLeftClick_Prefix))
			);
			harmony.Patch(
				original: AccessTools.Method(typeof(JunimoNoteMenu), nameof(JunimoNoteMenu.receiveGamePadButton)),
				prefix: new HarmonyMethod(typeof(JunimoNoteMenuPatcher), nameof(JunimoNoteMenuPatcher.ReceiveGamePadButton_Prefix))
			);
			harmony.Patch(
				original: AccessTools.Method(typeof(JunimoNoteMenu), "doNonSpecificBundlePageJoystick"),
				transpiler: new HarmonyMethod(typeof(JunimoNoteMenuPatcher), nameof(JunimoNoteMenuPatcher.DoNonSpecificBundlePageJoystick_Transpiler))
			);
		}

		harmony.Patch(
			original: AccessTools.Method(typeof(JunimoNoteMenu), nameof(JunimoNoteMenu.update)),
			transpiler: new HarmonyMethod(typeof(JunimoNoteMenuPatcher), nameof(JunimoNoteMenuPatcher.Update_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.Method(typeof(JunimoNoteMenu), nameof(JunimoNoteMenu.draw), [typeof(SpriteBatch)]),
			transpiler: new HarmonyMethod(typeof(JunimoNoteMenuPatcher), nameof(JunimoNoteMenuPatcher.Draw_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.Method(typeof(JunimoNoteMenu), "checkIfBundleIsComplete"),
			transpiler: new HarmonyMethod(typeof(JunimoNoteMenuPatcher), nameof(JunimoNoteMenuPatcher.HandleRestoreArea_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.Method(typeof(JunimoNoteMenu), nameof(JunimoNoteMenu.receiveLeftClick)),
			transpiler: new HarmonyMethod(typeof(JunimoNoteMenuPatcher), nameof(JunimoNoteMenuPatcher.HandleRestoreArea_Transpiler))
		);
		harmony.Patch(
			original: AccessTools.Method(typeof(JunimoNoteMenu), nameof(JunimoNoteMenu.setUpMenu)),
			transpiler: new HarmonyMethod(typeof(JunimoNoteMenuPatcher), nameof(JunimoNoteMenuPatcher.HandleRestoreArea_Transpiler))
		);

	}

	// allow purchaseButton showing in the vault page
	public static IEnumerable<CodeInstruction> SetUpBundleSpecificPage_Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		/*
            // if (this.whichArea == 4)
            IL_0018: ldarg.0
            IL_0019: ldfld int32 StardewValley.Menus.JunimoNoteMenu::whichArea
            IL_001e: ldc.i4.4
            IL_001f: bne.un IL_00b2

            // if (!this.fromGameMenu)
            IL_0024: ldarg.0
            IL_0025: ldfld bool StardewValley.Menus.JunimoNoteMenu::fromGameMenu
            IL_002a: brtrue IL_0461

            // this.purchaseButton = new ClickableTextureComponent(...
         */

		CodeMatcher matcher = new(instructions);
		matcher.MatchStartForward(
			new CodeMatch(OpCodes.Ldarg_0),
			new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(JunimoNoteMenu), nameof(JunimoNoteMenu.whichArea))),
			new CodeMatch(OpCodes.Ldc_I4_4),
			new CodeMatch(OpCodes.Bne_Un),

			new CodeMatch(OpCodes.Ldarg_0),
			new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(JunimoNoteMenu), nameof(JunimoNoteMenu.fromGameMenu))),
			new CodeMatch(i => i.opcode == OpCodes.Brtrue || i.opcode == OpCodes.Brtrue_S) // skip
			)
			.ThrowIfNotMatch($"Could not find entry point for setUpBundleSpecificPage")
			.Advance(6)
			.Set(OpCodes.Pop, null); // Replace brtrue with pop, pop the bool from the top of the stack, and continue to execute if-block

		return matcher.InstructionEnumeration();
	}

	// enable menu closure and page swapping on mutex acquisition failure
	public static bool ReceiveLeftClick_Prefix(JunimoNoteMenu __instance, int x, int y, bool playSound = true)
	{
		if (__instance.fromGameMenu && !JunimoNoteMenuPatcher.CanClick)
		{
			// enable menu closure
			// it's hard to calling base methods. <see href="https://harmony.pardeike.net/articles/patching-edgecases.html">
			if (__instance.upperRightCloseButton != null && __instance.readyToClose() && __instance.upperRightCloseButton.containsPoint(x, y))
			{
				__instance.exitThisMenu();
			}

			// SwapPage will not be called on Android
			if (Constants.TargetPlatform != GamePlatform.Android)
			{
				if (__instance.areaNextButton.containsPoint(x, y))
				{
					__instance.SwapPage(1);
				}
				else if (__instance.areaBackButton.containsPoint(x, y))
				{
					__instance.SwapPage(-1);
				}
			}
		}
		return JunimoNoteMenuPatcher.CanClick;
	}

	// swap page without setting any SnappedComponent
	// derived from JunimoNoteMenu.releaseLeftClick
	// specified for Android
	public static bool SwapPage(JunimoNoteMenu menu, int x, int y)
	{
		CommunityCenter communityCenter = Game1.RequireLocation<CommunityCenter>("CommunityCenter");
		int num = 6;
		if (menu.areaNextButton != null && menu.areaNextButton.visible && menu.areaNextButton.containsPoint(x, y))
		{
			for (int i = 1; i < num + 1; i++)
			{
				if (communityCenter.shouldNoteAppearInArea((menu.whichArea + i) % num))
				{
					Game1.activeClickableMenu = new JunimoNoteMenu(fromGameMenu: true, (menu.whichArea + i) % num, fromThisMenu: true);
					return true;
				}
			}
		}
		else
		{
			if (menu.areaBackButton == null || !menu.areaBackButton.visible || !menu.areaBackButton.containsPoint(x, y))
			{
				return false;
			}
			int num2 = menu.whichArea;
			for (int j = 1; j < num + 1; j++)
			{
				num2--;
				if (num2 == -1)
				{
					num2 = num;
				}
				if (communityCenter.shouldNoteAppearInArea(num2))
				{
					Game1.activeClickableMenu = new JunimoNoteMenu(fromGameMenu: true, num2, fromThisMenu: true);
					return true;
				}
			}
		}
		return false;
	}

	// enable menu closure and page swapping on mutex acquisition failure
	// specified for Android
	public static bool ReleaseLeftClick_Prefix(JunimoNoteMenu __instance, int x, int y)
	{
		if (__instance.fromGameMenu && !JunimoNoteMenuPatcher.CanClick)
		{
			// enable menu closure
			// derived from IClickableMenu.releaseLeftClick
			if (__instance.upperRightCloseButton != null && __instance.readyToClose() && __instance.upperRightCloseButton.containsPoint(x, y))
			{
				__instance.exitThisMenu();
			}
			// enable page swapping
			if (JunimoNoteMenuPatcher.SwapPage(__instance, x, y)) return false;
		}
		return JunimoNoteMenuPatcher.CanClick;
	}

	// same as ReceiveLeftClick_Prefix
	// specified for Android
	public static bool ReceiveGamePadButton_Prefix(JunimoNoteMenu __instance, Buttons b)
	{
		if (__instance.fromGameMenu && !JunimoNoteMenuPatcher.CanClick)
		{
			// derived from IClickableMenu.receiveGamePadButton on Android
			// enable menu closure
			if (b == Buttons.B && __instance.upperRightCloseButton != null && __instance.readyToClose())
			{
				__instance.exitThisMenu();
			}
		}
		return JunimoNoteMenuPatcher.CanClick;
	}

	// allow the rewards menu to be opened
	public static IEnumerable<CodeInstruction> HandleLeftClick_Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		//if (this.presentButton != null && this.presentButton.containsPoint(x, y) && !this.fromGameMenu && !this.fromThisMenu)
		//{
		//    this.openRewardsMenu();
		//}

		CodeMatcher matcher = new(instructions);
		matcher.MatchStartForward(
			new CodeMatch(OpCodes.Ldarg_0),
			new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(JunimoNoteMenu), nameof(JunimoNoteMenu.fromGameMenu))),
			new CodeMatch(i => i.opcode == OpCodes.Brtrue_S || i.opcode == OpCodes.Brtrue), // skip

			new CodeMatch(OpCodes.Ldarg_0),
			new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(JunimoNoteMenu), nameof(JunimoNoteMenu.fromThisMenu))),
			new CodeMatch(i => i.opcode == OpCodes.Brtrue_S || i.opcode == OpCodes.Brtrue), // skip

			new CodeMatch(OpCodes.Ldarg_0),
			new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(JunimoNoteMenu), "openRewardsMenu"))
		)
			.ThrowIfNotMatch($"Could not find entry point for {(Constants.TargetPlatform == GamePlatform.Android ? "releaseLeftClick" : "receiveLeftClick")}")
			.Advance(2)
			.Set(OpCodes.Pop, null)
			.Advance(3)
			.Set(OpCodes.Pop, null); // fromThisMenu will be assigned true in SwapPage

		return matcher.InstructionEnumeration();
	}

	// allow the rewards menu to be opened
	// specified for Android
	public static IEnumerable<CodeInstruction> DoNonSpecificBundlePageJoystick_Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		/*
            if (this.presentButton != null && !this.fromGameMenu && !this.fromThisMenu)
            {
                if (b == Buttons.A)
                {
                    this.openRewardsMenu();
                    this.highlightedBundle = -1;
                }
                return;
            }
        */

		CodeMatcher matcher = new(instructions);
		matcher.MatchStartForward(
			new CodeMatch(OpCodes.Ldarg_0),
			new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(JunimoNoteMenu), nameof(JunimoNoteMenu.fromGameMenu))),
			new CodeMatch(i => i.opcode == OpCodes.Brtrue_S || i.opcode == OpCodes.Brtrue), // skip

			new CodeMatch(OpCodes.Ldarg_0),
			new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(JunimoNoteMenu), nameof(JunimoNoteMenu.fromThisMenu))),
			new CodeMatch(i => i.opcode == OpCodes.Brtrue_S || i.opcode == OpCodes.Brtrue), // skip

			new CodeMatch(OpCodes.Ldarg_1),
			new CodeMatch(OpCodes.Ldc_I4, (int)Buttons.A),
			new CodeMatch(i => i.opcode == OpCodes.Bne_Un_S || i.opcode == OpCodes.Bne_Un),

			new CodeMatch(OpCodes.Ldarg_0),
			new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(JunimoNoteMenu), "openRewardsMenu"))
		)
			.ThrowIfNotMatch($"Could not find entry point for HandleLeftClick_Transpiler(step 1)")
			.Advance(2)
			.Set(OpCodes.Pop, null)
			.Advance(3)
			.Set(OpCodes.Pop, null);

		return matcher.InstructionEnumeration();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	public static bool IsLockNotHeld(JunimoNoteMenu menu)
	{
		NetMutex? mutex = ModUtilities.GetBundleMutex(menu.whichArea);
		return mutex == null || !mutex.IsLockHeld();
	}

	// reopen menu if bundles changed
	public static IEnumerable<CodeInstruction> Update_Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		//if (this.bundlesChanged && this.fromGameMenu)
		//{
		//    this.reOpenThisMenu();
		//}

		CodeMatcher matcher = new(instructions);
		matcher.MatchStartForward(
			new CodeMatch(OpCodes.Ldarg_0),
			new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(JunimoNoteMenu), nameof(JunimoNoteMenu.bundlesChanged))),
			new CodeMatch(i => i.opcode == OpCodes.Brfalse || i.opcode == OpCodes.Brfalse_S),

			new CodeMatch(OpCodes.Ldarg_0),
			new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(JunimoNoteMenu), nameof(JunimoNoteMenu.fromGameMenu))),
			new CodeMatch(i => i.opcode == OpCodes.Brfalse || i.opcode == OpCodes.Brfalse_S), // skip

			new CodeMatch(OpCodes.Ldarg_0),
			new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(JunimoNoteMenu), "reOpenThisMenu"))
		)
			.ThrowIfNotMatch($"Could not find entry point for {nameof(JunimoNoteMenu.update)}")
			.Advance(4)
			.Set(OpCodes.Call, AccessTools.Method(typeof(JunimoNoteMenuPatcher), nameof(JunimoNoteMenuPatcher.IsLockNotHeld)));

		return matcher.InstructionEnumeration();
	}

	// draw ingredientSlot with Color.White instead of Color.LightGray
	public static IEnumerable<CodeInstruction> Draw_Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		// c.draw(b, (this.fromGameMenu ? (Color.LightGray * 0.5f) : Color.White) * alpha_mult, 0.89f);

		CodeMatcher matcher = new(instructions);
		matcher.MatchStartForward(
			new CodeMatch(OpCodes.Ldarg_0),
			new CodeMatch(OpCodes.Ldfld, AccessTools.Field(typeof(JunimoNoteMenu), nameof(JunimoNoteMenu.fromGameMenu))),
			new CodeMatch(i => i.opcode == OpCodes.Brtrue_S || i.opcode == OpCodes.Brtrue), // skip

			new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(Color), nameof(Color.White))),
			new CodeMatch(i => i.opcode == OpCodes.Br_S || i.opcode == OpCodes.Br),

			new CodeMatch(OpCodes.Call, AccessTools.PropertyGetter(typeof(Color), nameof(Color.LightGray)))
		)
			.ThrowIfNotMatch($"Could not find entry point for {nameof(JunimoNoteMenu.draw)}")
			.Advance(2)
			.Set(OpCodes.Pop, null);
		return matcher.InstructionEnumeration();
	}

	public static void OnReceivedAreaCompletedMessage(AreaCompletedMessage message)
	{
		Monitor.Log($"receive AreaCompletedMessage(area={message.whichArea}, from={message.who}) to {Game1.player.UniqueMultiplayerID}", LogLevel.Trace);
		CommunityCenter cc = Game1.RequireLocation<CommunityCenter>("CommunityCenter");
		cc.areasComplete[message.whichArea] = true;
		Helper.Reflection.GetMethod(cc, "checkForMissedRewards").Invoke();
		cc.loadArea(message.whichArea);
	}

	public static void HandleRestoreArea(JunimoNoteMenu menu)
	{
		CommunityCenter cc = Game1.RequireLocation<CommunityCenter>("CommunityCenter");

		if (menu.fromGameMenu)
		{
			cc.areasComplete[menu.whichArea] = true;
			AreaCompletedMessage message = new(menu.whichArea, Game1.player.UniqueMultiplayerID);
			Helper.Multiplayer.SendMessage(message, "AreaCompletedMessage", modIDs: [ModManifest.UniqueID]);
			// it will not send msg to the Mod itself, invoke it
			JunimoNoteMenuPatcher.OnReceivedAreaCompletedMessage(message);
			Monitor.Log($"send AreaCompletedMessage(area={menu.whichArea}, who={Game1.player.UniqueMultiplayerID}", LogLevel.Trace);
		}
		else
		{
			cc.markAreaAsComplete(menu.whichArea);
			MethodInfo method = AccessTools.Method(typeof(JunimoNoteMenu), "restoreAreaOnExit");
			menu.exitFunction = (IClickableMenu.onExit)Delegate.CreateDelegate(typeof(IClickableMenu.onExit), menu, method);
		}
		cc.areaCompleteReward(menu.whichArea);
	}

	// load area immedialy instead of loading a cut scene
	public static IEnumerable<CodeInstruction> HandleRestoreArea_Transpiler(IEnumerable<CodeInstruction> instructions)
	{
		// replaced the following block with HandleRestoreArea
		/*
			// JunimoNoteMenu.receiveLeftClick / checkIfBundleIsComplete
			communityCenter.markAreaAsComplete(this.whichArea);
			base.exitFunction = restoreAreaOnExit;
			communityCenter.areaCompleteReward(this.whichArea);

			// JunimoNoteMenu.setUpMenu
			communityCenter.markAreaAsComplete(whichArea);
			base.exitFunction = restoreAreaOnExit;
			communityCenter.areaCompleteReward(whichArea);
		*/

		// IL code fragment of JunimoNoteMenu.receiveLeftClick / checkIfBundleIsComplete
		/*
			// communityCenter.markAreaAsComplete(this.whichArea);
			IL_01cd: ldloc.3
			IL_01ce: ldarg.0
			IL_01cf: ldfld int32 StardewValley.Menus.JunimoNoteMenu::whichArea
			IL_01d4: callvirt instance void StardewValley.Locations.CommunityCenter::markAreaAsComplete(int32)
			// base.exitFunction = restoreAreaOnExit;
			IL_01d9: ldarg.0
			IL_01da: ldarg.0
			IL_01db: ldftn instance void StardewValley.Menus.JunimoNoteMenu::restoreAreaOnExit()
			IL_01e1: newobj instance void StardewValley.Menus.IClickableMenu/onExit::.ctor(object, native int)
			IL_01e6: stfld class StardewValley.Menus.IClickableMenu/onExit StardewValley.Menus.IClickableMenu::exitFunction
			// communityCenter.areaCompleteReward(this.whichArea);
			IL_01eb: ldloc.3
			IL_01ec: ldarg.0
			IL_01ed: ldfld int32 StardewValley.Menus.JunimoNoteMenu::whichArea
			IL_01f2: callvirt instance void StardewValley.Locations.CommunityCenter::areaCompleteReward(int32)
		*/

		// IL code fragment of JunimoNoteMenu.setUpMenu
		/*
			// CommunityCenter communityCenter = Game1.RequireLocation<CommunityCenter>("CommunityCenter");
			IL_0386: ldstr "CommunityCenter"
			IL_038b: ldc.i4.0
			IL_038c: call !!0 StardewValley.Game1::RequireLocation<class StardewValley.Locations.CommunityCenter>(string, bool)
			// communityCenter.markAreaAsComplete(whichArea);

			IL_0391: dup
			IL_0392: ldarg.1
			IL_0393: callvirt instance void StardewValley.Locations.CommunityCenter::markAreaAsComplete(int32)
			// base.exitFunction = restoreAreaOnExit;
			IL_0398: ldarg.0
			IL_0399: ldarg.0
			IL_039a: ldftn instance void StardewValley.Menus.JunimoNoteMenu::restoreAreaOnExit()
			IL_03a0: newobj instance void StardewValley.Menus.IClickableMenu/onExit::.ctor(object, native int)
			IL_03a5: stfld class StardewValley.Menus.IClickableMenu/onExit StardewValley.Menus.IClickableMenu::exitFunction
			// communityCenter.areaCompleteReward(whichArea);
			IL_03aa: ldarg.1
			IL_03ab: callvirt instance void StardewValley.Locations.CommunityCenter::areaCompleteReward(int32)
		*/


		CodeMatcher matcher = new(instructions);

		matcher.MatchStartForward(
			new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(CommunityCenter), nameof(CommunityCenter.markAreaAsComplete)))
		).ThrowIfNotMatch($"Could not find entry point for HandleRestoreArea_Transpiler(markAreaAsComplete)");
		int start = matcher.Pos;
		matcher.MatchStartForward(
			new CodeMatch(OpCodes.Callvirt, AccessTools.Method(typeof(CommunityCenter), nameof(CommunityCenter.areaCompleteReward)))
		).ThrowIfNotMatch($"Could not find entry point for HandleRestoreArea_Transpiler(areaCompleteReward)");
		int end = matcher.Pos;
		matcher.Start().Advance(start);
		// -2: JunimoNoteMenu.setUpMenu
		// -3: JunimoNoteMenu.receiveLeftClick / checkIfBundleIsComplete
		matcher.Advance(matcher.InstructionAt(-2).opcode == OpCodes.Dup ? -2 : -3);
		matcher
			.SetAndAdvance(OpCodes.Ldarg_0, null)
			.SetAndAdvance(OpCodes.Call, AccessTools.Method(typeof(JunimoNoteMenuPatcher), nameof(JunimoNoteMenuPatcher.HandleRestoreArea)));
		foreach (int i in Enumerable.Range(0, end - matcher.Pos + 1))
		{
			matcher.SetAndAdvance(OpCodes.Nop, null);
		}
		return matcher.InstructionEnumeration();
	}
}
