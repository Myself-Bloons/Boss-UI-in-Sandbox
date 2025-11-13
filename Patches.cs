using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Bloons;
using Il2CppAssets.Scripts.Models.Difficulty;
using Il2CppAssets.Scripts.Simulation.Bloons;
using Il2CppAssets.Scripts.Unity;
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

[HarmonyPatch(typeof(BossUI), nameof(BossUI.SetArriveInTxt))]
internal static class BossUI_SetArriveInTxt
{
    [HarmonyPrefix]
    private static bool Prefix(BossUI __instance)
    {
        if (Main.SandboxBossActive)
        {
            if (__instance.bossArrivalTxt != null)
            {
                __instance.bossArrivalTxt.gameObject.SetActive(false);
            }
            return false;
        }
        return true;
    }
}

[HarmonyPatch(typeof(BossUI), "ShowInBetweenBossesUI")]
internal static class BossUI_ShowInBetweenBossesUI
{
    [HarmonyPrefix]
    private static bool Prefix(BossUI __instance)
    {
        if (Main.SandboxBossActive)
        {
            if (__instance.noBossObj != null)
            {
                __instance.noBossObj.SetActive(false);
            }
            return false;
        }
        return true;
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

[HarmonyPatch(typeof(UnityToSimulation), nameof(UnityToSimulation.GetBossBloonTier))]
internal static class UnityToSimulation_GetBossBloonTier
{
    [HarmonyPrefix]
    private static bool Prefix(UnityToSimulation __instance, ref Il2CppSystem.Nullable<int> __result)
    {
        if (!Main.SandboxBossActive) return true;

        var spawner = Main.GetSpawnerFromSimulation(__instance.simulation);
        if (spawner?.bossBloonManager != null)
        {
            int tier = spawner.bossBloonManager.CurrentBossTier;
            __result = new Il2CppSystem.Nullable<int>(tier);
            return false;
        }

        __result = new Il2CppSystem.Nullable<int>();
        return false;
    }
}

[HarmonyPatch(typeof(BossUI), nameof(BossUI.HideShowOnPlacement))]
internal static class BossUI_HideShowOnPlacement
{
    [HarmonyPrefix]
    private static bool Prefix(BossUI __instance)
    {
        if (!Main.SandboxBossActive) return true;

        if (__instance.noBossObj != null)
        {
            var parent = __instance.noBossObj.transform.parent;
            if (parent?.gameObject.name == Main.BossWaitContainer)
            {
                parent.gameObject.SetActive(false);
            }
            else
            {
                __instance.noBossObj.SetActive(false);
            }
        }

        return false;
    }
}

[HarmonyPatch(typeof(InGame), nameof(InGame.RetrieveTopScoreAndPostAnalytics))]
internal static class InGame_RetrieveTopScoreAndPostAnalytics
{
    [HarmonyPrefix]
    private static bool Prefix()
    {
        return !Main.SandboxBossActive;
    }
}
