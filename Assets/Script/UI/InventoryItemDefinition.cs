using UnityEngine;

[CreateAssetMenu(menuName = "Game/Inventory Item", fileName = "New Inventory Item")]
public sealed class InventoryItemDefinition : ScriptableObject
{
    public string id;
    public string displayName;
    [TextArea] public string description;
    public Sprite icon;
    public enum Rarity { Common, Fine, Rare, Epic, Legendary }
    public Rarity rarity;
    [Header("Equipment (frame-locked weapons)")]
    public TestWeaponSwap.WeaponFrame[] weaponFrames;
    public bool IsWeapon => weaponFrames != null && weaponFrames.Length > 0;
    [Min(1)] public int maxStack = 99;
    public enum UseEffect { None, RestoreHealth, RestoreStamina }
    public UseEffect useEffect;
    [Min(0)] public float restoreAmount = 50;
}
