
//#define CHRONO //define this to output timing data for each frame to a csv file.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
#if CHRONO
using System.Diagnostics;
#endif

public class FuzzyLogicCarController : CarController{

	[SerializeField] float weightAvgCenter = 1;
	[SerializeField] float weightTwrdsGoal = 1;

#if UNITY_EDITOR
	[Space]
	[Header("ANN training data")]
	[SerializeField] TrainingSet trainingSet;
	[SerializeField] bool recordTrainingPatterns = false;//whether to record training patterns at all
	[SerializeField] int trainingPatternEvery = 10;//every 10 frames, record a pattern

	[Space]
	[Header("Minimums and maximums of input data")]
	[SerializeField] float minFront = 1;
	[SerializeField] float maxFront = 0;
	[SerializeField] float minEmergency = 1;
	[SerializeField] float maxEmergency = 0;
	[SerializeField] float minFrontLeft = 1;
	[SerializeField] float maxFrontLeft = 0;
	[SerializeField] float minFrontRight = 1;
	[SerializeField] float maxFrontRight = 0;
	[SerializeField] float minLeft = 1;
	[SerializeField] float maxLeft = 0;
	[SerializeField] float minRight = 1;
	[SerializeField] float maxRight = 0;
	[SerializeField] float minTowardsGoal = Mathf.PI;
	[SerializeField] float maxTowardsGoal = -Mathf.PI;
#endif


#if CHRONO
	Stopwatch stopwatch = new Stopwatch();
	List<float> times;
#endif

	int frames = 0;

    // Start is called before the first frame update
    new void Start(){
		base.Start();
    }

	private void OnEnable() {
#if CHRONO
		times = new List<float>();
#endif
	}

	void OnDisable() {
#if UNITY_EDITOR
		if(trainingSet && recordTrainingPatterns) {
			EditorUtility.SetDirty(trainingSet);
			AssetDatabase.SaveAssets();
		}
#endif
#if CHRONO
		//write the times to a csv file
		string contents = "FL timings in milliseconds:\n";
		foreach(float t in times) {
			contents += t + ",";
		}
		string path = "TimingResults/FuzzyLogicCarController-timings-" + System.DateTime.Now.ToString().Replace('\\', '-').Replace('/', '-').Replace('.', '-').Replace(':', '-').Replace(' ', '-') + ".csv";
		//create the directory if necessary
		System.IO.Directory.CreateDirectory("TimingResults");
		//write the file
		System.IO.File.WriteAllText(path, contents);
		UnityEngine.Debug.Log("Wrote timing data to " + path);
#endif
	}

	static void FuzzyLogic(float front, float emergency, float frontLeft, float frontRight, float left, float right, float towardsGoal, float weightTwrdsGoal, float weightAvgCenter, out float Steer, out float Acceleration) {
		//center will be 0 when we're at the center, -1 when we're too far left and +1 when we're too far right
		float center = left - right;//  1-0 --> 1  //  0.5-0.5 --> 0  //  0-1 --> -1  //  0.1-0.9 --> -0.8
		float centerFront = frontLeft - frontRight; // same thing for the front captors
		float avgCenter = (center + centerFront) * 0.5f;//amount of steering coming from the Left/Right captors

		//apply a weighting between the two values to determine which way (and how much) to steer
		Steer = -(towardsGoal * weightTwrdsGoal + avgCenter * weightAvgCenter) / (weightAvgCenter + weightTwrdsGoal);

		//in an emergency, we need to steer even more:
		if(Steer != 0 && emergency < 1) {//note that the only time this isnt triggered is Steer == 0 (going straight anyways) or emergency == 1 (nothing in front of the car at all)
			Steer = Mathf.Lerp(Mathf.Sign(Steer) * Mathf.Sqrt(Mathf.Abs(Steer)), Steer, emergency);
		}

		//apply acceleration based on what's in front
		Acceleration = 2 * ((front * front * emergency * emergency) - 0.5f);//go forward if there's nothing in the way!

		//Maximum acceleration is only when the road is cleared up ahead totally (ie frontLeft == frontRight == 1)
		Acceleration *= frontLeft * frontRight;
	}

    // Update is called once per frame
    new void FixedUpdate(){
		++frames;

		//gather data - Fuzzification -
		float front = FrontCaptorDistance;//0..1
		float emergency = EmergencyCaptorDistance;//0..1
		float frontLeft = FrontLeftCaptorDistance;//0..1
		float frontRight = FrontRightCaptorDistance;//0..1
		float left = LeftCaptorDistance;//0..1
		float right = RightCaptorDistance;//0..1
		Vector2 toGoal = ToGoal.normalized;//length=1
		Vector2 fwd = new Vector2(transform.forward.x, transform.forward.z);
		//detect how much we should steer to reach our goal; in range -pi..pi
		float towardsGoal = Mathf.Atan2(toGoal.y, toGoal.x) - Mathf.Atan2(fwd.y, fwd.x);
		towardsGoal = Mathf.Repeat(towardsGoal, Mathf.PI * 2);
		towardsGoal = towardsGoal > Mathf.PI ? -2 * Mathf.PI + towardsGoal : towardsGoal;

#if UNITY_EDITOR //helpers to gather the maximum and minimum of each value along a track
		minFront = Mathf.Min(front, minFront);
		maxFront = Mathf.Max(front, maxFront);
		minEmergency = Mathf.Min(emergency, minEmergency);
		maxEmergency = Mathf.Max(emergency, maxEmergency);
		minFrontLeft = Mathf.Min(frontLeft, minFrontLeft);
		maxFrontLeft = Mathf.Max(frontLeft, maxFrontLeft);
		minFrontRight = Mathf.Min(frontRight, minFrontRight);
		maxFrontRight = Mathf.Max(frontRight, maxFrontRight);
		minLeft = Mathf.Min(left, minLeft);
		maxLeft = Mathf.Max(left, maxLeft);
		minRight = Mathf.Min(right, minRight);
		maxRight = Mathf.Max(right, maxRight);
		minTowardsGoal = Mathf.Min(towardsGoal, minTowardsGoal);
		maxTowardsGoal = Mathf.Max(towardsGoal, maxTowardsGoal);
#endif

		//apply fuzzy rules to determine steer and acceleration
		float steer = 0, acceleration = 0;

#if CHRONO
		stopwatch.Start();

		List<float> outputs = new List<float>();
		for(int i = 0; i < 1000; ++i) {//run 1000 iterations to get meaningful results
			FuzzyLogic(front, emergency, frontLeft, frontRight, left, right, towardsGoal, weightTwrdsGoal, weightAvgCenter, out steer, out acceleration);
		}

		stopwatch.Stop();
		times.Add((float)stopwatch.ElapsedMilliseconds * 0.001f);//take note of the time in milliseconds
		stopwatch.Reset();
#else

		FuzzyLogic(front, emergency, frontLeft, frontRight, left, right, towardsGoal, weightTwrdsGoal, weightAvgCenter, out steer, out acceleration);

#endif

		//Defuzzification happens essentially within the CarController script since Steer and Acceleration are still fuzzy
		Steer = steer; Acceleration = acceleration;
		
#if UNITY_EDITOR
		//Record patterns for ANN?
		if(recordTrainingPatterns && trainingSet) {
			if(frames % trainingPatternEvery == 0) {
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
				trainingSet.AddPattern(inputs, new List<float>{ Steer*0.5f+0.5f, Acceleration*0.5f+0.5f });
			}
		}
#endif
		
		base.FixedUpdate();
    }

#if UNITY_EDITOR
	/// <summary>
	/// Uses randomized inputs to generate a completely random pattern with outputs that still obey the fuzzy logic
	/// </summary>
	/// <returns>A randomized training pattern to feed ANN with</returns>
	public static TrainingPattern getRandomPattern(float weightAvgCenter, float weightTwrdsGoal, float minFront, float maxFront, float minEmergency, float maxEmergency, float minFrontLeft, float maxFrontLeft, float minFrontRight, float maxFrontRight, float minLeft, float maxLeft, float minRight, float maxRight, float minTowardsGoal, float maxTowardsGoal) {

		//generate randomized data
		float front = Random.Range(minFront, maxFront);//0..1
		float emergency = Random.Range(minEmergency, maxEmergency);//0..1
		float frontLeft = Random.Range(minFrontLeft, maxFrontLeft);//0..1
		float frontRight = Random.Range(minFrontRight, maxFrontRight);//0..1
		float left = Random.Range(minLeft, maxLeft);//0..1
		float right = Random.Range(minRight, maxRight);//0..1
		float towardsGoal = Random.Range(minTowardsGoal, maxTowardsGoal);


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
		float steer, acceleration;
		FuzzyLogic(front, emergency, frontLeft, frontRight, left, right, towardsGoal, weightTwrdsGoal, weightAvgCenter, out steer, out acceleration);
		List<float> outputs = new List<float> { steer*0.5f+0.5f, acceleration*0.5f+0.5f };

		return new TrainingPattern(inputs, outputs);
	}
#endif

}
