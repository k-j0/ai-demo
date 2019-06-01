#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Haze;

[CreateAssetMenu(fileName = "Training Set", menuName = "Haze/AI/ANN/Training Set")]
public class TrainingSet : ScriptableObject{
	
	[SerializeField] int numberOfRandomPatterns = 1024;
	[SerializeField] float weightAvgCenter = 1;
	[SerializeField] float weightTwrdsGoal = 1;
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

	[Header("Internal data")] [Space(15)]
	[SerializeField] List<TrainingPattern> patterns;

	public List<TrainingPattern> Patterns { get { return patterns; } }

	public void AddPattern(List<float> inputs, List<float> outputs) {
		patterns.Add(new TrainingPattern(inputs, outputs));
	}

	public void Shuffle() {
		patterns.Shuffle();
		EditorUtility.SetDirty(this);
		AssetDatabase.SaveAssets();
	}

	public void RandomPatterns() {
		for(int i = 0; i<numberOfRandomPatterns; ++i) {
			patterns.Add(FuzzyLogicCarController.getRandomPattern(weightAvgCenter, weightTwrdsGoal, minFront, maxFront, minEmergency, maxEmergency, minFrontLeft, maxFrontLeft, minFrontRight, maxFrontRight, minLeft, maxLeft, minRight, maxRight, minTowardsGoal, maxTowardsGoal));
		}
		EditorUtility.SetDirty(this);
		AssetDatabase.SaveAssets();
	}

	public void Reset() {
		patterns = new List<TrainingPattern>();
		EditorUtility.SetDirty(this);
		AssetDatabase.SaveAssets();
	}

}

[Serializable]
public class TrainingPattern {

	[SerializeField] List<float> inputs;
	[SerializeField] List<float> outputs;

	public List<float> Inputs { get { return inputs; } }
	public List<float> Outputs { get { return outputs; } }

	public TrainingPattern(List<float> i, List<float> o) {
		inputs = i;
		outputs = o;
	}

}

[CustomEditor(typeof(TrainingSet))]
public class TrainingSetEditor : Editor {
	public override void OnInspectorGUI() {

		TrainingSet ts = target as TrainingSet;
		if(ts) {
			if(GUILayout.Button("Generate random patterns")) {
				ts.RandomPatterns();
			}
			if(GUILayout.Button("Shuffle patterns")) {
				ts.Shuffle();
			}
			if(GUILayout.Button("Reset")) {
				ts.Reset();
			}
			GUILayout.Label("Size of training set: " + ts.Patterns.Count);
		}

		GUILayout.Space(40);
		DrawDefaultInspector();
	}
}
#endif