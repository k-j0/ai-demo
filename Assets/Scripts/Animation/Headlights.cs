using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Headlights : MonoBehaviour
{
	[SerializeField] List<GameObject> lights;
	[SerializeField] bool turnOnOnStart = true;

	void Start() {
		if(turnOnOnStart) TurnOn();
		else TurnOff();
	}

	public void TurnOn() {
		foreach(GameObject go in lights) go.SetActive(true);
	}

	public void TurnOff() {
		foreach(GameObject go in lights) go.SetActive(false);
	}
}
