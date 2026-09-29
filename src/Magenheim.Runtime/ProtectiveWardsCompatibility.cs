using System;
using System.Reflection;
using HarmonyLib;

namespace Magenheim.Runtime;

[HarmonyPatch]
internal static class ProtectiveWardsWardTaxiRpcCompatibilityPatch
{
    private const string WardTaxiTypeName = "ProtectiveWards.WardTaxi, ProtectiveWards";

    internal static bool Prepare() => Type.GetType(WardTaxiTypeName, throwOnError: false) is not null;

    internal static MethodBase TargetMethod()
    {
        var type = Type.GetType(WardTaxiTypeName, throwOnError: false)
            ?? throw new TypeLoadException(WardTaxiTypeName);
        return type.GetMethod(
                   "RegisterRPCs",
                   BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                   binder: null,
                   types: Type.EmptyTypes,
                   modifiers: null)
            ?? throw new MissingMethodException(type.FullName, "RegisterRPCs()");
    }

    private static bool Prefix() =>
        ValheimWorldInstanceExecution.Active is not { InstanceId: { IsUnderworld: true } };
}
