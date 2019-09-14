using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace VRPen {

    public class MarkerInput : VRPenInput {


        //public vars
        public Transform modelParent;
        public GameObject markerModel;
        public GameObject eyedropperModel;
        public GameObject eraserModel;
        public MeshRenderer markerColorIndicator;
        public MeshRenderer eyedropperColorIndicator;
        public Transform followTarget;
        
        public AnimationCurve pressureCurve;
        public float pressureDistanceMultiplier = 1;
        protected float raycastDistance = 0.2f;
		private float pressure;
		public float Pressure { get => pressure; }

		//snap vars
		protected Transform snappedTo;
        Display snappedDisplay = null;
		bool snappedToChecker = false;


		void Start() {
            base.Start();
			SceneManager.activeSceneChanged += SceneChanged;
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

		void SceneChanged(Scene oldScene, Scene newScene) {
			Destroy(gameObject);
		}

        public override void switchTool(MarkerState newState) {
            //if no change return
            if (state == newState) return;

            //turn off old tool
            switch (state) {
                case MarkerState.NORMAL:
                    markerModel.SetActive(false);
                    break;
                case MarkerState.EYEDROPPER:
                    eyedropperModel.SetActive(false);
                    break;
                case MarkerState.ERASE:
					eraserModel.SetActive(false);
					break;
            }

            //switch state
            base.switchTool(newState);

            //turn on new tool
            switch (state) {
                case MarkerState.NORMAL:
                    markerModel.SetActive(true);
                    break;
                case MarkerState.EYEDROPPER:
                    eyedropperModel.SetActive(true);
                    break;
                case MarkerState.ERASE:
					eraserModel.SetActive(true);
					break;
            }

        }
        protected override InputData getInputData() {

            //init returns
            InputData data = new InputData();

            //raycast
            Vector3 origin = transform.position - snappedTo.forward * raycastDistance;
            RaycastHit[] hits;
            hits = Physics.RaycastAll(origin, snappedTo.forward, 1f);
            Debug.DrawRay(origin, snappedTo.forward, Color.red, Time.deltaTime);


            //detect hit based off priority
            raycastPriorityDetection(ref data, hits);


            //pressure
            float rawPressure = Mathf.Clamp01(pressureDistanceMultiplier * ((raycastDistance - data.hit.distance) / raycastDistance));
			pressure = pressureCurve.Evaluate(rawPressure);
            data.pressure = pressure;
			if (state == MarkerState.ERASE) data.pressure *= 4;

            //display
            data.display = snappedDisplay;

            //move marker pos;
            modelParent.position = data.hit.point;

            return data;

        }

        protected override void updateColorIndicator(Color32 color) {

            markerColorIndicator.materials[1].color = color;
            eyedropperColorIndicator.materials[2].color = color;

        }

        private void OnTriggerEnter(Collider other) {
            Tag tag;
            if ((tag = other.gameObject.GetComponent<Tag>()) != null) {
                //if (tag.tag.Equals("marker-visible")) visuals.SetActive(true);
                if (tag.tag.Equals("snap")) {
                    snappedTo = other.transform;
					snappedToChecker = true;
					snappedDisplay = snappedTo.parent.parent.GetComponent<Display>();

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
