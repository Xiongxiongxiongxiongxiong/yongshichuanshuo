using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class DisplayLanguageSettings : MonoBehaviour
{
    public TMP_Dropdown resolutionDropdown,languageDropdown;
    public GameLanguage language;
    static readonly Vector2Int[] Sizes={new Vector2Int(1280,720),new Vector2Int(1366,768),new Vector2Int(1600,900),new Vector2Int(1920,1080),new Vector2Int(2560,1440)};
    void Start()
    {
        var labels=new List<string>();int selected=3;
        for(int i=0;i<Sizes.Length;i++){labels.Add(Sizes[i].x+" × "+Sizes[i].y);if(Sizes[i].x==Screen.width&&Sizes[i].y==Screen.height)selected=i;}
        resolutionDropdown.ClearOptions();resolutionDropdown.AddOptions(labels);
        selected=Mathf.Clamp(PlayerPrefs.GetInt("EmberWalker.Settings.Resolution",selected),0,Sizes.Length-1);
        resolutionDropdown.SetValueWithoutNotify(selected);
        languageDropdown.ClearOptions();languageDropdown.AddOptions(new List<string>{"简体中文","English","繁體中文"});
        languageDropdown.SetValueWithoutNotify(language.Language);
        resolutionDropdown.onValueChanged.AddListener(ChangeResolution);languageDropdown.onValueChanged.AddListener(language.SetLanguage);
        language.Changed+=SyncLanguage;
        if(PlayerPrefs.HasKey("EmberWalker.Settings.Resolution"))Apply(selected);
    }
    void SyncLanguage(){languageDropdown.SetValueWithoutNotify(language.Language);}
    void Apply(int index){if(!Application.isEditor)Screen.SetResolution(Sizes[index].x,Sizes[index].y,Screen.fullScreenMode);}
    public void ChangeResolution(int index)
    {
        if(index<0||index>=Sizes.Length)return;
        PlayerPrefs.SetInt("EmberWalker.Settings.Resolution",index);PlayerPrefs.Save();Apply(index);
    }
    void OnDestroy(){resolutionDropdown.onValueChanged.RemoveListener(ChangeResolution);languageDropdown.onValueChanged.RemoveListener(language.SetLanguage);language.Changed-=SyncLanguage;}
}
