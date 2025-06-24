using System;
using System.Runtime.CompilerServices;
using Fai0.StardewValleyMods.Common;
using HarmonyLib;
using Pathoschild.Stardew.Common.Patching;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Network;

namespace Fai0.StardewValleyMods.DonateBundleAnywhere.Patches;

/// <summary>
/// Auto request lock on setting JunimoNoteMenu to Game1.activeClickableMenu.
/// </summary>
internal class Game1Patcher : BasePatcher
{
    private static IMonitor Monitor = null!;
    private static HUDMessageHelper MsgHelper = null!;
    public Game1Patcher(IMonitor monitor, HUDMessageHelper msgHelper)
    {
        Monitor = monitor;
        MsgHelper = msgHelper;
    }

    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: AccessTools.PropertySetter(typeof(Game1), nameof(Game1.activeClickableMenu)),
            prefix: new HarmonyMethod(typeof(Game1Patcher), nameof(Game1Patcher.ActiveClickableMenuSetter_Prefix)),
            postfix: new HarmonyMethod(typeof(Game1Patcher), nameof(Game1Patcher.ActiveClickableMenuSetter_Postfix))
        );
    }

    // release lock for remote donation
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ActiveClickableMenuSetter_Prefix(Game1 __instance, IClickableMenu value)
    {
        try
        {
            if (Game1.activeClickableMenu is not JunimoNoteMenu old)
                return;

            Monitor.Log($"unsetting JunimoNoteMenu{ModUtilities.MenuDetails(old)}.", LogLevel.Trace);

            NetMutex? mutex = ModUtilities.GetBundleMutex(old.whichArea);
            if (mutex == null)
            {
                if (old.whichArea != -1) Monitor.Log("mutex is null.", LogLevel.Error);
                return;
            }
            if (mutex.IsLocked())
            {
                if (!mutex.IsLockHeld())
                {
                    Monitor.Log("lock held by other.", LogLevel.Trace);
                }
                else
                {
                    if (value is ItemGrabMenu)
                    {
                        Monitor.Log("keep lock for ItemGrabMenu.", LogLevel.Trace);
                    }
                    else
                    {
                        Monitor.Log("release lock.", LogLevel.Trace);
                        mutex.ReleaseLock();
                    }
                }
            }
            else
            {
                // known logics calling ReleaseLock:
                //  - Raccoon:
                //      - released by behaviorBeforeCleanup before unsetting JunimoNoteMenu
                //      - released on dayUpdate/bundleCompleteAfterSwipe
                Monitor.Log("unmagened ReleaseLock called.", LogLevel.Trace);
            }
        }
        catch (Exception e)
        {
            Monitor.Log($"Failed in {nameof(ActiveClickableMenuSetter_Prefix)}:\n{e}", LogLevel.Error);
        }

    }

    // request lock for remote donation
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ActiveClickableMenuSetter_Postfix(Game1 __instance, IClickableMenu value)
    {
        try
        {
            if (value is not JunimoNoteMenu @new)
                return;
            JunimoNoteMenuPatcher.CanClick = true;
            Monitor.Log($"setting JunimoNoteMenu{ModUtilities.MenuDetails(@new)}.", LogLevel.Trace);
            if (@new.fromGameMenu)
            {
                NetMutex? mutex = ModUtilities.GetBundleMutex(@new.whichArea);
                if (mutex == null)
                {
                    if (@new.whichArea != -1) Monitor.Log("mutex is null.", LogLevel.Error);
                    return;
                }
                JunimoNoteMenuPatcher.CanClick = false;
                if (!mutex.IsLocked())
                {
                    mutex.RequestLock(
                        acquired: delegate
                        {
                            if (Game1.activeClickableMenu is JunimoNoteMenu)
                            {
                                JunimoNoteMenuPatcher.CanClick = true;
                            }
                            Monitor.Log("succced to acquire lock.", LogLevel.Trace);
                        },
                        failed: delegate
                        {
                            if (Game1.activeClickableMenu is JunimoNoteMenu _menu)
                            {
                                MsgHelper.AddHUDMessage(new HUDMessage(I18n.Hud_Msg_BundleAreaBusy(CommunityCenter.getAreaDisplayNameFromNumber(_menu.whichArea)), HUDMessage.error_type));
                            }
                            Monitor.Log("failed to acquire lock.", LogLevel.Trace);
                        }
                    );
                    Monitor.Log("request lock.", LogLevel.Trace);
                }
                else
                {
                    if (!mutex.IsLockHeld())
                    {
                        MsgHelper.AddHUDMessage(new HUDMessage(I18n.Hud_Msg_BundleAreaBusy(CommunityCenter.getAreaDisplayNameFromNumber(@new.whichArea)), HUDMessage.error_type));
                        Monitor.Log("lock already held by other.", LogLevel.Trace);
                    }
                    else
                    {
                        JunimoNoteMenuPatcher.CanClick = true;
                        Monitor.Log("already owned this lock.", LogLevel.Trace);
                    }
                }
            }
            else
            {
                // knwon logics calling RequestLock (active JunimoNoteMenu by other methods than GameMenu):
                //  - touching JunimoNote(golden scrolls) in CommunityCenter or AbandonedJojaMart
                //  - Raccoon: 
                //      - talking to raccoon (it depends on another NetField called SeasonOfCurrentRacconBundle)
                //      - clicked the raccoon icon this mod added(same as previous one)
                Monitor.Log("unmagened RequestLock.", LogLevel.Trace);
            }
        }
        catch (Exception e)
        {
            Monitor.Log($"Failed in {nameof(ActiveClickableMenuSetter_Postfix)}:\n{e}", LogLevel.Error);
        }

    }
}
