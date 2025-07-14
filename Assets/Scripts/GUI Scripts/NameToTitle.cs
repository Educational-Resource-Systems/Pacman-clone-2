using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class NameToTitle : MonoBehaviour {

	public Text title;


	void OnMouseEnter()
	{
		switch(name)
		{
		case "Runner":
			title.color = new Color(48f/255f, 185f/255f, 236f/255f);
			break;

		case "Blanko":
			// red YELLOW
			title.color = Color.yellow;
			break;

		case "Fizzle":
			// pink POWDER
			title.color = new Color(200f/255f, 214f/255f, 235f/255f);
			break;

		case "Misalingo":
			//blue TEAL
			title.color = new Color(153f/255f, 186f/255f, 169f/255f);
			break;

		case "Clutterbug":
			//yellow CORAL
			title.color = new Color(221f/255f, 182f/255f, 139f/255f);
			break;
		}
		
		title.text = name;
	}

	void OnMouseExit()
	{
		title.text = "Dose Runner";
		title.color = Color.white;
	}
}
