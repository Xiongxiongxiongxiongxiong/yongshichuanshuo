using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Generate padded sprite tiles from one coherent panorama, keeping town world geometry fixed.
public static class TownHighResolution
{
    const string Source = "Assets/GeneratedTown/HD/TwilightRiverTown_HD.png";
    const string RootName = "05 High Resolution Town Panorama";

    public static string Stitch()
    {
        var sources = new Texture2D[3];
        var pixels = new Color[3][];
        int[] left = { 0, 700, 1424 }, right = { 748, 1472, 2172 };
        int height = int.MaxValue;
        for (int i = 0; i < 3; i++)
        {
            sources[i] = new Texture2D(2, 2, TextureFormat.RGB24, false);
            sources[i].LoadImage(File.ReadAllBytes("Assets/GeneratedTown/HD/Region" + i + "_HD.png"));
            height = Mathf.Min(height, sources[i].height);
            pixels[i] = sources[i].GetPixels();
        }
        if (height < 1000) throw new InvalidOperationException("Generated regions are too small for HD.");
        int width = height * 3;
        var result = new Color32[width * height];
        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            float originalX = (x + .5f) / width * 2172;
            Color color = Color.clear;
            float weightSum = 0;
            for (int i = 0; i < 3; i++)
            {
                if (originalX < left[i] || originalX > right[i]) continue;
                float weight = 1;
                if (i > 0) weight *= Mathf.Clamp01((originalX - left[i]) / 48f);
                if (i < 2) weight *= Mathf.Clamp01((right[i] - originalX) / 48f);
                float px = (originalX - left[i]) / (right[i] - left[i]) * sources[i].width - .5f;
                float py = (y + .5f) / height * sources[i].height - .5f;
                int x0 = Mathf.Clamp(Mathf.FloorToInt(px), 0, sources[i].width - 1);
                int y0 = Mathf.Clamp(Mathf.FloorToInt(py), 0, sources[i].height - 1);
                int x1 = Mathf.Min(x0 + 1, sources[i].width - 1), y1 = Mathf.Min(y0 + 1, sources[i].height - 1);
                var data = pixels[i]; int w = sources[i].width;
                var sample = Color.Lerp(Color.Lerp(data[y0*w+x0], data[y0*w+x1], px - Mathf.Floor(px)),
                    Color.Lerp(data[y1*w+x0], data[y1*w+x1], px - Mathf.Floor(px)), py - Mathf.Floor(py));
                color += sample * weight;
                weightSum += weight;
            }
            color /= weightSum; color.a = 1;
            result[y * width + x] = color;
        }
        var output = new Texture2D(width, height, TextureFormat.RGB24, false);
        output.SetPixels32(result); output.Apply();
        File.WriteAllBytes(Source, output.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(output);
        foreach (var source in sources) UnityEngine.Object.DestroyImmediate(source);
        return "Stitched " + width + "x" + height + " panorama, feathered overlapping artwork.";
    }

    public static void Capture(string prefix)
    {
        var scene = SceneManager.GetSceneByName("SampleScene_25");
        var camera = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)).First();
        var position = camera.transform.position;
        float size = camera.orthographicSize, aspect = camera.aspect;
        var target = camera.targetTexture;
        var active = RenderTexture.active;
        var rt = new RenderTexture(1920, 1080, 24);
        var texture = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        try
        {
            camera.targetTexture = rt;
            camera.aspect = 1920f / 1080;
            camera.orthographicSize = 5.5f;
            float[] xs = { 10, 42.67f, 64, 77 };
            float[] ys = { 15, 12, 12, 17 };
            for (int i = 0; i < xs.Length; i++)
            {
                camera.transform.position = new Vector3(xs[i], ys[i], position.z);
                camera.Render();
                RenderTexture.active = rt;
                texture.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
                texture.Apply();
                File.WriteAllBytes("Docs/AI/TownHD/" + prefix + "-" + i + ".png", texture.EncodeToPNG());
            }
        }
        finally
        {
            camera.transform.position = position;
            camera.orthographicSize = size;
            camera.aspect = aspect;
            camera.targetTexture = target;
            RenderTexture.active = active;
            UnityEngine.Object.DestroyImmediate(texture);
            UnityEngine.Object.DestroyImmediate(rt);
        }
    }

    public static string Apply()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var scene = SceneManager.GetSceneByName("SampleScene_25");
        if (!scene.isLoaded) throw new InvalidOperationException("Open town first.");
        var renderers = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<SpriteRenderer>(true));
        var original = renderers.First(r => r.sprite != null && AssetDatabase.GetAssetPath(r.sprite) == "Assets/GeneratedTown/TwilightRiverTown.png");
        if (original.transform.Find(RootName) != null) throw new InvalidOperationException("HD town already installed.");
        string collision = UpgradeScene8Foreground.CollisionFingerprint(scene);
        var texture = new Texture2D(2, 2, TextureFormat.RGB24, false);
        if (!texture.LoadImage(File.ReadAllBytes(Source))) throw new InvalidOperationException("Cannot decode HD artwork.");
        int width = texture.width, height = texture.height;
        if (width < 3000 || Mathf.Abs(width / (float)height - 3f) > .01f)
            throw new InvalidOperationException("Expected a high resolution 3:1 panorama.");
        var root = new GameObject(RootName);
        root.transform.SetParent(original.transform, false);
        var bounds = original.sprite.bounds;
        int columns = Mathf.CeilToInt(width / 2040f), rows = Mathf.CeilToInt(height / 2040f);
        float ppu = width / bounds.size.x;
        for (int row = 0; row < rows; row++)
        for (int column = 0; column < columns; column++)
        {
            int x = column * width / columns, xEnd = (column + 1) * width / columns;
            int y = row * height / rows, yEnd = (row + 1) * height / rows;
            int left = Mathf.Max(0, x - 2), bottom = Mathf.Max(0, y - 2);
            int right = Mathf.Min(width, xEnd + 2), top = Mathf.Min(height, yEnd + 2);
            var tile = new Texture2D(right - left, top - bottom, TextureFormat.RGB24, false);
            tile.SetPixels(texture.GetPixels(left, bottom, tile.width, tile.height));
            tile.Apply();
            string path = "Assets/GeneratedTown/HD/Town_" + row + "_" + column + ".png";
            File.WriteAllBytes(path, tile.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tile);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu;
            importer.spritePivot = new Vector2(.5f, .5f);
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.maxTextureSize = 2048;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.isReadable = false;
            importer.SaveAndReimport();
            var go = new GameObject("Town HD Tile " + row + "-" + column);
            go.layer = original.gameObject.layer;
            go.transform.SetParent(root.transform, false);
            go.transform.localPosition = new Vector3(bounds.min.x + (left + right) * .5f / width * bounds.size.x,
                bounds.min.y + (bottom + top) * .5f / height * bounds.size.y, 0);
            // Tiles overlap by their two-pixel gutters, using identical source texels along every join.
            go.transform.localScale = new Vector3(1, bounds.size.y * ppu / height, 1);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            renderer.sharedMaterial = original.sharedMaterial;
            renderer.color = original.color;
            renderer.sortingLayerID = original.sortingLayerID;
            renderer.sortingOrder = original.sortingOrder;
            renderer.maskInteraction = original.maskInteraction;
        }
        original.enabled = false;
        UnityEngine.Object.DestroyImmediate(texture);
        if (collision != UpgradeScene8Foreground.CollisionFingerprint(scene)) throw new InvalidOperationException("Town collision changed.");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        return width + "x" + height + "; " + columns + "x" + rows + " padded tiles; collision unchanged.";
    }
}
