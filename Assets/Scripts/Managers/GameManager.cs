using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
	public static int Level = 0;
	public static int lives = 3;
	public static int score;

	public enum GameState { Init, Game, Dead, Scores }
	public static GameState gameState;

	private GameObject pacman;
	private GameObject blinky;
	private GameObject pinky;
	private GameObject inky;
	private GameObject clyde;
	private GameGUINavigation gui;

	public static bool scared;
	public float scareLength;
	private float _timeToCalm;
	public float SpeedPerLevel;

	public AudioClip beginningSound;
	public AudioClip chompSound; // This variable can remain, but it's no longer used
	public AudioClip deathSound;
	private AudioSource audioSource;
	private static bool isMuted = false;
	private bool scoreSaved = false;

	private static GameManager _instance;

	public static GameManager instance
	{
		get
		{
			if (_instance == null)
			{
				_instance = GameObject.FindObjectOfType<GameManager>();
				DontDestroyOnLoad(_instance.gameObject);
			}
			return _instance;
		}
	}

	void Awake()
	{
		if (_instance == null)
		{
			_instance = this;
			DontDestroyOnLoad(this);
		}
		else
		{
			if (this != _instance)
				Destroy(this.gameObject);
		}

		AssignGhosts();
		audioSource = gameObject.AddComponent<AudioSource>();
		audioSource.playOnAwake = false;
	}

	void Start()
	{
		gameState = GameState.Init;
		scoreSaved = false;
	}

	void OnLevelWasLoaded()
	{
		if (Level == 0) lives = 3;

		Debug.Log("Level " + Level + " Loaded!");
		AssignGhosts();
		ResetVariables();

		if (clyde != null) clyde.GetComponent<GhostMove>().speed += Level * SpeedPerLevel;
		if (blinky != null) blinky.GetComponent<GhostMove>().speed += Level * SpeedPerLevel;
		if (pinky != null) pinky.GetComponent<GhostMove>().speed += Level * SpeedPerLevel;
		if (inky != null) inky.GetComponent<GhostMove>().speed += Level * SpeedPerLevel;
		if (pacman != null) pacman.GetComponent<PlayerController>().speed += Level * SpeedPerLevel / 2;

		if (SceneManager.GetActiveScene().buildIndex == 3 && beginningSound != null && !isMuted)
		{
			audioSource.PlayOneShot(beginningSound);
		}
	}

	private void ResetVariables()
	{
		_timeToCalm = 0.0f;
		scared = false;
		PlayerController.killstreak = 0;
		scoreSaved = false;
	}

	void Update()
	{
		if (scared && _timeToCalm <= Time.time)
			CalmGhosts();

		if (Input.GetKeyDown(KeyCode.M))
		{
			ToggleMute();
		}
	}

	public void ResetScene()
	{
		CalmGhosts();

		if (pacman != null) pacman.transform.position = new Vector3(15f, 11f, 0f);
		if (blinky != null) blinky.transform.position = new Vector3(15f, 20f, 0f);
		if (pinky != null) pinky.transform.position = new Vector3(14.5f, 17f, 0f);
		if (inky != null) inky.transform.position = new Vector3(16.5f, 17f, 0f);
		if (clyde != null) clyde.transform.position = new Vector3(12.5f, 17f, 0f);

		if (pacman != null) pacman.GetComponent<PlayerController>().ResetDestination();
		if (blinky != null) blinky.GetComponent<GhostMove>().InitializeGhost();
		if (pinky != null) pinky.GetComponent<GhostMove>().InitializeGhost();
		if (inky != null) inky.GetComponent<GhostMove>().InitializeGhost();
		if (clyde != null) clyde.GetComponent<GhostMove>().InitializeGhost();

		gameState = GameState.Init;
		if (gui != null) gui.H_ShowReadyScreen();
		else Debug.LogWarning("GUI is null, cannot show ready screen");
	}

	public void ToggleScare()
	{
		if (!scared) ScareGhosts();
		else CalmGhosts();
	}

	public void ScareGhosts()
	{
		scared = true;
		if (blinky != null) blinky.GetComponent<GhostMove>().Frighten();
		if (pinky != null) pinky.GetComponent<GhostMove>().Frighten();
		if (inky != null) inky.GetComponent<GhostMove>().Frighten();
		if (clyde != null) clyde.GetComponent<GhostMove>().Frighten();
		_timeToCalm = Time.time + scareLength;

		Debug.Log("Ghosts Scared");
	}

	public void CalmGhosts()
	{
		scared = false;
		if (blinky != null) blinky.GetComponent<GhostMove>().Calm();
		if (pinky != null) pinky.GetComponent<GhostMove>().Calm();
		if (inky != null) inky.GetComponent<GhostMove>().Calm();
		if (clyde != null) clyde.GetComponent<GhostMove>().Calm();
		PlayerController.killstreak = 0;
	}

	void AssignGhosts()
	{
		clyde = GameObject.Find("clyde");
		pinky = GameObject.Find("pinky");
		inky = GameObject.Find("inky");
		blinky = GameObject.Find("blinky");
		pacman = GameObject.Find("pacman");

		if (clyde == null || pinky == null || inky == null || blinky == null)
			Debug.LogWarning("One or more ghosts are NULL");
		if (pacman == null)
			Debug.LogWarning("Pacman is NULL");

		gui = GameObject.FindObjectOfType<GameGUINavigation>();
		if (gui == null)
			Debug.LogWarning("GUI Handle Null!");
	}

	public void LoseLife()
	{
		lives--;
		Debug.Log("LoseLife called, Lives: " + lives);
		gameState = GameState.Dead;

		UIScript ui = GameObject.FindObjectOfType<UIScript>();
		if (ui != null && ui.lives != null && ui.lives.Count > 0)
		{
			Destroy(ui.lives[ui.lives.Count - 1]);
			ui.lives.RemoveAt(ui.lives.Count - 1);
		}
		else
		{
			Debug.LogWarning("UIScript or ui.lives is null or empty, cannot remove life UI");
		}

		if (lives <= 0 && !scoreSaved)
		{
			Debug.Log("Game Over! Saving score to PlayerPrefs: " + score);
			PlayerPrefs.SetInt("PlayerScore", score);
			PlayerPrefs.Save();
			scoreSaved = true;
			score = 0;
		}
	}

	public void EndGame()
	{
		gameState = GameState.Scores;
		Debug.Log("EndGame called, transitioning to Scores state");
		SceneManager.LoadScene("scores");
	}

	public static void DestroySelf()
	{
		score = 0;
		Level = 0;
		lives = 3;
		GameObject gm = GameObject.Find("Game Manager");
		if (gm != null) Destroy(gm);
	}

	public void PlayDeathSound()
	{
		if (deathSound != null && !isMuted)
		{
			Debug.Log("Playing death sound");
			audioSource.PlayOneShot(deathSound);
		}
		else if (isMuted)
		{
			Debug.Log("Death sound muted");
		}
		else
		{
			Debug.LogWarning("deathSound is not assigned!");
		}
	}

	public void ToggleMute()
	{
		isMuted = !isMuted;
		Debug.Log("Sound muted: " + isMuted);
	}

	public bool IsMuted()
	{
		return isMuted;
	}
}