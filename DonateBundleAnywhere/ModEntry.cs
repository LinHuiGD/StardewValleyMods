using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.Menus;
using Pathoschild.Stardew.Common.Patching;
using Fai0.StardewValleyMods.DonateBundleAnywhere.Patches;
using StardewValley.Locations;

namespace Fai0.StardewValleyMods.DonateBundleAnywhere;

/// <summary>
/// Not intended to release this mod, just for fun.
/// </summary>
internal class ModEntry : Mod
{

    public override void Entry(IModHelper helper)
    {
        helper.Events.Display.MenuChanged += OnMenuChanged;

        HarmonyPatcher.Apply(this,
            new JunimoNoteMenuPatcher(Monitor)
        );

        helper.ConsoleCommands.Add("donate-bundle", "TODO\n\nUsage: TODO [help|TODO]", this.OnCommand);
    }

    private void OnMenuChanged(object? sender, MenuChangedEventArgs e)
    {
        Monitor.Log($"Menu changed: {e.OldMenu?.GetType().Name} -> {e.NewMenu?.GetType().Name}", LogLevel.Debug);
        if (!Context.IsWorldReady || e.NewMenu is not JunimoNoteMenu menu)
            return;
        //foreach (Bundle bundle in menu.bundles)
        //{
        //    bundle.depositsAllowed = true;
        //}
    }

    private void OnCommand(string command, string[] args)
    {
        Monitor.Log($"OnCommand, args={args}", LogLevel.Info);
        string subCMD = args[0];
        int baseIndex = 1;
        if (subCMD.Equals("check"))
        {
            int area = ArgUtility.GetInt(args, baseIndex, 1);
            CommunityCenter cc = Game1.RequireLocation<CommunityCenter>("CommunityCenter");
            cc.checkBundle(area);
            Monitor.Log($"checkbundle, area={CommunityCenter.getAreaDisplayNameFromNumber(area)}", LogLevel.Info);
            // it worked, but there is a trivial issue.
            // For example, after donating all bundles of an area, if the player immediately enter the community center, it can move and Junimo's animation is playing.
            // However, the original intention of ConcernedApe should be that the player cannot move when playing the Junimo's animation.
        }
    }
}
