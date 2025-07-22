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
	public AudioClip chompSound;
	public AudioClip deathSound;
	private AudioSource audioSource;
	private static bool isMuted = false;
	private bool scoreSaved = false; // Prevent multiple PlayerPrefs saves

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

		clyde.GetComponent<GhostMove>().speed += Level * SpeedPerLevel;
		blinky.GetComponent<GhostMove>().speed += Level * SpeedPerLevel;
		pinky.GetComponent<GhostMove>().speed += Level * SpeedPerLevel;
		inky.GetComponent<GhostMove>().speed += Level * SpeedPerLevel;
		pacman.GetComponent<PlayerController>().speed += Level * SpeedPerLevel / 2;

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

		pacman.transform.position = new Vector3(15f, 11f, 0f);
		blinky.transform.position = new Vector3(15f, 20f, 0f);
		pinky.transform.position = new Vector3(14.5f, 17f, 0f);
		inky.transform.position = new Vector3(16.5f, 17f, 0f);
		clyde.transform.position = new Vector3(12.5f, 17f, 0f);

		pacman.GetComponent<PlayerController>().ResetDestination();
		blinky.GetComponent<GhostMove>().InitializeGhost();
		pinky.GetComponent<GhostMove>().InitializeGhost();
		inky.GetComponent<GhostMove>().InitializeGhost();
		clyde.GetComponent<GhostMove>().InitializeGhost();

		gameState = GameState.Init;
		gui.H_ShowReadyScreen();
	}

	public void ToggleScare()
	{
		if (!scared) ScareGhosts();
		else CalmGhosts();
	}

	public void ScareGhosts()
	{
		scared = true;
		blinky.GetComponent<GhostMove>().Frighten();
		pinky.GetComponent<GhostMove>().Frighten();
		inky.GetComponent<GhostMove>().Frighten();
		clyde.GetComponent<GhostMove>().Frighten();
		_timeToCalm = Time.time + scareLength;

		Debug.Log("Ghosts Scared");
	}

	public void CalmGhosts()
	{
		scared = false;
		blinky.GetComponent<GhostMove>().Calm();
		pinky.GetComponent<GhostMove>().Calm();
		inky.GetComponent<GhostMove>().Calm();
		clyde.GetComponent<GhostMove>().Calm();
		PlayerController.killstreak = 0;
	}

	void AssignGhosts()
	{
		clyde = GameObject.Find("clyde");
		pinky = GameObject.Find("pinky");
		inky = GameObject.Find("inky");
		blinky = GameObject.Find("blinky");
		pacman = GameObject.Find("pacman");

		if (clyde == null || pinky == null || inky == null || blinky == null) Debug.Log("One of ghosts are NULL");
		if (pacman == null) Debug.Log("Pacman is NULL");

		gui = GameObject.FindObjectOfType<GameGUINavigation>();
		if (gui == null) Debug.Log("GUI Handle Null!");
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
			gameState = GameState.Scores;
		}
	}

	public static void DestroySelf()
	{
		score = 0;
		Level = 0;
		lives = 3;
		Destroy(GameObject.Find("Game Manager"));
	}

	public void PlayChompSound()
	{
		if (chompSound != null && !isMuted)
		{
			Debug.Log("Playing chomp sound");
			audioSource.PlayOneShot(chompSound);
		}
		else if (isMuted)
		{
			Debug.Log("Chomp sound muted");
		}
		else
		{
			Debug.LogWarning("chompSound is not assigned!");
		}
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