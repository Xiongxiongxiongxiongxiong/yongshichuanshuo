using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>Authors independent atlases into the same frame-locked contract as the calibrated frost sword.</summary>
public static class BuildSwordCollection
{
    [Serializable] sealed class SliceLayout { public int[] supplementRows; }
    public const string Root = "Assets/player/SwordCollection/";
    public static readonly string[] Ids = { "iron_longsword", "iron_greatsword", "jade_longsword", "verdant_greatsword", "frost_longsword", "tide_greatsword", "amethyst_longsword", "thunder_greatsword", "dragon_longsword", "solar_greatsword" };
    public static readonly string[] Names = { "旧铁长剑", "铁锋重剑", "翠玉长剑", "青纹重剑", "霜晶长剑", "沧澜重剑", "暗曜符文剑", "紫霆裂空剑", "炎龙圣剑", "曜阳圣翼剑" };
    static readonly string[] Rarities = { "普通", "精良", "稀有", "史诗", "传说" };

    [MenuItem("Tools/Player/Build Ten Frame Locked Swords")]
    public static void Build() { BuildCount(10, false); }
    public static void BuildAvailable()
    { BuildCount(Ids.TakeWhile(id => File.Exists(Root + id + "/Main.png") && File.Exists(Root + id + "/Supplement.png")).Count(), false); }
    static void BuildCount(int count, bool onlyMissing)
    { BuildSet(Root, "sword.", Ids, Names, "单手剑", "重剑", "Docs/AI/SwordCollection/", count, onlyMissing); }
    public static void BuildSet(string root, string prefix, string[] ids, string[] names, string lightType, string heavyType, string evidence, int count, bool onlyMissing = false)
    {
        string Root = root; string[] Ids = ids, Names = names;
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var swap = UnityEngine.Object.FindObjectOfType<TestWeaponSwap>(true);
        if (swap == null || swap.animatedFrames.Length != 108) throw new InvalidOperationException("Open the calibrated persistent player first.");
        // Preflight all textures before changing any assets or scene references.
        if (count == 0) throw new InvalidOperationException("No complete atlas pair yet.");
        foreach (var id in Ids.Take(count)) foreach (var file in new[] { "Main", "Supplement" })
            if (!File.Exists(Root + id + "/" + file + ".png")) throw new FileNotFoundException(id + "/" + file);
        var items = new List<InventoryItemDefinition>();
        var report = new System.Text.StringBuilder("Sword,Body,Atlas,Rect,Grip,PPU,Hidden\n");
        for (int n = 0; n < count; n++)
        {
            string folder = Root + Ids[n] + "/";
            var existing = AssetDatabase.LoadAssetAtPath<InventoryItemDefinition>(folder + "Item.asset");
            if (onlyMissing && existing != null && existing.weaponFrames != null && existing.weaponFrames.Length == 108 && existing.weaponFrames.All(f => f != null && (f.hidden || f.sprite != null)))
            { items.Add(existing); continue; }
            var main = Prepare(folder + "Main.png"); var supplement = Prepare(folder + "Supplement.png");
            var layout = JsonUtility.FromJson<SliceLayout>(File.ReadAllText(folder + "Slices.json"));
            var mainPixels = Read(folder + "Main.png"); var extraPixels = Read(folder + "Supplement.png");
            AssetDatabase.StartAssetEditing();
            try
            {
                var frames = new TestWeaponSwap.WeaponFrame[swap.animatedFrames.Length];
                for (int i = 0; i < frames.Length; i++)
                {
                    var donor = swap.animatedFrames[i]; var source = donor.source;
                    bool extra = source.name.StartsWith("RedKnightSkill_");
                    int index = int.Parse(source.name.Substring(extra ? 15 : 10));
                    bool attack = extra ? index >= 22 && index < 30 : index >= 6 && index < 14;
                    bool upgrade = !extra && index >= 54 && index < 62;
                    var texture = extra || attack || upgrade ? supplement : main;
                    var pixels = extra || attack || upgrade ? extraPixels : mainPixels;
                    Rect cell;
                    if (attack)
                    {
                        int pose = extra ? new[] { 2, 1, 3, 4, 4, 5, 6, 7 }[index - 22] : index - 6;
                        cell = SupplementCell(texture, pose, 7, layout);
                    }
                    else if (upgrade) cell = SupplementCell(texture, new[] { 0, 0, 1, 2, 3, 1, 2, 3 }[index - 54], 8, layout);
                    else if (extra)
                    {
                        int row = index < 8 ? 0 : index < 10 ? 1 : index < 18 ? 2 : index < 22 ? 3 : index < 30 ? 4 : index < 38 ? 5 : 6;
                        int column = index - new[] { 0, 8, 10, 18, 22, 30, 38 }[row];
                        cell = SupplementCell(texture, column, row, layout);
                    }
                    else
                    {
                        // Original recovery cells 65–67 are empty; the existing frost setup deliberately
                        // reuses authored poses 62–64. Preserve that explicit recovery mapping.
                        var poseSource = index >= 65 ? swap.originalFrames.First(f => f.name == "RedKnight_" + (62 + index - 65).ToString("D2")) : source;
                        var r = poseSource.rect; float sx = (float)texture.width / source.texture.width, sy = (float)texture.height / source.texture.height;
                        cell = Clamp(new Rect(r.x * sx, r.y * sy, r.width * sx, r.height * sy), texture);
                    }
                    Sprite saved = null;
                    Vector2 grip = Vector2.one * .5f;
                    Rect rect = cell;
                    float ppu = source.pixelsPerUnit * texture.width / source.texture.width;
                    if (!donor.hidden)
                    {
                        Vector2 expected;
                        if (attack)
                        {
                            int pose = extra ? new[] { 2, 1, 3, 4, 4, 5, 6, 7 }[index - 22] : index - 6;
                            float cw = donor.sprite.texture.width / 4f, ch = donor.sprite.texture.height / 2f;
                            var relative = donor.sprite.rect.position + donor.sprite.pivot - new Vector2(pose % 4 * cw, (1 - pose / 4) * ch);
                            expected = cell.position + Vector2.Scale(new Vector2(relative.x / cw, relative.y / ch), cell.size);
                        }
                        else if (upgrade) expected = cell.position + Vector2.Scale(new Vector2(donor.sprite.pivot.x / donor.sprite.rect.width, donor.sprite.pivot.y / donor.sprite.rect.height), cell.size);
                        else
                        {
                            var old = donor.sprite.rect.position + donor.sprite.pivot;
                            float sx = (float)texture.width / donor.sprite.texture.width;
                            if (extra)
                            {
                                int row = index < 8 ? 0 : index < 10 ? 1 : index < 18 ? 2 : 3;
                                int column = index - new[] { 0, 8, 10, 18 }[row];
                                float cw = donor.sprite.texture.width / 8f, ch = donor.sprite.texture.height / 7f;
                                var relative = old - new Vector2(column * cw, (6 - row) * ch);
                                expected = cell.position + Vector2.Scale(new Vector2(relative.x / cw, relative.y / ch), cell.size);
                            }
                            else expected = new Vector2(old.x * sx, old.y * texture.height / donor.sprite.texture.height);
                        }
                        // Isolate a single connected weapon, keeping the crop inside its cell.
                        Rect search = extra && !attack && !upgrade ? new Rect(0, cell.y, pixels.width, cell.height) : cell;
                        if (attack)
                        {
                            float mx = cell.width * .14f, my = cell.height * .08f;
                            search = Clamp(new Rect(cell.x - mx, cell.y - my, cell.width + mx * 2, cell.height + my * 2), pixels);
                        }
                        rect = Isolate(pixels, search, expected);
                        grip = Grip(pixels, rect, expected);
                        if (UsesUprightDaoRest(prefix + Ids[n], source)) grip = UprightGrip(pixels, rect);
                        // Match the calibrated weapon's world-space reach, including authored perspective.
                        float reach = Reach(pixels, rect, grip);
                        float oldReach = DonorReach(donor);
                        if (reach < 3 || oldReach < .1f) throw new InvalidOperationException("Empty/invalid weapon pose: " + Ids[n] + "/" + source.name);
                        ppu = reach / oldReach;
                        saved = SaveSprite(texture, rect, grip, ppu, folder + source.name + ".asset", Ids[n] + "_" + source.name);
                    }
                    frames[i] = new TestWeaponSwap.WeaponFrame {
                        source = source, sprite = saved, hand = donor.hand, hidden = donor.hidden,
                        sortingOffset = donor.sortingOffset, bodyRect = donor.bodyRect, bodyPivot = donor.bodyPivot,
                        cropBody = donor.cropBody, flipHorizontal = donor.flipHorizontal, flipVertical = donor.flipVertical
                    };
                    if (!donor.hidden && IsRest(source) && OppositeRest(donor, pixels, rect, grip))
                    { frames[i].flipHorizontal = !donor.flipHorizontal; frames[i].flipVertical = !donor.flipVertical; }
                    if (UsesUprightDaoRest(prefix + Ids[n], source))
                    { frames[i].flipHorizontal = false; frames[i].flipVertical = false; }
                    if (prefix + Ids[n] == "dao.verdant_cleaver" && IsRest(source))
                        frames[i].flipHorizontal = !frames[i].flipHorizontal;
                    report.AppendLine(Ids[n] + "," + source.name + "," + texture.name + ",\"" + rect + "\",\"" + grip + "\"," + ppu.ToString(System.Globalization.CultureInfo.InvariantCulture) + "," + donor.hidden);
                }
                var item = AssetDatabase.LoadAssetAtPath<InventoryItemDefinition>(folder + "Item.asset");
                if (item == null) { item = ScriptableObject.CreateInstance<InventoryItemDefinition>(); AssetDatabase.CreateAsset(item, folder + "Item.asset"); }
                item.id = prefix + Ids[n]; item.displayName = Names[n]; item.maxStack = 1;
                item.rarity = (InventoryItemDefinition.Rarity)(n / 2);
                item.description = DescriptionFor(n, lightType, heavyType);
                item.weaponFrames = frames;
                // Separate tight icon without changing animation pivots.
                var iconPose = frames.First(f => !f.hidden && f.source.name == "RedKnight_06").sprite;
                item.icon = SaveSprite(iconPose.texture, iconPose.rect, Vector2.one * .5f, 100, folder + "Icon.asset", Ids[n] + "_Icon");
                EditorUtility.SetDirty(item); items.Add(item);
            }
            finally { AssetDatabase.StopAssetEditing(); UnityEngine.Object.DestroyImmediate(mainPixels); UnityEngine.Object.DestroyImmediate(extraPixels); }
        }
        var inventory = swap.GetComponent<PlayerInventory>();
        inventory.itemDefinitions = inventory.itemDefinitions.Where(item => item != null && !items.Any(s => s.id == item.id)).Concat(items).ToArray();
        swap.temporaryBKey = false;
        EditorUtility.SetDirty(inventory); EditorUtility.SetDirty(swap);
        EditorSceneManager.MarkSceneDirty(swap.gameObject.scene); EditorSceneManager.SaveScene(swap.gameObject.scene);
        AssetDatabase.SaveAssets();
        Directory.CreateDirectory(evidence); File.WriteAllText(evidence + "Calibration.csv", report.ToString());
        Debug.Log("Built " + count + " independent " + prefix + " animation sets. Backpack grant is a separate explicit operation.");
    }

    [MenuItem("Tools/Inventory/Grant Ten Designed Swords Once")]
    public static void Grant()
    { GrantSet(Ids, "sword.", "Docs/AI/SwordCollection/"); }
    public static void GrantSet(string[] ids, string prefix, string evidence)
    {
        var inventory = UnityEngine.Object.FindObjectOfType<PlayerInventory>(true);
        if (inventory == null || !inventory.Load()) throw new InvalidOperationException("Inventory save unavailable.");
        var items = ids.Select(id => inventory.Definition(prefix + id)).ToArray();
        if (items.Any(item => item == null)) throw new InvalidOperationException("Build sword item definitions first.");
        var missing = items.Where(item => !inventory.Backpack.stacks.Any(s => s.itemId == item.id) && !inventory.Warehouse.stacks.Any(s => s.itemId == item.id)).ToArray();
        if (InventoryCollection.Capacity - inventory.Backpack.Count < missing.Length) throw new InvalidOperationException("Backpack does not have enough empty slots; nothing granted.");
        Directory.CreateDirectory(evidence);
        string backup = evidence + "InventoryBeforeGrant.json";
        if (!File.Exists(backup)) File.WriteAllText(backup, PlayerPrefs.GetString(PlayerInventory.SaveKey, ""));
        foreach (var item in missing) if (!inventory.TryAdd(item.id, 1)) throw new InvalidOperationException("Could not grant " + item.id);
        Debug.Log("Granted " + missing.Length + " swords; existing backpack and warehouse preserved.");
    }
    public static string DescriptionFor(int index, string lightType, string heavyType)
    {
        string[] flavor = {
            "铁刃留下旅途的磨痕，皮革握柄依旧牢靠。",
            "翠玉镶入护手，精钢映出清冽的锋芒。",
            "晶蓝寒光凝于刃间，银纹如流水般延展。",
            "暗色金属刻满符文，紫色辉光沿着锋刃流转。",
            index % 2 == 0 ? "金龙盘踞护手，赤焰在锋刃间翻涌。" : "金羽环绕曜阳宝石，刃面流转温暖辉光。"
        };
        return Rarities[index / 2] + " · " + (index % 2 == 0 ? lightType : heavyType) + "\n" + flavor[index / 2];
    }

    static readonly Dictionary<Sprite, float> donorReach = new Dictionary<Sprite, float>();
    static readonly Dictionary<Sprite, Vector2> donorDirection = new Dictionary<Sprite, Vector2>();
    static bool UsesUprightDaoRest(string id, Sprite source)
    {
        if (!source.name.StartsWith("RedKnight_") || int.Parse(source.name.Substring(10)) >= 6) return false;
        return new[] { "dao.frost_dao", "dao.tide_cleaver", "dao.amethyst_dao", "dao.thunder_cleaver", "dao.dragon_dao", "dao.phoenix_cleaver" }.Contains(id);
    }
    static Vector2 UprightGrip(Texture2D t, Rect r)
    {
        int w = (int)r.width, h = (int)r.height; var p = t.GetPixels((int)r.x, (int)r.y, w, h);
        Vector2 sum = Vector2.zero, target = new Vector2(w * .25f, h * .25f); float total = 0;
        for (int y = 0; y < h * .55f; y++) for (int x = 0; x < w * .55f; x++)
        {
            Color c = p[y * w + x]; if (c.a < .8f || c.maxColorComponent > .65f) continue;
            var point = new Vector2(x + .5f, y + .5f);
            float weight = (1 - c.maxColorComponent) / (1 + (point - target).sqrMagnitude * .02f);
            sum += point * weight; total += weight;
        }
        if (total < .01f) throw new InvalidOperationException("Missing upright dao grip: " + t.name);
        return new Vector2(sum.x / total / w, sum.y / total / h);
    }
    public static bool IsRest(Sprite source)
    {
        bool extra = source.name.StartsWith("RedKnightSkill_");
        int index = int.Parse(source.name.Substring(extra ? 15 : 10));
        return extra ? index < 4 || index == 8 || index == 9 : index < 6;
    }
    static Vector2 Direction(Texture2D t, Rect r, Vector2 pivot)
    {
        int w = (int)r.width, h = (int)r.height; var p = t.GetPixels((int)r.x, (int)r.y, w, h);
        var anchor = Vector2.Scale(pivot, r.size); Vector2 sum = Vector2.zero;
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
        {
            Color c = p[y * w + x]; if (c.a < .8f || c.maxColorComponent < .25f) continue;
            Vector2 v = new Vector2(x + .5f, y + .5f) - anchor;
            sum += v * v.sqrMagnitude * c.maxColorComponent;
        }
        return sum.normalized;
    }
    static bool OppositeRest(TestWeaponSwap.WeaponFrame donor, Texture2D texture, Rect rect, Vector2 pivot)
    {
        Vector2 target;
        if (!donorDirection.TryGetValue(donor.sprite, out target))
        {
            var t = Read(AssetDatabase.GetAssetPath(donor.sprite.texture));
            try { target = Direction(t, donor.sprite.rect, new Vector2(donor.sprite.pivot.x / donor.sprite.rect.width, donor.sprite.pivot.y / donor.sprite.rect.height)); }
            finally { UnityEngine.Object.DestroyImmediate(t); }
            donorDirection[donor.sprite] = target;
        }
        return Vector2.Dot(target, Direction(texture, rect, pivot)) < -.35f;
    }
    public static string RepairRestDirections()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
        var inventory = UnityEngine.Object.FindObjectOfType<PlayerInventory>(true);
        var donors = inventory.GetComponent<TestWeaponSwap>().animatedFrames;
        int changed = 0; var report = new List<string>();
        foreach (var item in inventory.itemDefinitions.Where(i => i != null && i.IsWeapon))
        {
            var textures = new Dictionary<string, Texture2D>();
            try
            {
                foreach (var frame in item.weaponFrames.Where(f => !f.hidden && IsRest(f.source)))
                {
                    var donor = donors.Single(d => d.source == frame.source);
                    string path = AssetDatabase.GetAssetPath(frame.sprite.texture);
                    Texture2D texture; if (!textures.TryGetValue(path, out texture)) { texture = Read(path); textures.Add(path, texture); }
                    if (UsesUprightDaoRest(item.id, frame.source))
                    {
                        var pivot = UprightGrip(texture, frame.sprite.rect);
                        float ppu = Reach(texture, frame.sprite.rect, pivot) / DonorReach(donor);
                        var sprite = frame.sprite;
                        SaveSprite(sprite.texture, sprite.rect, pivot, ppu, AssetDatabase.GetAssetPath(sprite), sprite.name);
                        frame.flipHorizontal = frame.flipVertical = false;
                        changed++; report.Add(item.id + "," + frame.source.name + ",calibratedUprightGrip");
                        continue;
                    }
                    bool opposite = OppositeRest(donor, texture, frame.sprite.rect, new Vector2(frame.sprite.pivot.x / frame.sprite.rect.width, frame.sprite.pivot.y / frame.sprite.rect.height));
                    frame.flipHorizontal = donor.flipHorizontal ^ opposite; frame.flipVertical = donor.flipVertical ^ opposite;
                    if (item.id == "dao.verdant_cleaver") frame.flipHorizontal = !frame.flipHorizontal;
                    if (opposite) { changed++; report.Add(item.id + "," + frame.source.name + ",corrected180"); }
                }
                EditorUtility.SetDirty(item);
            }
            finally { foreach (var texture in textures.Values) UnityEngine.Object.DestroyImmediate(texture); }
        }
        AssetDatabase.SaveAssets(); Directory.CreateDirectory("Docs/AI/WeaponCollection");
        report.Add("Corrected " + changed + " authored resting poses. Hand anchors and other action directions unchanged.");
        File.WriteAllLines("Docs/AI/WeaponCollection/RestDirectionValidation.txt", report); return report.Last();
    }
    static float DonorReach(TestWeaponSwap.WeaponFrame frame)
    {
        float value;
        if (donorReach.TryGetValue(frame.sprite, out value)) return value;
        var t = Read(AssetDatabase.GetAssetPath(frame.sprite.texture));
        try { value = Reach(t, frame.sprite.rect, new Vector2(frame.sprite.pivot.x / frame.sprite.rect.width, frame.sprite.pivot.y / frame.sprite.rect.height)) / frame.sprite.pixelsPerUnit; }
        finally { UnityEngine.Object.DestroyImmediate(t); }
        donorReach[frame.sprite] = value; return value;
    }
    static Sprite SaveSprite(Texture2D texture, Rect rect, Vector2 pivot, float ppu, string path, string name)
    {
        var created = Sprite.Create(texture, rect, pivot, ppu, 0, SpriteMeshType.FullRect); created.name = name;
        var saved = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (saved == null) { AssetDatabase.CreateAsset(created, path); return created; }
        EditorUtility.CopySerialized(created, saved); EditorUtility.SetDirty(saved); UnityEngine.Object.DestroyImmediate(created); return saved;
    }
    static Texture2D Prepare(string path)
    {
        AssetDatabase.ImportAsset(path); var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true; importer.mipmapEnabled = false; importer.maxTextureSize = 4096;
        importer.textureCompression = TextureImporterCompression.Uncompressed; importer.filterMode = FilterMode.Bilinear;
        importer.SaveAndReimport(); return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }
    static Texture2D Read(string path) { var t = new Texture2D(2, 2); t.LoadImage(File.ReadAllBytes(path)); t.name = path; return t; }
    static Rect Cell(Texture2D texture, int column, int row, int columns, int rows)
    { return Clamp(new Rect(column * texture.width / (float)columns, texture.height - (row + 1) * texture.height / (float)rows, texture.width / (float)columns, texture.height / (float)rows), texture); }
    static Rect SupplementCell(Texture2D texture, int column, int row, SliceLayout layout)
    { return Clamp(new Rect(column * texture.width / 8f, texture.height - layout.supplementRows[row + 1], texture.width / 8f, layout.supplementRows[row + 1] - layout.supplementRows[row]), texture); }
    static Rect Clamp(Rect r, Texture2D t)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt(r.x), 0, t.width - 1), y = Mathf.Clamp(Mathf.RoundToInt(r.y), 0, t.height - 1);
        return new Rect(x, y, Mathf.Min(Mathf.FloorToInt(r.width), t.width - x), Mathf.Min(Mathf.FloorToInt(r.height), t.height - y));
    }
    static bool Leather(Color c) { return c.a > .65f && c.r > .12f && c.r < .65f && c.g < .36f && c.b < .28f && c.r > c.g * 1.16f && c.g > c.b * 1.08f; }
    static Vector2 Grip(Texture2D t, Rect r, Vector2 expected)
    {
        var p = t.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height);
        Vector2 sum = Vector2.zero; float weight = 0;
        for (int y = 0; y < r.height; y++) for (int x = 0; x < r.width; x++)
        {
            var c = p[y * (int)r.width + x];
            float distance = Vector2.Distance(r.position + new Vector2(x + .5f, y + .5f), expected);
            // Grip colors differ by weapon. Constrain the refinement to the calibrated hand
            // neighborhood so golden blades and colored rune cores cannot pull the anchor away.
            float radius = Mathf.Max(r.width, r.height) * .35f + 4;
            if (c.a < .65f || c.maxColorComponent > .7f) continue;
            float w = (Leather(c) ? 2 : .6f) * Mathf.Exp(-distance * distance / (radius * radius * .35f));
            sum += new Vector2(x + .5f, y + .5f) * w; weight += w;
        }
        if (weight <= .01f) throw new InvalidOperationException("No leather grip in " + t.name + " " + r);
        return new Vector2(sum.x / weight / r.width, sum.y / weight / r.height);
    }
    static float Reach(Texture2D t, Rect r, Vector2 pivot)
    {
        int w = (int)r.width, h = (int)r.height;
        var p = t.GetPixels((int)r.x, (int)r.y, w, h);
        float reach = 0; Vector2 anchor = Vector2.Scale(pivot, r.size);
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            if (p[y * w + x].a > .8f) reach = Mathf.Max(reach, Vector2.Distance(anchor, new Vector2(x + .5f, y + .5f)));
        return reach;
    }
    static Rect Isolate(Texture2D t, Rect r, Vector2 expected)
    {
        int w = (int)r.width, h = (int)r.height; var p = t.GetPixels((int)r.x, (int)r.y, w, h);
        var seen = new bool[p.Length]; var queue = new int[p.Length]; float best = 0; Rect answer = r;
        for (int n = 0; n < p.Length; n++)
        {
            if (seen[n] || p[n].a < .65f) continue;
            int read = 0, write = 0, leather = 0, minX = w, minY = h, maxX = 0, maxY = 0;
            float closest = float.MaxValue; queue[write++] = n; seen[n] = true;
            while (read < write)
            {
                int at = queue[read++], x = at % w, y = at / w; if (Leather(p[at])) leather++;
                if (p[at].a > .65f) closest = Mathf.Min(closest, Vector2.SqrMagnitude(r.position + new Vector2(x + .5f, y + .5f) - expected));
                minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x); minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                {
                    int xx = x + dx, yy = y + dy; if (xx < 0 || yy < 0 || xx >= w || yy >= h) continue;
                    int j = yy * w + xx; if (seen[j] || p[j].a < .65f) continue; seen[j] = true; queue[write++] = j;
                }
            }
            float score = (write + Mathf.Min(leather, 50) * 100) / (1 + closest * .01f);
            if (write < 8 || score <= best) continue;
            best = score; minX = Mathf.Max(0, minX - 1); minY = Mathf.Max(0, minY - 1); maxX = Mathf.Min(w - 1, maxX + 1); maxY = Mathf.Min(h - 1, maxY + 1);
            answer = new Rect(r.x + minX, r.y + minY, maxX - minX + 1, maxY - minY + 1);
        }
        return answer;
    }
}
