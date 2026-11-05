using UnityEngine.SceneManagement;
using UnityEngine;

public class SceneLoader : MonoBehaviour
{
    public int expNumber;
    // This function can be linked to a UI Button in the Inspector
    // Call this method from a UI Button
    public void LoadSceneByIndex()
    {
         SceneManager.LoadScene(expNumber);


    }
}