using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    public string sceneswitch = "SampleScene";

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Switch();
        }
    }

    public void Switch()
    {
        string targetScene = string.IsNullOrWhiteSpace(sceneswitch) || !Application.CanStreamedLevelBeLoaded(sceneswitch)
            ? "SampleScene"
            : sceneswitch;
        SceneManager.LoadScene(targetScene);
    }
}
