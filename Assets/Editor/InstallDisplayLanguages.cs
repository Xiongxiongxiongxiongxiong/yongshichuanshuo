using System;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using TMPro;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

public static class InstallDisplayLanguages
{
    [DllImport("kernel32.dll",CharSet=CharSet.Unicode)]static extern int LCMapStringEx(string locale,uint flags,string source,int count,[Out] char[] target,int size,IntPtr version,IntPtr reserved,IntPtr sort);
    static string Traditional(string text){var b=new char[text.Length*3+20];int count=LCMapStringEx("zh-TW",0x04000000,text,text.Length,b,b.Length,IntPtr.Zero,IntPtr.Zero,IntPtr.Zero);if(count<=0)throw new Exception("Traditional conversion failed");return new string(b,0,count).Replace("并","並");}
    public static string RefreshTables()
    {
        var owner=UnityEngine.Object.FindObjectOfType<GameLanguage>(true);
        var rows=System.IO.File.ReadAllLines("Assets/Localization/UITranslations.txt").Where(l=>l.Contains("|")).Select(l=>l.Split('|')).ToArray();
        for(int i=0;i<3;i++){foreach(var r in rows)owner.tables[i].AddEntry(r[0],i==0?r[0]:i==1?r[1]:Traditional(r[0]));EditorUtility.SetDirty(owner.tables[i]);EditorUtility.SetDirty(owner.tables[i].SharedData);}
        owner.fonts[0].fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>{owner.fonts[2]};
        owner.fonts[1].fallbackFontAssetTable=new System.Collections.Generic.List<TMP_FontAsset>{owner.fonts[0],owner.fonts[2]};
        string missing;owner.fonts[2].TryAddCharacters(string.Concat(rows.Select(r=>Traditional(r[0])))+"简体中文繁體中文",out missing);
        foreach(var font in owner.fonts){foreach(var atlas in font.atlasTextures)if(atlas!=null&&!AssetDatabase.Contains(atlas))AssetDatabase.AddObjectToAsset(atlas,font);EditorUtility.SetDirty(font);}
        AssetDatabase.SaveAssets();return rows.Length+" keys refreshed; traditional missing="+missing;
    }
    public static string Install()
    {
        var menu=UnityEngine.Object.FindObjectOfType<EmberTitleMenu>(true);
        if(Application.isPlaying||menu==null)throw new InvalidOperationException("Open persistent in Edit Mode.");
        if(menu.GetComponent<DisplayLanguageSettings>()!=null)throw new InvalidOperationException("Already installed.");
        System.IO.Directory.CreateDirectory("Docs/AI/DisplayLanguages");
        EditorSceneManager.SaveScene(menu.gameObject.scene,"Docs/AI/DisplayLanguages/persistent-before.unity",true);
        AssetDatabase.Refresh();
        var settings=LocalizationEditorSettings.ActiveLocalizationSettings;
        if(settings==null){settings=ScriptableObject.CreateInstance<LocalizationSettings>();AssetDatabase.CreateAsset(settings,"Assets/Localization/LocalizationSettings.asset");LocalizationEditorSettings.ActiveLocalizationSettings=settings;}
        string[] codes={"zh-Hans","en","zh-Hant"};
        foreach(var code in codes)if(!LocalizationEditorSettings.GetLocales().Any(l=>l.Identifier.Code==code)){var locale=Locale.CreateLocale(code);AssetDatabase.CreateAsset(locale,"Assets/Localization/"+code+".asset");LocalizationEditorSettings.AddLocale(locale);}
        var collection=LocalizationEditorSettings.CreateStringTableCollection("GameUI","Assets/Localization");
        var rows=System.IO.File.ReadAllLines("Assets/Localization/UITranslations.txt").Where(l=>l.Contains("|")).Select(l=>l.Split('|')).ToArray();
        var owner=menu.gameObject.AddComponent<GameLanguage>();owner.tables=new StringTable[3];owner.fonts=new TMP_FontAsset[3];owner.legacyFonts=new Font[3];
        var assets=LocalizationEditorSettings.CreateAssetTableCollection("GameFonts","Assets/Localization");
        string[] paths={"Assets/UI/RedKnight/Fonts/SimHei.ttf","Assets/Localization/Fonts/English.ttf","Assets/Localization/Fonts/TraditionalChinese.ttc"};
        for(int i=0;i<3;i++)
        {
            var table=collection.StringTables.First(t=>t.LocaleIdentifier.Code==codes[i]);owner.tables[i]=table;
            foreach(var row in rows)table.AddEntry(row[0],i==0?row[0]:i==1?row[1]:Traditional(row[0]));
            owner.legacyFonts[i]=AssetDatabase.LoadAssetAtPath<Font>(paths[i]);
            if(owner.legacyFonts[i]==null)throw new InvalidOperationException("Font import failed: "+paths[i]);
            var font=TMP_FontAsset.CreateFontAsset(owner.legacyFonts[i],48,6,UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
            font.name="GameUI-"+codes[i];font.isMultiAtlasTexturesEnabled=true;
            string fontPath="Assets/Localization/Fonts/"+font.name+".asset";AssetDatabase.CreateAsset(font,fontPath);
            AssetDatabase.AddObjectToAsset(font.material,font);
            foreach(var atlas in font.atlasTextures)if(atlas!=null)AssetDatabase.AddObjectToAsset(atlas,font);
            string glyphs=string.Concat(rows.Select(r=>i==0?r[0]:i==1?r[1]:Traditional(r[0])))+"简体中文繁體中文English0123456789% ×↑↓";
            string missing;font.TryAddCharacters(glyphs,out missing);
            if(!string.IsNullOrEmpty(missing))Debug.LogWarning(codes[i]+" missing glyphs: "+missing);
            foreach(var atlas in font.atlasTextures)if(atlas!=null&&!AssetDatabase.Contains(atlas))AssetDatabase.AddObjectToAsset(atlas,font);
            owner.fonts[i]=font;
            var assetTable=assets.AssetTables.First(t=>t.LocaleIdentifier.Code==codes[i]);assetTable.AddEntry("UIFont",AssetDatabase.AssetPathToGUID(fontPath));
            var aa=UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;aa.CreateOrMoveEntry(AssetDatabase.AssetPathToGUID(fontPath),aa.DefaultGroup);
            EditorUtility.SetDirty(table);EditorUtility.SetDirty(font);EditorUtility.SetDirty(assetTable);
        }
        EditorUtility.SetDirty(collection.SharedData);EditorUtility.SetDirty(assets.SharedData);EditorUtility.SetDirty(settings);
        var panel=(RectTransform)menu.settingsPanel.transform.Find("Settings Panel");panel.sizeDelta=new Vector2(900,900);
        ((RectTransform)panel.Find("Panel Backdrop")).sizeDelta=new Vector2(850,850);
        foreach(Transform t in panel)
        {
            var rect=t as RectTransform;if(rect==null)continue;
            float y=t.name=="设  置"?370:t.name=="立即生效，并自动保存"?315:t.name=="Master Volume"?220:t.name=="Music Volume"?140:t.name=="Effects Volume"?60:t.name=="Fullscreen"?-35:t.name=="Close Settings"?-350:rect.anchoredPosition.y;
            rect.anchoredPosition=new Vector2(rect.anchoredPosition.x,y);
        }
        var display=menu.gameObject.AddComponent<DisplayLanguageSettings>();display.language=owner;
        display.resolutionDropdown=Dropdown(panel,"Resolution","分辨率",-135,owner.fonts[0]);
        display.languageDropdown=Dropdown(panel,"Language","语言",-225,owner.fonts[0]);
        EditorUtility.SetDirty(owner);EditorUtility.SetDirty(display);AssetDatabase.SaveAssets();
        foreach(var font in owner.fonts)if(!AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(font)).Any(a=>a is Material))throw new Exception("Font material not persisted.");
        EditorSceneManager.MarkSceneDirty(menu.gameObject.scene);EditorSceneManager.SaveScene(menu.gameObject.scene);
        return "Installed display/language dropdowns; "+rows.Length+" keys x 3 locales; embedded fonts and material subassets persisted.";
    }
    static TMP_Dropdown Dropdown(Transform parent,string name,string label,float y,TMP_FontAsset font)
    {
        var caption=new GameObject(label,typeof(RectTransform),typeof(TextMeshProUGUI));caption.transform.SetParent(parent,false);
        var r=(RectTransform)caption.transform;r.sizeDelta=new Vector2(190,50);r.anchoredPosition=new Vector2(-265,y);
        var text=caption.GetComponent<TextMeshProUGUI>();text.font=font;text.fontSize=27;text.text=label;text.alignment=TextAlignmentOptions.MidlineRight;text.raycastTarget=false;text.color=new Color(.98f,.91f,.76f);
        var go=TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());go.name=name;go.transform.SetParent(parent,false);
        var rect=(RectTransform)go.transform;rect.sizeDelta=new Vector2(400,56);rect.anchoredPosition=new Vector2(80,y);
        var dropdown=go.GetComponent<TMP_Dropdown>();
        foreach(var t in go.GetComponentsInChildren<TMP_Text>(true)){t.font=font;t.fontSize=25;t.color=new Color(.98f,.91f,.76f);t.enableWordWrapping=false;}
        foreach(var image in go.GetComponentsInChildren<UnityEngine.UI.Image>(true))image.color=new Color(.12f,.14f,.15f,1);
        StyleDropdown(dropdown);
        dropdown.template.sizeDelta=new Vector2(0,240);
        return dropdown;
    }
    public static void StyleDropdown(TMP_Dropdown dropdown)
    {
        // DefaultControls without sprite resources creates invisible arrows/checkmarks.
        foreach(var image in dropdown.GetComponentsInChildren<UnityEngine.UI.Image>(true))
        {
            if(image.name=="Arrow"||image.name=="Item Checkmark")
            {
                image.gameObject.SetActive(false);
                continue;
            }
        }
        if(dropdown.transform.Find("Arrow Glyph")==null)
        {
            var go=new GameObject("Arrow Glyph",typeof(RectTransform),typeof(TextMeshProUGUI));go.transform.SetParent(dropdown.transform,false);
            var rect=(RectTransform)go.transform;rect.anchorMin=rect.anchorMax=new Vector2(1,.5f);rect.anchoredPosition=new Vector2(-22,0);rect.sizeDelta=new Vector2(30,40);
            var text=go.GetComponent<TextMeshProUGUI>();text.font=dropdown.captionText.font;text.text="v";text.fontSize=25;text.alignment=TextAlignmentOptions.Center;text.color=new Color(.98f,.81f,.48f);text.raycastTarget=false;
        }
        foreach(var selectable in dropdown.GetComponentsInChildren<UnityEngine.UI.Selectable>(true))
        {
            var colors=selectable.colors;colors.normalColor=Color.white;colors.highlightedColor=colors.selectedColor=new Color(.6f,.5f,.28f);colors.pressedColor=new Color(.4f,.32f,.18f);selectable.colors=colors;
        }
        dropdown.template.pivot=new Vector2(.5f,0);
        dropdown.template.anchorMin=new Vector2(0,1);dropdown.template.anchorMax=new Vector2(1,1);
        dropdown.template.anchoredPosition=Vector2.zero;
        dropdown.template.sizeDelta=new Vector2(0,240);
        var item=dropdown.template.GetComponentInChildren<UnityEngine.UI.Toggle>(true).transform as RectTransform;
        item.sizeDelta=new Vector2(item.sizeDelta.x,44);
        var content=item.parent as RectTransform;content.sizeDelta=new Vector2(content.sizeDelta.x,44);
    }
}
