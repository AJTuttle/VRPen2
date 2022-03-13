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
        [Header("Necessary Input Parameters")]
        [Space(10)]
        [System.NonSerialized]
        public bool UIClickDown = false;

        //drawing data
        float xFloat = 0f;
        float yFloat = 0f;


        //hover
        public enum HoverState {
            NONE, NODRAW, SELECTABLE, DRAW, PALETTE, GRABBABLE, AREAINPUT
        }
        [System.NonSerialized]
        public HoverState hover = HoverState.NONE;




        //grab
        UIGrabbable grabbed;


        //abstract methods
        protected abstract InputData getInputData();

        //current line
        //[System.NonSerialized]
        public VectorLine currentLine;
        
        //undo stack
        //[System.NonSerialized]
        //public List<VectorGraphic> undoStack = new List<VectorGraphic>();
        

		

        protected void Start() {

            
            //base
            base.Start();


        }

        protected void Update() {
            base.Update();
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
		

		

        protected void input() {

            
            //raycast and retrieve data from cast
            InputData data = getInputData();
            hover = data.hover;
            
            //cancel if no display
            if (data.display == null) return;

            //if currently grabbing a uigrabbable
            if (grabbed != null && data.hover != HoverState.NONE) {
                Vector3 pos = grabbed.parent.parent.InverseTransformPoint(data.hit.point);
                grabbed.updatePosRelativeToGrab(pos.x, pos.y);
                return;
            }

            
            //use raycast data to do stuff
            if (hover == HoverState.DRAW) canvasHover(data);
            else if (hover == HoverState.AREAINPUT) areaInputHover(data);
            else if (hover == HoverState.GRABBABLE) grabbableHover(data);
            else if (hover == HoverState.NODRAW || hover == HoverState.NONE) noDrawHover(data);
            else if (hover == HoverState.PALETTE) paletteHover(data);
            else if (hover == HoverState.SELECTABLE) selectableHover(data);
            
            
			
        }

        protected void raycastPriorityDetection(ref InputData data, RaycastHit[] hits) {

            foreach (RaycastHit hit in hits) {

                //get depth data
                bool useDepth = false;
                int currDepth = 0;
                if (data.hover == HoverState.GRABBABLE) {
                    currDepth = data.hit.collider.GetComponent<Graphic>().depth;
                    useDepth = true;
                }
                else if (data.hover == HoverState.SELECTABLE) {
                    if (data.hit.collider.GetComponent<Slider>())
                        currDepth = data.hit.collider.GetComponent<Slider>().targetGraphic.depth;
                    else currDepth = data.hit.collider.GetComponent<Graphic>().depth;
                    useDepth = true;
                }
                
                //
                if (hit.collider.GetComponent<Selectable>() != null) {
                    
                    if (useDepth) {
                        int depth;
                        if (hit.collider.GetComponent<Slider>())
                            depth = hit.collider.GetComponent<Slider>().targetGraphic.depth;
                        else depth = hit.collider.GetComponent<Graphic>().depth;
                        if (depth > currDepth) {
                            data.hover = HoverState.SELECTABLE;
                            data.hit = hit;
                        }
                    }
                    else {
                        data.hover = HoverState.SELECTABLE;
                        data.hit = hit;
                    }


                }
                else if (hit.collider.GetComponent<UIGrabbable>() != null) {
                    if (useDepth) {
                        if (hit.collider.GetComponent<Graphic>().depth > currDepth) {
                            data.hover = HoverState.GRABBABLE;
                            data.hit = hit;
                        }
                    }
                    else {
                        data.hover = HoverState.GRABBABLE;
                        data.hit = hit;
                    }

                }
                else if (hit.collider.GetComponent<UIInputArea>() != null &&
                         (data.hover == HoverState.NONE || data.hover == HoverState.NODRAW || data.hover == HoverState.DRAW 
                          || data.hover == HoverState.PALETTE)) {

                    data.hover = HoverState.AREAINPUT;
                    data.hit = hit;

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

        void areaInputHover(InputData data) {
            UIInputArea area = data.hit.collider.GetComponent<UIInputArea>();
            
            //get point in the collider's space
            Vector3 point = data.hit.collider.transform.InverseTransformPoint(data.hit.point);
                
            //invoke input
            area.input(point.x, point.y, data.pressure);
            
            //endline
            endLine();
        }

        void grabbableHover(InputData data) {
            
            UIGrabbable currentGrab = data.hit.collider.GetComponent<UIGrabbable>();
            
            //grab event
            if (grabbed == null && currentGrab != null) {
                grabbed = currentGrab;
                Vector3 pos = grabbed.parent.parent.InverseTransformPoint(data.hit.point);
                grabbed.grab(pos.x, pos.y);
            }
            
            //endline
            endLine();
            
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
                BoxCollider sliderCol = (BoxCollider) data.hit.collider;
                float scale = sliderCol.size.x - sliderCol.size.y; //the minus y is for since the furthest left or right
                                                                   //is half the diameter of the bubble (y/2) off 
                value = value / scale + 0.5f;

                UISlider slidyBoi = button.GetComponent<UISlider>();

                //set value
                if (slidyBoi != null) slidyBoi.setPos(value, true);
                else {

                    //scale value to slider extremes
                    value = ((Slider) button).minValue +
                            value * (((Slider) button).maxValue - ((Slider) button).minValue);
                    ((Slider) button).value = value;
                }

            }

            //if click event
            if (UIClickDown) {

                //select
                button.Select();

                //button state
                if (button is Button) {
                    ((Button) button).onClick.Invoke();
                }

                //toggle state
                else if (button is Toggle) {
                    ((Toggle) button).isOn = !((Toggle) button).isOn;
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
                Transform canvas = data.hit.collider.transform;
                Vector3 pos = canvas.InverseTransformPoint(data.hit.point);
                float xCoord = pos.x + 0.5f;
                float yCoord = pos.y + 0.5f;
                Texture2D palette = data.hit.collider.GetComponent<Renderer>().material.mainTexture as Texture2D;
                Color32 paletteColor = palette.GetPixel((int)(xCoord * palette.width), (int)(yCoord * palette.height));


                //set          
				updateColor(paletteColor, true);

            }

            //endline
            endLine();

        }

        

        void canvasHover(InputData data) {

            //return if no canvas
            if (data.display.currentLocalCanvas == null) {
                return;
            }

            //get vars from ray
            Transform canvas = data.hit.collider.transform;
            Vector3 pos = canvas.InverseTransformPoint(data.hit.point);
            float aspectRatio = data.display.currentLocalCanvas.aspectRatio;
            xFloat = pos.x * aspectRatio;
            yFloat = pos.y;
            
            //do stuff
            switch (state) {

                case ToolState.NORMAL:
                    
                    //update thickness if the display has a thickness modifer
                    if (data.display.UIMan.optionalLineThicknessModifier != null) {
                        data.pressure *= data.display.UIMan.optionalLineThicknessModifier.value;
                    }
                    
                    //end line if 0 pressure
                    if (data.pressure == 0) {
                        endLine();
                        return;
                    }
                    
                    //add new line if need new one
                    if (currentLine == null) {
                        currentLine = VectorDrawing.s_instance.createNewLine(NetworkManager.s_instance.getLocalPlayerID(), getColor(), NetworkManager.s_instance.localGraphicIndex,
                            data.display.currentLocalCanvas, true);
                        NetworkManager.s_instance.localGraphicIndex++;
                        VectorDrawing.s_instance.undoStack.Add(currentLine);
                    }
                    
                    VectorDrawing.s_instance.draw(NetworkManager.s_instance.getLocalPlayerID(), currentLine.localIndex, false, getColor(), xFloat, yFloat, data.pressure, data.display.currentLocalCanvas.canvasId, true);
                    if(data.pressure == 0) UnityEngine.Debug.LogError("test no pressh");
                    break;

                case ToolState.EYEDROPPER:

                    if (UIClickDown) {

                        //make copy of rendertexture to texture2d to get pixel
                        RenderTexture tex = (RenderTexture)data.display.currentLocalCanvas.GetComponent<Renderer>().material.mainTexture;
                        int xPos = tex.width - (int)((pos.x + .5f) * tex.width);
                        int yPos = (int)((pos.y + .5f) * tex.height);
                        //int yPos = tex.height - (int)((pos.y + .5f) * tex.height); //for some reason, need to do this to make it work on webgl
                        Texture2D tempTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
                        RenderTexture.active = tex;
                        tempTexture.ReadPixels(new Rect(xPos - 1, yPos - 1, tex.width, tex.height), 0, 0);
                        tempTexture.Apply();

                        //get color
                        Color32 tempCol = tempTexture.GetPixel(0, 0);

                        //make sure alpha is 255
                        tempCol.a = 255;

                        //update indicator
                        updateColor(tempCol, true);

                    }

                    endLine();

                    break;

                case ToolState.ERASE:

                    //end line if 0 pressure
                    if (data.pressure == 0) {
                        endLine();
                        return;
                    }
                    
                    //add new line if need new one
                    if (currentLine == null) {
                        currentLine = VectorDrawing.s_instance.createNewLine(NetworkManager.s_instance.getLocalPlayerID(), data.display.currentLocalCanvas.bgColor, NetworkManager.s_instance.localGraphicIndex,
                            data.display.currentLocalCanvas, true);
                        NetworkManager.s_instance.localGraphicIndex++;
                        VectorDrawing.s_instance.undoStack.Add(currentLine);
                    }
                    
                    VectorDrawing.s_instance.draw(NetworkManager.s_instance.getLocalPlayerID(), currentLine.localIndex, false, data.display.currentLocalCanvas.bgColor, xFloat, yFloat, data.pressure, data.display.currentLocalCanvas.canvasId, true);
                    if(data.pressure == 0) UnityEngine.Debug.LogError("test no pressh");
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

        protected void endLine() {
            if (currentLine != null) {
                VectorDrawing.s_instance.endLineEvent(NetworkManager.s_instance.getLocalPlayerID(), currentLine.localIndex, currentLine.canvasId, true);
            }
            currentLine = null;
        }
        

    }

}
