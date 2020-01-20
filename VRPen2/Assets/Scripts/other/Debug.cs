using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPen {

    public class Debug : MonoBehaviour {

        public static Debug instance;

        DebugUI[] UIs;

        void Start() {
            instance = this;
            UIs = FindObjectsOfType<DebugUI>();
        }
        
        public void LogErrorWork(string str) {

            UnityEngine.Debug.LogError("VRPen: " + str);
            foreach(DebugUI ui in UIs) {
                ui.display(str);
            }

        }

        public static void LogError(string str) {

            instance.LogErrorWork(str);
        }

        public static void LogWarning(string str) {
            UnityEngine.Debug.LogWarning("VRPen: " + str);
        }

        public static void Log(string str) {
            UnityEngine.Debug.Log("VRPen: " + str);
        }

    }
}
