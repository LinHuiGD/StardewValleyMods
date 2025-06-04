using StardewModdingAPI;
using StardewValley;
using Pathoschild.Stardew.Common.Patching;
using Fai0.StardewValleyMods.DonateBundleAnywhere.Patches;

namespace Fai0.StardewValleyMods.FishingProbe;

/// <summary>
/// Not intended to release this mod, just for fun.
/// </summary>
internal class ModEntry : Mod
{

    public override void Entry(IModHelper helper)
    {
        HarmonyPatcher.Apply(this,
            new FishingRodPatcher(Monitor)
        );

        helper.ConsoleCommands.Add("fishing-probe", "TODO\n\nUsage: TODO [help|TODO]", this.OnCommand);
    }

    private void OnCommand(string command, string[] args)
    {
        Monitor.Log($"OnCommand, args={args}", LogLevel.Info);
        string subCMD = args[0];
        int baseIndex = 1;
        if (subCMD.Equals("check"))
        {
            int area = ArgUtility.GetInt(args, baseIndex, 1);
        }
    }
}
