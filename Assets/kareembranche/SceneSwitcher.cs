using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneSwitcher : MonoBehaviour
{
    public string sceneswitch;



    public void Switch()
    {
        SceneManager.LoadScene(sceneswitch);
    }
}
