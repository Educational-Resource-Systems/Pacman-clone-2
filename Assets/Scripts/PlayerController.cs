using System;
using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
	public float speed = 0.4f;
	Vector2 _dest = Vector2.zero;
	Vector2 _dir = Vector2.zero;
	Vector2 _nextDir = Vector2.zero;

	[Serializable]
	public class PointSprites
	{
		public GameObject[] pointSprites;
	}

	public PointSprites points;
	public static int killstreak = 0;

	private GameGUINavigation GUINav;
	private GameManager GM;
	private ScoreManager SM;

	private bool _deadPlaying = false;

	void Start()
	{
		GM = GameObject.Find("Game Manager") ? GameObject.Find("Game Manager").GetComponent<GameManager>() : null;
		SM = GameObject.Find("Game Manager") ? GameObject.Find("Game Manager").GetComponent<ScoreManager>() : null;
		GameObject uiManager = GameObject.Find("UI Manager");
		GUINav = uiManager ? uiManager.GetComponent<GameGUINavigation>() : null;
		if (GUINav == null)
		{
			Debug.LogWarning("UI Manager or GameGUINavigation not found!");
		}
		if (GM == null)
		{
			Debug.LogWarning("Game Manager or GameManager component not found!");
		}
		if (SM == null)
		{
			Debug.LogWarning("Game Manager or ScoreManager component not found!");
		}
		_dest = transform.position;
	}

	void FixedUpdate()
	{
		if (GM == null)
		{
			Debug.LogWarning("GameManager is null in FixedUpdate!");
			return;
		}
		switch (GameManager.gameState)
		{
		case GameManager.GameState.Game:
			ReadInputAndMove();
			Animate();
			break;

		case GameManager.GameState.Dead:
			if (!_deadPlaying)
			{
				Debug.Log("Starting PlayDeadAnimation, Lives: " + GameManager.lives);
				StartCoroutine(PlayDeadAnimation());
			}
			break;
		}
	}

	IEnumerator PlayDeadAnimation()
	{
		_deadPlaying = true;
		Debug.Log("Starting death animation");
		if (GM != null)
		{
			GM.PlayDeathSound();
		}
		else
		{
			Debug.LogWarning("GameManager is null, cannot play death sound");
		}
		Animator animator = GetComponent<Animator>();
		if (animator != null)
		{
			animator.SetBool("Die", true);
			yield return new WaitForSeconds(1); // Death animation duration
			animator.SetBool("Die", false);
			Debug.Log("Death animation complete");
		}
		else
		{
			Debug.LogWarning("Animator component not found, skipping animation");
			yield return new WaitForSeconds(1); // Fallback delay
		}
		_deadPlaying = false;

		if (GameManager.lives <= 0)
		{
			Debug.Log("Game Over! Checking high score: " + (SM != null ? SM.LowestHigh().ToString() : "ScoreManager null"));
			if (GUINav != null)
			{
				if (SM != null && GameManager.score >= SM.LowestHigh())
				{
					Debug.Log("Showing high score menu and submitting score");
					GUINav.getScoresMenu();
					// Assume getScoresMenu submits score
				}
				else
				{
					Debug.Log("Showing Game Over screen");
					GUINav.H_ShowGameOverScreen();
					// Submit score for non-high scores
					string playerName = PlayerPrefs.GetString("PlayerName", "");
					string playerEmail = PlayerPrefs.GetString("PlayerEmail", "");
					int playerScore = PlayerPrefs.GetInt("PlayerScore", 0);
					if (!string.IsNullOrEmpty(playerName) && !string.IsNullOrEmpty(playerEmail) && SM != null)
					{
						Debug.Log("Submitting non-high score");
						StartCoroutine(SM.SubmitScore(playerName, playerEmail, playerScore));
					}
				}
				yield return new WaitForSeconds(2); // Wait for UI display
			}
			else
			{
				Debug.LogWarning("GUINav is null, skipping UI display");
				yield return new WaitForSeconds(1); // Fallback delay
			}
			Debug.Log("Loading Scores scene");
			SceneManager.LoadScene("Scores");
		}
		else
		{
			Debug.Log("Resetting scene");
			if (GM != null)
			{
				GM.ResetScene();
			}
			else
			{
				Debug.LogWarning("GameManager is null, cannot reset scene");
			}
		}
	}

	void Animate()
	{
		Vector2 dir = _dest - (Vector2)transform.position;
		Animator animator = GetComponent<Animator>();
		if (animator != null)
		{
			animator.SetFloat("DirX", dir.x);
			animator.SetFloat("DirY", dir.y);
		}
	}

	bool Valid(Vector2 direction)
	{
		Vector2 pos = transform.position;
		direction += new Vector2(direction.x * 0.45f, direction.y * 0.45f);
		RaycastHit2D hit = Physics2D.Linecast(pos + direction, pos);
		return hit.collider != null && (hit.collider.name == "pacdot" || hit.collider == GetComponent<Collider2D>());
	}

	public void ResetDestination()
	{
		_dest = new Vector2(15f, 11f);
		Animator animator = GetComponent<Animator>();
		if (animator != null)
		{
			animator.SetFloat("DirX", 1);
			animator.SetFloat("DirY", 0);
		}
	}

	void ReadInputAndMove()
	{
		Vector2 p = Vector2.MoveTowards(transform.position, _dest, speed);
		Rigidbody2D rb = GetComponent<Rigidbody2D>();
		if (rb != null)
		{
			rb.MovePosition(p);
		}

		if (Input.GetAxis("Horizontal") > 0) _nextDir = Vector2.right;
		if (Input.GetAxis("Horizontal") < 0) _nextDir = -Vector2.right;
		if (Input.GetAxis("Vertical") > 0) _nextDir = Vector2.up;
		if (Input.GetAxis("Vertical") < 0) _nextDir = -Vector2.up;

		if (Vector2.Distance(_dest, transform.position) < 0.00001f)
		{
			if (Valid(_nextDir))
			{
				_dest = (Vector2)transform.position + _nextDir;
				_dir = _nextDir;
			}
			else
			{
				if (Valid(_dir))
					_dest = (Vector2)transform.position + _dir;
			}
		}
	}

	public Vector2 getDir()
	{
		return _dir;
	}

	public void UpdateScore()
	{
		killstreak++;
		if (killstreak > 4) killstreak = 4;
		Instantiate(points.pointSprites[killstreak - 1], transform.position, Quaternion.identity);
		GameManager.score += (int)Mathf.Pow(2, killstreak) * 100;
	}
}