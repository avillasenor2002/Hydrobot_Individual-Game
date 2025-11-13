using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSelector : MonoBehaviour
{
    /// <summary>
    /// Call this from any UI button and pass in the scene name.
    /// </summary>
    /// <param name="sceneName">Name of the scene to load</param>
    public void LoadSceneByName(string sceneName)
    {
        if (!string.IsNullOrEmpty(sceneName))
        {
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            Debug.LogWarning("Scene name is empty or null.");
        }
    }
}
