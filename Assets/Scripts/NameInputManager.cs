using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems; // Added for EventSystem

public class NameInputManager : MonoBehaviour
{
	public InputField nameInputField; // Assign in Inspector
	public InputField emailInputField; // Assign in Inspector
	public Button submitButton; // Assign in Inspector
	public Text feedbackText; // Optional: Assign for feedback

	void Start()
	{
		submitButton.onClick.AddListener(SubmitPlayerInfo);
	}

	void Update()
	{
		// Handle Tab key to switch between input fields
		if (Input.GetKeyDown(KeyCode.Tab))
		{
			if (EventSystem.current.currentSelectedGameObject == nameInputField.gameObject)
			{
				EventSystem.current.SetSelectedGameObject(emailInputField.gameObject);
			}
			else if (EventSystem.current.currentSelectedGameObject == emailInputField.gameObject)
			{
				EventSystem.current.SetSelectedGameObject(nameInputField.gameObject); // Optional: Loop back
			}
		}
	}

	void SubmitPlayerInfo()
	{
		string playerName = nameInputField.text;
		string playerEmail = emailInputField.text;

		if (string.IsNullOrEmpty(playerName) || string.IsNullOrEmpty(playerEmail))
		{
			if (feedbackText != null)
				feedbackText.text = "Please enter both name and email.";
			Debug.LogWarning("Name and email are required!");
			return;
		}

		if (!IsValidEmail(playerEmail))
		{
			if (feedbackText != null)
				feedbackText.text = "Please enter a valid email.";
			Debug.LogWarning("Invalid email format!");
			return;
		}

		PlayerPrefs.SetString("PlayerName", playerName);
		PlayerPrefs.SetString("PlayerEmail", playerEmail);
		PlayerPrefs.Save();

		if (feedbackText != null)
			feedbackText.text = "Info saved! Loading menu...";
		Debug.Log(string.Format("Saved name: {0}, email: {1}", playerName, playerEmail));

		SceneManager.LoadScene("game");
	}

	bool IsValidEmail(string email)
	{
		try
		{
			var addr = new System.Net.Mail.MailAddress(email);
			return addr.Address == email;
		}
		catch
		{
			return false;
		}
	}
}