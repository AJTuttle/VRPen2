using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VRPen {

    public class MarkerInput : VRPenInput {
         

        //public varz
        [Header("Marker Parameters")]
        [Space(10)]
        public Transform modelParent;
        public Transform followTarget;
        

        public AnimationCurve pressureCurve;
        public float pressureDistanceMultiplier = 1;
        protected float raycastDistance = 0.2f;
		private float pressure;
		public float Pressure { get => pressure; }

        //snap vars
        [System.NonSerialized]
        public Transform snappedTo;
        Display snappedDisplay = null;
		bool snappedToChecker = false;

        Vector3 snappedPos;
        bool snappedPosExists;

		new void Start() {
            base.Start();
        }

        void Update() {
            
            //follow a target
            if (followTarget != null) {
                modelParent.position = followTarget.position;
                transform.position = followTarget.position;
                modelParent.rotation = followTarget.rotation;
                transform.rotation = followTarget.rotation;
            }

            //check for marker input
            if (snappedTo != null) input();
            else idle();
            
            //reset click value at end of frame
            UIClickDown = false;
        }

		private void FixedUpdate() {
			if (snappedToChecker) {
				snappedToChecker = false;
			}
			else {
				if (snappedTo != null) {
					snappedTo = null;
					snappedDisplay = null;
					modelParent.localPosition = Vector3.zero;
				}
			}
		}


        private void LateUpdate() {
        
            //snapping is in lateupdate to avoid the snapped happening before the marker is moved in the frame
            if (snappedPosExists) {
                modelParent.position = snappedPos;
                snappedPosExists = false;
            } else {
                modelParent.localPosition = Vector3.zero;
            }
        }
        
        protected override InputData getInputData() {

            //init returns
            InputData data = new InputData();

            //raycast
            Vector3 origin = transform.position - snappedTo.forward * raycastDistance;
            RaycastHit[] hits;
            hits = Physics.RaycastAll(origin, snappedTo.forward, 1f);
            UnityEngine.Debug.DrawRay(origin, snappedTo.forward, Color.red, Time.deltaTime);


            //detect hit based off priority
            raycastPriorityDetection(ref data, hits);

            //break if there is nothing detected
            if (data.hit.collider == null) {
                Debug.LogWarning("Marker is inside of the snap zone, but it couldnt find anything to snap to when raycasting. IDK if this will cause an issue. Be wary.");
                return data;
            }


            //pressure
            float rawPressure = Mathf.Clamp01(pressureDistanceMultiplier * ((raycastDistance - data.hit.distance) / raycastDistance));
			pressure = pressureCurve.Evaluate(rawPressure);
            data.pressure = pressure;
			if (state == ToolState.ERASE) data.pressure *= 4;

            //display
            data.display = snappedDisplay;

            //move marker pos;
            snappedPos = data.hit.point;
            snappedPosExists = true;

            return data;

        }

        

        private void OnTriggerEnter(Collider other) {
            Tag tag;
            if ((tag = other.gameObject.GetComponent<Tag>()) != null) {
                //if (tag.tag.Equals("marker-visible")) visuals.SetActive(true);
                if (tag.tag.Equals("snap")) {
                    snappedTo = other.transform;
					snappedToChecker = true;
					snappedDisplay = snappedTo.parent.parent.parent.parent.GetComponent<Display>();
                    int i = 0;

                }
            }
        }

		private void OnTriggerStay(Collider other) {
			if (snappedTo != null && other.transform == snappedTo) snappedToChecker = true;
		}

		private void OnTriggerExit(Collider other) {

            Tag tag;
            if ((tag = other.gameObject.GetComponent<Tag>()) != null) {
                //if (tag.tag.Equals("marker-visible")) visuals.SetActive(false);
                if (tag.tag.Equals("snap") && snappedTo != null) {

					//check for click
					UIClickDown = true;
					input();

                    snappedTo = null;
                    snappedDisplay = null;
                    modelParent.localPosition = Vector3.zero;

				}

            }

        }

    }

}
