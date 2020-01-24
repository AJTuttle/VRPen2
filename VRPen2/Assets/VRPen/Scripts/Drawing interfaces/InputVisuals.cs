using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPen {

	public class InputVisuals : MonoBehaviour {

		public List<Renderer> colorIndicatorRenderers;
		public List<int> colorIndicatorRenderersIndex;


		protected virtual void updateModel(VRPen.VRPenInput.ToolState newState) {
		
		}

		protected void updateColorIndicators(Color32 color) {
			if (colorIndicatorRenderers.Count != colorIndicatorRenderersIndex.Count) {
				Debug.Log("There is not the same ammount of color indicators as there are indices.");
				return;
			}
			for(int x = 0; x < colorIndicatorRenderers.Count; x++) { 
				colorIndicatorRenderers[colorIndicatorRenderersIndex[x]].material.color = color;
			}
		}
	}
}