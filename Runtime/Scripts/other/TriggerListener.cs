using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace VRPen {
    public class TriggerListener : MonoBehaviour {

        public class TriggerEvent : UnityEvent<Collider> { };

        public TriggerEvent triggerEnter = new TriggerEvent();
        public TriggerEvent triggerStay = new TriggerEvent();
        public TriggerEvent triggerExit = new TriggerEvent();


        private void OnTriggerEnter(Collider other) {
            triggerEnter.Invoke(other);
        }

        private void OnTriggerStay(Collider other) {
            triggerStay.Invoke(other);
        }

        private void OnTriggerExit(Collider other) {
            triggerExit.Invoke(other);
        }

    }
    
}


