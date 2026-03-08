using UnityEngine;
using UnityEngine.SceneManagement;

// ATTACH TO SCENEMANAGER OBJECT IN THE SCENE (OR CREATE A NEW EMPTY GAMEOBJECT AND ATTACH THIS TO IT), must be present in every scene that needs to load other scenes
// and make sure the scene(s) you want to load are added to Build Settings (File > Build Settings > Scenes In Build)
// to use, place under OnCLick() for desired button, and type the name of the scene or build index you want to load (make sure it's added to Build Settings)  
public class SceneLoader : MonoBehaviour
{
    // Function to load a scene by its name
    public void LoadSceneByName(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    // Function to load a scene by its build index
    public void LoadSceneByIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }
}
