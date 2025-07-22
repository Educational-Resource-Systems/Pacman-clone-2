using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
	private string TopScoresURL = "https://ers-dev.com/ERS/_pacman/build5/topscores.php";
	private string username;
	private int _highscore = 99999;
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

	void OnEnable()
	{
		scoreSubmitted = false;
	}

	void OnLevelWasLoaded(int level)
	{
		if (level == 2) // Scores scene
		{
			StartCoroutine(UpdateGUIText());
		}
		if (level == 1) // Game scene
		{
			_lowestHigh = _highscore = 99999;
			StartCoroutine(GetHighestScore());
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

		WWWForm form = new WWWForm();
		form.AddField("player_name", playerName);
		form.AddField("email", email);
		form.AddField("score", score);

		WWW submitRequest = new WWW(TopScoresURL, form);
		float timeout = 10f; // 10-second timeout
		float elapsed = 0f;

		while (!submitRequest.isDone && elapsed < timeout)
		{
			elapsed += Time.deltaTime;
			yield return null;
		}

		if (!submitRequest.isDone)
		{
			Debug.LogError("Score submission timed out after " + timeout + " seconds");
			yield break;
		}

		if (submitRequest.error != null)
		{
			Debug.LogError("ERROR SUBMITTING SCORE: " + submitRequest.error);
		}
		else
		{
			string responseText = submitRequest.text;
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

		yield return StartCoroutine(ReadScoresFromDB());
		yield return StartCoroutine(UpdateGUIText());
	}

	IEnumerator GetHighestScore()
	{
		Debug.Log("GETTING HIGHEST SCORE");
		yield return StartCoroutine(ReadScoresFromDB());

		if (scoreList.Count > 0)
		{
			_highscore = scoreList[0].score;
			_lowestHigh = scoreList[scoreList.Count - 1].score;
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
		WWW getScoresAttempt = new WWW(TopScoresURL);
		float timeout = 10f;
		float elapsed = 0f;

		while (!getScoresAttempt.isDone && elapsed < timeout)
		{
			elapsed += Time.deltaTime;
			yield return null;
		}

		if (!getScoresAttempt.isDone)
		{
			Debug.LogError("Score retrieval timed out after " + timeout + " seconds");
			scoreList.Add(new Score("TIMEOUT ERROR", 1234));
			yield return StartCoroutine(UpdateGUIText());
			yield break;
		}

		if (getScoresAttempt.error != null)
		{
			Debug.LogError("ERROR GETTING SCORES: " + getScoresAttempt.error);
			scoreList.Add(new Score(getScoresAttempt.error, 1234));
			yield return StartCoroutine(UpdateGUIText());
		}
		else
		{
			string[] textlist = getScoresAttempt.text.Split(new string[] { "\n", "\t" }, StringSplitOptions.RemoveEmptyEntries);

			if (textlist.Length <= 1)
			{
				scoreList.Clear();
				scoreList.Add(new Score(textlist[0], -123));
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
						scoreList.Add(new Score(names[i], scores[i]));
					}
					catch (Exception e)
					{
						Debug.LogError("Error parsing score: " + e.Message);
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