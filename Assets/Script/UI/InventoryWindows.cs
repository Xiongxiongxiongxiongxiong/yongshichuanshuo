using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>Shared modal owner for the bag, keeper conversation and warehouse.</summary>
public sealed class InventoryWindows : MonoBehaviour
{
    public PlayerInventory inventory;
    public PlayerController player;
    public SceneLoader loader;
    public TownHubNavigation navigation;
    public RedKnightHud hud;
    public TMP_FontAsset font;
    public Sprite frameSprite;
    public bool IsOpen => overlay != null && overlay.activeSelf;
    public bool WarehouseOpen => IsOpen && page == Page.Warehouse;
    public bool BagOpen => IsOpen && page == Page.Bag;
    public int LastCloseFrame { get; private set; } = -1;
    public WarehouseKeeper CurrentKeeper { get; private set; }

    private enum Page { Bag, Dialogue, Warehouse }
    private Page page;
    private GameObject overlay, bag, warehouse, dialogue;
    private RectTransform safeArea;
    private TMP_Text status, bagEmpty, storeBagEmpty, storeEmpty;
    private TMP_Text detailName, detailText, detailQuantity;
    private UnityEngine.UI.Image detailIcon;
    private UnityEngine.UI.Button use, discard, deposit, withdraw;
    private Slot[] bagSlots, depositSlots, warehouseSlots;
    private int bagSelection = -1, storeSelection = -1, warehouseSelection = -1;
    private float previousTime;
    private bool restoreControls;
    private int openedFrame;
    private Rect previousSafeArea;
    private readonly Color gold = new Color(.68f, .45f, .19f);
    private readonly Color ivory = new Color(.98f, .9f, .7f);
    private readonly Color ink = new Color(.045f, .043f, .046f, 1);

    private sealed class Slot
    {
        public UnityEngine.UI.Button button;
        public UnityEngine.UI.Image border, icon;
        public TMP_Text quantity, fallback;
    }

    private void Start()
    {
        Build();
        inventory.Changed += Refresh;
    }

    private void Update()
    {
        UpdateSafeArea();
        if (!IsOpen) return;
        if (player == null || !player.gameObject.activeInHierarchy || player.isDead || loader.IsLoading ||
            loader.CurrentScene == null || loader.CurrentScene.sceneType == SceneType.Menu)
        { Close(); return; }
        if (page != Page.Bag && (CurrentKeeper == null || !CurrentKeeper.InRange(player)))
        { Close(); return; }
        var keyboard = Keyboard.current;
        if (keyboard == null || Time.frameCount == openedFrame) return;
        if (keyboard.escapeKey.wasPressedThisFrame || (page == Page.Bag && keyboard.bKey.wasPressedThisFrame)) Close();
    }

    private bool CanOpen()
    {
        return overlay != null && player != null && player.gameObject.activeInHierarchy && !player.isDead &&
            !player.isHurt && !player.IsWeaponUpgrading && !player.IsUsingSwordSkill &&
            loader.CurrentScene != null && loader.CurrentScene.sceneType != SceneType.Menu && !loader.IsLoading &&
            !navigation.MapOpen && !hud.WindowOpen && Time.timeScale > 0 &&
            player.inputcontroller != null && player.inputcontroller.GamePlayer.Move.enabled;
    }

    public void ToggleBag() { if (BagOpen) Close(); else OpenBag(); }
    public bool OpenBag() => Open(Page.Bag, null);
    public bool Talk(WarehouseKeeper keeper)
    {
        return keeper != null && keeper.InRange(player) && Open(Page.Dialogue, keeper);
    }

    private bool Open(Page destination, WarehouseKeeper keeper)
    {
        if (!CanOpen()) return false;
        previousTime = Time.timeScale;
        restoreControls = player.inputcontroller.GamePlayer.Move.enabled;
        var slide = player.GetComponent<PlayerSlide>();
        if (slide != null) slide.EndSlide();
        player.inputcontroller.GamePlayer.Disable();
        var body = player.GetComponent<Rigidbody2D>();
        if (body != null) body.velocity = Vector2.zero;
        Time.timeScale = 0;
        CurrentKeeper = keeper;
        overlay.SetActive(true);
        openedFrame = Time.frameCount;
        bagSelection = storeSelection = warehouseSelection = -1;
        ShowPage(destination);
        return true;
    }

    public bool OpenWarehouse()
    {
        if (!IsOpen || page != Page.Dialogue || CurrentKeeper == null || !CurrentKeeper.InRange(player)) return false;
        ShowPage(Page.Warehouse);
        return true;
    }

    private void ShowPage(Page value)
    {
        page = value;
        bag.SetActive(value == Page.Bag);
        warehouse.SetActive(value == Page.Warehouse);
        dialogue.SetActive(value == Page.Dialogue);
        status.text = "";
        Refresh();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    public void Close()
    {
        if (!IsOpen) return;
        overlay.SetActive(false);
        LastCloseFrame = Time.frameCount;
        CurrentKeeper = null;
        Time.timeScale = previousTime;
        if (restoreControls && player != null && player.gameObject.activeInHierarchy && !player.isDead && !loader.IsLoading)
            player.inputcontroller.GamePlayer.Enable();
        if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
    }

    public void TransferSelected(bool intoWarehouse)
    {
        if (!WarehouseOpen || CurrentKeeper == null || !CurrentKeeper.InRange(player)) return;
        string message;
        inventory.Transfer(intoWarehouse, intoWarehouse ? storeSelection : warehouseSelection, out message);
        status.text = message;
    }

    public void UseSelected()
    {
        if (!BagOpen) return;
        string message;
        var stack = inventory.Backpack.At(bagSelection);
        var item = stack != null ? inventory.Definition(stack.itemId) : null;
        if (item != null && item.IsWeapon) inventory.Equip(bagSelection, out message);
        else inventory.Use(bagSelection, player, out message);
        status.text = message;
    }

    public void DiscardSelected()
    {
        if (BagOpen && inventory.Discard(bagSelection)) status.text = "已丢弃 1 个物品";
    }

    private static int ClampSelection(int index, InventoryCollection collection)
    { return index < 0 || collection.Count == 0 ? -1 : Mathf.Min(index, collection.Count - 1); }

    private void Refresh()
    {
        if (bagSlots == null) return;
        bagSelection = ClampSelection(bagSelection, inventory.Backpack);
        storeSelection = ClampSelection(storeSelection, inventory.Backpack);
        warehouseSelection = ClampSelection(warehouseSelection, inventory.Warehouse);
        RefreshGrid(bagSlots, inventory.Backpack, bagSelection);
        RefreshGrid(depositSlots, inventory.Backpack, storeSelection);
        RefreshGrid(warehouseSlots, inventory.Warehouse, warehouseSelection);
        bagEmpty.gameObject.SetActive(inventory.Backpack.Count == 0);
        storeBagEmpty.gameObject.SetActive(inventory.Backpack.Count == 0);
        storeEmpty.gameObject.SetActive(inventory.Warehouse.Count == 0);
        var selected = inventory.Backpack.At(bagSelection);
        var item = selected != null ? inventory.Definition(selected.itemId) : null;
        detailIcon.sprite = item != null ? item.icon : null;
        detailIcon.enabled = detailIcon.sprite != null;
        detailName.text = selected == null ? "未选择物品" : item != null ? item.displayName : selected.itemId;
        detailText.text = selected == null ? "点击左侧物品查看详情" : item != null ? item.description : "此物品的资料尚未配置，物品数据已保留。";
        bool weapon = item != null && item.IsWeapon;
        bool equipped = weapon && inventory.EquippedWeaponId == item.id;
        detailQuantity.text = selected == null ? "" : "数量：" + selected.quantity + (equipped ? "  ·  已装备" : "");
        use.GetComponentInChildren<TMP_Text>().text = weapon ? (equipped ? "卸下" : "装备") : "使用";
        use.interactable = selected != null && item != null && (weapon || item.useEffect != InventoryItemDefinition.UseEffect.None) && inventory.SaveAvailable;
        discard.interactable = selected != null && inventory.SaveAvailable;
        deposit.interactable = storeSelection >= 0 && inventory.SaveAvailable;
        withdraw.interactable = warehouseSelection >= 0 && inventory.SaveAvailable;
        if (!inventory.SaveAvailable) status.text = "物品存档读取失败，原存档已保留";
    }

    private void RefreshGrid(Slot[] slots, InventoryCollection collection, int selection)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            var stack = collection.At(i);
            var item = stack != null ? inventory.Definition(stack.itemId) : null;
            Color[] rarityColors = { new Color(.55f,.55f,.55f), new Color(.2f,.7f,.35f), new Color(.25f,.55f,1), new Color(.7f,.3f,1), new Color(1,.6f,.15f) };
            slots[i].border.color = i == selection ? new Color(1, .74f, .3f) : item != null && item.IsWeapon ? rarityColors[(int)item.rarity] : new Color(.32f, .28f, .22f);
            slots[i].button.interactable = stack != null;
            slots[i].icon.sprite = item != null ? item.icon : null;
            slots[i].icon.enabled = item != null && item.icon != null;
            slots[i].quantity.text = stack != null && stack.quantity > 1 ? stack.quantity.ToString() : "";
            slots[i].fallback.text = stack == null ? "◇" : (item == null || item.icon == null) ? (item != null ? item.displayName : stack.itemId) : "";
            slots[i].fallback.color = stack == null ? new Color(.3f, .26f, .2f, .55f) : ivory;
        }
    }

    private void Build()
    {
        var canvasObject = new GameObject("Inventory and Warehouse Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);
        var canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero; canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = canvasRect.offsetMax = Vector2.zero;
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 150;
        safeArea = Rect(canvasObject.transform, "Safe Area", Vector2.zero, Vector2.zero);
        safeArea.anchorMin = Vector2.zero; safeArea.anchorMax = Vector2.one;
        safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
        overlay = Image(safeArea, "Modal Shade", Vector2.zero, Vector2.zero, new Color(0, 0, 0, .6f)).gameObject;
        var shade = (RectTransform)overlay.transform;
        shade.anchorMin = Vector2.zero; shade.anchorMax = Vector2.one;
        shade.offsetMin = shade.offsetMax = Vector2.zero;
        overlay.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
        bag = Frame(shade, "Backpack", Vector2.zero, new Vector2(1500, 860)).gameObject;
        Header(bag.transform, "背 包", 360, 680);
        Label(bag.transform, "物品", new Vector2(-300, 285), new Vector2(760, 45), 28);
        bagSlots = Grid(bag.transform, new Vector2(-285, -5), 142, i => { bagSelection = i; Refresh(); });
        bagEmpty = Label(bag.transform, "背包是空的", new Vector2(-285, -327), new Vector2(650, 38), 23);
        Image(bag.transform, "Divider", new Vector2(175, -5), new Vector2(1, 650), gold);
        Label(bag.transform, "物品详情", new Vector2(435, 285), new Vector2(460, 45), 28);
        var iconFrame = Frame(bag.transform, "Item Preview", new Vector2(435, 110), new Vector2(190, 190));
        detailIcon = Image(iconFrame, "Item Icon", Vector2.zero, new Vector2(164, 164), Color.white);
        detailIcon.preserveAspect = true;
        detailName = Label(bag.transform, "", new Vector2(435, -30), new Vector2(430, 55), 30);
        detailQuantity = Label(bag.transform, "", new Vector2(435, -88), new Vector2(430, 38), 22);
        detailText = Label(bag.transform, "", new Vector2(435, -177), new Vector2(430, 130), 23);
        use = Button(bag.transform, "使用", new Vector2(316, -320), new Vector2(205, 62), UseSelected, true);
        discard = Button(bag.transform, "丢弃", new Vector2(555, -320), new Vector2(205, 62), DiscardSelected);

        warehouse = Frame(shade, "Warehouse", Vector2.zero, new Vector2(1600, 870)).gameObject;
        Header(warehouse.transform, "仓 库", 365, 730);
        Label(warehouse.transform, "背包", new Vector2(-385, 287), new Vector2(650, 44), 28);
        Label(warehouse.transform, "仓库", new Vector2(385, 287), new Vector2(650, 44), 28);
        Image(warehouse.transform, "Divider", new Vector2(0, -10), new Vector2(1, 650), gold);
        depositSlots = Grid(warehouse.transform, new Vector2(-385, -5), 126, i => { storeSelection = i; warehouseSelection = -1; Refresh(); });
        warehouseSlots = Grid(warehouse.transform, new Vector2(385, -5), 126, i => { warehouseSelection = i; storeSelection = -1; Refresh(); });
        storeBagEmpty = Label(warehouse.transform, "背包是空的", new Vector2(-385, -285), new Vector2(600, 35), 22);
        storeEmpty = Label(warehouse.transform, "仓库是空的", new Vector2(385, -285), new Vector2(600, 35), 22);
        deposit = Button(warehouse.transform, "存入仓库  →", new Vector2(-385, -355), new Vector2(420, 66), () => TransferSelected(true), true);
        withdraw = Button(warehouse.transform, "←  取回背包", new Vector2(385, -355), new Vector2(420, 66), () => TransferSelected(false), true);

        dialogue = Frame(shade, "Keeper Dialogue", Vector2.zero, new Vector2(880, 470)).gameObject;
        Header(dialogue.transform, "仓库管理员", 165, 380);
        Label(dialogue.transform, "旅途中的物品，就放心交给我保管吧。\n你可以随时存入或取回。", new Vector2(0, 30), new Vector2(760, 145), 28);
        Button(dialogue.transform, "打开仓库", new Vector2(-205, -130), new Vector2(330, 65), () => OpenWarehouse(), true);
        Button(dialogue.transform, "下次再来", new Vector2(205, -130), new Vector2(330, 65), Close);
        status = Label(shade, "", new Vector2(0, -475), new Vector2(1450, 45), 24);
        overlay.SetActive(false);
        UpdateSafeArea();
        Refresh();
    }

    private void Header(Transform parent, string title, float y, float closeX)
    {
        Label(parent, title, new Vector2(0, y), new Vector2(650, 65), 40);
        Button(parent, "×", new Vector2(closeX, y), new Vector2(54, 54), Close, true);
    }

    private Slot[] Grid(Transform parent, Vector2 center, float pitch, System.Action<int> select)
    {
        var result = new Slot[InventoryCollection.Capacity];
        for (int i = 0; i < result.Length; i++)
        {
            int index = i;
            var pos = center + new Vector2((i % 5 - 2) * pitch, (1.5f - i / 5) * pitch);
            var border = Image(parent, "Slot " + (i + 1), pos, Vector2.one * (pitch - 12), gold);
            var inner = Image(border.transform, "Inset", Vector2.zero, Vector2.one * (pitch - 16), ink);
            var button = border.gameObject.AddComponent<UnityEngine.UI.Button>();
            border.raycastTarget = true;
            button.targetGraphic = inner;
            var slotColors = button.colors;
            slotColors.disabledColor = Color.white;
            button.colors = slotColors;
            button.onClick.AddListener(() => select(index));
            var icon = Image(inner.transform, "Icon", Vector2.zero, Vector2.one * (pitch - 29), Color.white);
            icon.preserveAspect = true;
            var quantity = Label(inner.transform, "", new Vector2(0, -pitch / 2 + 27), new Vector2(pitch - 28, 26), 23);
            quantity.alignment = TextAlignmentOptions.Right;
            var fallback = Label(inner.transform, "", Vector2.zero, Vector2.one * (pitch - 30), 21);
            result[i] = new Slot { button = button, border = border, icon = icon, quantity = quantity, fallback = fallback };
        }
        return result;
    }

    private RectTransform Rect(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var rt = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rt.SetParent(parent, false);
        rt.anchorMin = rt.anchorMax = rt.pivot = Vector2.one * .5f;
        rt.sizeDelta = size; rt.anchoredPosition = position;
        return rt;
    }

    private UnityEngine.UI.Image Image(Transform parent, string name, Vector2 position, Vector2 size, Color color)
    {
        var image = Rect(parent, name, position, size).gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = color; image.raycastTarget = false;
        return image;
    }

    private RectTransform Frame(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var image = Image(parent, name, position, size, Color.white);
        image.sprite = frameSprite;
        image.type = UnityEngine.UI.Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 3;
        image.raycastTarget = true;
        if (size.x >= 800)
            Image(image.transform, "Stone Shade", Vector2.zero, size - Vector2.one * 32, new Color(0, 0, 0, .55f));
        return image.rectTransform;
    }

    private TMP_Text Label(Transform parent, string text, Vector2 position, Vector2 size, int pointSize)
    {
        var label = Rect(parent, text == "" ? "Label" : text, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font; label.text = text; label.fontSize = pointSize; label.color = ivory;
        label.alignment = TextAlignmentOptions.Center; label.raycastTarget = false;
        return label;
    }

    private UnityEngine.UI.Button Button(Transform parent, string title, Vector2 position, Vector2 size, UnityAction callback, bool primary = false)
    {
        var border = Image(parent, title, position, size, gold);
        var fill = Image(border.transform, "Fill", Vector2.zero, size - Vector2.one * 4,
            primary ? new Color(.34f, .035f, .05f) : new Color(.1f, .09f, .09f));
        border.raycastTarget = true;
        var button = border.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = fill; button.onClick.AddListener(callback);
        var colors = button.colors;
        colors.highlightedColor = new Color(1.2f, 1.2f, 1.2f);
        colors.disabledColor = new Color(.4f, .4f, .4f);
        button.colors = colors;
        Label(border.transform, title, Vector2.zero, size - Vector2.one * 8, 27);
        return button;
    }

    private void UpdateSafeArea()
    {
        if (safeArea == null || previousSafeArea == Screen.safeArea || Screen.width == 0 || Screen.height == 0) return;
        previousSafeArea = Screen.safeArea;
        safeArea.anchorMin = previousSafeArea.position / new Vector2(Screen.width, Screen.height);
        safeArea.anchorMax = (previousSafeArea.position + previousSafeArea.size) / new Vector2(Screen.width, Screen.height);
        safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
    }

    private void OnDisable() { Close(); }
    private void OnDestroy() { if (inventory != null) inventory.Changed -= Refresh; }
}
