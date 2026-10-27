# Backpack and warehouse

Implemented 2026-10-26 in the existing uGUI Red Knight HUD.

## Player controls
- B or the existing backpack HUD button opens the backpack; B, Escape or X closes it.
- Approach the existing WarehouseKeeper on the town lower riverside and press E. Choose 打开仓库 in the conversation.
- The warehouse shows backpack items on the left and warehouse items on the right. Select a stack, then use the left 存入仓库 or right 取回背包 button. Transfers move the whole selected stack. No item details or quantity selector are shown in the warehouse.
- Backpack shows 20 slots and item details with Use and Discard (one item per operation). Use is enabled only for consumables with a configured effect.
- Both containers start EMPTY. No item definitions, demonstration items, pickup objects or chest rewards were installed. Existing treasure/weapon-upgrade behavior is unchanged.

## Ownership and persistence
PlayerInventory on the persistent player owns two InventoryCollection instances. Each has 20 slots; max stack is supplied by the item definition. A transfer checks all destination capacity before changing the source. Both containers are saved together under PlayerPrefs key Yongshi.Inventory.v1 after a mutation. They survive scene travel/application restart. Invalid or newer saves are preserved and mutations blocked; unknown saved IDs remain stored and retrievable.

InventoryWindows on Red Knight HUD builds the runtime modal UI with the user's existing IronFrame sprite and TMP font. It owns pause and player-action restoration across bag, keeper conversation and warehouse. Town navigation and the old HUD respect this modal. The temporary TestWeaponSwap B-key option is disabled; the component's existing default idle-sword rendering remains to avoid removing the player's idle weapon.

WarehouseKeeper is attached to the existing authored NPC instance in SampleScene_25. The prefab artwork/Animator and town geometry were not rebuilt. E requires proximity; there is no remote warehouse hotkey.

## Future item authoring
Create a ScriptableObject with Assets > Create > Game > Inventory Item. Set a stable unique id, displayName, description, icon, maxStack and optional useEffect/restoreAmount. Register it in the itemDefinitions array of PlayerInventory on the persistent player. A future pickup can call PlayerInventory.TryAdd(id, quantity); false means unregistered item, invalid amount, unreadable save or insufficient room. No equipment/stat/appearance swapping was implemented as part of these container systems.

## Changed integration
Assets/Scenes/persistent.unity: PlayerInventory and InventoryWindows references, HUD/navigation modal coordination, temporary B test disabled.
Assets/Scenes/SampleScene_25.unity: WarehouseKeeper interaction component only.
Assets/Script/UI: item definitions, bounded stack rules, saved containers, modal UI and keeper interaction.
PlayerStamina.Restore supports future stamina consumables.
Existing GameUI translation tables and UITranslations.txt contain the new simplified Chinese/English/traditional Chinese UI strings.

## Validation
- Unity Editor compilation passed; no missing scripts in the saved persistent scene.
- Tools > Inventory > Validate Data and Persistence passed 16 assertions: stack splitting, rejected invalid amounts, full-container atomic transfer failure, merge into partial stacks, deposit/withdraw conservation, save/reload and initially empty containers. The validator restores the original PlayerPrefs value in finally.
- Input System queued B press/release: opens empty bag, pauses time/disables gameplay actions, repeated B closes and restores time/actions. Temporary weapon swap remains false.
- Input System queued E near the authored NPC opens conversation; clicking Open Warehouse opens empty warehouse. Both transfer buttons disabled with no item selected.
- Input System queued Escape closes warehouse without reopening the old HUD pause modal; time and gameplay actions restored.
- Temporary in-memory item definition used to exercise actual slot/button handlers: seven items deposited/withdrawn without duplication; health consumable heals 40 to 90 and consumes one; discard removes one. All temporary items, definitions, health and saved inventory data restored in finally.
- Restarted Play Mode: both containers still empty, definition array empty, no inventory save key left by validation.
- English WAREHOUSE and traditional 倉庫管理員 translations verified and original language restored.
- Final runtime screenshots: BackpackFinal.png, WarehouseFinal.png. Play Mode stopped after verification.
- Editor Play Mode only; no standalone build was produced.
