using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace VRPen {
    public class StarTablet2 : MonoBehaviour {
        
        //enums/classes
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
        
        #region plugins
            
            #if UNITY_ANDROID && !UNITY_EDITOR
                    AndroidJavaClass androidPlugin;
		            int androidPlugin_res = -1;
		            AndroidJavaObject context;
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
        
        #endregion

        
        //vars
        public List<PenSample> penSamples = new List<PenSample>();
        
        
        #region start

            private void Start() {
                
                //start
                #if UNITY_ANDROID && !UNITY_EDITOR
				    androidPlugin = new AndroidJavaClass("vel.engr.uga.edu.hidplugin.HidPlugin");
				    AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
				    AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
				    context = activity.Call<AndroidJavaObject>("getApplicationContext");
				    androidPlugin_res = androidPlugin.CallStatic<int>("runTablet", context);
                #else
                    startStar();
                #endif
            }

        #endregion

        #region update

            private void Update() {
                
                //clear samples
                penSamples.Clear();
                
                #if UNITY_ANDROID && !UNITY_EDITOR
                    
                    
                    int[] penValues = new int[11];
                    if (androidPlugin_res < 0) {
				        androidPlugin_res = androidPlugin.CallStatic<int>("runTablet", context);
			        }
			        else {
				        penValues = androidPlugin.CallStatic<int[]>("getTabletValues");
				        if (penValues != null)
				        {

					        //tipLocation = new Vector3(penValues[0]/50800f*.254f, 0 , .152f-penValues[1] / (6 * 5080f) * .152f);
					        //tipPressure = penValues[2] / 8192f;

                            Vector2 point = new Vector2(penValues[0]/50800f*.254f, .152f-penValues[1] / (6 * 5080f) * .152f);
                            float pressure = penValues[2] / 8192f;
                            PenState state = (PenState)penValues[3];
                            penSamples.Add(new PenSample(point, pressure, state));
				        }
			        }
                #else
                    
                    //read raw data
                    int[] penValues = new int[800];
                    int n = 0;
                    n = readPen(penValues, 100);
                    
                    //get pen samples for each data point
                    for (int i = 0; i < n; i++) {
                        Vector2 point = new Vector2(1 - (float) penValues[i * 8 + 0] / penValues[i * 8 + 6],
                            (float) penValues[i * 8 + 1] / penValues[i * 8 + 7]);
                        float pressure = (float) penValues[i * 8 + 2] / penValues[i * 8 + 5];
                        PenState state = (PenState) penValues[i * 8 + 3];
                        penSamples.Add(new PenSample(point, pressure, state));
                    }

                #endif

            }

        #endregion
        
        void OnApplicationQuit() {
            #if UNITY_ANDROID && !UNITY_EDITOR
				androidPlugin.CallStatic("stopTablet");
            #else
                stopStar();
            #endif
        }

        private void OnDisable() {
            #if UNITY_ANDROID && !UNITY_EDITOR
				androidPlugin.CallStatic("stopTablet");
            #endif
        }

        
    }
    
}