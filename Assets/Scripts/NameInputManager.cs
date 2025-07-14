using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class NameInputManager : MonoBehaviour
{
	public InputField nameInputField;   // Assign in Inspector
	public InputField emailInputField;  // Assign in Inspector
	public Button submitButton;         // Assign in Inspector

	void Start()
	{
		submitButton.onClick.AddListener(OnSubmit);
	}

	void OnSubmit()
	{
		string playerName = nameInputField.text.Trim();
		string playerEmail = emailInputField.text.Trim();

		if (!string.IsNullOrEmpty(playerName) && IsValidEmail(playerEmail))
		{
			// Save name and email to PlayerPrefs
			PlayerPrefs.SetString("PlayerName", playerName);
			PlayerPrefs.SetString("PlayerEmail", playerEmail);
			PlayerPrefs.Save();
			// Load the main game scene
			SceneManager.LoadScene("menu"); // Replace with your game scene name
		}
		else
		{
			Debug.LogWarning("Please enter a valid name and email!");
			// Optionally add UI feedback
		}
	}

	// Basic email validation
	private bool IsValidEmail(string email)
	{
		if (string.IsNullOrEmpty(email))
			return false;

		// Simple check for @ and . characters
		return email.Contains("@") && email.Contains(".") && email.Length > 5;
	}
}