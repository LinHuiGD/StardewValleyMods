using HarmonyLib;
using Pathoschild.Stardew.Common.Patching;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Locations;
using StardewValley.Menus;
using StardewValley.Tools;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Fai0.StardewValleyMods.DonateBundleAnywhere.Patches;

/// <summary>
/// Unused patcher, just for fun
/// </summary>
internal class FishingRodPatcher : BasePatcher
{
    private static IMonitor Monitor = null!; // set when constructor is called
    public FishingRodPatcher(IMonitor monitor)
    {
        FishingRodPatcher.Monitor = monitor;
    }
    public override void Apply(Harmony harmony, IMonitor monitor)
    {
        harmony.Patch(
            original: AccessTools.Method(typeof(FishingRod), "DoFunction"),
            transpiler: new HarmonyMethod(typeof(FishingRodPatcher), nameof(FishingRodPatcher.DoFunction_Transpiler))
        );
    }

    // Find the effective rectangular area for fishing bubbles
    public static IEnumerable<CodeInstruction> DoFunction_Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var codes = new List<CodeInstruction>(instructions);
        var monitorLog = AccessTools.Method(typeof(FishingRodPatcher), nameof(LogFlag3AndClearWaterDistance));
        // 1. Find ldfld int32 StardewValley.Tools.FishingRod::clearWaterDistance
        // 2. Find ldloc.s 15 (flag3)
        // 3. Insert logging code before calling getFish
        // 4. flag3 variable index is 15, clearWaterDistance is a field of this

        int flag3Index = -1;
        int insertIndex = -1;

        for (int i = 0; i < codes.Count - 5; i++)
        {
            // Match ldarg.0 ldfld int32 StardewValley.Tools.FishingRod::clearWaterDistance
            if (codes[i].opcode == OpCodes.Ldarg_0 &&
                codes[i + 1].opcode == OpCodes.Ldfld &&
                codes[i + 1].operand is FieldInfo fi &&
                fi.Name == "clearWaterDistance")
            {
                // Followed by ldloc.s 15
                if (codes[i + 2].opcode == OpCodes.Ldloc_S && codes[i + 2].operand is LocalBuilder lb1)
                {
                    flag3Index = ((LocalBuilder)codes[i + 2].operand).LocalIndex;
                    // Find callvirt getFish to insert before
                    for (int j = i + 2; j < codes.Count; j++)
                    {
                        if (codes[j].opcode == OpCodes.Callvirt &&
                            codes[j].operand is MethodInfo mi &&
                            mi.Name == "getFish")
                        {
                            insertIndex = j;
                            break;
                        }
                    }
                    break;
                }
            }
        }

        if (insertIndex > 0 && flag3Index >= 0)
        {
            // Insert log code
            // Load this, then flag3
            codes.Insert(insertIndex, new CodeInstruction(OpCodes.Ldarg_0)); // this
            codes.Insert(insertIndex + 1, new CodeInstruction(OpCodes.Ldloc_S, (byte)flag3Index)); // flag3
            codes.Insert(insertIndex + 2, new CodeInstruction(OpCodes.Call, monitorLog));
        }

        return codes;
    }

    // flag3 = splashPoint = fishSplashRect2.Intersects(bobberRect);
    // Log flag3 and clearWaterDistance
    public static void LogFlag3AndClearWaterDistance(FishingRod __instance, bool flag3)
    {
        Monitor.Log($"[FishingRodPatcher] clearWaterDistance={__instance.clearWaterDistance}, flag3={flag3}", LogLevel.Info);
    }
}
