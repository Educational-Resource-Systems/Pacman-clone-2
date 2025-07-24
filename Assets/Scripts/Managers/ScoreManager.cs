using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking; // Required for UnityWebRequest

public class ScoreManager : MonoBehaviour
{
	private string TopScoresURL = "https://ers-dev.com/ERS/_pacman/build5/topscores.php";
	private string username;
	private int _highscore = 99999; // Initial fallback value
	private int _lowestHigh = 99999;
	private bool _scoresRead;
	private bool _isTableFound;
	private bool scoreSubmitted = false;

	public class Score
	{
		public string name { get; set; }
		public int score { get; set; }

		public Score(string n, int s)
		{
			name = n;
			score = s;
		}

		public Score(string n, string s)
		{
			name = n;
			score = Int32.Parse(s);
		}
	}

	private List<Score> scoreList = new List<Score>(10);

	void Awake()
	{
		// Ensure ScoreManager is persistent across scenes
		if (FindObjectsOfType<ScoreManager>().Length > 1)
		{
			Destroy(gameObject);
		}
		else
		{
			DontDestroyOnLoad(gameObject);
			// Fetch high score immediately when this manager is created
			// This ensures it's fetched once and kept updated
			StartCoroutine(GetHighestScore());
		}
	}

	void OnEnable()
	{
		scoreSubmitted = false;
		// If the game scene loads and Awake hasn't fetched it yet, or if this object is re-enabled,
		// we can safely re-attempt fetching without resetting _highscore to 99999 initially.
		// However, relying on Awake for a persistent object is generally better.
	}

	// OnLevelWasLoaded is deprecated. Consider using SceneManager.sceneLoaded += OnSceneLoaded;
	// However, for Unity 5.6.7f1, it still works.
	void OnLevelWasLoaded(int level)
	{
		if (level == 2) // Scores scene
		{
			StartCoroutine(UpdateGUIText());
		}
		// IMPORTANT: Removed the _lowestHigh = _highscore = 99999; line for game scene
		// because we want the fetched high score to persist and not be reset.
		if (level == 1) // Game scene
		{
			// You can optionally force a re-fetch here if you want to ensure the latest score
			// from the database is always displayed when the game scene loads.
			// StartCoroutine(GetHighestScore()); 
		}
	}

	public IEnumerator SubmitScore(string playerName, string email, int score)
	{
		if (scoreSubmitted)
		{
			Debug.Log("Score already submitted, skipping");
			yield break;
		}

		if (string.IsNullOrEmpty(playerName) || string.IsNullOrEmpty(email))
		{
			Debug.LogWarning("Cannot submit score: PlayerName or Email is empty");
			yield break;
		}

		scoreSubmitted = true;
		Debug.Log("Submitting score: " + score + ", Name: " + playerName + ", Email: " + email);

		// Using UnityWebRequest for POST
		WWWForm form = new WWWForm();
		form.AddField("player_name", playerName);
		form.AddField("email", email);
		form.AddField("score", score.ToString()); // score needs to be string for AddField

		UnityWebRequest submitRequest = UnityWebRequest.Post(TopScoresURL, form);

		// Set timeout
		submitRequest.timeout = 10; // in seconds

		yield return submitRequest.SendWebRequest(); // Use SendWebRequest for UnityWebRequest

		if (submitRequest.result == UnityWebRequest.Result.ConnectionError || submitRequest.result == UnityWebRequest.Result.ProtocolError)
		{
			Debug.LogError("ERROR SUBMITTING SCORE: " + submitRequest.error);
		}
		else
		{
			string responseText = submitRequest.downloadHandler.text;
			Debug.Log("Server response: " + responseText);

			if (responseText == "Score saved successfully")
			{
				Debug.Log("Score submitted successfully!");
				PlayerPrefs.DeleteKey("PlayerName");
				PlayerPrefs.DeleteKey("PlayerEmail");
				PlayerPrefs.DeleteKey("PlayerScore");
				PlayerPrefs.Save();
			}
			else
			{
				Debug.LogError("Server error: " + responseText);
			}
		}

		// Re-read scores after submission to update current high score in game
		yield return StartCoroutine(GetHighestScore()); // Use GetHighestScore to update _highscore
		yield return StartCoroutine(UpdateGUIText()); // For the scores scene, if active
	}

	IEnumerator GetHighestScore()
	{
		Debug.Log("GETTING HIGHEST SCORE");
		yield return StartCoroutine(ReadScoresFromDB());

		if (scoreList.Count > 0)
		{
			// Ensure the first item is a valid score, not an error message
			if (scoreList[0].score != 1234 && scoreList[0].score != -123 && scoreList[0].name != "TIMEOUT ERROR" && scoreList[0].name != "DATABASE TEMPORARILY UNAVAILABLE")
			{
				_highscore = scoreList[0].score;
				// Update _lowestHigh only if there are enough scores
				if (scoreList.Count > 1) { // Check if there's more than one score to define lowest high
					_lowestHigh = scoreList[scoreList.Count - 1].score;
				} else {
					_lowestHigh = _highscore; // If only one score, lowest high is the high score itself
				}
				Debug.Log($"High score updated to: {_highscore}, Lowest High updated to: {_lowestHigh}");
			}
			else
			{
				Debug.LogWarning($"High score not updated due to an error entry as first score: {scoreList[0].name}, {scoreList[0].score}");
			}
		}
		else
		{
			Debug.LogWarning("Score list is empty after fetching. High score remains " + _highscore);
		}
	}

	IEnumerator UpdateGUIText()
	{
		yield return StartCoroutine(ReadScoresFromDB());

		if (scoreList.Count == 0)
		{
			scoreList.Add(new Score("DATABASE TEMPORARILY UNAVAILABLE", 999999));
		}

		GameObject scoresTextObject = GameObject.FindGameObjectWithTag("ScoresText");
		if (scoresTextObject != null)
		{
			Scores scoresComponent = scoresTextObject.GetComponent<Scores>();
			if (scoresComponent != null)
			{
				scoresComponent.UpdateGUIText(scoreList);
			}
			else
			{
				Debug.LogError("Scores component not found on ScoresText object!");
			}
		}
		else
		{
			Debug.LogError("ScoresText object not found!");
		}
	}

	IEnumerator ReadScoresFromDB()
	{
		Debug.Log("Reading scores from DB: " + TopScoresURL);
		// Using UnityWebRequest for GET
		UnityWebRequest getScoresAttempt = UnityWebRequest.Get(TopScoresURL);

		// Set timeout
		getScoresAttempt.timeout = 10; // in seconds

		yield return getScoresAttempt.SendWebRequest(); // Use SendWebRequest for UnityWebRequest

		if (getScoresAttempt.result == UnityWebRequest.Result.ConnectionError || getScoresAttempt.result == UnityWebRequest.Result.ProtocolError)
		{
			Debug.LogError("ERROR GETTING SCORES from DB: " + getScoresAttempt.error);
			scoreList.Clear(); // Clear any previous scores
			scoreList.Add(new Score("CONNECTION ERROR", 1234));
			_scoresRead = false;
		}
		else
		{
			string rawResponse = getScoresAttempt.downloadHandler.text;
			Debug.Log("Raw response from topscores.php (ReadScoresFromDB): " + rawResponse);

			string[] textlist = rawResponse.Split(new string[] { "\n", "\t" }, StringSplitOptions.RemoveEmptyEntries);

			if (textlist.Length <= 1)
			{
				Debug.LogWarning("No scores or invalid format received from PHP (ReadScoresFromDB). Textlist length: " + textlist.Length);
				scoreList.Clear();
				if (textlist.Length > 0)
				{
					scoreList.Add(new Score(textlist[0], -123)); // Add the message if there's one
				} else {
					scoreList.Add(new Score("NO DATA", 0)); // Indicate no data was received
				}
				_scoresRead = false;
			}
			else
			{
				scoreList.Clear();
				string[] names = new string[Mathf.FloorToInt(textlist.Length / 2)];
				string[] scores = new string[names.Length];

				for (int i = 0; i < textlist.Length; i++)
				{
					if (i % 2 == 0)
					{
						names[Mathf.FloorToInt(i / 2)] = textlist[i];
					}
					else
					{
						scores[Mathf.FloorToInt(i / 2)] = textlist[i];
					}
				}

				for (int i = 0; i < names.Length; i++)
				{
					try
					{
						// Skip the header row if it's present and not a score
						if (names[i].ToLower() == "name" && scores[i].ToLower() == "score") continue;

						scoreList.Add(new Score(names[i], scores[i]));
						Debug.Log($"Parsed Score in ReadScoresFromDB: Name='{names[i]}', Value={scores[i]}");
					}
					catch (Exception e)
					{
						Debug.LogError("Error parsing score in ReadScoresFromDB: " + e.Message + " For Name: " + names[i] + " Score: " + scores[i]);
					}
				}
				_scoresRead = true;
			}
		}
	}

	public int High()
	{
		return _highscore;
	}

	public int LowestHigh()
	{
		return _lowestHigh;
	}
}using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking; // Required for UnityWebRequest

public class ScoreManager : MonoBehaviour
{
	private string TopScoresURL = "https://ers-dev.com/ERS/_pacman/build5/topscores.php";
	private string username;
	private int _highscore = 99999; // Initial fallback value
	private int _lowestHigh = 99999;
	private bool _scoresRead;
	private bool _isTableFound;
	private bool scoreSubmitted = false;

	public class Score
	{
		public string name { get; set; }
		public int score { get; set; }

		public Score(string n, int s)
		{
			name = n;
			score = s;
		}

		public Score(string n, string s)
		{
			name = n;
			score = Int32.Parse(s);
		}
	}

	private List<Score> scoreList = new List<Score>(10);

	void Awake()
	{
		// Ensure ScoreManager is persistent across scenes
		if (FindObjectsOfType<ScoreManager>().Length > 1)
		{
			Destroy(gameObject);
		}
		else
		{
			DontDestroyOnLoad(gameObject);
			// Fetch high score immediately when this manager is created
			// This ensures it's fetched once and kept updated
			StartCoroutine(GetHighestScore());
		}
	}

	void OnEnable()
	{
		scoreSubmitted = false;
		// If the game scene loads and Awake hasn't fetched it yet, or if this object is re-enabled,
		// we can safely re-attempt fetching without resetting _highscore to 99999 initially.
		// However, relying on Awake for a persistent object is generally better.
	}

	// OnLevelWasLoaded is deprecated. Consider using SceneManager.sceneLoaded += OnSceneLoaded;
	// However, for Unity 5.6.7f1, it still works.
	void OnLevelWasLoaded(int level)
	{
		if (level == 2) // Scores scene
		{
			StartCoroutine(UpdateGUIText());
		}
		// IMPORTANT: Removed the _lowestHigh = _highscore = 99999; line for game scene
		// because we want the fetched high score to persist and not be reset.
		if (level == 1) // Game scene
		{
			// You can optionally force a re-fetch here if you want to ensure the latest score
			// from the database is always displayed when the game scene loads.
			// StartCoroutine(GetHighestScore()); 
		}
	}

	public IEnumerator SubmitScore(string playerName, string email, int score)
	{
		if (scoreSubmitted)
		{
			Debug.Log("Score already submitted, skipping");
			yield break;
		}

		if (string.IsNullOrEmpty(playerName) || string.IsNullOrEmpty(email))
		{
			Debug.LogWarning("Cannot submit score: PlayerName or Email is empty");
			yield break;
		}

		scoreSubmitted = true;
		Debug.Log("Submitting score: " + score + ", Name: " + playerName + ", Email: " + email);

		// Using UnityWebRequest for POST
		WWWForm form = new WWWForm();
		form.AddField("player_name", playerName);
		form.AddField("email", email);
		form.AddField("score", score.ToString()); // score needs to be string for AddField

		UnityWebRequest submitRequest = UnityWebRequest.Post(TopScoresURL, form);

		// Set timeout
		submitRequest.timeout = 10; // in seconds

		yield return submitRequest.SendWebRequest(); // Use SendWebRequest for UnityWebRequest

		if (submitRequest.result == UnityWebRequest.Result.ConnectionError || submitRequest.result == UnityWebRequest.Result.ProtocolError)
		{
			Debug.LogError("ERROR SUBMITTING SCORE: " + submitRequest.error);
		}
		else
		{
			string responseText = submitRequest.downloadHandler.text;
			Debug.Log("Server response: " + responseText);

			if (responseText == "Score saved successfully")
			{
				Debug.Log("Score submitted successfully!");
				PlayerPrefs.DeleteKey("PlayerName");
				PlayerPrefs.DeleteKey("PlayerEmail");
				PlayerPrefs.DeleteKey("PlayerScore");
				PlayerPrefs.Save();
			}
			else
			{
				Debug.LogError("Server error: " + responseText);
			}
		}

		// Re-read scores after submission to update current high score in game
		yield return StartCoroutine(GetHighestScore()); // Use GetHighestScore to update _highscore
		yield return StartCoroutine(UpdateGUIText()); // For the scores scene, if active
	}

	IEnumerator GetHighestScore()
	{
		Debug.Log("GETTING HIGHEST SCORE");
		yield return StartCoroutine(ReadScoresFromDB());

		if (scoreList.Count > 0)
		{
			// Ensure the first item is a valid score, not an error message
			if (scoreList[0].score != 1234 && scoreList[0].score != -123 && scoreList[0].name != "TIMEOUT ERROR" && scoreList[0].name != "DATABASE TEMPORARILY UNAVAILABLE")
			{
				_highscore = scoreList[0].score;
				// Update _lowestHigh only if there are enough scores
				if (scoreList.Count > 1) { // Check if there's more than one score to define lowest high
					_lowestHigh = scoreList[scoreList.Count - 1].score;
				} else {
					_lowestHigh = _highscore; // If only one score, lowest high is the high score itself
				}
				Debug.Log($"High score updated to: {_highscore}, Lowest High updated to: {_lowestHigh}");
			}
			else
			{
				Debug.LogWarning($"High score not updated due to an error entry as first score: {scoreList[0].name}, {scoreList[0].score}");
			}
		}
		else
		{
			Debug.LogWarning("Score list is empty after fetching. High score remains " + _highscore);
		}
	}

	IEnumerator UpdateGUIText()
	{
		yield return StartCoroutine(ReadScoresFromDB());

		if (scoreList.Count == 0)
		{
			scoreList.Add(new Score("DATABASE TEMPORARILY UNAVAILABLE", 999999));
		}

		GameObject scoresTextObject = GameObject.FindGameObjectWithTag("ScoresText");
		if (scoresTextObject != null)
		{
			Scores scoresComponent = scoresTextObject.GetComponent<Scores>();
			if (scoresComponent != null)
			{
				scoresComponent.UpdateGUIText(scoreList);
			}
			else
			{
				Debug.LogError("Scores component not found on ScoresText object!");
			}
		}
		else
		{
			Debug.LogError("ScoresText object not found!");
		}
	}

	IEnumerator ReadScoresFromDB()
	{
		Debug.Log("Reading scores from DB: " + TopScoresURL);
		// Using UnityWebRequest for GET
		UnityWebRequest getScoresAttempt = UnityWebRequest.Get(TopScoresURL);

		// Set timeout
		getScoresAttempt.timeout = 10; // in seconds

		yield return getScoresAttempt.SendWebRequest(); // Use SendWebRequest for UnityWebRequest

		if (getScoresAttempt.result == UnityWebRequest.Result.ConnectionError || getScoresAttempt.result == UnityWebRequest.Result.ProtocolError)
		{
			Debug.LogError("ERROR GETTING SCORES from DB: " + getScoresAttempt.error);
			scoreList.Clear(); // Clear any previous scores
			scoreList.Add(new Score("CONNECTION ERROR", 1234));
			_scoresRead = false;
		}
		else
		{
			string rawResponse = getScoresAttempt.downloadHandler.text;
			Debug.Log("Raw response from topscores.php (ReadScoresFromDB): " + rawResponse);

			string[] textlist = rawResponse.Split(new string[] { "\n", "\t" }, StringSplitOptions.RemoveEmptyEntries);

			if (textlist.Length <= 1)
			{
				Debug.LogWarning("No scores or invalid format received from PHP (ReadScoresFromDB). Textlist length: " + textlist.Length);
				scoreList.Clear();
				if (textlist.Length > 0)
				{
					scoreList.Add(new Score(textlist[0], -123)); // Add the message if there's one
				} else {
					scoreList.Add(new Score("NO DATA", 0)); // Indicate no data was received
				}
				_scoresRead = false;
			}
			else
			{
				scoreList.Clear();
				string[] names = new string[Mathf.FloorToInt(textlist.Length / 2)];
				string[] scores = new string[names.Length];

				for (int i = 0; i < textlist.Length; i++)
				{
					if (i % 2 == 0)
					{
						names[Mathf.FloorToInt(i / 2)] = textlist[i];
					}
					else
					{
						scores[Mathf.FloorToInt(i / 2)] = textlist[i];
					}
				}

				for (int i = 0; i < names.Length; i++)
				{
					try
					{
						// Skip the header row if it's present and not a score
						if (names[i].ToLower() == "name" && scores[i].ToLower() == "score") continue;

						scoreList.Add(new Score(names[i], scores[i]));
						Debug.Log($"Parsed Score in ReadScoresFromDB: Name='{names[i]}', Value={scores[i]}");
					}
					catch (Exception e)
					{
						Debug.LogError("Error parsing score in ReadScoresFromDB: " + e.Message + " For Name: " + names[i] + " Score: " + scores[i]);
					}
				}
				_scoresRead = true;
			}
		}
	}

	public int High()
	{
		return _highscore;
	}

	public int LowestHigh()
	{
		return _lowestHigh;
	}
}