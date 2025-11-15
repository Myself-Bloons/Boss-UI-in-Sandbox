using System.Collections.Generic;
using Il2CppAssets.Scripts.Data.Boss;
using Il2CppAssets.Scripts.Models.Bloons;

namespace BossUIinSandbox;

internal static class BossLeakDamageHelper
{
    private static readonly Dictionary<(BossType, int, bool), float> KnownBossLeakDamage = new()
    {
        { (BossType.Bloonarius, 1, false), 250f },
        { (BossType.Bloonarius, 2, false), 1250f },
        { (BossType.Bloonarius, 3, false), 6250f },
        { (BossType.Bloonarius, 4, false), 31250f },
        { (BossType.Bloonarius, 5, false), 156250f },

        { (BossType.Bloonarius, 1, true), 500f },
        { (BossType.Bloonarius, 2, true), 2500f },
        { (BossType.Bloonarius, 3, true), 12500f },
        { (BossType.Bloonarius, 4, true), 62500f },
        { (BossType.Bloonarius, 5, true), 312500f },

        { (BossType.Lych, 1, false), 250f },
        { (BossType.Lych, 2, false), 1250f },
        { (BossType.Lych, 3, false), 6250f },
        { (BossType.Lych, 4, false), 31250f },
        { (BossType.Lych, 5, false), 156250f },

        { (BossType.Lych, 1, true), 500f },
        { (BossType.Lych, 2, true), 2500f },
        { (BossType.Lych, 3, true), 12500f },
        { (BossType.Lych, 4, true), 62500f },
        { (BossType.Lych, 5, true), 312500f },

        { (BossType.Vortex, 1, false), 200f },
        { (BossType.Vortex, 2, false), 1000f },
        { (BossType.Vortex, 3, false), 5000f },
        { (BossType.Vortex, 4, false), 25000f },
        { (BossType.Vortex, 5, false), 125000f },

        { (BossType.Vortex, 1, true), 400f },
        { (BossType.Vortex, 2, true), 2000f },
        { (BossType.Vortex, 3, true), 10000f },
        { (BossType.Vortex, 4, true), 50000f },
        { (BossType.Vortex, 5, true), 250000f },

        { (BossType.Dreadbloon, 1, false), 300f },
        { (BossType.Dreadbloon, 2, false), 1500f },
        { (BossType.Dreadbloon, 3, false), 7500f },
        { (BossType.Dreadbloon, 4, false), 37500f },
        { (BossType.Dreadbloon, 5, false), 187500f },

        { (BossType.Dreadbloon, 1, true), 600f },
        { (BossType.Dreadbloon, 2, true), 3000f },
        { (BossType.Dreadbloon, 3, true), 15000f },
        { (BossType.Dreadbloon, 4, true), 75000f },
        { (BossType.Dreadbloon, 5, true), 375000f },

        { (BossType.Phayze, 1, false), 200f },
        { (BossType.Phayze, 2, false), 1000f },
        { (BossType.Phayze, 3, false), 5000f },
        { (BossType.Phayze, 4, false), 25000f },
        { (BossType.Phayze, 5, false), 125000f },

        { (BossType.Phayze, 1, true), 400f },
        { (BossType.Phayze, 2, true), 2000f },
        { (BossType.Phayze, 3, true), 10000f },
        { (BossType.Phayze, 4, true), 50000f },
        { (BossType.Phayze, 5, true), 250000f },

        { (BossType.Blastapopoulos, 1, false), 250f },
        { (BossType.Blastapopoulos, 2, false), 1250f },
        { (BossType.Blastapopoulos, 3, false), 6250f },
        { (BossType.Blastapopoulos, 4, false), 31250f },
        { (BossType.Blastapopoulos, 5, false), 156250f },

        { (BossType.Blastapopoulos, 1, true), 500f },
        { (BossType.Blastapopoulos, 2, true), 2500f },
        { (BossType.Blastapopoulos, 3, true), 12500f },
        { (BossType.Blastapopoulos, 4, true), 62500f },
        { (BossType.Blastapopoulos, 5, true), 312500f },
    };

    public static float GetBossLeakDamage(BossType bossType, int tier, bool isElite, BloonModel model)
    {
        if (model.leakDamage > 0)
        {
            return model.leakDamage;
        }

        if (KnownBossLeakDamage.TryGetValue((bossType, tier, isElite), out float knownDamage))
        {
            return knownDamage;
        }

        return GetEstimatedLeakDamage(tier, isElite);
    }

    private static float GetEstimatedLeakDamage(int tier, bool isElite)
    {
        float baseDamage = tier switch
        {
            1 => 200f,
            2 => 1000f,
            3 => 5000f,
            4 => 25000f,
            5 => 125000f,
            _ => 200f
        };

        return isElite ? baseDamage * 2f : baseDamage;
    }
}