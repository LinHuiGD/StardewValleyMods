using StardewModdingAPI;
using System;
using System.Reflection;
namespace Fai0.StardewValleyMods.Common;

internal static class DebugHelper
{
    public static string Dump(object obj)
    {
        if (obj == null) throw new System.ArgumentNullException(nameof(obj));

        System.Type type = obj.GetType();
        var output = new System.Text.StringBuilder();

        // class header
        output.AppendLine("");
        output.AppendLine("==========================");
        output.AppendLine($"[{type.Name}] Instance");

        // binding
        var flags = BindingFlags.Public | BindingFlags.Instance;

        // fields
        output.AppendLine("[Fields]");
        foreach (FieldInfo field in type.GetFields(flags))
        {
            output.AppendLine($"{field.Name}, {field.GetValue(obj)}");
        }

        // Properties 
        output.AppendLine("[Properties]");
        foreach (PropertyInfo prop in type.GetProperties(flags))
        {
            if (prop.GetIndexParameters().Length > 0) continue; // skip indexes
            output.AppendLine($"{prop.Name}, {prop.GetValue(obj)}");
        }

        return output.ToString();
    }

    public static FieldInfo? GetFieldIncludingBase(Type? type, string fieldName)
    {
        while (type is not null)
        {
            FieldInfo? field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null) return field;
            type = type.BaseType;
        }
        return null;
    }

    public static void SetPrivateField(object obj, string fieldName, object value)
    {
        if (obj == null) throw new System.ArgumentNullException(nameof(obj));
        System.Type type = obj.GetType();
        FieldInfo? field = GetFieldIncludingBase(type, fieldName);
        if (field == null)
        {
            throw new System.ArgumentException($"Field '{fieldName}' not found in type '{type.Name}'.");
        }
        field.SetValue(obj, value);
    }
}