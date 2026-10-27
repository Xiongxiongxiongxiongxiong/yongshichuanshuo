using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ImportSporeMelee
{
    private const string Folder = "Assets/GeneratedBoss/SecondBoss/";
    public static Sprite[] LoadFrames()
    {
        return Enumerable.Range(0, 8).Select(i => AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "SporeCrownMeleeFrames/SporeCrownMelee_" + i + ".asset")).ToArray();
    }

    public static string Import()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Import outside Play Mode.");
        var scene = SceneManager.GetSceneByName("BossScene_2");
        if (scene.isLoaded && scene.isDirty) throw new InvalidOperationException("Save the authored BossScene_2 changes first.");
        string path = Folder + "SporeCrownMelee.png";
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) throw new InvalidOperationException("Melee sheet missing.");
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.isReadable = true; importer.alphaIsTransparency = true; importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 2048; importer.SaveAndReimport();
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        var reference = AssetDatabase.LoadAssetAtPath<Sprite>(Folder + "SporeCrownElderFrames/SporeCrownElder_0_0.asset");
        Rect refBounds = AlphaBounds(reference.texture, reference.rect);
        float targetHeight = refBounds.height / reference.pixelsPerUnit;
        int width = texture.width / 4, height = texture.height / 2;
        float ppu = AlphaBounds(texture, new Rect(0, height, width, height)).height / targetHeight;
        string frameFolder = Folder + "SporeCrownMeleeFrames";
        if (!AssetDatabase.IsValidFolder(frameFolder)) AssetDatabase.CreateFolder(Folder.TrimEnd('/'), "SporeCrownMeleeFrames");
        for (int i = 0; i < 8; i++)
        {
            var rect = new Rect(i % 4 * width, (1 - i / 4) * height, width, height);
            var bounds = AlphaBounds(texture, rect);
            if (bounds.width <= 0 || bounds.height <= 0) throw new InvalidOperationException("Empty melee frame " + i);
            // Anchor the feet rather than the extended fist, keeping the body stable while swiping.
            var feet = AlphaBounds(texture, new Rect(rect.x, rect.y + bounds.y, width, Mathf.Max(2, bounds.height * 0.06f)));
            float anchorX = feet.width > 0 ? feet.center.x : bounds.center.x;
            var pivot = new Vector2(anchorX / width, bounds.y / height);
            var sprite = Sprite.Create(texture, rect, pivot, ppu, 0, SpriteMeshType.FullRect);
            sprite.name = "SporeCrownMelee_" + i;
            string spritePath = frameFolder + "/" + sprite.name + ".asset";
            var existing = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (existing == null) AssetDatabase.CreateAsset(sprite, spritePath);
            else { EditorUtility.CopySerialized(sprite, existing); UnityEngine.Object.DestroyImmediate(sprite); EditorUtility.SetDirty(existing); }
        }
        var previous = SceneManager.GetActiveScene();
        bool opened = !scene.isLoaded;
        if (opened) scene = EditorSceneManager.OpenScene(BuildSporeBossLevel.ScenePath, OpenSceneMode.Additive);
        var boss = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SporeCrownBoss>(true)).Single();
        boss.meleeFrames = LoadFrames();
        EditorSceneManager.MarkSceneDirty(scene); EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        if (opened) EditorSceneManager.CloseScene(scene, true);
        if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
        return "Eight melee frames imported, foot anchors calibrated, BossScene_2 linked.";
    }

    private static Rect AlphaBounds(Texture2D texture, Rect rect)
    {
        int width = Mathf.FloorToInt(rect.width), height = Mathf.FloorToInt(rect.height);
        int minX = width, minY = height, maxX = -1, maxY = -1;
        for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
        {
            if (texture.GetPixel((int)rect.x + x, (int)rect.y + y).a < 0.1f) continue;
            minX = Mathf.Min(minX, x); minY = Mathf.Min(minY, y); maxX = Mathf.Max(maxX, x); maxY = Mathf.Max(maxY, y);
        }
        return maxX < 0 ? Rect.zero : new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }
}
