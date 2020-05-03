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

    public abstract class VRPenInput : InputVisuals {
		

        //public vars
		public InputDevice.InputDeviceType deviceType;
        public bool UIClickDown = false;

        //drawing data
        float xFloat = 0f;
        float yFloat = 0f;


        //hover
        public enum HoverState {
            NONE, NODRAW, SELECTABLE, DRAW, PALETTE
        }
        [System.NonSerialized]
        public HoverState hover = HoverState.NONE;


        //stamp
        public GameObject stampPrefab;
        [System.NonSerialized]
        public StampGenerator currentStamp;


        //grab
        UIGrabbable grabbed;


        //abstract methods
        protected abstract InputData getInputData();

        

		

        protected void Start() {

            //base
            base.Start();

            //get script var
            SceneManager.activeSceneChanged += grabReferences;


        }
        void grabReferences(Scene old, Scene newScene) {

            //make sure drawing references are there 
            if (vectorMan == null)
                vectorMan = FindObjectOfType<VectorDrawing>();
            if (network == null)
                network = FindObjectOfType<NetworkManager>();
        }

        protected void idle() {
            hover = HoverState.NONE;
            endLine();

            if (grabbed != null) {
                grabbed.unGrab();
                grabbed = null;
            }
        }


        public void switchTool(ToolState newState) {

			updateModel(newState, true);
        }
		

		public void newStamp(Transform parent, Display display, int stampIndex) {
            if (currentStamp != null) currentStamp.close();

            GameObject obj = Instantiate(stampPrefab, parent);
            currentStamp = obj.GetComponent<StampGenerator>();
            currentStamp.instantiate(this, vectorMan, network.getLocalPlayer(), display, stampIndex);
        }

        protected void input() {

            //stampIndicator.SetActive(false);

            //raycast and retrieve data from cast
            InputData data = getInputData();
            hover = data.hover;

            //if currently grabbing a uigrabbable
            if (grabbed != null && data.hover != HoverState.NONE) {
                Vector3 pos = grabbed.parent.parent.InverseTransformPoint(data.hit.point);
                grabbed.updatePosRelativeToGrab(pos.x, pos.y);
                return;
            }

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

            //add data to passthrough
            ButtonPassthrough bp;
            if ((bp = button.GetComponent<ButtonPassthrough>()) != null) {
                bp.clickedBy = this;
            }

            //grabbable if not grabbing something
            if (grabbed == null && button.GetComponent<UIGrabbable>() != null) {
                grabbed = button.GetComponent<UIGrabbable>();
                Vector3 pos = grabbed.parent.parent.InverseTransformPoint(data.hit.point);
                grabbed.grab(pos.x, pos.y);
            }

            //slider state
            if (button is Slider) {

                //get value [0-1]
                float value = data.hit.collider.transform.InverseTransformPoint(data.hit.point).x;
                float scale = ((BoxCollider)data.hit.collider).size.x;
                value = value / scale + 0.5f;

                //set value
                UISlider slidyBoi = button.GetComponent<UISlider>();
                if(slidyBoi != null) slidyBoi.setPos(value, true);

            }

            //if click event
            if (UIClickDown) {

                //select
                button.Select();


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

            //endline
            endLine();


        }

        void paletteHover(InputData data) {

            //set current color if click event
            if (UIClickDown) {

				

				//get vars from raycast
                Transform canvas = data.hit.transform;
                Vector3 pos = canvas.InverseTransformPoint(data.hit.point);
                float xCoord = pos.x + 0.5f;
                float yCoord = pos.y + 0.5f;
                Texture2D palette = data.hit.collider.GetComponent<Renderer>().material.mainTexture as Texture2D;
                Color32 paletteColor = palette.GetPixel((int)(xCoord * palette.width), (int)(yCoord * palette.height));


                //set            
                currentColor = paletteColor;

				//indicator
				updateColorIndicators(currentColor, true);

            }

            //endline
            endLine();

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
            

            //get vars from ray
            Transform canvas = data.hit.transform;
            Vector3 pos = canvas.InverseTransformPoint(data.hit.point);
            float aspectRatio = data.display.currentLocalCanvas.aspectRatio;
            xFloat = pos.x * aspectRatio;
            yFloat = pos.y;

            //do stuff
            switch (state) {

                case ToolState.NORMAL:
                    
                    vectorMan.draw(network.getLocalPlayer(), deviceData.deviceIndex, false, currentColor, xFloat, yFloat, data.pressure, data.display.currentLocalCanvas.canvasId, true);
                    
                    break;

                case ToolState.EYEDROPPER:

                    if (UIClickDown) {

                        //make copy of rendertexture to texture2d to get pixel
                        RenderTexture tex = (RenderTexture)data.display.currentLocalCanvas.GetComponent<Renderer>().material.mainTexture;
                        int xPos = tex.width - (int)((pos.x + .5f) * tex.width);
                        int yPos = (int)((pos.y + .5f) * tex.height);
                        Texture2D tempTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                        RenderTexture.active = tex;
                        tempTexture.ReadPixels(new Rect(xPos - 1, yPos - 1, tex.width, tex.height), 0, 0);
                        tempTexture.Apply();

                        //get color
                        currentColor = tempTexture.GetPixel(0, 0);

                        //make sure alpha is 255
                        currentColor.a = 255;

                        //update indicator
                        updateColorIndicators(currentColor, true);

                    }

                    endLine();

                    break;

                case ToolState.ERASE:

                    vectorMan.draw(network.getLocalPlayer(), deviceData.deviceIndex, false, data.display.currentLocalCanvas.bgColor, xFloat, yFloat, data.pressure, data.display.currentLocalCanvas.canvasId, true);
                    break;

                //case ToolState.STAMP:

                    

                //    if (UIClickDown) {

                        
                //        stampIndicator.SetActive(true);
                //        stampIndicator.transform.position = data.hit.point;
                //        stampIndicator.transform.localScale = Vector3.one * stampSize * 0.6f;
                //        //vectorMan.stamp(vectorMan.stampTest, network.localPlayer, deviceData.deviceIndex, xFloat, yFloat, stampSize, data.display.currentLocalCanvas.canvasId, true);
                //    }
                //    break;
            }



        }

        void noDrawHover(InputData data) {
            endLine();
        }

        void endLine() {
            if (deviceData != null && deviceData.currentGraphic != null) vectorMan.endLineEvent(network.getLocalPlayer(), deviceData.deviceIndex, true);
        }
        

    }

}
