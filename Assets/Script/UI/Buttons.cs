using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Buttons : MonoBehaviour
{
    public Button B;

    public VoidEventSO NewGameEvent;

    private SceneLoader sceneLoader;
    // Start is called before the first frame update
    void Start()
    {
        sceneLoader = GameObject.Find("SceneLoadManager").GetComponent<SceneLoader>();
        B.onClick.AddListener(GGG);
        
// B.onClick.AddListener((() =>
// {
//     NewGameEvent.onEventRaised += sceneLoader.NewGame;
// }));
        
    }

    public void GGG()
    {
        Debug.Log("111");
        sceneLoader.LoadEventSO.RaiseLoadRequesEvent(sceneLoader.firstLoadScene,sceneLoader.firstPosition,true);
    }
    //
    // private void OnEnable()
    // {
    //    
    // }


    // Update is called once per frame
    void Update()
    {
        
    }
}
