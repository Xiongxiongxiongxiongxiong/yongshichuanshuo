using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
public class Memu : MonoBehaviour
{
    public GameObject newGameButton;
    private Coroutine selectButtonCoroutine;

    private void OnEnable()
    {
        selectButtonCoroutine = StartCoroutine(SelectNewGameButton());
    }

    private IEnumerator SelectNewGameButton()
    {
        // Allow scene EventSystems to finish OnEnable before selecting a button.
        yield return null;

        var eventSystem = EventSystem.current;
        if (eventSystem == null)
        {
            // Menu can also be played directly, without the persistent scene.
            var eventSystemObject = new GameObject("Menu EventSystem");
            eventSystemObject.transform.SetParent(transform, false);
            eventSystem = eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
        }

        if (newGameButton != null)
        {
            eventSystem.SetSelectedGameObject(newGameButton);
        }

        selectButtonCoroutine = null;
    }

    private void OnDisable()
    {
        if (selectButtonCoroutine != null)
        {
            StopCoroutine(selectButtonCoroutine);
            selectButtonCoroutine = null;
        }
    }

   public  void ExitGame()
    {
        Debug.Log("退出游戏");
        
        Application.Quit();
    }
}
