using System;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Bloons;
using Il2CppAssets.Scripts.Models.Difficulty;
using Il2CppAssets.Scripts.Simulation.Bloons;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Analytics;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.Races;
using HarmonyLib;

namespace BossUIinSandbox;

[HarmonyPatch(typeof(Il2CppAssets.Scripts.Unity.UI_New.InGame.BloonMenu.BloonMenu), nameof(Il2CppAssets.Scripts.Unity.UI_New.InGame.BloonMenu.BloonMenu.CreateBloonButtons))]
internal static class BloonMenu_CreateBloonButtons
{
    [HarmonyPrefix]
    private static bool Prefix(Il2CppSystem.Collections.Generic.List<BloonModel> sortedBloons)
    {
        var editableData = InGameData.Editable;
        if (editableData?.gameType != GameType.Standard) return true;

        var allBloons = Game.instance.model.bloons;
        foreach (var bloon in allBloons)
        {
            if ((bloon.isBoss || bloon.baseId.Contains("Golden") || bloon.baseId.Contains("DreadRock")) && !sortedBloons.Contains(bloon))
            {
                sortedBloons.Add(bloon);
            }
        }
        return true;
    }
}

[HarmonyPatch(typeof(BossUI), nameof(BossUI.IsBossMode), MethodType.Getter)]
internal static class BossUI_IsBossMode
{
    [HarmonyPostfix]
    private static void Postfix(ref bool __result)
    {
        if (Main.SandboxBossActive) __result = true;
    }
}

[HarmonyPatch(typeof(BossUI), nameof(BossUI.IsSandboxOrEditorMode), MethodType.Getter)]
internal static class BossUI_IsSandboxOrEditorMode
{
    [HarmonyPostfix]
    private static void Postfix(ref bool __result)
    {
        if (Main.SandboxBossActive) __result = false;
    }
}

[HarmonyPatch(typeof(UnityToSimulation), nameof(UnityToSimulation.GetBossBloon))]
internal static class UnityToSimulation_GetBossBloon
{
    [HarmonyPostfix]
    private static void Postfix(ref Bloon __result)
    {
        if (Main.SandboxBossActive && Main.Instance?.currentBoss != null)
        {
            __result = Main.Instance.currentBoss;
        }
    }
}


[HarmonyPatch(typeof(BossUI), nameof(BossUI.HideShowOnPlacement))]
internal static class BossUI_HideShowOnPlacement
{
    [HarmonyPrefix]
    private static bool Prefix(BossUI __instance, bool hide)
    {
        if (__instance == null || Main.Instance?.gameBossUI == __instance)
        {
            return false;
        }

        if (__instance.gameObject == null || __instance.bossAnim == null || __instance.noBossAnim == null)
        {
            return false;
        }

        return true;
    }
}