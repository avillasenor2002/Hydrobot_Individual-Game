using UnityEngine;

public class QuitButton : MonoBehaviour
{
    /// <summary>
    /// Call this function from a UI Button's OnClick event.
    /// Quits the game in a build or stops play mode in the editor.
    /// </summary>
    public void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false; // stops play mode in editor
#else
        Application.Quit(); // quits the actual game build
#endif
        Debug.Log("QuitApplication called.");
    }
}
