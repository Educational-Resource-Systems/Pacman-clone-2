using UnityEngine;
public class TestScoresText : MonoBehaviour
{
	void Start()
	{
		GameObject scoresText = GameObject.FindGameObjectWithTag("ScoresText");
		if (scoresText != null)
			Debug.Log("Found ScoresText with tag!");
		else
			Debug.LogError("ScoresText not found!");
	}
}