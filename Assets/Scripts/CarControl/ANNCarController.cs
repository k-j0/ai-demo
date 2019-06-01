
//#define CHRONO //define this to output timing data for each frame to a csv file.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Haze;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if CHRONO
using System.Diagnostics;
#endif

public class ANNCarController : CarController{

	[SerializeField] NeuralNetwork neuralNetwork = null;

#if CHRONO
	Stopwatch stopwatch = new Stopwatch();
	List<float> times;
#endif

	// Start is called before the first frame update
	new void Start(){
		base.Start();

		if(neuralNetwork == null) {
			UnityEngine.Debug.LogError("Unassigned neural network in " + name + "!");
		}
    }

	private void OnEnable() {
#if CHRONO
		times = new List<float>();
#endif
	}

	void OnDisable() {
#if UNITY_EDITOR
		if(neuralNetwork != null)
			EditorUtility.SetDirty(neuralNetwork);
#endif
#if CHRONO
		//write the times to a csv file
		string contents = "ANN timings in milliseconds:\n";
		foreach(float t in times) {
			contents += t + ",";
		}
		string path = "TimingResults/ANNCarController-timings-" + System.DateTime.Now.ToString().Replace('\\', '-').Replace('/', '-').Replace('.', '-').Replace(':', '-').Replace(' ', '-') + ".csv";
		//create the directory if necessary
		System.IO.Directory.CreateDirectory("TimingResults");
		//write the file
		System.IO.File.WriteAllText(path, contents);
		UnityEngine.Debug.Log("Wrote timing data to " + path);
#endif
	}

    // Update is called once per frame
    new void FixedUpdate(){

		if(neuralNetwork == null) return;

		//gather data
		float front = FrontCaptorDistance;//0..1
		float emergency = EmergencyCaptorDistance;//0..1
		float frontLeft = FrontLeftCaptorDistance;//0..1
		float frontRight = FrontRightCaptorDistance;//0..1
		float left = LeftCaptorDistance;//0..1
		float right = RightCaptorDistance;//0..1
		Vector2 toGoal = ToGoal.normalized;//length=1
		Vector2 fwd = new Vector2(transform.forward.x, transform.forward.z);
		float towardsGoal = Mathf.Atan2(toGoal.y, toGoal.x) - Mathf.Atan2(fwd.y, fwd.x);
		towardsGoal = Mathf.Repeat(towardsGoal, Mathf.PI * 2);
		towardsGoal = towardsGoal > Mathf.PI ? -2 * Mathf.PI + towardsGoal : towardsGoal;

		//remap inputs to -1..1
		List<float> inputs = new List<float> {
			left-right,
			left*2-1,
			right*2-1,
			frontLeft-frontRight,
			frontLeft*2-1,
			frontRight*2-1,
			Utilities.Map(-Mathf.PI, Mathf.PI, -1, 1, towardsGoal),
			emergency*2-1,
			front*2-1
		};

#if CHRONO
		stopwatch.Start();

		List<float> outputs = new List<float>();
		for(int i = 0; i<100; ++i) {//run 100 iterations to get meaningful results
			outputs = neuralNetwork.RunForward(inputs);
		}

		stopwatch.Stop();
		times.Add((float)stopwatch.ElapsedMilliseconds * 0.01f);//take note of the time in milliseconds
		stopwatch.Reset();
#else
		//process the neural network
		List<float> outputs = neuralNetwork.RunForward(inputs);//grab outputs from neural network
#endif

		Steer = outputs[0]*2-1;//remap from 0..1 to -1..1
		Acceleration = outputs[1]*2-1;

		base.FixedUpdate();
    }
	
}
