using UnityEngine;

public class TownSafetyReturn : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other)
    {
        if(other.GetComponentInParent<PlayerController>()==null)return;
        var loader=FindObjectOfType<SceneLoader>();
        if(loader!=null&&!loader.IsLoading)loader.ReturnToTown();
    }
}
