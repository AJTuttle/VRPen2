using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace VRPen {

	public class InputVisuals : MonoBehaviour {

		[Header("Optional Visual Parameters")]
        [Space(10)]
        public List<GameObject> markerModels;
        public List<GameObject> eyedropperModels;
        public List<GameObject> eraserModels;

        public bool colorIndicatorsForceOpaque;
        public List<Image> colorIndicatorUIImages;
        public List<Renderer> colorIndicatorRenderers;
		public List<int> colorIndicatorRenderersIndex;

		public Color32 defaultColor;
		public ToolState defaultState;
		
        //[System.NonSerialized]
        //public InputDevice deviceData;

        private Color32 currentColor = new Color32(0, 0, 0, 255);

        [Header("Optional Vars for Network Syncing Visuals")] 
        [Space(10)]
        public bool sendVisualUpdates;
        [Tooltip("The playerID of the person who owns the device (must match the one used in the networking system)")]
        public ulong ownerID;
        [Tooltip("Any unique identifier for the device (only needs to be unique for the owner's devices)")]
        public int uniqueIdentifier;
		[Tooltip("Should the target pos and rot be sent (cannot change after start)")]
        public bool sendTargetTransform;
        [Tooltip("Rate to send target transform data (if applicable) (cannot change after start)")]
        public float targetTransformSendRepeatRate;
        [Tooltip("Target to be sent and to sync")]
        public Transform targetTransform;
        [Tooltip("Target should be sent as local to a display (false = global space)")]
        public bool targetLocalToDisplay;
        [Tooltip("Display ID to set the target local to")]
        public byte targetLocalToDisplayID;
        [Tooltip("Target will be constantly interpolating to the most recent network update")]
        public bool interpolateTargetFromNetwork;
        [FormerlySerializedAs("interpolationRate")] [Tooltip("Percent interpolation per frame [0,1]")]
        public float interpolateTargetRate;
        private Vector3 interpolatePosTarget;
        private Quaternion interpolateRotTarget;


        //state
        public enum ToolState {
            NORMAL, EYEDROPPER, ERASE
        }
        [System.NonSerialized]
        public ToolState state = ToolState.NORMAL;

        protected void Start() {

	        //add to input list
	        VectorDrawing.s_instance.inputDevices.Add(this);

	        //set default stuff
	        updateColor(defaultColor, false);
            updateModel(defaultState, false);
            
            //set default interpolation values
            if (targetTransform != null) {
	            interpolatePosTarget = targetTransform.position;
	            interpolateRotTarget = targetTransform.rotation;
            }
            
            //start sending target
            if (sendTargetTransform) InvokeRepeating(nameof(sendTarget), 0.1f, targetTransformSendRepeatRate);

        }

        protected void Update() {
	        interpolateTarget();
        }

        private void OnDestroy() {
	        VectorDrawing.s_instance.inputDevices.Remove(this);
        }
        
        public void updateColor(Color32 color, bool localInput) {
	        currentColor = color;
	        updateColorIndicators(color, localInput);
	        
        }

        public Color32 getColor() {
	        return currentColor;
        }
        
        void sendTarget() {

	        //ignore if not connected or if no target set
	        if (targetTransform == null|| !NetworkManager.s_instance.connectedAndCaughtUp) return;
	        
	        //send target based on whether it needs to be sent as local to another transform
	        if (targetLocalToDisplay) {
		        Display display = VectorDrawing.s_instance.displays.Find(x => x.uniqueIdentifier == targetLocalToDisplayID);
		        
		        //cant find display
		        if (display == null) {
			        //error
			        UnityEngine.Debug.LogError("Could not find display to sync input device transform to.");
			        
			        //send
			        NetworkManager.s_instance.sendInputVisualTarget(ownerID, uniqueIdentifier, targetTransform.gameObject, targetTransform.position, targetTransform.rotation, false, 0);
		        }
		        
		        //found local display
		        else {
			        
			        //get local vars
			        Vector3 localPos = display.transform.InverseTransformPoint(targetTransform.position);
			        Quaternion localRot = Quaternion.Inverse(display.transform.rotation) * targetTransform.rotation;
			        
			        //send
			        NetworkManager.s_instance.sendInputVisualTarget(ownerID, uniqueIdentifier, targetTransform.gameObject, localPos, localRot, true, targetLocalToDisplayID);
		        }
	        }
	        
	        //global
	        else {
		        //send
		        NetworkManager.s_instance.sendInputVisualTarget(ownerID, uniqueIdentifier, targetTransform.gameObject, targetTransform.position, targetTransform.rotation, false, 0);
	        }
	        
        }

        public void receiveTarget(Vector3 pos, Quaternion rot, bool active, bool localToDisplay, byte localToDisplayID) {
	        //set target
	        if (targetTransform != null) {
		        
		        //if local
		        if (localToDisplay) {
			        
			        //get display
			        Display display = VectorDrawing.s_instance.displays.Find(x => x.uniqueIdentifier == localToDisplayID);
			        
			        //error, use global
			        if (display == null) {
				        interpolatePosTarget = pos;
				        interpolateRotTarget = rot;
			        }
			        
			        //set local
			        else {
				        interpolatePosTarget = display.transform.TransformPoint(pos);
				        interpolateRotTarget = display.transform.rotation * rot;
			        }
		        }
		        
		        //set global
		        else {
			        interpolatePosTarget = pos;
			        interpolateRotTarget = rot;
		        }
		        
		        //set active
		        targetTransform.gameObject.SetActive(active);
		        
	        }
        }

        void interpolateTarget() {
	        if (interpolateTargetFromNetwork) {
		        targetTransform.position = Vector3.Lerp(targetTransform.position, interpolatePosTarget, interpolateTargetRate);
		        targetTransform.rotation = Quaternion.Lerp(targetTransform.rotation, interpolateRotTarget, interpolateTargetRate);
	        }
        }

        public void updateModel(VRPen.VRPenInput.ToolState newState, bool localInput) {

            //change state
            state = newState;

            //switch models
            if (markerModels != null) {
	            foreach (GameObject obj in markerModels) {
		            obj.SetActive(newState == ToolState.NORMAL);
	            }
            }
            if (eraserModels != null) {
	            foreach (GameObject obj in eraserModels) {
		            obj.SetActive(newState == ToolState.ERASE);
	            }
            }
            if (eyedropperModels != null) {
	            foreach (GameObject obj in eyedropperModels) {
		            obj.SetActive(newState == ToolState.EYEDROPPER);
	            }
            }

            //network
            if (localInput && sendVisualUpdates) NetworkManager.s_instance.sendInputVisualEvent(ownerID, uniqueIdentifier, currentColor, newState);
        }

		private void updateColorIndicators(Color32 color, bool localInput)
		{

			//set opacity if needed
			if (colorIndicatorsForceOpaque) color = new Color32(color.r, color.g, color.b, Byte.MaxValue);
			
			if (colorIndicatorRenderers.Count != colorIndicatorRenderersIndex.Count) {
				Debug.Log("There is not the same ammount of color indicators as there are indices.");
				return;
			}
			for(int x = 0; x < colorIndicatorRenderers.Count; x++) { 
				colorIndicatorRenderers[x].materials[colorIndicatorRenderersIndex[x]].color = color;

			}
            foreach (Image i in colorIndicatorUIImages) {
                i.color = color;
            }

            
            //network
            if (localInput && sendVisualUpdates) NetworkManager.s_instance.sendInputVisualEvent(ownerID, uniqueIdentifier, color, state);
		}
	}
}