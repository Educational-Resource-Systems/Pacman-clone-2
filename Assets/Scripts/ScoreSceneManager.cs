using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; // Added for Button


public class ScoreSceneManager : MonoBehaviour
{
    public Button backButton; // Optional: Assign to return to Menu

    void Start()
    {
        if (backButton != null)
            backButton.onClick.AddListener(() => SceneManager.LoadScene("Menu"));
    }
}