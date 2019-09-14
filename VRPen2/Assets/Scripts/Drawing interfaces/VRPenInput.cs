using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace VRPen {

    //datastruct
    public struct InputData {
        public VRPenInput.HoverState hover;
        public float pressure;
        public RaycastHit hit;
        public Display display;
    }

    public abstract class VRPenInput : MonoBehaviour {



        //script refs
        VectorDrawing vectorMan;
        NetworkManager network;
        InputDevice deviceData;

        //public vars
        public bool UIClickDown = false;

        //drawing data
        float xFloat = 0f;
        float yFloat = 0f;
        bool newline = true;
        Color32 currentColor = new Color32(0, 0, 0, 255);
        


        //hover
        public enum HoverState {
            NONE, NODRAW, SELECTABLE, DRAW, PALETTE
        }
        [System.NonSerialized]
        public HoverState hover = HoverState.NONE;

        //state
        public enum MarkerState {
            NORMAL, EYEDROPPER, ERASE
        }
        [System.NonSerialized]
        public MarkerState state = MarkerState.NORMAL;



        //abstract methods
        protected abstract InputData getInputData();
        protected abstract void updateColorIndicator(Color32 color);

		//logging data
		float lineTime = 0;
		float lastLineInputTime = 0;


        protected void Start() {

            //get script vars
            vectorMan = FindObjectOfType<VectorDrawing>();
            network = FindObjectOfType<NetworkManager>();
            deviceData = GetComponent<InputDevice>();
            SceneManager.activeSceneChanged += grabReferences;

			//set colors
			updateColorIndicator(currentColor);

        }

        void grabReferences(Scene old, Scene newScene) {

            //make sure drawing references are there 
            if (vectorMan == null)
                vectorMan = FindObjectOfType<VectorDrawing>();
            if (network == null)
                network = FindObjectOfType<NetworkManager>();
            if (deviceData == null)
                deviceData = GetComponent<InputDevice>();
            
        }

        protected void idle() {
            
            hover = HoverState.NONE;
            newline = true;
			//log
			if (lineTime > 0.001f) {
				List<string> logData = new List<string> {
						deviceData.deviceIndex.ToString(),
						lineTime.ToString(),
						"draw",
						"null"
					};
				Logger.LogRow("marker_data", logData);
				lineTime = 0;
			}

        }


        public virtual void switchTool(MarkerState newState) {
            //change state
            state = newState;
        }

        protected void input() {

            //raycast and retrieve data from cast
            InputData data = getInputData();
            hover = data.hover;

            //use raycast data to do stuff
            if (hover == HoverState.DRAW) canvasHover(data);
            else if (hover == HoverState.NODRAW || hover == HoverState.NONE) noDrawHover(data);
            else if (hover == HoverState.PALETTE) paletteHover(data);
            else if (hover == HoverState.SELECTABLE) selectableHover(data);
		
			
        }


        void selectableHover(InputData data) {

            //update vars
            xFloat = 0f;
            yFloat = 0f;
            
            //get button
            Selectable button = data.hit.collider.GetComponent<Selectable>();


            //if click event
            if (UIClickDown) {

                //select
                button.Select();

				//logging
				List<string> logData = new List<string> {
						deviceData.deviceIndex.ToString(),
						"null",
						button.gameObject.name,
						"null"
					};
				Logger.LogRow("marker_data", logData);

				//add data to passthrough
				ButtonPassthrough bp;
                if((bp = button.GetComponent<ButtonPassthrough>()) != null) {
                    bp.clickedBy = this;
                }

                //button state
                if (button is Button) {
                    ((Button)button).onClick.Invoke();

                }

                //toggle state
                else if (button is Toggle) {
                    ((Toggle)button).isOn = !((Toggle)button).isOn;

                }
            }

            //still select if no click
            else {
                button.Select();
            }


            //newline 
            newline = true;

        }

        void paletteHover(InputData data) {

            //set current color if click event
            if (UIClickDown) {

				

				//get vars from raycast
				Vector3 pos = data.hit.collider.transform.InverseTransformPoint(data.hit.point);
                float xCoord = (pos.x + 0.5f) / 1f;
                float yCoord = (pos.y + 0.5f) / 1f;
                Texture2D palette = data.hit.collider.GetComponent<Renderer>().material.mainTexture as Texture2D;
                Color32 paletteColor = palette.GetPixel((int)(xCoord * palette.width), (int)(yCoord * palette.height));

                //set            
                currentColor = paletteColor;

				//logging
				List<string> logData = new List<string> {
						deviceData.deviceIndex.ToString(),
						"null",
						"pallete",
						currentColor.ToString()
					};
				Logger.LogRow("marker_data", logData);

				//indicator
				updateColorIndicator(currentColor);

            }

            //newline
            newline = true;

        }

        protected void raycastPriorityDetection(ref InputData data, RaycastHit[] hits) {

            foreach (RaycastHit hit in hits) {

                if (hit.collider.GetComponent<Selectable>() != null) {

                    data.hover = HoverState.SELECTABLE;
                    data.hit = hit;
                    break;

                }
                else if (hit.collider.GetComponent<Tag>() != null && hit.collider.GetComponent<Tag>().tag.Equals("Palette") &&
                   (data.hover == HoverState.NONE || data.hover == HoverState.NODRAW || data.hover == HoverState.DRAW)) {

                    data.hover = HoverState.PALETTE;
                    data.hit = hit;

                }
                else if (hit.collider.GetComponent<Tag>() != null && hit.collider.GetComponent<Tag>().tag.Equals("NoDraw") &&
                    (data.hover == HoverState.NONE || data.hover == HoverState.DRAW)) {

                    data.hover = HoverState.NODRAW;
                    data.hit = hit;

                }
                else if (hit.collider.GetComponent<Tag>() != null && hit.collider.GetComponent<Tag>().tag.Equals("Draw_Area") &&
                    (data.hover == HoverState.NONE)) {

                    data.hover = HoverState.DRAW;
                    data.hit = hit;

                }

            }
            
        }

        void canvasHover(InputData data) {

            
			//logging
			if(state == MarkerState.ERASE || state == MarkerState.NORMAL) {
				if (!newline) lineTime += Time.time - lastLineInputTime;
				lastLineInputTime = Time.time;
			}


            //get vars from ray
            Transform canvas = data.hit.collider.transform;
            Vector3 pos = canvas.InverseTransformPoint(data.hit.point);
            xFloat = (pos.x + 1) / 2f;
            yFloat = (pos.y + 0.6f) / 1.2f;


            //do stuff
            switch (state) {

                case MarkerState.NORMAL:

                    if (data.pressure < 0.001f) newline = true;
                    else {
                        vectorMan.draw(network.localPlayer, deviceData.deviceIndex, newline, currentColor, xFloat, yFloat, data.pressure, data.display.currentLocalCanvas.canvasId, true);
                        //newline 
                        newline = false;
                    }

                    break;

                case MarkerState.EYEDROPPER:

                    if (UIClickDown) {

                        //make copy of rendertexture to texture2d to get pixel
                        RenderTexture tex = (RenderTexture)data.display.currentLocalCanvas.GetComponent<Renderer>().material.mainTexture;
                        int xPos = tex.width - (int)(xFloat * tex.width);
                        int yPos = (int)(yFloat * tex.height);
                        Texture2D tempTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                        RenderTexture.active = tex;
                        tempTexture.ReadPixels(new Rect(xPos - 1, yPos - 1, tex.width, tex.height), 0, 0);
                        tempTexture.Apply();

                        //get color
                        currentColor = tempTexture.GetPixel(0, 0);

                        //make sure alpha is 255
                        currentColor.a = 255;

                        //update indicator
                        updateColorIndicator(currentColor);

                        //newline 
                        newline = true;

                    }
                    break;

                case MarkerState.ERASE:

                    vectorMan.draw(network.localPlayer, deviceData.deviceIndex, newline, data.display.currentLocalCanvas.bgColor, xFloat, yFloat, data.pressure, data.display.currentLocalCanvas.canvasId, true);

                    //newline 
                    newline = false;
                    break;
            }



        }

        void noDrawHover(InputData data) {

            newline = true;
        }

        

    }

}
