﻿using System;
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
        bool[] btn = new bool[9];
        bool[] btnDown = new bool[9];
        bool[] btnUp = new bool[9];
        int wheelMagnitudeTracker = 0;
        
        
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
                    
                    //pen values
                    int[] penValues = new int[11];
                    if (androidPlugin_res < 0) {
				        androidPlugin_res = androidPlugin.CallStatic<int>("runTablet", context);
			        }
			        else {
				        penValues = androidPlugin.CallStatic<int[]>("getTabletValues");
				        if (penValues != null)
				        {
                            Vector2 point = new Vector2(1-penValues[0]/50800f, 1-penValues[1]/30480f);
                            float pressure = penValues[2] / 8192f;
                            PenState state = (PenState)penValues[3];
                            penSamples.Add(new PenSample(point, pressure, state));
				        }
			        }
                
                    //btns
                    for (int i = 0; i < 9; i++) {
                        btnDown[i] = false;
                        btnUp[i] = false;
                    }
                    for (int x = 0; x < 6; x++){
                        bool b = penValues[x+5] > 0;
                        if (!btn[x] && b) {
                            btnDown[x] = true;
                        }
                        if (btn[x] && !b) {
                            btnUp[x] = true;
                        }
                        btn[x] = b;
                    }
                    
                    //wheel
                    wheelMagnitudeTracker += penValues[4];
                
                #else
                    
                    //read raw data
                    int[] penValues = new int[800];
                    int numPen = 0;
                    numPen = readPen(penValues, 100);
                    
                    //get pen samples for each data point
                    for (int i = 0; i < numPen; i++) {
                        Vector2 point = new Vector2(1 - (float) penValues[i * 8 + 0] / penValues[i * 8 + 6],
                            (float) penValues[i * 8 + 1] / penValues[i * 8 + 7]);
                        float pressure = (float) penValues[i * 8 + 2] / penValues[i * 8 + 5];
                        PenState state = (PenState) penValues[i * 8 + 3];
                        penSamples.Add(new PenSample(point, pressure, state));
                    }
                    
                    //button
                    int[] btnValues = new int[900];
                    int numBtn = 0;
                    numBtn = readButtons(btnValues, 100);
                    
                    //reset down and released
                    for (int i = 0; i < 9; i++) {
                        btnDown[i] = false;
                        btnUp[i] = false;
                    }
                    for (int i = 0; i < numBtn; i++) {
                        for (int j = 0; j < 9; j++) {
                            bool b = btnValues[i * 9 + j] > 0;
                            if (!btn[j] && b) {
                                btnDown[j] = true;
                            }
                            if (btn[j] && !b) {
                                btnUp[j] = true;
                            }
                            btn[j] = b;
                            //wheel
                            if (j == 8) {
                                wheelMagnitudeTracker += btnValues[i * 9 + j];
                            }
                        }
                    }

                #endif

            }

        #endregion
        
        public bool getButtonDown(int button) {
            if (button > 8 || button < 0) {
                return false;
            }
            return btnDown[button];
        }
        public bool getButtonUp(int button) {
            if (button > 8 || button < 0) {
                return false;
            }

            return btnUp[button];
        }
        public bool getButton(int button) {
            return btn[button];
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