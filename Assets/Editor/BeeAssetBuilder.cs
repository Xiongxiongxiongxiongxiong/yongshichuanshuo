using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

// Rebuilds the reusable prefab; level placement is deliberately separate.
public static class BeeAssetBuilder
{
    private const string Root = "Assets/Legacy-Fantasy - High Forest 2.3/Mob/Small Bee/";

    private static Sprite[] ImportFrames(string action)
    {
        string path = Root + action + "/" + action + "-Sheet.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 32;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        var slices = new SpriteMetaData[4];
        for (int i = 0; i < slices.Length; i++)
            slices[i] = new SpriteMetaData { name = "Bee" + action + "_" + i,
                rect = new Rect(i * 64, 0, 64, 64), alignment = 0, pivot = new Vector2(0.5f, 0.5f) };
#pragma warning disable 0618
        importer.spritesheet = slices;
#pragma warning restore 0618
        importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
    }

    [MenuItem("Tools/Enemies/Rebuild Bee Prefab")]
    public static void Build()
    {
        var fly = ImportFrames("Fly");
        var attack = ImportFrames("Attack");
        var hit = ImportFrames("Hit");
        if (fly.Length != 4 || attack.Length != 4 || hit.Length != 4)
            throw new InvalidOperationException("Bee sprite sheets must contain four frames each.");
        var boar = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Boar.prefab");
        var go = new GameObject("Bee");
        go.SetActive(false);
        go.layer = LayerMask.NameToLayer("Enetmy");
        var renderer = go.AddComponent<SpriteRenderer>();
        var boarRenderer = boar.GetComponent<SpriteRenderer>();
        renderer.sharedMaterial = boarRenderer.sharedMaterial;
        renderer.sortingLayerID = boarRenderer.sortingLayerID;
        renderer.sortingOrder = 1;
        renderer.sprite = fly[0];
        var body = go.AddComponent<Rigidbody2D>();
        body.gravityScale = 0;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        var collider = go.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.32f;
        var health = go.AddComponent<Character>();
        health.newGameEvent = boar.GetComponent<Character>().newGameEvent;
        health.maxHp = 30;
        health.CurrentHp = 30;
        health.invulnerableDuration = 0.3f;
        health.OnHealthChange = new UnityEvent<Character>();
        health.OnTakeDamage = new UnityEvent<Transform>();
        health.OnDead = new UnityEvent();
        var attackObject = new GameObject("Stinger");
        attackObject.layer = go.layer;
        attackObject.transform.SetParent(go.transform, false);
        var damage = attackObject.AddComponent<Attack>();
        damage.enabled = false;
        damage.damage = 10;
        var bee = go.AddComponent<Bee>();
        bee.flyFrames = fly;
        bee.attackFrames = attack;
        bee.hitFrames = hit;
        bee.damageSource = damage;
        var flash = go.AddComponent<DamageFlash>();
        flash.flashMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DamageFlash.mat");
        bee.cameraShakeEvent = AssetDatabase.LoadAssetAtPath<VoidEventSO>(
            AssetDatabase.GUIDToAssetPath("ea2e8e7353c5e0a4bbe71cb1f906ba86"));
        go.SetActive(true);
        PrefabUtility.SaveAsPrefabAsset(go, "Assets/Prefabs/Bee.prefab");
        UnityEngine.Object.DestroyImmediate(go);
        AssetDatabase.SaveAssets();
    }
}
