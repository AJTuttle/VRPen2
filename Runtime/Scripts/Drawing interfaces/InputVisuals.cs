using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPen {

	public class InputVisuals : MonoBehaviour {


        //script refs
        protected VectorDrawing vectorMan;
        protected NetworkManager network;

        public List<Renderer> colorIndicatorRenderers;
		public List<int> colorIndicatorRenderersIndex;


        [System.NonSerialized]
        public InputDevice deviceData;

        protected Color32 currentColor = new Color32(0, 0, 0, 255);


        //state
        public enum ToolState {
            NORMAL, EYEDROPPER, ERASE
        }
        [System.NonSerialized]
        public ToolState state = ToolState.NORMAL;

        protected void Start() {

            vectorMan = FindObjectOfType<VectorDrawing>();
            network = FindObjectOfType<NetworkManager>();
        }


        public virtual void updateModel(VRPen.VRPenInput.ToolState newState, bool localInput) {
		
		}

		public void updateColorIndicators(Color32 color, bool localInput) {
			if (colorIndicatorRenderers.Count != colorIndicatorRenderersIndex.Count) {
				Debug.Log("There is not the same ammount of color indicators as there are indices.");
				return;
			}
			for(int x = 0; x < colorIndicatorRenderers.Count; x++) { 
				colorIndicatorRenderers[x].materials[colorIndicatorRenderersIndex[x]].color = color;

			}

            if (localInput) network.sendInputVisualEvent(deviceData.deviceIndex, color, state);
		}
	}
}