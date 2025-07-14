using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    private string TopScoresURL = "https://ers-dev.com/ERS/_pacman/topscores.php";
    private string username;
    private int _highscore = 99999;
    private int _lowestHigh = 99999;
    private bool _scoresRead;
    private bool _isTableFound;

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

    List<Score> scoreList = new List<Score>(10);

    void OnLevelWasLoaded(int level)
    {
        if (level == 2) // Scores scene
        {
            // Automatically submit score
            string playerName = PlayerPrefs.GetString("PlayerName", "");
            string playerEmail = PlayerPrefs.GetString("PlayerEmail", "");
            int playerScore = PlayerPrefs.GetInt("PlayerScore", 0);

            if (!string.IsNullOrEmpty(playerName) && !string.IsNullOrEmpty(playerEmail))
            {
                StartCoroutine(SubmitScore(playerName, playerEmail, playerScore));
            }
            else
            {
                Debug.LogError("Name or email not found in PlayerPrefs!");
                StartCoroutine(UpdateGUIText()); // Show scores anyway
            }
        }
        if (level == 1) // Game scene
        {
            _lowestHigh = _highscore = 99999;
            StartCoroutine(GetHighestScore());
        }
    }

    public IEnumerator SubmitScore(string playerName, string email, int score)
    {
        // Create form for POST request
        WWWForm form = new WWWForm();
        form.AddField("player_name", playerName);
        form.AddField("email", email);
        form.AddField("score", score);

        WWW submitRequest = new WWW(TopScoresURL, form);
        yield return submitRequest;

        if (submitRequest.error != null)
        {
            Debug.LogError(string.Format("ERROR SUBMITTING SCORE: {0}", submitRequest.error));
        }
        else
        {
            string responseText = submitRequest.text;
            Debug.Log(string.Format("Server response: {0}", responseText));

            if (responseText == "Score saved successfully")
            {
                Debug.Log("Score submitted successfully!");
                // Clear PlayerPrefs to prevent resubmission
                PlayerPrefs.DeleteKey("PlayerName");
                PlayerPrefs.DeleteKey("PlayerEmail");
                PlayerPrefs.DeleteKey("PlayerScore");
                PlayerPrefs.Save();
            }
            else
            {
                Debug.LogError(string.Format("Server error: {0}", responseText));
            }
        }

        // Always refresh high scores
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
        yield return getScoresAttempt;

        if (getScoresAttempt.error != null)
        {
            Debug.LogError(string.Format("ERROR GETTING SCORES: {0}", getScoresAttempt.error));
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
                        Debug.LogError(string.Format("Error parsing score: {0}", e.Message));
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