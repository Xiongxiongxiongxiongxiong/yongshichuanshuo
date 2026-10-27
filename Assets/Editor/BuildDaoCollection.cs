using System.IO;
using System.Linq;
using UnityEditor;

/// <summary>Dao content uses the same calibrated authoring path as the sword collection.</summary>
public static class BuildDaoCollection
{
    public const string Root = "Assets/player/DaoCollection/";
    public const string Evidence = "Docs/AI/DaoCollection/";
    public static readonly string[] Ids = { "iron_dao", "iron_cleaver", "jade_dao", "verdant_cleaver", "frost_dao", "tide_cleaver", "amethyst_dao", "thunder_cleaver", "dragon_dao", "phoenix_cleaver" };
    public static readonly string[] Names = { "旧铁柳叶刀", "厚背砍刀", "翠玉雁翎刀", "青纹破阵刀", "霜月刀", "沧浪刀", "紫月幽刃", "暗雷斩魄刀", "炎龙偃月刀", "曜日凤翎刀" };
    [MenuItem("Tools/Player/Build Ten Frame Locked Dao")]
    public static void Build()
    { BuildSwordCollection.BuildSet(Root, "dao.", Ids, Names, "弯刀", "重型战刀", Evidence, 10); }
    public static void BuildAvailable()
    {
        int count = Ids.TakeWhile(id => File.Exists(Root + id + "/Main.png") && File.Exists(Root + id + "/Supplement.png") && File.Exists(Root + id + "/Slices.json")).Count();
        BuildSwordCollection.BuildSet(Root, "dao.", Ids, Names, "弯刀", "重型战刀", Evidence, count);
    }
    [MenuItem("Tools/Inventory/Grant Ten Designed Dao Once")]
    public static void Grant() { BuildSwordCollection.GrantSet(Ids, "dao.", Evidence); }
}
