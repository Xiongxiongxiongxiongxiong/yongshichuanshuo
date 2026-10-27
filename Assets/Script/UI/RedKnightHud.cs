using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Presentation for the persistent player's existing health, stamina and combat systems.</summary>
public sealed class RedKnightHud : MonoBehaviour
{
    public PlayerController player;
    public SceneLoader loader;
    public TownHubNavigation navigation;
    public RectTransform safeArea;
    public CanvasGroup visibility;
    public UnityEngine.UI.Image healthFill, damageFill, staminaFill;
    public TMP_Text healthText, staminaText, areaText, mapText;
    public UnityEngine.UI.Button[] actions;
    public UnityEngine.UI.Image[] cooldownMasks;
    public TMP_Text[] cooldownTexts;
    public UnityEngine.UI.Button mapButton;
    public UnityEngine.UI.Button pauseButton, resumeButton;
    public UnityEngine.UI.Button[] utilityButtons;
    public GameObject window;
    public TMP_Text windowTitle, windowBody;
    public InventoryWindows inventoryWindows;

    private Character health;
    private PlayerStamina stamina;
    private PlayerSwordSkills skills;
    private PlayerSlide slide;
    private Rect lastSafeArea;
    private int screenWidth, screenHeight, mode;
    private float nextTextRefresh, timeBeforeWindow;
    private bool controlsBeforeWindow;
    public bool WindowOpen => (window != null && window.activeSelf) || (inventoryWindows != null && inventoryWindows.IsOpen);

    private void OnEnable()
    {
        health = player.GetComponent<Character>();
        stamina = player.GetComponent<PlayerStamina>();
        skills = player.GetComponent<PlayerSwordSkills>();
        slide = player.GetComponent<PlayerSlide>();
    }

    private void Awake()
    {
        window.SetActive(false);
        visibility.alpha = 0;
        visibility.interactable = visibility.blocksRaycasts = false;
        healthFill.fillAmount = damageFill.fillAmount = 1;
    }

    private void Update()
    {
        UpdateSafeArea();
        bool visible = player != null && player.gameObject.activeInHierarchy && loader != null &&
            loader.CurrentScene != null && loader.CurrentScene.sceneType != SceneType.Menu && !loader.IsLoading;
        visibility.alpha = visible ? 1 : 0;
        visibility.interactable = visibility.blocksRaycasts = visible;
        if (!visible) { if (WindowOpen) CloseWindow(); return; }
        if (player.isDead && WindowOpen) CloseWindow();
        float hp = health.maxHp > 0 ? Mathf.Clamp01(health.CurrentHp / health.maxHp) : 0;
        healthFill.fillAmount = hp;
        damageFill.fillAmount = hp >= damageFill.fillAmount ? hp : Mathf.MoveTowards(damageFill.fillAmount, hp, Time.unscaledDeltaTime * .45f);
        staminaFill.fillAmount = stamina.maxStamina > 0 ? Mathf.Clamp01(stamina.Current / stamina.maxStamina) : 0;
        float thrust = skills.ThrustCooldownRemaining, spin = skills.SpinCooldownRemaining;
        cooldownMasks[0].fillAmount = Mathf.Clamp01(thrust / 3f);
        cooldownMasks[1].fillAmount = Mathf.Clamp01(spin / 5f);
        bool ready = CanControl();
        actions[0].interactable = ready;
        actions[1].interactable = ready && stamina.Current >= slide.staminaCost && !slide.IsSliding;
        actions[2].interactable = ready && thrust <= 0 && stamina.Current >= 30;
        actions[3].interactable = ready && spin <= 0 && stamina.Current >= 40;
        mapButton.interactable = !player.isDead && !WindowOpen && Time.timeScale > 0;
        if (Time.unscaledTime >= nextTextRefresh)
        {
            nextTextRefresh = Time.unscaledTime + .1f;
            healthText.SetText("{0} / {1}", Mathf.Ceil(health.CurrentHp), health.maxHp);
            staminaText.SetText("{0} / {1}", Mathf.Floor(stamina.Current), stamina.maxStamina);
            cooldownTexts[0].text = thrust > 0 ? thrust.ToString("0.0") : "";
            cooldownTexts[1].text = spin > 0 ? spin.ToString("0.0") : "";
            mapText.text = loader.CurrentScene == navigation.town ? "冒险地图  M" : "返回主城  M";
            areaText.text = AreaName();
            if (window.activeSelf) RefreshWindow();
        }
        var keyboard = Keyboard.current;
        if (inventoryWindows != null && (inventoryWindows.IsOpen || inventoryWindows.LastCloseFrame == Time.frameCount)) return;
        if (keyboard == null || navigation.MapOpen || navigation.LastMapCloseFrame == Time.frameCount) return;
        if (keyboard.escapeKey.wasPressedThisFrame) { if (WindowOpen) CloseWindow(); else OpenWindow(0); }
        else if (keyboard.cKey.wasPressedThisFrame) ToggleWindow(1);
        else if (keyboard.bKey.wasPressedThisFrame) ToggleBag();
        else if (keyboard.mKey.wasPressedThisFrame && !WindowOpen && Time.timeScale > 0) Travel();
    }

    private bool CanControl() => player != null && !player.isDead && !player.isHurt && !WindowOpen &&
        Time.timeScale > 0 && !navigation.MapOpen && player.inputcontroller != null &&
        player.inputcontroller.GamePlayer.Move.enabled && !player.IsWeaponUpgrading && !player.IsUsingSwordSkill;

    public void UseAction(int index)
    {
        if (!CanControl()) return;
        if (index == 0) player.AttackFromHud();
        else if (index == 1) slide.TryStartSlide();
        else if (index == 2) skills.TryUseSkill(false);
        else if (index == 3) skills.TryUseSkill(true);
    }

    public void Attack() => UseAction(0);
    public void Slide() => UseAction(1);
    public void Thrust() => UseAction(2);
    public void Spin() => UseAction(3);
    public void TogglePause() => ToggleWindow(0);
    public void ToggleCharacter() => ToggleWindow(1);
    public void ToggleBag()
    {
        if (inventoryWindows == null) return;
        if (window.activeSelf) CloseWindow();
        inventoryWindows.ToggleBag();
    }
    public void Travel() { if (!WindowOpen && !player.isDead && Time.timeScale > 0) navigation.TravelFromHud(); }
    public void ToggleWindow(int page)
    {
        if (page == 2) { ToggleBag(); return; }
        if (inventoryWindows != null && inventoryWindows.IsOpen) inventoryWindows.Close();
        if (WindowOpen && mode == page) CloseWindow();
        else if (WindowOpen) { mode = page; RefreshWindow(); }
        else OpenWindow(page);
    }
    public void OpenWindow(int page)
    {
        if (page == 2) { ToggleBag(); return; }
        if (WindowOpen || !CanControl()) return;
        mode = page;
        timeBeforeWindow = Time.timeScale;
        controlsBeforeWindow = player.inputcontroller.GamePlayer.Move.enabled;
        player.inputcontroller.GamePlayer.Disable();
        Time.timeScale = 0;
        window.SetActive(true);
        RefreshWindow();
    }
    public void CloseWindow()
    {
        if (inventoryWindows != null && inventoryWindows.IsOpen) { inventoryWindows.Close(); return; }
        if (!WindowOpen) return;
        window.SetActive(false);
        Time.timeScale = timeBeforeWindow;
        if (controlsBeforeWindow && player != null && player.gameObject.activeInHierarchy && !player.isDead && !loader.IsLoading)
            player.inputcontroller.GamePlayer.Enable();
    }
    private void RefreshWindow()
    {
        windowTitle.text = mode == 0 ? "暂 停" : mode == 1 ? "赤焰骑士 · 角色" : "行 囊";
        if (mode == 0) windowBody.text = "旅途暂歇，锋芒未熄。\n\nJ  普攻     K  滑铲\nU  突刺     I  旋风斩\n\nM  地图 / 返回主城\nC  角色     B  背包\n\n按 Esc 或下方按钮继续冒险";
        else if (mode == 1)
        {
            int damage = 0;
            foreach (var attack in player.GetComponentsInChildren<Attack>(true))
                if (attack.gameObject.name != "Sword Skill Damage") damage = Mathf.Max(damage, attack.damage);
            windowBody.text = "生命    " + Mathf.CeilToInt(health.CurrentHp) + " / " + health.maxHp +
                "\n体力    " + Mathf.FloorToInt(stamina.Current) + " / " + stamina.maxStamina +
                "\n武器伤害    " + damage + "\n\n剑与盾 · 红披风\n\n滑铲消耗 50 体力\n突刺消耗 30 体力 · 冷却 3 秒\n旋风斩消耗 40 体力 · 冷却 5 秒";
        }
        else windowBody.text = "尚未获得可收纳物品\n\n当前装备：剑与盾\n\n宝箱中的武器强化会直接应用到角色。\n可在角色面板查看当前武器伤害。";
    }
    private string AreaName()
    {
        if (loader.CurrentScene == navigation.town) return "暮光河畔镇";
        for (int i = 0; i < navigation.destinations.Length && i < navigation.destinationNames.Length; i++)
            if (loader.CurrentScene == navigation.destinations[i]) return navigation.destinationNames[i];
        return "冒险之境";
    }
    private void UpdateSafeArea()
    {
        if (lastSafeArea == Screen.safeArea && screenWidth == Screen.width && screenHeight == Screen.height) return;
        lastSafeArea = Screen.safeArea; screenWidth = Screen.width; screenHeight = Screen.height;
        if (screenWidth <= 0 || screenHeight <= 0) return;
        safeArea.anchorMin = lastSafeArea.position / new Vector2(screenWidth, screenHeight);
        safeArea.anchorMax = (lastSafeArea.position + lastSafeArea.size) / new Vector2(screenWidth, screenHeight);
        safeArea.offsetMin = safeArea.offsetMax = Vector2.zero;
    }
    private void OnDisable() { CloseWindow(); }
}
