using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportPoint : MonoBehaviour,IInteractable
{
    public SceneLoadEventSO loadEventsSO;
    public GameSceneSO sceneToGo; //要传送的场景
    public Vector3 positionToGo;
    public void TriggerAction()
    {
        loadEventsSO.RaiseLoadRequesEvent(sceneToGo,positionToGo,true);
    }
}
