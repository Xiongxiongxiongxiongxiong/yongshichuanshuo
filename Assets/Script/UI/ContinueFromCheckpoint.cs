using UnityEngine;

public class ContinueFromCheckpoint : MonoBehaviour
{
    public void ContinueGame()
    {
        var loader = FindObjectOfType<SceneLoader>();
        if (loader != null) loader.ContinueGame();
    }
}
