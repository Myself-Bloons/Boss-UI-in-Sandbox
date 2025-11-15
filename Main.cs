using System;
using MelonLoader;
using BTD_Mod_Helper;
using BTD_Mod_Helper.Extensions;
using Il2Cpp;
using Il2CppAssets.Scripts.Models;
using Il2CppAssets.Scripts.Models.Bloons;
using Il2CppAssets.Scripts.Models.Difficulty;
using Il2CppAssets.Scripts.Simulation.Bloons;
using Il2CppAssets.Scripts.Unity;
using Il2CppAssets.Scripts.Unity.Audio;
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
    internal BossUI? gameBossUI;
    internal int currentBossTier = 1;

    internal const string BossUiName = "BossUI";
    internal const string BossWaitContainer = "AnimatedContainerBossWait";
    internal const string AnimatorState = "VisibleState";

    public override void OnApplicationStart()
    {
        Instance = this;
        ModHelper.Msg<Main>("BossUi in Sandbox Loaded.");
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
            currentBossTier = ExtractTierFromName(model.name ?? model.id);
            SetupLeakDamage(model);
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
            _ when baseId.Contains("Dreadbloon") => BossType.Dreadbloon,
            _ when baseId.Contains("Phayze") => BossType.Phayze,
            _ when baseId.Contains("Blastapopoulos") => BossType.Blastapopoulos,
            _ => null
        };
    }

    private int ExtractTierFromName(string name)
    {
        if (string.IsNullOrEmpty(name)) return 1;
        var lastChar = name[^1];
        return char.IsDigit(lastChar) ? lastChar - '0' : 1;
    }

    private void SetupLeakDamage(BloonModel model)
    {
        if (model.leakDamage <= 0 && currentBossType.HasValue)
        {
            model.leakDamage = BossLeakDamageHelper.GetBossLeakDamage(
                currentBossType.Value,
                currentBossTier,
                isElite,
                model
            );
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
                MelonCoroutines.Start(LoadBossUICoroutine(inGame));
            }
        }
        else
        {
            SandboxBossActive = true;
        }
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

        SandboxBossActive = true;

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

            gameBossUI.ShowSkulls();
            gameBossUI.SetupArmourBars();
            gameBossUI.UpdateArmourUI();
            gameBossUI.UpdateHealthAndShield();

            if (!gameBossUI.gameObject.active)
            {
                gameBossUI.gameObject.SetActive(true);
            }
        }

        PlayBossMusic();
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

        ConfigureBossStars(bossUI, currentBossTier);
    }

    private void ConfigureBossStars(BossUI bossUI, int tier)
    {
        if (bossUI?.stars == null) return;

        int maxStars = Math.Min(bossUI.stars.Length, 5);

        for (int i = 0; i < maxStars; i++)
        {
            var star = bossUI.stars[i];
            if (star?.gameObject == null) continue;

            star.gameObject.SetActive(true);
            star.color = (i < tier) ? bossUI.starColorOn : bossUI.starColorOff;
        }
    }

    private void PlayBossMusic()
    {
        if (!currentBossType.HasValue) return;

        try
        {
            var audioFactory = Game.instance?.audioFactory;
            if (audioFactory == null) return;

            var bossData = GameData.Instance.bosses.GetBossData(currentBossType.Value);
            var bossClip = bossData?.musicTrack?.Clip;

            if (bossClip != null)
            {
                audioFactory.StopMusic();
                audioFactory.PlayMusic(bossClip);

                if (audioFactory.musicFactory != null)
                {
                    audioFactory.musicFactory.bossMusicClip = bossClip;
                    audioFactory.musicFactory.BossMusicIsPlaying = true;
                }
            }
        }
        catch
        {
        }
    }

    private void StopBossMusic()
    {
        try
        {
            var audioFactory = Game.instance?.audioFactory;
            if (audioFactory != null)
            {
                audioFactory.FadeMusic();

                if (audioFactory.musicFactory != null)
                {
                    audioFactory.musicFactory.BossMusicIsPlaying = false;
                    audioFactory.musicFactory.bossMusicClip = null;
                    audioFactory.musicFactory.StartInGameMusic();
                }
            }
        }
        catch
        {
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
        currentBossTier = 1;

        StopBossMusic();
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
}