using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;

namespace Fai0.StardewValleyMods.Common.Patching;

internal static class PatchHelper
{
    public static void DumpIL(IEnumerable<CodeInstruction> instructions, IMonitor Monitor)
    {
        var codeList = new List<CodeInstruction>(instructions);

        for (int i = 0; i < codeList.Count; i++)
        {
            var instr = codeList[i];
            string operandStr = instr.operand switch
            {
                Label label => "IL_????",
                MethodBase method => $"{method.DeclaringType?.Name}.{method.Name}",
                FieldInfo field => $"{field.DeclaringType?.Name}.{field.Name}",
                _ => instr.operand?.ToString() ?? ""
            };
            // head with index instead of IL address
            // because it is not easy to measure IL address which depends on operand
            Monitor.Log($"IDX_{i:04}: {instr.opcode}  {operandStr}", LogLevel.Debug);
        }
    }
}