using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioDefination : MonoBehaviour
{
    public PlayerAudioEventSO playerAudioEvent;
    public AudioClip audioClip;
    public bool playerOnEnable;

    private void OnEnable()
    {
        if (playerOnEnable)
        {
            PlayerAudioClip();
        }
    }

    public void PlayerAudioClip()
    {
        playerAudioEvent.OnEventRaised(audioClip);
    }
}
