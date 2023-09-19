using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuDisplay : MonoBehaviour
{
    [SerializeField] private string sceneToLoad = "Gameplay";
    
    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
    }

    public void StartServer()
    {
        NetworkManager.Singleton.StartServer();
        NetworkManager.Singleton.SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }
}
