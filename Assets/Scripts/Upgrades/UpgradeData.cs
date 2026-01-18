using UnityEngine;

[CreateAssetMenu(fileName = "New Upgrade", menuName = "Game/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    [Header("Basic Info")]
    public string upgradeName = "Upgrade";
    public string description = "Beschreibung des Upgrades";
    public Sprite icon;

    [Header("Upgrade Type")]
    public UpgradeType upgradeType;

    [Header("Movement Upgrade")]
    public float moveSpeedMultiplier = 1.5f; // z.B. 1.5 = 50% schneller

    [Header("Weapon Upgrade")]
    public BulletData weaponBulletData; // Für Spreadshot, Burst, etc.
    public float fireRateMultiplier = 1.5f; // Für schneller schießen

    [Header("Damage Upgrade")]
    public int damageMultiplier = 2; // z.B. 2 = Doppelter Schaden
}

public enum UpgradeType
{
    MoveSpeed,      // Schneller laufen
    FireRate,       // Schneller schießen
    WeaponChange,   // Waffenart ändern (Spreadshot, Burst)
    DamageDouble    // Doppelter Schaden
}