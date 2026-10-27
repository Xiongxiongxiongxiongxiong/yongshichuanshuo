using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;

// One persistent owner; scene text adapters also handle UI created by existing presenters at runtime.
[DefaultExecutionOrder(-1000)]
public sealed class GameLanguage : MonoBehaviour
{
    public TMP_FontAsset[] fonts;
    public Font[] legacyFonts;
    public StringTable[] tables;
    public UnityEngine.UI.Image titleBackground;
    public Sprite[] titleArt;
    public static GameLanguage Current { get; private set; }
    public int Language { get; private set; }
    public event Action Changed;
    string[] keys;
    void Awake()
    {
        Current=this; Language=Mathf.Clamp(PlayerPrefs.GetInt("EmberWalker.Settings.Language",0),0,2);
        keys=tables[0].Values.Select(e=>e.Key).OrderByDescending(k=>k.Length).ToArray();
        SceneManager.sceneLoaded+=OnSceneLoaded;
    }
    IEnumerator Start() { yield return LocalizationSettings.InitializationOperation; SetLanguage(Language); Scan(); }
    void OnSceneLoaded(Scene s,LoadSceneMode mode) { StartCoroutine(ScanNextFrame()); }
    IEnumerator ScanNextFrame() { yield return null; Scan(); }
    float nextScan;
    void Update() { if(Time.unscaledTime>=nextScan){nextScan=Time.unscaledTime+1;Scan();} }
    public void SetLanguage(int index)
    {
        Language=Mathf.Clamp(index,0,2);
        string code=new[]{"zh-Hans","en","zh-Hant"}[Language];
        var locale=LocalizationSettings.AvailableLocales.GetLocale(code);
        if(locale!=null)LocalizationSettings.SelectedLocale=locale;
        if(titleBackground!=null && titleArt!=null && titleArt.Length>Language)titleBackground.sprite=titleArt[Language];
        PlayerPrefs.SetInt("EmberWalker.Settings.Language",Language);PlayerPrefs.Save();
        Changed?.Invoke();
    }
    public string Translate(string source)
    {
        if(string.IsNullOrEmpty(source)||Language==0||source=="简体中文"||source=="繁體中文")return source;
        var table=tables[Language]; var exact=table.GetEntry(source);
        if(exact!=null)return exact.LocalizedValue;
        // Presenters compose numbers and keyboard hints around translated phrases.
        var output=new System.Text.StringBuilder();int start=0;
        for(int i=0;i<source.Length;)
        {
            string match=null;
            foreach(var key in keys)if(i+key.Length<=source.Length && string.CompareOrdinal(source,i,key,0,key.Length)==0){match=key;break;}
            if(match==null){i++;continue;}
            output.Append(source,start,i-start);output.Append(table.GetEntry(match).LocalizedValue);
            i+=match.Length;start=i;
        }
        output.Append(source,start,source.Length-start);return output.ToString();
    }
    public void Scan()
    {
        // One pass per text type also covers world-space TMP and nested canvases.
        foreach(var t in FindObjectsOfType<TMP_Text>(true))if(t.GetComponent<GameLocalizedText>()==null)t.gameObject.AddComponent<GameLocalizedText>();
        foreach(var t in FindObjectsOfType<UnityEngine.UI.Text>(true))if(t.GetComponent<GameLocalizedText>()==null)t.gameObject.AddComponent<GameLocalizedText>();
        foreach(var t in FindObjectsOfType<TextMesh>(true))if(t.GetComponent<GameLocalizedText>()==null)t.gameObject.AddComponent<GameLocalizedText>();
    }
    void OnDestroy(){SceneManager.sceneLoaded-=OnSceneLoaded;if(Current==this)Current=null;}
}

[DefaultExecutionOrder(1000)]
public sealed class GameLocalizedText : MonoBehaviour
{
    TMP_Text tmp;UnityEngine.UI.Text legacy;TextMesh mesh;
    string source,lastOutput;GameLanguage owner;
    void Awake(){tmp=GetComponent<TMP_Text>();legacy=GetComponent<UnityEngine.UI.Text>();mesh=GetComponent<TextMesh>();if(tmp!=null){tmp.fontSizeMax=tmp.fontSize;tmp.fontSizeMin=Mathf.Min(12,tmp.fontSize);tmp.enableAutoSizing=true;}if(legacy!=null){legacy.resizeTextMaxSize=legacy.fontSize;legacy.resizeTextMinSize=Mathf.Min(10,legacy.fontSize);legacy.resizeTextForBestFit=true;}}
    void OnEnable(){owner=GameLanguage.Current;if(owner!=null)owner.Changed+=Refresh;Refresh();}
    void LateUpdate(){string current=Read();if(current!=lastOutput){source=current;Refresh();}}
    string Read()=>tmp!=null?tmp.text:legacy!=null?legacy.text:mesh.text;
    void Refresh()
    {
        if(owner==null)return;
        // Dropdown captions and presenters may replace their text immediately before the locale event.
        if(source==null || Read()!=lastOutput)source=Read();lastOutput=owner.Translate(source);
        if(tmp!=null){tmp.font=owner.fonts[owner.Language];tmp.text=lastOutput;}
        else if(legacy!=null){legacy.font=owner.legacyFonts[owner.Language];legacy.text=lastOutput;}
        else {mesh.font=owner.legacyFonts[owner.Language];GetComponent<MeshRenderer>().sharedMaterial=mesh.font.material;mesh.text=lastOutput;}
    }
    void OnDisable(){if(owner!=null)owner.Changed-=Refresh;}
}
