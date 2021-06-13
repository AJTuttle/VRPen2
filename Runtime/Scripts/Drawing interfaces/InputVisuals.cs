using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VRPen {

	public class InputVisuals : MonoBehaviour {

		[Header("Optional Visual Parameters")]
        [Space(10)]
        public GameObject markerModel;
        public GameObject eyedropperModel;
        public GameObject eraserModel;

        public List<Image> colorIndicatorUIImages;
        public List<Renderer> colorIndicatorRenderers;
		public List<int> colorIndicatorRenderersIndex;


        //[System.NonSerialized]
        //public InputDevice deviceData;

        protected  Color32 currentColor = new Color32(0, 0, 0, 255);

        [Header("Optional Vars for Network Syncing Visuals")] 
        [Space(10)]
        public bool sendVisualUpdates;
        [Tooltip("The playerID of the person who owns the device (must match the one used in the networking system)")]
        public ulong ownerID;
        [Tooltip("Any unique identifier for the device (only needs to be unique for the owner's devices)")]
        public int uniqueIdentifier;


        //state
        public enum ToolState {
            NORMAL, EYEDROPPER, ERASE
        }
        [System.NonSerialized]
        public ToolState state = ToolState.NORMAL;

        protected void Start() {

	        //add to input list
	        VectorDrawing.s_instance.inputDevices.Add(this);

	        //set color indicators
            updateColorIndicators(currentColor, false);

        }

        public void updateColor(Color32 color, bool localInput) {
	        
	        currentColor = color;
	        updateColorIndicators(color, localInput);
	        
        }

        public void updateModel(VRPen.VRPenInput.ToolState newState, bool localInput) {

            //change state
            state = newState;

            //switch models
            if (markerModel != null) markerModel.SetActive(newState == ToolState.NORMAL);
            if (eraserModel != null) eraserModel.SetActive(newState == ToolState.ERASE);
            if (eyedropperModel != null) eyedropperModel.SetActive(newState == ToolState.EYEDROPPER);

            //network
            if (localInput && sendVisualUpdates) NetworkManager.s_instance.sendInputVisualEvent(ownerID, uniqueIdentifier, currentColor, newState);
        }

		private void updateColorIndicators(Color32 color, bool localInput) {
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