using StardewValley.Network;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using System.Collections.Generic;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Fai0.StardewValleyMods.DonateBundleAnywhere;

internal static class ModUtilities
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static IEnumerable<CodeInstruction> Update_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        CodeMatcher matcher = new(instructions);
        MethodInfo updateFromLocationInfo = AccessTools.Method(typeof(NetMutex), nameof(NetMutex.Update), [typeof(GameLocation)]);
        MethodInfo mutexUpdateInfo = AccessTools.Method(typeof(ModUtilities), nameof(ModUtilities.Update));

        matcher.MatchStartForward(
            new CodeMatch(operand: updateFromLocationInfo)
        )
        .ThrowIfNotMatch("Could not find entry point for Update_Transpiler")
        .Set(OpCodes.Call, mutexUpdateInfo);

        return matcher.InstructionEnumeration();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void Update(NetMutex mutex, GameLocation location)
    {
        // mutex will be released if owner disconnect with server
        mutex.Update(Game1.getOnlineFarmers());
    }

    public static NetMutex? GetBundleMutex(int whichArea)
    {
        if (whichArea < CommunityCenter.AREA_Pantry || whichArea > CommunityCenter.AREA_AbandonedJojaMart)
            return null;
        if (whichArea == CommunityCenter.AREA_AbandonedJojaMart)
        {
            AbandonedJojaMart ajm = Game1.RequireLocation<AbandonedJojaMart>("AbandonedJojaMart");
            return ajm.bundleMutex;
        }
        else
        {
            CommunityCenter cc = Game1.RequireLocation<CommunityCenter>("CommunityCenter");
            return cc.bundleMutexes[whichArea];
        }
    }

    public static string MenuDetails(JunimoNoteMenu? menu)
    {
        if (menu == null)
        {
            return "";
        }
        string area = menu.whichArea == -1 ? "Raccoon" : CommunityCenter.getAreaEnglishDisplayNameFromNumber(menu.whichArea);
        return $"(area={area}, fromGameMenu={menu.fromGameMenu}, fromThisMenu={menu.fromThisMenu})";
    }
}
