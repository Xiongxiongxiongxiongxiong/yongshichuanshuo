using System;
using System.Linq;
using Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class BuildMossBossArena
{
    public static string Build()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Build outside Play Mode.");
        var scene = SceneManager.GetSceneByName("SampleScene_7");
        if (!scene.isLoaded) scene = EditorSceneManager.OpenScene("Assets/Scenes/SampleScene_7.unity", OpenSceneMode.Additive);
        if (scene.GetRootGameObjects().Any(g => g.name == "Moss Tusk King Arena")) return "Arena already exists.";
        var source = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Tilemap>(true)).First(t => t.name == "map");
        var goal = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<LevelGoal>(true)).First();
        var bee = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Bee.prefab");
        int enemyLayer = LayerMask.NameToLayer("Enetmy"), groundLayer = LayerMask.NameToLayer("Ground");
        if (enemyLayer < 0 || groundLayer < 0) throw new InvalidOperationException("Required layers missing.");
        var root = new GameObject("Moss Tusk King Arena"); SceneManager.MoveGameObjectToScene(root, scene);
        var terrain = new GameObject("Boss Arena Terrain", typeof(Tilemap), typeof(TilemapRenderer));
        terrain.transform.SetParent(source.transform.parent, false);
        var map = terrain.GetComponent<Tilemap>();
        var render = terrain.GetComponent<TilemapRenderer>(); var sourceRender = source.GetComponent<TilemapRenderer>();
        render.sortingLayerID = sourceRender.sortingLayerID; render.sortingOrder = sourceRender.sortingOrder + 10;
        var grass = source.GetTile(new Vector3Int(49, 9, 0)); var wall = source.GetTile(new Vector3Int(49, 8, 0));
        if (grass == null || wall == null) throw new InvalidOperationException("Ground tiles missing.");
        for (int x = 52; x <= 87; x++) for (int y = 0; y <= 9; y++)
            map.SetTile(new Vector3Int(x, y, 0), y == 9 ? grass : wall);
        var floor = Child(root.transform, "Boss Arena Ground", new Vector3(70, 9.5f, 0)); floor.layer = groundLayer;
        floor.AddComponent<BoxCollider2D>().size = new Vector2(36, 1);
        var left = Gate(root.transform, "Arena Entrance Seal", 54, groundLayer);
        var right = Gate(root.transform, "Arena Exit Seal", 85, groundLayer);
        for (int i = 0; i < 2; i++)
        {
            var platform = Child(root.transform, "Arena Raised Platform " + i, new Vector3(i == 0 ? 59 : 80, 11.5f, 0));
            platform.layer = groundLayer;
            var sr = platform.AddComponent<SpriteRenderer>(); sr.sprite = source.GetSprite(new Vector3Int(49, 9, 0));
            sr.sortingLayerID = sourceRender.sortingLayerID; sr.sortingOrder = sourceRender.sortingOrder + 12;
            sr.drawMode = SpriteDrawMode.Tiled; sr.size = new Vector2(3, 1);
            var box = platform.AddComponent<BoxCollider2D>(); box.size = new Vector2(3, 0.35f); box.offset = new Vector2(0, 0.325f);
        }
        var oldBackground = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Renderer>(true))
            .FirstOrDefault(sr => sr.name == "Forest Background - senlinBG");
        if (oldBackground != null)
        {
            var bg = UnityEngine.Object.Instantiate(oldBackground.gameObject, root.transform);
            bg.name = "Boss Arena Forest Background"; bg.transform.position = new Vector3(70, 11, 10);
            bg.transform.localScale = new Vector3(60, 38, 60);
        }
        foreach (var bounds in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<PolygonCollider2D>(true)))
        {
            if (!bounds.CompareTag("Bounds")) continue;
            Vector2[] points = bounds.points; float oldRight = bounds.bounds.max.x, oldTop = bounds.bounds.max.y;
            for (int i = 0; i < points.Length; i++)
            {
                Vector3 world = bounds.transform.TransformPoint(points[i]);
                if (Mathf.Abs(world.x - oldRight) < 0.1f) world.x = 94;
                if (Mathf.Abs(world.y - oldTop) < 0.1f) world.y = 32;
                points[i] = bounds.transform.InverseTransformPoint(world);
            }
            bounds.points = points;
        }
        var anchor = Child(root.transform, "Boss Camera Target", new Vector3(69.5f, 15, 0));
        var cameraGo = Child(root.transform, "Boss Arena Camera", anchor.transform.position);
        var camera = cameraGo.AddComponent<CinemachineVirtualCamera>(); camera.Follow = anchor.transform;
        camera.m_Lens.OrthographicSize = 9.5f; camera.m_Lens.NearClipPlane = 0.1f; camera.m_Lens.FarClipPlane = 1000;
        camera.AddCinemachineComponent<CinemachineTransposer>().m_FollowOffset = new Vector3(0, 0, -10);
        camera.Priority = -10;

        var go = Child(root.transform, "Moss Tusk King", new Vector3(74, 10.02f, 0)); go.SetActive(false); go.layer = enemyLayer;
        var visual = go.AddComponent<SpriteRenderer>(); visual.sortingLayerID = bee.GetComponent<SpriteRenderer>().sortingLayerID; visual.sortingOrder = 8;
        var body = go.AddComponent<Rigidbody2D>(); body.gravityScale = 3; body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous; body.mass = 8;
        var collider = go.AddComponent<BoxCollider2D>(); collider.size = new Vector2(5.4f, 3.5f); collider.offset = new Vector2(0, 1.75f);
        var health = go.AddComponent<Character>(); health.newGameEvent = bee.GetComponent<Character>().newGameEvent;
        health.maxHp = health.CurrentHp = 600; health.invulnerableDuration = 0.16f;
        health.OnHealthChange = new UnityEvent<Character>(); health.OnTakeDamage = new UnityEvent<Transform>(); health.OnDead = new UnityEvent();
        var damageGo = Child(go.transform, "Boss Damage Source", go.transform.position);
        var damage = damageGo.AddComponent<Attack>(); damage.damage = 20;
        var boss = go.AddComponent<MossTuskBoss>(); boss.damageSource = damage; boss.entranceGate = left; boss.exitGate = right;
        boss.arenaCamera = camera; boss.exitGoal = goal; boss.completionUI = goal.completionUI;
        boss.effectRoot = root.transform; boss.frames = LoadFrames("MossTuskKing", 8); boss.effectFrames = LoadFrames("MossTuskEffects", 4);
        visual.sprite = boss.frames[0]; visual.flipX = true;
        var flash = go.AddComponent<DamageFlash>(); flash.flashMaterial = bee.GetComponent<DamageFlash>().flashMaterial;
        var hud = go.AddComponent<MossBossHud>();
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath("Assets/Legacy-Fantasy - High Forest 2.3/HUD/Base-01.png"))
        {
            if (asset.name == "Base-01_7") hud.backgroundSprite = asset as Sprite;
            if (asset.name == "Base-01_29") hud.healthSprite = asset as Sprite;
            if (asset.name == "Base-01_14") hud.trailSprite = asset as Sprite;
        }
        goal.transform.position = new Vector3(83, 11.5f, 0); goal.GetComponent<Collider2D>().enabled = false;
        foreach (var teleport in scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TeleportPoint>(true)))
            if (teleport.transform.position.x >= 48) teleport.gameObject.SetActive(false);
        var label = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<TextMesh>(true)).FirstOrDefault(t => t.name == "Goal Label");
        if (label != null) { label.transform.position = new Vector3(56, 14.5f, 0); label.text = "古林守卫的领地"; }
        var pedestal = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).FirstOrDefault(t => t.name == "Exit Pedestal");
        if (pedestal != null) pedestal.position = new Vector3(83, 11.1f, 0);
        go.SetActive(true); left.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        return "Boss arena saved: x54..85, floor10, boss600HP, 32 animation frames and16 effect frames.";
    }
    private static GameObject Child(Transform parent, string name, Vector3 position)
    { var go = new GameObject(name); go.transform.SetParent(parent, false); go.transform.position = position; return go; }
    private static GameObject Gate(Transform parent, string name, float x, int layer)
    {
        var gate = Child(parent, name, new Vector3(x, 14, 0)); gate.layer = layer;
        gate.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 12);
        var stone = Child(gate.transform, "Rune Seal", new Vector3(x, 10, -0.1f));
        var sr = stone.AddComponent<SpriteRenderer>(); sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GeneratedBoss/MossTuskEffectsFrames/MossTuskEffects_3_1.asset");
        sr.sortingLayerName = "player"; sr.sortingOrder = 20; stone.transform.localScale = new Vector3(1, 1.5f, 1);
        var pulse = stone.AddComponent<MossBossRuneSeal>(); pulse.frames = new Sprite[4];
        for (int i = 0; i < 4; i++) pulse.frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GeneratedBoss/MossTuskEffectsFrames/MossTuskEffects_3_" + i + ".asset");
        return gate;
    }
    private static Sprite[] LoadFrames(string name, int rows)
    {
        var frames = new Sprite[rows * 4];
        for (int r = 0; r < rows; r++) for (int c = 0; c < 4; c++)
            frames[r * 4 + c] = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/GeneratedBoss/" + name + "Frames/" + name + "_" + r + "_" + c + ".asset");
        if (frames.Any(s => s == null)) throw new InvalidOperationException("Boss sprites missing.");
        return frames;
    }
}
