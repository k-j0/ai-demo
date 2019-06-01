using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using Unity.EditorCoroutines.Editor;
#endif

[CreateAssetMenu(fileName = "Neural Network", menuName = "Haze/AI/ANN/Neural Network")]
public class NeuralNetwork : ScriptableObject{

	[SerializeField] int inputLayerSize = 1;
	[SerializeField] int hiddenLayerSize = 1024;
	[SerializeField] int outputLayerSize = 1;
	[SerializeField] float β = 5;//makes the sigmoid function map from approx. -1..1 to ~ 0..1
	[SerializeField] bool shouldCapSigmoidInput = false;//if this is true, will take into account the capSigmoidInput value so as to prevent sigmoid function from taking too large or too small an input
	[SerializeField] float capSigmoidInput = 1;//treat any input to the sigmoid function larger CAP_SIGMOID_INPUT as CAP_SIGMOID_INPUT and smaller than -CAP_SIGMOID_INPUT as -CAP_SIGMOID_INPUT, to prevent having derivatives way too smalls
#if UNITY_EDITOR
	[SerializeField] float η = 0.1f;//initial learning rate
	[SerializeField] float δη = 0.95f;//learning rate acceleration
	[SerializeField] bool adaptiveLearningRate = true;//whether to alter the learning rate based on the error factors
	[SerializeField] float subset = 0.8f;//how much of the full set to show to the network when training
	[SerializeField] int iterations = 100;//how many iterations to use in training
	[SerializeField] public TrainingSet trainingSet = null;
	[SerializeField] bool shuffleSetAfterEachEpoch = true;
#endif

	[Header("Internal data")] [Space(15)]
	[SerializeField] float[] inputLayer = null;
	[SerializeField] Neurone[] hiddenLayer = null;
	[SerializeField] Neurone[] outputLayer = null;

	[Serializable]
	class Neurone {
		public float[] weights;// one more weight than there are inputs (last one is the bias)
		public float value;// the weighted sum of the neurone's inputs
		public float sigmoidValue;// sigmoid(value)
		public float δ;// value used for delta rule and backpropagation during training
	}

#if UNITY_EDITOR
	[NonSerialized] public EditorCoroutine currentCoroutine = null;
	string __msg = "";//message to display in the inspector
	public string msg {
		get { return __msg; }
		set { __msg = value; EditorUtility.SetDirty(this); }
	}
#endif



	float sigmoid(float input) {
		if(shouldCapSigmoidInput)
			input = Mathf.Clamp(input, -capSigmoidInput, capSigmoidInput);//prevent the input from being any larger than CAP_SIGMOID_INPUT and any smaller than -CAP_SIGMOID_INPUT
		float output = 1.0f / (1.0f + Mathf.Exp(-β * input));
		return output;
	}

	float sigmoidPrime(float input) {
		float output = β * sigmoid(input) * (1.0f - sigmoid(input));
		return output;
	}


	/// <summary>
	/// Runs through the neural network once and fills in the output data.
	/// </summary>
	/// <param name="inputs">Floats representing the inputs to the network.</param>
	/// <returns></returns>
	public List<float> RunForward(List<float> inputs) {

		CheckNetwork();
#if UNITY_EDITOR
		if(inputs.Count != inputLayerSize) {
			Debug.LogError("Error: inputs to neural network are of size " + inputs.Count + " instead of the expected " + inputLayerSize + ". This will result in undefined behaviour in a production build.");
			return null;
		}
#endif

		//copy inputs over
		for(int i = 0; i < inputLayerSize; ++i) {
			inputLayer[i] = inputs[i];
		}
		List<float> outputs = new List<float>();

		//process from input layer to hidden layer
		for(int j = 0; j<hiddenLayerSize; ++j) {
			hiddenLayer[j].value = 0;
			//accumulate inputs
			for(int i = 0; i<inputLayerSize; ++i) {
				hiddenLayer[j].value += hiddenLayer[j].weights[i] * sigmoid(inputs[i]);
			}
			//add bias
			hiddenLayer[j].value += hiddenLayer[j].weights[inputLayerSize] * 1;
			hiddenLayer[j].sigmoidValue = sigmoid(hiddenLayer[j].value);
		}

		//process from hidden layer to output layer
		for(int k = 0; k<outputLayerSize; ++k) {
			outputLayer[k].value = 0;
			//accumulate inputs
			for(int j = 0; j<hiddenLayerSize; ++j) {
				outputLayer[k].value += outputLayer[k].weights[j] * hiddenLayer[j].sigmoidValue;
			}
			//add biad
			outputLayer[k].value += outputLayer[k].weights[hiddenLayerSize] * 1;
			outputLayer[k].sigmoidValue = sigmoid(outputLayer[k].value);
			outputs.Add(outputLayer[k].sigmoidValue);//returns the value between 0..1
		}

		return outputs;
	}


#if UNITY_EDITOR

	void save() {
		EditorUtility.SetDirty(this);
		AssetDatabase.SaveAssets();
	}

	/// <summary>
	/// Computes the individual error for each of the perceptrons in the output layer (computed value goes into delta) and returns the total error.
	/// </summary>
	/// <param name="targetOutputs">The outputs we're expecting, to compare against the actual outputs we got</param>
	/// <returns></returns>
	float computeError(List<float> targetOutputs) {
		CheckNetwork();

		if(targetOutputs.Count != outputLayerSize) {
			Debug.LogError("Error: outputs to neural network are of size " + targetOutputs.Count + " instead of the expected " + outputLayerSize + ".");
			return 0;
		}


		float error = 0;
		for(int k = 0; k<outputLayerSize; ++k) {
			float val = targetOutputs[k] - outputLayer[k].sigmoidValue;
			outputLayer[k].δ = val;//keep this value around for the delta rule to operate
			error += val * val;
		}
		error *= 0.5f;

		if(error > outputLayerSize*100 || float.IsNaN(error) || float.IsPositiveInfinity(error) || float.IsNegativeInfinity(error)) {
			error = outputLayerSize*100;
		}

		return error;
	}



	/// <summary>
	/// To be called once runForward has been called, to determine the error and adjust the weights (editor-only).
	/// </summary>
	/// <param name="targetOutputs">The pattern we're expecting as an output</param>
	/// <param name="η">The current learning rate.</param>
	/// <returns></returns>
	public float deltaAndBackpropagate(List<float> targetOutputs, float η) {
		CheckNetwork();

		if(targetOutputs.Count != outputLayerSize) {
			Debug.LogError("Error: outputs to neural network are of size " + targetOutputs.Count + " instead of the expected " + outputLayerSize + ".");
			return 0;
		}

		float error = computeError(targetOutputs);


		//use delta rule to adjust weights in output layer
		for(int k = 0; k<outputLayerSize; ++k) {
			outputLayer[k].δ = sigmoidPrime(outputLayer[k].value) * outputLayer[k].δ;//use the pre-computed error value to figure out the delta value
			//adjust weights for the perceptron
			for(int j = 0; j<hiddenLayerSize; ++j) {
				float δW = outputLayer[k].δ * hiddenLayer[j].sigmoidValue * η;//use learning coefficient to be able to tweak the weight change
				outputLayer[k].weights[j] = outputLayer[k].weights[j] + δW;
			}
			//adjust bias
			float δBias = outputLayer[k].δ * 1 * η;
			outputLayer[k].weights[hiddenLayerSize] = outputLayer[k].weights[hiddenLayerSize] + δBias;
		}


		//backpropagate to hidden layer
		for(int j = 0; j<hiddenLayerSize; ++j) {
			float sumDeltas = 0;
			for(int k = 0; k<outputLayerSize; ++k) {
				float addDelta = outputLayer[k].δ * outputLayer[k].weights[j];
				sumDeltas += addDelta;
			}
			hiddenLayer[j].δ = hiddenLayer[j].sigmoidValue * sumDeltas;
			//adjust weights for the node
			for(int i = 0; i<inputLayerSize; ++i) {
				float δW = hiddenLayer[j].δ * sigmoid(inputLayer[i]) * η;
				hiddenLayer[j].weights[i] = hiddenLayer[j].weights[i] + δW;
			}
			//adjust bias
			float δBias = hiddenLayer[j].δ * 1 * η;
			hiddenLayer[j].weights[inputLayerSize] = hiddenLayer[j].weights[inputLayerSize] + δBias;
		}
		
		return error;
	}



	/// <summary>
	/// Trains the neural network using a single pattern
	/// </summary>
	/// <param name="pattern">the pattern to feed the network</param>
	/// <param name="η">The learning rate</param>
	/// <returns></returns>
	float train(TrainingPattern pattern, float η) {
		RunForward(pattern.Inputs);
		return deltaAndBackpropagate(pattern.Outputs, η);
	}


	/// <summary>
	/// Trains the neural network using a subset of a full training set, over several iterations
	/// </summary>
	/// <param name="set">The training set to use</param>
	public IEnumerator train(TrainingSet set) {

		if(set.Patterns.Count <= 0) {
			Debug.LogError("Not enough patterns in set! Cannot train.");
			msg = "Cannot train network: set does not contain enough patterns.";
			yield break;
		}

		if(set.Patterns[0].Inputs.Count != inputLayerSize) {
			inputLayerSize = set.Patterns[0].Inputs.Count;
			CheckNetwork();
		}
		if(set.Patterns[0].Outputs.Count != outputLayerSize) {
			outputLayerSize = set.Patterns[0].Outputs.Count;
			CheckNetwork();
		}

		string m = "";
		int progress = 0;

		int patternSize = (int)(set.Patterns.Count * subset);
		float initialError = -1, finalError = -1;
		float previousError = -1;
		int frames = 0;
		msg = "Training neural network using " + patternSize + " patterns (" + set.Patterns.Count + " in set); " + iterations + " iterations.";
		yield return null;

		for(int i = 0; i<iterations; ++i) {
			++progress;
			float totalError = 0;
			for(int pattern = 0; pattern < patternSize; ++pattern) {
				totalError += train(set.Patterns[pattern], η);
				++frames;
				if(frames > 25) {//give the application a short break every now and then to make sure it doesn't freeze for too long
					frames = 0;
					yield return null;
				}
			}
			totalError /= patternSize;
			if(initialError == -1) initialError = totalError;
			finalError = totalError;
			m += "\n" + i + ": error = " + totalError + ", learning rate = " + η;
			msg = "Progress: " + (int)((float)progress / iterations * 100.0f) + "% (" + progress*patternSize + " of " + iterations*patternSize + ")" + m;
			if(adaptiveLearningRate) {
				if(previousError > -1) {
					float errDiff = previousError - totalError;//a big number means we're doing good! a negative number is really bad, we're un-learning..
					if(errDiff < 0) {
						η *= 0.1f;
					} else {
						η *= δη;
					}
				}
			} else {
				η *= δη;//slow down learning rate
			}
			previousError = totalError;

			//Shuffle set after epoch (https://stats.stackexchange.com/questions/245502/shuffling-data-in-the-mini-batch-training-of-neural-network):
			if(shuffleSetAfterEachEpoch)
				trainingSet.Shuffle();
			yield return null;
		}
		save();
		msg = "Finished training. Initial error = " + initialError + "; final error = " + finalError;
		currentCoroutine = null;//flag as done
	}


	/// <summary>
	/// Runs the neural network on a full set to check the outputs given on known and unknown data
	/// </summary>
	/// <param name="set">The training set to use</param>
	public IEnumerator check(TrainingSet set) {

		msg = ("Checking neural network using " + set.Patterns.Count + " patterns.");
		string m = "";
		int progress = 0;

		float totalError = 0;
		foreach(TrainingPattern p in set.Patterns) {
			++progress;
			RunForward(p.Inputs);
			float error = computeError(p.Outputs);
			totalError += error;
			m += "\nError: " + error;
			if(progress % 50 == 0) {
				msg = "Progress: " + (int)((float)progress / set.Patterns.Count * 100.0f) + "% (" + progress + " of " + set.Patterns.Count + ")" + m;
				yield return null;
			}
		}
		totalError /= set.Patterns.Count;
		msg = ("Average error after check = " + totalError + ".");
		currentCoroutine = null;
	}

	public void reset() {
		inputLayer = null;
		hiddenLayer = null;
		outputLayer = null;
		save();
	}

#endif



	/// <summary>
	/// Checks that the neural network data is consistent with its size.
	/// Note: only happens in-engine; no checking in production builds.
	/// </summary>
	public void CheckNetwork() {
#if UNITY_EDITOR

		if(inputLayerSize <= 0 || hiddenLayerSize <= 0 || outputLayerSize <= 0){
			Debug.LogError("Neural network has layer size <= 0. This will result in a runtime error.");
		}

		bool ok = true;//flag will be set to false if anything was wrong with the network

		if(inputLayer == null || inputLayer.Length != inputLayerSize) {
			inputLayer = new float[inputLayerSize];
			ok = false;
		}

		//Check both layers for identical neurones
		if(hiddenLayer != null && outputLayer != null) {
			foreach(Neurone a in hiddenLayer) {
				foreach(Neurone b in outputLayer) {
					if(a == b) {
						Debug.LogError("Error: Same neurone in both layers!");
						return;
					}
				}
			}
		}

		//Do we have the correct amount of neurones in the hidden layer?
		if(hiddenLayer == null || hiddenLayer.Length != hiddenLayerSize) {
			hiddenLayer = new Neurone[hiddenLayerSize];
			for(int j = 0; j<hiddenLayerSize; ++j) {
				hiddenLayer[j] = new Neurone();
			}
			ok = false;
		}

		//Do neurones in the hidden layer have the correct amount of weights?
		for(int j = 0; j<hiddenLayerSize; ++j) {
			if(hiddenLayer[j].weights == null || hiddenLayer[j].weights.Length != inputLayerSize+1) {
				hiddenLayer[j].weights = new float[inputLayerSize+1];
				//assign random weights
				for(int i = 0; i<inputLayerSize+1; ++i) {
					hiddenLayer[j].weights[i] = UnityEngine.Random.Range(-1.0f, 1.0f);
				}
				ok = false;
			}
		}

		//Do we have the correct amount of neurones in the output layer?
		if(outputLayer == null || outputLayer.Length != outputLayerSize) {
			outputLayer = new Neurone[outputLayerSize];
			for(int k = 0; k < outputLayerSize; ++k) {
				outputLayer[k] = new Neurone();
			}
			ok = false;
		}

		//Do neurones in the output layer have the correct amount of weights?
		for(int k = 0; k < outputLayerSize; ++k) {
			if(outputLayer[k].weights == null || outputLayer[k].weights.Length != hiddenLayerSize+1) {
				outputLayer[k].weights = new float[hiddenLayerSize+1];
				//assign random weights
				for(int j = 0; j < hiddenLayerSize+1; ++j) {
					outputLayer[k].weights[j] = UnityEngine.Random.Range(-1.0f, 1.0f);
				}
				ok = false;
			}
		}

		if(!ok) {
			Debug.LogWarning("Refreshed Neural Network " + name + ".");
			save();
		}
#endif
	}

}

#if UNITY_EDITOR
[CustomEditor(typeof(NeuralNetwork))]
public class NeuralNetworkEditor : Editor {
	public override void OnInspectorGUI() {
		NeuralNetwork nn = target as NeuralNetwork;
		bool drawDefault = true;

		if(nn) {
			if(nn.currentCoroutine != null) {
				GUILayout.Label("Processing...");
				if(GUILayout.Button("Stop")) {
					EditorCoroutineUtility.StopCoroutine(nn.currentCoroutine);
					nn.currentCoroutine = null;
					nn.msg = "Interrupted.";
				}
				GUILayout.Label(nn.msg);
				drawDefault = false;
			} else {

				if(GUILayout.Button("Save")) {
					EditorUtility.SetDirty(target);
					AssetDatabase.SaveAssets();
				}

				if(nn.trainingSet) {
					if(GUILayout.Button("Train network")) {
						nn.currentCoroutine = EditorCoroutineUtility.StartCoroutine(nn.train(nn.trainingSet), this);
					}
					if(GUILayout.Button("Check network")) {
						nn.currentCoroutine = EditorCoroutineUtility.StartCoroutine(nn.check(nn.trainingSet), this);
					}
					GUILayout.Label(nn.msg);
				} else {
					GUILayout.Label("Cannot train without a training set.");
				}

				if(GUILayout.Button("Reset")) {
					nn.reset();
				}
			}
		}

		if(drawDefault) {//dont draw default inspector when processing network
			GUILayout.Space(40);
			DrawDefaultInspector();
		}
	}
}
#endif