﻿using System;
using UnityEngine;
using System.Collections; // Added for IEnumerator

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
		GM = GameObject.Find("Game Manager").GetComponent<GameManager>();
		SM = GameObject.Find("Game Manager").GetComponent<ScoreManager>();
		GUINav = GameObject.Find("UI Manager").GetComponent<GameGUINavigation>();
		_dest = transform.position;
	}

	void FixedUpdate()
	{
		switch (GameManager.gameState)
		{
		case GameManager.GameState.Game:
			ReadInputAndMove();
			Animate();
			break;

		case GameManager.GameState.Dead:
			if (!_deadPlaying)
				StartCoroutine("PlayDeadAnimation");
			break;
		}
	}

	IEnumerator PlayDeadAnimation()
	{
		_deadPlaying = true;
		GM.PlayDeathSound(); // Play death sound
		GetComponent<Animator>().SetBool("Die", true);
		yield return new WaitForSeconds(1);
		GetComponent<Animator>().SetBool("Die", false);
		_deadPlaying = false;

		if (GameManager.lives <= 0)
		{
			Debug.Log("Treshold for High Score: " + SM.LowestHigh());
			if (GameManager.score >= SM.LowestHigh())
				GUINav.getScoresMenu();
			else
				GUINav.H_ShowGameOverScreen();
		}
		else
			GM.ResetScene();
	}

	void Animate()
	{
		Vector2 dir = _dest - (Vector2)transform.position;
		GetComponent<Animator>().SetFloat("DirX", dir.x);
		GetComponent<Animator>().SetFloat("DirY", dir.y);
	}

	bool Valid(Vector2 direction)
	{
		Vector2 pos = transform.position;
		direction += new Vector2(direction.x * 0.45f, direction.y * 0.45f);
		RaycastHit2D hit = Physics2D.Linecast(pos + direction, pos);
		return hit.collider.name == "pacdot" || (hit.collider == GetComponent<Collider2D>());
	}

	public void ResetDestination()
	{
		_dest = new Vector2(15f, 11f);
		GetComponent<Animator>().SetFloat("DirX", 1);
		GetComponent<Animator>().SetFloat("DirY", 0);
	}

	void ReadInputAndMove()
	{
		Vector2 p = Vector2.MoveTowards(transform.position, _dest, speed);
		GetComponent<Rigidbody2D>().MovePosition(p);

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