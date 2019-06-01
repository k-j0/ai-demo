using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HumanCarController : CarController {

    // Start is called before the first frame update
    new void Start(){
		base.Start();
    }

    // Update is called once per frame
    new void FixedUpdate(){

		Steer = Input.GetAxis("Horizontal");
		Acceleration = 2*(Input.GetAxis("Vertical")-0.5f);
		
		base.FixedUpdate();
	}
}
