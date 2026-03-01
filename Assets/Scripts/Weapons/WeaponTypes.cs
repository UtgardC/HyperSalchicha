using System;

namespace HyperManzana.Weapons
{
    public enum WeaponFireMode
    {
        Hitscan = 0,
        Projectile = 1
    }

    public enum WeaponReloadMode
    {
        Magazine = 0,
        ShellByShell = 1
    }

    [Flags]
    public enum WeaponEnhancementFlags
    {
        None = 0,
        Quantum = 1 << 0,
        Heated = 1 << 1,
        Overclocked = 1 << 2
    }
}
