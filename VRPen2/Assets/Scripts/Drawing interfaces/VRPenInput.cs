﻿using System.Collections;
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
        public InputDevice deviceData;

        //public vars
        public bool UIClickDown = false;

        //drawing data
        float xFloat = 0f;
        float yFloat = 0f;
        Color32 currentColor = new Color32(0, 0, 0, 255);


        //stamp data
        float stampSize=.1f;


        //hover
        public enum HoverState {
            NONE, NODRAW, SELECTABLE, DRAW, PALETTE
        }
        [System.NonSerialized]
        public HoverState hover = HoverState.NONE;

        //state
        public enum ToolState {
            NORMAL, EYEDROPPER, ERASE
        }
        [System.NonSerialized]
        public ToolState state = ToolState.NORMAL;

        //stamp
        public GameObject stampIndicator;
        public GameObject stampPrefab;
        public Stamp currentStamp;
        


        //abstract methods
        protected abstract InputData getInputData();
        protected abstract void updateColorIndicator(Color32 color);

        

		

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
            endLine();
        }


        public virtual void switchTool(ToolState newState) {
            //change state
            state = newState;
        }

        public void newStamp(Transform parent, Display display) {
            if (currentStamp != null) currentStamp.close();

            GameObject obj = Instantiate(stampPrefab, parent);
            currentStamp = obj.GetComponent<Stamp>();
            currentStamp.instantiate(this, vectorMan, network.localPlayer, display);
        }

        protected void input() {

            //stampIndicator.SetActive(false);

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

            //add data to passthrough
            ButtonPassthrough bp;
            if ((bp = button.GetComponent<ButtonPassthrough>()) != null) {
                bp.clickedBy = this;
            }

            //slider state
            if (button is Slider) {

                //get value [0-1]
                float value = data.hit.collider.transform.InverseTransformPoint(data.hit.point).x;
                float scale = ((BoxCollider)data.hit.collider).size.x;
                value = value / scale + 0.5f;

                //set value
                ((Slider)button).value = value;

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
				Vector3 pos = data.hit.collider.transform.InverseTransformPoint(data.hit.point);
                float xCoord = (pos.x + 0.5f) / 1f;
                float yCoord = (pos.y + 0.5f) / 1f;
                Texture2D palette = data.hit.collider.GetComponent<Renderer>().material.mainTexture as Texture2D;
                Color32 paletteColor = palette.GetPixel((int)(xCoord * palette.width), (int)(yCoord * palette.height));

                //set            
                currentColor = paletteColor;

				//indicator
				updateColorIndicator(currentColor);

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
            Transform canvas = data.hit.collider.transform;
            Vector3 pos = canvas.InverseTransformPoint(data.hit.point);
            xFloat = (pos.x + 1) / 2f;
            yFloat = (pos.y + 0.6f) / 1.2f;


            //do stuff
            switch (state) {

                case ToolState.NORMAL:
                    
                    vectorMan.draw(network.localPlayer, deviceData.deviceIndex, false, currentColor, xFloat, yFloat, data.pressure, data.display.currentLocalCanvas.canvasId, true);
                    
                    break;

                case ToolState.EYEDROPPER:

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

                    }

                    endLine();

                    break;

                case ToolState.ERASE:

                    vectorMan.draw(network.localPlayer, deviceData.deviceIndex, false, data.display.currentLocalCanvas.bgColor, xFloat, yFloat, data.pressure, data.display.currentLocalCanvas.canvasId, true);
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
            if (deviceData.currentGraphic != null) vectorMan.endLineEvent(network.localPlayer, deviceData.deviceIndex, true);
        }
        

    }

}
