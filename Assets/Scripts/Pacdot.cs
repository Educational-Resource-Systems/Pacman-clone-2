using UnityEngine;

public class Pacdot : MonoBehaviour
{
	private GameManager GM; // Reference to GameManager

	void Start()
	{
		GM = GameObject.Find("Game Manager").GetComponent<GameManager>(); // Get GameManager
	}

	void OnTriggerEnter2D(Collider2D other)
	{
		if (other.name == "pacman")
		{
			GameManager.score += 10;
			GameObject[] pacdots = GameObject.FindGameObjectsWithTag("pacdot");
			Destroy(gameObject);



			if (pacdots.Length == 1)
			{
				GameObject.FindObjectOfType<GameGUINavigation>().LoadLevel();
			}
		}
	}
}