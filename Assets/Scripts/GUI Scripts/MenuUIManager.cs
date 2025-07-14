using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MenuUIManager : MonoBehaviour
{
	public Text playerNameText; // Assign in Inspector
	public Button nameInputButton; // Assign in Inspector

	void Start()
	{
		// Display player name
		string playerName = PlayerPrefs.GetString("PlayerName", "Anonymous");
		if (playerNameText != null)
		{
			playerNameText.text = "Player: " + playerName;
		}
		else
		{
			Debug.LogError("playerNameText is not assigned!");
		}

		// Set up button click event
		if (nameInputButton != null)
		{
			nameInputButton.onClick.AddListener(OnNameInputButtonClick);
		}
		else
		{
			Debug.LogError("nameInputButton is not assigned!");
		}
	}

	void OnNameInputButtonClick()
	{
		Debug.Log("Loading NameInputScene");
		SceneManager.LoadScene("NameInputScene"); // Ensure scene name matches
	}
}