using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarController : MonoBehaviour{

	[SerializeField] float maxSteerSpeed = 30;
	[SerializeField] float maxForwardSpeed = 50;
	[SerializeField] float maxAcceleration = 0.4f;
	[SerializeField] Transform goals;
	[SerializeField] DistanceCaptor frontCaptor;
	[SerializeField] DistanceCaptor emergencyCaptor;
	[SerializeField] DistanceCaptor frontLeftCaptor;
	[SerializeField] DistanceCaptor frontRightCaptor;
	[SerializeField] DistanceCaptor leftCaptor;
	[SerializeField] DistanceCaptor rightCaptor;
	[SerializeField] string wheelsPath = "FocE08/Tires/Tire_";//Appended: LF (Left Front) LR (Left Rear) RF (Right Front) RR (Right Rear)
	[SerializeField] float wheelRotationSpeed = 360 * 3;
	[SerializeField] string cameraPath = "Main Camera";

	float steer = 0;//left-right component (-1..1)
	float velocity = 0;//forward component (0..1)
	float acceleration = 0;//acceleration value (-1..1)

	Transform currentGoal;//the current transform we're going to on the track.

	Rigidbody body;

	//Car wheels animation
	Transform[] wheels;
	const int LF = 0, LR = 1, RF = 2, RR = 3;

	//FOV animation
	Camera camera;

	public float Steer {
		protected set {
			steer = Mathf.Clamp(value, -1, 1);
		}
		get {
			return steer;
		}
	}

	public float Acceleration {
		protected set {
			acceleration = Mathf.Clamp(value, -1, 1);
		}
		get {
			return acceleration;
		}
	}

	protected float FrontCaptorDistance {
		get {
			if(frontCaptor) return frontCaptor.CurrentDistance; else return -1;
		}
	}

	protected float EmergencyCaptorDistance {
		get {
			if(emergencyCaptor) return emergencyCaptor.CurrentDistance; else return -1;
		}
	}

	protected float FrontLeftCaptorDistance {
		get {
			if(frontLeftCaptor) return frontLeftCaptor.CurrentDistance; else return -1;
		}
	}

	protected float FrontRightCaptorDistance {
		get {
			if(frontRightCaptor) return frontRightCaptor.CurrentDistance; else return -1;
		}
	}

	protected float LeftCaptorDistance {
		get {
			if(leftCaptor) return leftCaptor.CurrentDistance; else return -1;
		}
	}

	protected float RightCaptorDistance {
		get {
			if(rightCaptor) return rightCaptor.CurrentDistance; else return -1;
		}
	}

	protected Vector2 ToGoal {
		get {
			if(currentGoal) return new Vector2(currentGoal.position.x - transform.position.x, currentGoal.position.z - transform.position.z); else return Vector2.zero;
		}
	}

	float FOV {
		set {//slowly move fov towards target
			if(camera) {
				if(camera.fieldOfView < value) {
					camera.fieldOfView += 10 * Time.deltaTime;
					if(camera.fieldOfView > value) camera.fieldOfView = value;
				}else if(camera.fieldOfView > value) {
					camera.fieldOfView -= 10 * Time.deltaTime;
					if(camera.fieldOfView < value) camera.fieldOfView = value;
				}
			}
		}
	}

	// Start is called before the first frame update
	protected void Start(){
		body = GetComponent<Rigidbody>();

		if(goals.childCount <= 0) {
			print("Error: goals must have at least one child transform...");
		} else {
			currentGoal = goals.GetChild(0);
		}

		//grab the wheels:
		wheels = new Transform[4];
		wheels[LF] = transform.Find(wheelsPath + "LF");
		wheels[LR] = transform.Find(wheelsPath + "LR");
		wheels[RF] = transform.Find(wheelsPath + "RF");
		wheels[RR] = transform.Find(wheelsPath + "RR");

		//grab the camera
		camera = transform.Find(cameraPath).GetComponent<Camera>();
	}

	private void Update() {
		foreach(Transform wheel in wheels) {
			//keep rotating
			wheel.Rotate(new Vector3(wheelRotationSpeed * velocity * Time.deltaTime, 0, 0));
		}
		//two front wheels match the current steering applied to seem like they orient the car
		Vector3 anglesFront = wheels[LF].localEulerAngles;
		anglesFront.y = Steer * maxSteerSpeed * (velocity+0.25f);
		wheels[LF].localEulerAngles = anglesFront;
		wheels[RF].localEulerAngles = anglesFront;

		//animate FOV to enhance speed perception
		FOV = 60 + 15 * velocity;//60..75
	}

	// Update is called once per frame
	protected void FixedUpdate(){

		//apply steering
		transform.Rotate(Vector3.up, steer * Time.fixedDeltaTime * maxSteerSpeed * (velocity+0.25f), Space.Self);

		//apply forward velocity / acceleration
		velocity += acceleration * Time.fixedDeltaTime * maxAcceleration;
		velocity = Mathf.Clamp(velocity, 0, 1);
		body.AddForce(transform.forward * velocity * maxForwardSpeed);

    }

	void OnTriggerEnter(Collider other) {
		if(other.isTrigger) {
			if(other.transform == currentGoal) {
				//Reached the current goal! - onto the next now
				int next = currentGoal.GetSiblingIndex() + 1;
				next %= goals.childCount;//might be the first time i use a %= operator haha
				currentGoal = goals.GetChild(next);
			}
		}
	}

#if UNITY_EDITOR
	void OnDrawGizmos() {
		if(currentGoal) {
			Gizmos.color = Color.yellow;
			Gizmos.DrawLine(transform.position, transform.position + new Vector3(ToGoal.x, 0, ToGoal.y));
		}
		Gizmos.color = Color.green;
		Gizmos.DrawWireSphere(transform.position + transform.up * 2, 0.3f);
		Gizmos.DrawLine(transform.position + transform.up*2, transform.position + transform.up*2 + transform.right * 10 * Steer);
	}
#endif
}
