using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
[CreateAssetMenu(menuName = "Event/SceneLoadEventSO")]
public class SceneLoadEventSO : ScriptableObject
{
    public UnityAction<GameSceneSO, Vector3, bool> LoadRequesEvent;
/// <summary>
/// 场景加载请求
/// </summary>
/// <param name="locationToLoad">要去的场景</param>
/// <param name="posToGo">Player的坐标</param>
/// <param name="fadeScreen">是否渐入渐出</param>
    public void RaiseLoadRequesEvent(GameSceneSO locationToLoad, Vector3 posToGo, bool fadeScreen)
    {
        LoadRequesEvent?.Invoke(locationToLoad,posToGo,fadeScreen);
    }
}
