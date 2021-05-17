using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VRPen {

	public class InputVisuals : MonoBehaviour {


        //script refs
        protected VectorDrawing vectorMan;
        protected NetworkManager network;
        
        [Header("Optional Visual Parameters")]
        [Space(10)]
        public GameObject markerModel;
        public GameObject eyedropperModel;
        public GameObject eraserModel;

        public List<Image> colorIndicatorUIImages;
        public List<Renderer> colorIndicatorRenderers;
		public List<int> colorIndicatorRenderersIndex;


        [System.NonSerialized]
        public VectorGraphic currentGraphic;
        //public InputDevice deviceData;

        protected Color32 currentColor = new Color32(0, 0, 0, 255);


        //state
        public enum ToolState {
            NORMAL, EYEDROPPER, ERASE
        }
        [System.NonSerialized]
        public ToolState state = ToolState.NORMAL;

        protected void Start() {

            //grab refs
            vectorMan = FindObjectOfType<VectorDrawing>();
            network = FindObjectOfType<NetworkManager>();
            
            //set colors
            updateColorIndicators(currentColor, false);

        }


        public void updateModel(VRPen.VRPenInput.ToolState newState, bool localInput) {

            //change state
            state = newState;

            //switch models
            if (markerModel != null) markerModel.SetActive(newState == ToolState.NORMAL);
            if (eraserModel != null) eraserModel.SetActive(newState == ToolState.ERASE);
            if (eyedropperModel != null) eyedropperModel.SetActive(newState == ToolState.EYEDROPPER);

            //network
            //if (localInput) network.sendInputVisualEvent(deviceData.deviceIndex, currentColor, newState);
        }

		public void updateColorIndicators(Color32 color, bool localInput) {
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

            //if (localInput) network.sendInputVisualEvent(deviceData.deviceIndex, color, state);
		}
	}
}