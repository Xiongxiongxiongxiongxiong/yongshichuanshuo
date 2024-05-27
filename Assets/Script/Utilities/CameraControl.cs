using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
public class CameraControl : MonoBehaviour
{
    [Header("事件监听")] public VoidEventSO afterSceneLoadedEvent;
    
    private CinemachineConfiner2D confiner2D;
    public CinemachineImpulseSource inpuImpulseSource;
    public VoidEventSO cameraShakeEvent;
    
    private void Awake()
    {
        confiner2D = GetComponent<CinemachineConfiner2D>();
    }

    private void OnEnable()
    {
        cameraShakeEvent.onEventRaised += OnCameraShakeEvent;
        afterSceneLoadedEvent.onEventRaised += OnAfterSceneLoadEvent;
    }




    private void OnDisable()
    {
        cameraShakeEvent.onEventRaised -= OnCameraShakeEvent;
        afterSceneLoadedEvent.onEventRaised -= OnAfterSceneLoadEvent;
    }

    private void OnAfterSceneLoadEvent()
    {
        GetNewCameraBounds();
    }
    
    
    private void OnCameraShakeEvent()
    {
        inpuImpulseSource.GenerateImpulse();
    }
    
    
    // private void Start()
    // {
    //
    //     GetNewCameraBounds();
    // }


    private void GetNewCameraBounds()
    {
        var obj = GameObject.FindGameObjectWithTag("Bounds");
        if (obj ==null)
        {
            return;
        }
        confiner2D.m_BoundingShape2D = obj.GetComponent<PolygonCollider2D>();
        //清理摄像机缓存
        confiner2D.InvalidateCache();
    }
    
}
