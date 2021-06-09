using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;
using UnityEngine.UI;

namespace VRPen {

	public class StarTablet : MonoBehaviour {
		
		#if UNITY_ANDROID && !UNITY_EDITOR
			AndroidJavaClass plugin;
		#else
			[DllImport("VelDevicePlugin")]
			private static extern int startStar();
			[DllImport("VelDevicePlugin")]
			private static extern int stopStar();
			[DllImport("VelDevicePlugin")]
			private static extern int readButtons(int[] buttons, int num);
			[DllImport("VelDevicePlugin")]
			private static extern int readPen(int[] pen, int num); //reads up to the last "num".  The last value in this is the latest
		#endif


		public Vector3 tipOffset = new Vector3(0, 0, -.15f);

		public List<Matrix4x4> penTrackerTransforms = new List<Matrix4x4>();
		public List<Matrix4x4> tabletTrackerTransforms = new List<Matrix4x4>();
		public List<Vector3> tabletPoints = new List<Vector3>();

		public enum PenState {
			HOVER = 0, TOUCH = 1, LBUTTON_AIR = 2, LBUTTON_TOUCH = 3, RBUTTON_TOUCH = 4, RBUTTON_AIR = 5
		}
		public class PenSample {
			public Vector2 point;
			public float pressure;
			public PenState state;
			public PenSample(Vector2 pt, float prs, PenState st) {
				point = pt;
				pressure = prs;
				state = st;
			}
		}
		
		public List<PenSample> penSamples = new List<PenSample>();
		int[] buttons = new int[0];
		int[] lastButtons = new int[9];
		int[] pressedEvents = new int[9];
		int[] releasedEvents = new int[9];

		int wheelMagnitudeTracker = 0;
		int buttonInputs;

		// Use this for initialization
		void Start() {
			#if UNITY_ANDROID && !UNITY_EDITOR
				plugin = new AndroidJavaClass("vel.engr.uga.edu.hidplugin.HidPlugin");
				AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
				AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
				AndroidJavaObject context = activity.Call<AndroidJavaObject>("getApplicationContext");
				plugin.CallStatic("runTablet", context);
			#else
				//start tablet (monoprice.cpp)
				startStar();
			#endif

			//init array values
			for (int i = 0; i < 9; i++) {
				lastButtons[i] = pressedEvents[i] = releasedEvents[i] = 0;
			}
		}

		// Update is called once per frame
		void Update() {

			int[] penValues = new int[800];
			int n = 0;
			#if UNITY_ANDROID && !UNITY_EDITOR
				penValues = plugin.CallStatic<int[]>("getTabletValues");
				if (penValues != null) {
					n = penValues.Length / 4;
				} else {
				}
			#else
				n = readPen(penValues, 100);
			#endif
			//Debug.Log(n);
			penSamples.Clear();


			for (int i = 0; i < n; i++) {
				#if UNITY_ANDROID && !UNITY_EDITOR
					Vector2 point = new Vector2(1 - (float)penValues[i * 8 + 0] / 50800, (float)penValues[i * 8 + 1] / 30480);
	                float pressure = (float)penValues[i * 8 + 2] / 8191;
	                PenState state = (PenState)penValues[i * 8 + 3];
	                penSamples.Add(new PenSample(point, pressure, state));
				#else
					Vector2 point = new Vector2(1 - (float)penValues[i * 8 + 0] / penValues[i * 8 + 6], (float)penValues[i * 8 + 1] / penValues[i * 8 + 7]);
					float pressure = (float)penValues[i * 8 + 2] / penValues[i * 8 + 5];
					PenState state = (PenState)penValues[i * 8 + 3];
					penSamples.Add(new PenSample(point, pressure, state));
				#endif
			}


			int[] buttonValues = new int[900];
			#if UNITY_ANDROID && !UNITY_EDITOR
			#else
				buttonInputs = readButtons(buttonValues, 100);
			#endif
			
			buttons = new int[buttonInputs * 9];

			for (int i = 0; i < 9; i++) {
				pressedEvents[i] = releasedEvents[i] = 0;
			}
			for (int i = 0; i < buttonInputs; i++) {
				for (int j = 0; j < 9; j++) {
					int b = buttonValues[i * 9 + j];
					if (lastButtons[j] == 0 && b == 1) {
						pressedEvents[j] = 1;
					}
					if (lastButtons[j] == 1 && b == 0) {
						releasedEvents[j] = 1;
					}
					lastButtons[j] = b;
					buttons[i * 9 + j] = b;

					//wheel
					if (j == 8) {
						//Debug.Log(b);
						wheelMagnitudeTracker += b;
					}
				}
			}
		}


		public bool getButtonDown(int button) {
			if (button > 8 || button < 0) {
				return false;
			}
			return pressedEvents[button] > 0;
		}
		public bool getButtonUp(int button) {
			if (button > 8 || button < 0) {
				return false;
			}
			return releasedEvents[button] > 0;
		}
		public bool getButton(int button) {
			return lastButtons[button] > 0;
		}

		public int getWheel() {
			int value = wheelMagnitudeTracker;
			return value;
		}

		public void resetWheelMag() {
			wheelMagnitudeTracker = 0;
		}

		void OnApplicationQuit() {
			#if UNITY_ANDROID && !UNITY_EDITOR
				plugin.CallStatic("stopTablet");
			#else
				stopStar();
			#endif
		}

        private void OnDisable() {
			#if UNITY_ANDROID && !UNITY_EDITOR
				plugin.CallStatic("stopTablet");
			#endif
        }

		//public static void function1_func(double[] x, ref double func, object obj) {
		//
		// 	StarTablet t = (StarTablet)obj;
		// 	func = 0;
		// 	Matrix4x4 m = Matrix4x4.TRS(new Vector3((float)x[3], (float)x[4], (float)x[5]), Quaternion.Euler(new Vector3((float)x[0] * 720.0f, (float)x[1] * 720.0f, (float)x[2] * 720.0f)), Vector3.one);
		// 	//Matrix4x4 m = Matrix4x4.TRS(new Vector3((float)x[0], (float)x[1], (float)x[2]), t.transform.localRotation, Vector3.one);
		//
		// 	for (int i = 0; i < t.tabletTrackerTransforms.Count; i++) {
		// 		//t*m*tabletpoint == pen*offset
		// 		Vector3 tabletPoint = (t.tabletTrackerTransforms[i] * m).MultiplyPoint(t.tabletPoints[i]);
		// 		Vector3 penPoint = (t.penTrackerTransforms[i]).MultiplyPoint(t.tipOffset);
		// 		func += (float)(tabletPoint - penPoint).sqrMagnitude;
		//
		//
		// 	}
		// 	//Debug.Log(func);
		//
		// }

	}

}
