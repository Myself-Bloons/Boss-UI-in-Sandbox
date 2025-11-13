using System;
using MelonLoader;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Extensions;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Difficulty;
using Il2CppAssets.Scripts.Simulation.Bloons;
using Il2CppAssets.Scripts.Simulation.Track;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Bridge;
using Il2CppAssets.Scripts.Unity.UI_New.InGame;
using Il2CppAssets.Scripts.Unity.UI_New.InGame.Races;
using Il2CppAssets.Scripts.Data;
using Il2CppAssets.Scripts.Data.Boss;
using MelonLoader.Utils;

[assembly: MelonInfo(typeof(BossUIinSandbox.Main), BossUIinSandbox.ModHelperData.Name, BossUIinSandbox.ModHelperData.Version, BossUIinSandbox.ModHelperData.Author)]
[assembly: MelonGame("Ninja Kiwi", "BloonsTD6")]

namespace BossUIinSandbox;

public class Main : BloonsTD6Mod
{
    internal static bool SandboxBossActive;
    internal static Main Instance { get; private set; } = null!;

    internal BossType? currentBossType;
    internal bool isElite;
    internal Bloon? currentBoss;
    private BossUI? gameBossUI;

    internal static System.Reflection.FieldInfo? _spawnerField;
    internal static System.Reflection.FieldInfo? _currentBossTierField;
    internal static readonly System.Reflection.BindingFlags _allInstanceFlags =
        System.Reflection.BindingFlags.Public |
        System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Instance;
    internal static readonly System.Reflection.BindingFlags _privateInstanceFlags =
        System.Reflection.BindingFlags.NonPublic |
        System.Reflection.BindingFlags.Instance;

    internal const string BossUiName = "BossUI";
    internal const string BossWaitContainer = "AnimatedContainerBossWait";
    internal const string AnimatorState = "VisibleState";

    public override void OnApplicationStart()
    {
        Instance = this;
        ModHelper.Msg<Main>("Bosses in Sandbox Loaded.");
    }

    internal static Spawner GetSpawnerFromSimulation(Il2CppAssets.Scripts.Simulation.Simulation simulation)
    {
        _spawnerField ??= typeof(Il2CppAssets.Scripts.Simulation.Simulation).GetField("spawner", _allInstanceFlags);
        return _spawnerField?.GetValue(simulation) as Spawner;
    }

    internal static void SetCurrentBossTier(BossBloonManager bossManager, int tier)
    {
        _currentBossTierField ??= typeof(BossBloonManager).GetField("currentBossTier", _privateInstanceFlags);
        _currentBossTierField?.SetValue(bossManager, tier);
    }

    private void DestroyExistingBossUI()
    {
        if (gameBossUI == null) return;

        if (gameBossUI.bossAnim != null)
        {
            gameBossUI.bossAnim.SetInteger(AnimatorState, 0);
        }
        if (gameBossUI.noBossAnim != null)
        {
            gameBossUI.noBossAnim.SetInteger(AnimatorState, 0);
        }

        if (gameBossUI.gameObject != null)
        {
            gameBossUI.gameObject.SetActive(false);
            UnityEngine.Object.DestroyImmediate(gameBossUI.gameObject);
        }

        gameBossUI = null;
    }

    private void ConfigureBossStars(BossUI bossUI, int tier)
    {
        if (bossUI?.stars == null) return;

        int maxStars = Math.Min(bossUI.stars.Length, 5);

        for (int i = 0; i < maxStars; i++)
        {
            if (bossUI.stars[i]?.gameObject == null) continue;

            bossUI.stars[i].gameObject.SetActive(true);
            bossUI.stars[i].color = (i < tier) ? bossUI.starColorOn : bossUI.starColorOff;
        }
    }

    public override void OnMatchStart()
    {
        CleanupBossUI();
    }

    public override void OnMatchEnd()
    {
        CleanupBossUI();
    }

    public override void OnRestart()
    {
        CleanupBossUI();
    }

    public override void OnUpdate()
    {
        if (!SandboxBossActive) return;

        if (currentBoss == null || currentBoss.IsDestroyed || currentBoss.bloonModel == null || currentBoss.health <= 0)
        {
            CleanupBossUI();
        }
    }

    private void CleanupBossUI()
    {
        var editableData = InGameData.Editable;
        if (editableData != null &&
            (editableData.gameType == GameType.BossBloon ||
             editableData.gameType == GameType.BossRush ||
             editableData.gameType == GameType.ContestedTerritory ||
             !string.IsNullOrEmpty(editableData.gameEventId)))
        {
            SandboxBossActive = false;
            currentBoss = null;
            currentBossType = null;
            isElite = false;
            return;
        }

        if (!SandboxBossActive && currentBoss == null && currentBossType == null) return;

        SandboxBossActive = false;
        currentBoss = null;
        currentBossType = null;
        isElite = false;

        DestroyExistingBossUI();

        var editable = InGameData.Editable;
        if (editable != null)
        {
            editable.gameType = GameType.Standard;
            editable.subGameType = SubGameType.NotSet;
            editable.bossData = null;
            editable.dcModel = null;
            editable.gameEventId = null;
            editable.CreateReadonlyCopyForGame();
        }
    }

    public override void OnBloonCreated(Bloon bloon)
    {
        if (bloon?.bloonModel == null || !bloon.bloonModel.isBoss) return;

        if (!SandboxBossActive)
        {
            var editableData = InGameData.Editable;
            if (editableData == null) return;

            if (editableData.gameType == GameType.BossBloon ||
                editableData.gameType == GameType.BossRush ||
                editableData.gameType == GameType.ContestedTerritory ||
                !string.IsNullOrEmpty(editableData.gameEventId))
            {
                return;
            }

            if (editableData.gameType != GameType.Standard && editableData.gameType != GameType.BossChallenge) return;
        }

        var model = bloon.bloonModel;
        DetectBossType(model.baseId);

        if (currentBossType.HasValue)
        {
            currentBoss = bloon;
            int tier = ExtractTierFromName(model.name);

            // Leaj damage for testing and seeing leaks (might adjust to find real values and in game method later)
            if (model.leakDamage <= 0)
            {
                model.leakDamage = tier switch
                {
                    1 => 200f,
                    2 => 1000f,
                    3 => 7500f,
                    4 => 60000f,
                    5 => 450000f,
                    _ => 200f
                };
            }

            SetBossManagerTier(tier);
            SetupBossModeAndActivateUI();
        }
    }

    private void DetectBossType(string baseId)
    {
        isElite = baseId.Contains("Elite");

        currentBossType = baseId switch
        {
            _ when baseId.Contains("Bloonarius") => BossType.Bloonarius,
            _ when baseId.Contains("Lych") => BossType.Lych,
            _ when baseId.Contains("Vortex") => BossType.Vortex,
            _ when baseId.Contains("Dreadbloon") || baseId.Contains("Dread") => BossType.Dreadbloon,
            _ when baseId.Contains("Phayze") => BossType.Phayze,
            _ when baseId.Contains("Blasta") => BossType.Blastapopoulos,
            _ => null
        };
    }

    private int ExtractTierFromName(string name)
    {
        if (string.IsNullOrEmpty(name)) return 1;
        var lastChar = name[^1];
        return char.IsDigit(lastChar) ? lastChar - '0' : 1;
    }

    private void SetBossManagerTier(int tier)
    {
        var simulation = InGame.instance.bridge.simulation;
        var spawner = GetSpawnerFromSimulation(simulation);

        if (spawner?.bossBloonManager != null)
        {
            SetCurrentBossTier(spawner.bossBloonManager, tier);
        }
    }

    private void SetupBossModeAndActivateUI()
    {
        var editableData = InGameData.Editable;
        if (editableData == null || editableData.gameType != GameType.Standard) return;

        if (gameBossUI == null)
        {
            var inGame = InGame.instance;
            if (inGame != null)
            {
                SandboxBossActive = true;
                MelonCoroutines.Start(LoadBossUICoroutine(inGame));
            }
        }
    }

    private void ApplyBossUISettings(BossUI bossUI)
    {
        if (!currentBossType.HasValue || currentBoss == null) return;

        var bossData = GameData.Instance.bosses.GetBossData(currentBossType.Value);
        if (bossData != null)
        {
            var iconRef = isElite ? bossData.eliteHudIcon : bossData.normalHudIcon;
            if (!string.IsNullOrEmpty(iconRef.guidRef))
            {
                bossUI.bossImg.SetSprite(iconRef.guidRef);
            }
        }

        int tier = ExtractTierFromName(currentBoss.bloonModel.name);
        ConfigureBossStars(bossUI, tier);
    }

    private System.Collections.IEnumerator LoadBossUICoroutine(InGame inGame)
    {
        DestroyExistingBossUI();

        var loadCoroutine = inGame.InstantiateUiObject(BossUiName, null);
        while (loadCoroutine.MoveNext())
        {
            yield return loadCoroutine.Current;
        }

        gameBossUI = UnityEngine.Object.FindObjectOfType<BossUI>(true);
        if (gameBossUI == null) yield break;

        if (!gameBossUI.gameObject.active)
        {
            gameBossUI.gameObject.SetActive(true);
        }

        bool initializeSucceeded = true;
        var initCoroutine = gameBossUI.Initialise();
        bool hasError = false;

        while (!hasError)
        {
            bool hasNext = false;
            try
            {
                hasNext = initCoroutine.MoveNext();
            }
            catch
            {
                initializeSucceeded = false;
                hasError = true;
            }

            if (hasError || !hasNext) break;
            yield return initCoroutine.Current;
        }

        if (initializeSucceeded)
        {
            try
            {
                gameBossUI.StartMatch();
                gameBossUI.ShowBossAliveUI();
            }
            catch
            {
                initializeSucceeded = false;
            }
        }

        if (!initializeSucceeded)
        {
            ApplyBossUISettings(gameBossUI);

            if (gameBossUI.arriveBossImg != null)
            {
                gameBossUI.arriveBossImg.enabled = false;
            }

            if (gameBossUI.bossArrivalTxt?.gameObject != null)
            {
                gameBossUI.bossArrivalTxt.gameObject.SetActive(false);
            }

            if (gameBossUI.noBossObj != null)
            {
                var parent = gameBossUI.noBossObj.transform.parent;
                if (parent?.gameObject.name == BossWaitContainer)
                {
                    parent.gameObject.SetActive(false);
                }
                else
                {
                    gameBossUI.noBossObj.SetActive(false);
                }
            }

            try
            {
                gameBossUI.ShowSkulls();
                gameBossUI.SetupArmourBars();
                gameBossUI.UpdateArmourUI();
                gameBossUI.ShowBossAliveUI();
                gameBossUI.UpdateHealthAndShield();
            }
            catch
            {
            }

            if (!gameBossUI.gameObject.active)
            {
                gameBossUI.gameObject.SetActive(true);
            }
        }
    }
}
