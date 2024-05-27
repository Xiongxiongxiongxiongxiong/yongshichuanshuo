using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public PlayerStatBar playerStatBar;
    [Header("事件监听")] 
    public CharacterEventSO healtEvent;

    public SceneLoadEventSO LoadEvent;

    private void OnEnable()
    {
        healtEvent.OnEventRaised += OnHealthEvent;
        LoadEvent.LoadRequesEvent += OnLoadEvent;
    }

    private void OnLoadEvent(GameSceneSO sceneToLoad, Vector3 arg1, bool arg2)
    {
        bool isMenu = sceneToLoad.sceneType == SceneType.Menu;
        
            playerStatBar.gameObject.SetActive(!isMenu);
        
    }

    private void OnHealthEvent(Character character)
    {
        var persentage = character.CurrentHp / character.maxHp;
        playerStatBar.OnHealthChange(persentage);
        
    }

    private void OnDisable()
    {
        healtEvent.OnEventRaised -= OnHealthEvent;
        LoadEvent.LoadRequesEvent -= OnLoadEvent;
    }
}
