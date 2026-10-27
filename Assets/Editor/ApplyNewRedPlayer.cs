using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class ApplyNewRedPlayer
{
    private const string Main = "Assets/player/png/red/char_red_3.png";
    private const string Skills = "Assets/player/png/red/char_red_4.png";

    [MenuItem("Tools/Player/Apply New Red Knight")]
    public static void Apply()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var player = UnityEngine.Object.FindObjectOfType<PlayerController>(true);
        if (player == null) throw new InvalidOperationException("Open persistent scene first.");
        var frames = Import(Main, false);
        var skills = Import(Skills, true);
        var animator = player.GetComponent<Animator>();
        int replaced = 0;
        foreach (var clip in animator.runtimeAnimatorController.animationClips.Distinct())
        {
            foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                if (binding.type != typeof(SpriteRenderer) || binding.propertyName != "m_Sprite") continue;
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                foreach (var key in keys)
                    if (key.value == null) throw new InvalidOperationException("Missing sprite in " + clip.name);
                for (int i = 0; i < keys.Length; i++)
                {
                    string name = keys[i].value.name;
                    int index;
                    if (name.StartsWith("char_green_4_")) index = int.Parse(name.Substring(13).Split('_')[0]);
                    else if (name.StartsWith("RedKnight_")) index = int.Parse(name.Substring(10));
                    else continue;
                    keys[i].value = frames[index];
                    replaced++;
                }
                AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            }
        }
        var renderer = player.GetComponent<SpriteRenderer>();
        renderer.sprite = frames[0];
        var hair = player.GetComponent<PlayerHairColor>();
        if (hair != null) { hair.enabled = false; EditorUtility.SetDirty(hair); }
        var material = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");
        if (material == null) throw new InvalidOperationException("Default sprite material unavailable.");
        renderer.sharedMaterial = material;
        renderer.SetPropertyBlock(null);
        player.GetComponent<PlayerSlide>().frames = skills.Skip(10).Take(8).ToArray();
        var so = new SerializedObject(player.GetComponent<PlayerSwordSkills>());
        var array = so.FindProperty("authoredFrames");
        array.arraySize = 8;
        int[] order = { 4, 5, 6, 7, 22, 23, 24, 25 };
        for (int i = 0; i < 8; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = skills[order[i]];
        so.ApplyModifiedPropertiesWithoutUndo();
        var animationData = new SerializedObject(player.GetComponent<PlayerAnimtion>());
        var climb = animationData.FindProperty("authoredClimbFrames");
        climb.arraySize = 10;
        for (int i = 0; i < 10; i++) climb.GetArrayElementAtIndex(i).objectReferenceValue = skills[30 + i];
        var upgrade = animationData.FindProperty("authoredUpgradeFrames");
        upgrade.arraySize = 8;
        for (int i = 0; i < 8; i++) upgrade.GetArrayElementAtIndex(i).objectReferenceValue = frames[54 + i];
        animationData.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(renderer);
        EditorUtility.SetDirty(player.GetComponent<PlayerSlide>());
        EditorSceneManager.MarkSceneDirty(player.gameObject.scene);
        EditorSceneManager.SaveScene(player.gameObject.scene);
        AssetDatabase.SaveAssets();
        Debug.Log("New red knight applied: " + replaced + " animation keys, slide and both sword skills.");
    }

    private static Sprite[] Import(string path, bool skill)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.maxTextureSize = 2048;
        importer.spritePixelsPerUnit = skill ? 41f : 34f;
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int[] counts = skill ? new[] { 8, 2, 8, 4, 8, 8, 2 } : new[] { 6, 8, 8, 8, 8, 4, 8, 4, 8, 3, 3 };
        // Revised char_red_4: stance/thrust, stance, slide, jump, slash, climb (two rows).
        float[] extraTops = { 35, 200, 360, 515, 660, 825, 980 };
        float[] extraBottoms = { 195, 355, 510, 655, 820, 975, 1140 };
        float[] extraFeet = { 179, 342, 499, 644, 807, 966, 1123 };
        var data = new System.Collections.Generic.List<SpriteMetaData>();
        for (int row = 0; row < counts.Length; row++)
        for (int col = 0; col < counts[row]; col++)
        {
            float x = Mathf.Round(col * texture.width / 8f);
            float right = Mathf.Round((col + 1) * texture.width / 8f);
            float top = skill ? extraTops[row] * texture.height / 1173f : (16 + row * 134) * texture.height / 1472f;
            float bottom = Mathf.Min(texture.height, skill ? extraBottoms[row] * texture.height / 1173f : (150 + row * 134) * texture.height / 1472f);
            // The wide slash crosses a grid boundary; keep it with its owner.
            if (!skill && row == 1 && col == 3) right += 22 * texture.width / 1069f;
            if (!skill && row == 1 && col == 4) x += 22 * texture.width / 1069f;
            // Death recovery lies above the next row's tall flame effect.
            if (!skill && row == 7) bottom = 1050 * texture.height / 1472f;
            var rect = new Rect(x, texture.height - bottom, right - x, bottom - top);
            data.Add(new SpriteMetaData {
                name = (skill ? "RedKnightSkill_" : "RedKnight_") + data.Count.ToString("D2"),
                rect = rect, alignment = (int)SpriteAlignment.Custom,
                pivot = new Vector2((Mathf.Round(col * texture.width / 8f) + texture.width / 8f * .48f - x) / rect.width,
                    (bottom - (skill ? extraFeet[row] * texture.height / 1173f : (143 + row * 134) * texture.height / 1472f)) / rect.height)
            });
        }
#pragma warning disable 618
        importer.spritesheet = data.ToArray();
#pragma warning restore 618
        importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
    }
}
