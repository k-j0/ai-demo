using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SelectCarAndAI : MonoBehaviour
{

	[SerializeField] Text currentCar;
	[SerializeField] Text currentAI;
	[SerializeField] Transform greyCar;
	[SerializeField] Transform redCar;
	Transform carInUse;

	string Car {
		set {
			if(currentCar) {
				currentCar.text = "Current car: " + value + " (R to change)";
			}
		}
	}

	string System {
		set {
			if(currentAI) {
				currentAI.text = "Current system: " + value + " (F to change)";
			}
		}
	}

    // Start is called before the first frame update
    void Start()
    {
		Car = "Grey";
		carInUse = greyCar;
    }

    // Update is called once per frame
    void Update()
    {
		//Switch car
		if(Input.GetKeyUp(KeyCode.R)) {
			//change cars
			carInUse.GetComponentInChildren<Camera>().enabled = false;
			if(carInUse.GetComponent<HumanCarController>().enabled) {//switch back to FuzzyLogic if the current controller was human
				carInUse.GetComponent<HumanCarController>().enabled = false;
				carInUse.GetComponent<FuzzyLogicCarController>().enabled = true;
			}
			if(carInUse == greyCar) {
				carInUse = redCar;
				Car = "Red";
			} else {
				carInUse = greyCar;
				Car = "Grey";
			}
			carInUse.GetComponentInChildren<Camera>().enabled = true;
		}

		//display ai system
		System = carInUse.GetComponent<ANNCarController>().enabled ? "ANN" : carInUse.GetComponent<FuzzyLogicCarController>().enabled ? "Fuzzy Logic" : "[WASD]";
		if(Input.GetKeyUp(KeyCode.F)) {
			if(carInUse.GetComponent<ANNCarController>().enabled) {
				carInUse.GetComponent<ANNCarController>().enabled = false;
				carInUse.GetComponent<FuzzyLogicCarController>().enabled = true;
			}else if(carInUse.GetComponent<FuzzyLogicCarController>().enabled) {
				carInUse.GetComponent<FuzzyLogicCarController>().enabled = false;
				carInUse.GetComponent<HumanCarController>().enabled = true;
			} else {
				carInUse.GetComponent<HumanCarController>().enabled = false;
				carInUse.GetComponent<ANNCarController>().enabled = true;
			}
		}

    }
}
