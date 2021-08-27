using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;
using System;

namespace VRPen {

	public class UIManager : MonoBehaviour {
        
        private bool stateQueued = false;

        //grabbable
        public UIGrabbable canvasListGrabbable;
        public UIGrabbable calculatorGrabbable;
        public UIGrabbable stampExplorerGrabbable;
        public UIGrabbable clearMenuGrabbable;
        public UIGrabbable movableMenuGrabbable;
        
		public GameObject slidingParent;
		public GameObject SlideMenu;
		public GameObject SlideMenuParent;
		public GameObject calculatorParent;
		public GameObject canvasMenuParent;
		public GameObject canvasListParent;
		public GameObject stampExplorerParent;
		public GameObject clearMenuParent;
		public GameObject menuArrow;
		public GameObject movableMenuParent;
        public UISlider stampExplorerSlider;
		public Display display;
		
        public Transform stampUIParent;

		public GameObject canvasButtonPrefab;

        public Slider optionalLineThicknessModifier;

		public bool sideMenuOpen = false;
		private bool sideMenuMoving = false;

		const float RESIZE_SPEED = .1f;

        private bool sendingStateOverNetwork = false;

        //used to set default slider values
        //public Slider stampSizeSlider;
        //const float DEFAULT_STAMP_SIZE = 0.1f;

        //stamp resources
        public Transform stampResourceParent;
        public GameObject stampResourcePrefab;


		//for outside access
		public Button invisButton;
		public GameObject colorPanel;
		public GameObject grayPanel;


        public GameObject addCanvasButton;
        //public GameObject addCanvasToggle;

        //additional ui that the user of vrpen can define
        public Transform additionalUIWindowParent;
        public GameObject additionalUIWindowPrefab;
        private List<AdditionalUIWindow> additionalUIWindows = new List<AdditionalUIWindow>();

        private const float UI_SYNC_PERIOD = 0.1f;

		private void Awake() {
            

            //grab stamp file names to put in explorer
            addFilesToStampExplorer();

            //set default stamp size
            //stampSizeSlider.value = DEFAULT_STAMP_SIZE;
            //stampSliderPassthrough()

		}

        private void Update() {
            //start packing data 
            if(!sendingStateOverNetwork && NetworkManager.s_instance.connectedAndCaughtUp) startPackingState(UI_SYNC_PERIOD);
        }


        public void openSideMenu(bool localInput) {
			if (sideMenuMoving) return;
			sideMenuMoving = true;

            // uncull things that will be visible
            SlideMenu.SetActive(true);

			StartCoroutine(resizeMenu(slidingParent,  slidingParent.transform.localPosition,
                slidingParent.transform.localPosition + new Vector3(200, 0, 0), true, localInput));
            StartCoroutine(resizeMenu(SlideMenuParent, SlideMenuParent.transform.localPosition,
                SlideMenuParent.transform.localPosition + new Vector3(200, 0, 0), true, localInput));
            sideMenuOpen = true;
			menuArrow.transform.GetChild(0).gameObject.SetActive(false);
			menuArrow.transform.GetChild(1).gameObject.SetActive(true);

			if (localInput) queueState();

        }

		public void closeSideMenu(bool localInput) {
			if (sideMenuMoving) return;
			sideMenuMoving = true;

			StartCoroutine(resizeMenu(slidingParent, slidingParent.transform.localPosition,
                slidingParent.transform.localPosition - new Vector3(200, 0, 0), false, localInput));
            StartCoroutine(resizeMenu(SlideMenuParent, SlideMenuParent.transform.localPosition,
                SlideMenuParent.transform.localPosition - new Vector3(200, 0, 0), false, localInput));
            sideMenuOpen = false;
			menuArrow.transform.GetChild(0).gameObject.SetActive(true);
			menuArrow.transform.GetChild(1).gameObject.SetActive(false);

            if (localInput) queueState();


        }

        public void additionalUIToggle(bool localInput, AdditionalUIWindow additionalUIWindow) {
            
            calculatorParent.SetActive(false);
            canvasMenuParent.SetActive(false);
            clearMenuParent.SetActive(false);
            stampExplorerParent.SetActive(false);
            foreach (var window in additionalUIWindows) {
                if (window == additionalUIWindow) {
                    if (window.isEnabled()) window.setActive(false);
                    else window.setActive(true);
                }
                else {
                    window.setActive(false);
                }
            }

            if (localInput) queueState();
        }
        
		public void calculatorToggle(bool localInput) {

			calculatorParent.SetActive(!calculatorParent.activeSelf);
			canvasMenuParent.SetActive(false);
			clearMenuParent.SetActive(false);
            stampExplorerParent.SetActive(false);
            foreach (var window in additionalUIWindows) {
                window.setActive(false);
            }



            if (localInput) queueState();

		}

		public void canvasMenuToggle(bool localInput) {

			canvasMenuParent.SetActive(!canvasMenuParent.activeSelf);
			calculatorParent.SetActive(false);
			clearMenuParent.SetActive(false);
            stampExplorerParent.SetActive(false);
            foreach (var window in additionalUIWindows) {
                window.setActive(false);
            }

            if (localInput) queueState();

        }
        public void clearMenuToggle(bool localInput) {

			clearMenuParent.SetActive(!clearMenuParent.activeSelf);
			calculatorParent.SetActive(false);
			canvasMenuParent.SetActive(false);
            stampExplorerParent.SetActive(false);
            foreach (var window in additionalUIWindows) {
                window.setActive(false);
            }

            if (localInput) queueState();

        }

        public void stampExplorerToggle(bool localInput) {

            clearMenuParent.SetActive(false);
            calculatorParent.SetActive(false);
            canvasMenuParent.SetActive(false);
            stampExplorerParent.SetActive(!stampExplorerParent.activeSelf);
            foreach (var window in additionalUIWindows) {
                window.setActive(false);
            }


            if (localInput) queueState();
        }

		public void closeMenus(bool localInput) {

            if (clearMenuParent.activeSelf) {

			    clearMenuParent.SetActive(false);
                if (localInput) queueState();
            }
            if (calculatorParent.activeSelf) {

                calculatorParent.SetActive(false);
                if (localInput) queueState();
            }
            if (canvasMenuParent.activeSelf) {

                canvasMenuParent.SetActive(false);
                if (localInput) queueState();
            }
            if (stampExplorerParent.activeSelf) {

                stampExplorerParent.SetActive(false);
                if (localInput) queueState();
            }
            
            foreach (var window in additionalUIWindows) {
                if (window.isEnabled()) {
                    window.setActive(false);
                    if (localInput) queueState();
                }
            }
            
		}
   

        public void highlightTimer(float time) {
            StartCoroutine(turnOffHighlight(time));
        }

        IEnumerator turnOffHighlight(float time) {
            yield return new WaitForSeconds(time);
            invisButton.Select();
            
        }

        IEnumerator resizeMenu(GameObject obj, Vector3 startPos, Vector3 endPos, bool opening, bool localInput) {

            if (opening) SlideMenu.SetActive(true);
            
            while (Vector3.Distance(obj.transform.localPosition, endPos) > 5) {
                obj.transform.localPosition = Vector3.Lerp(obj.transform.localPosition, endPos, RESIZE_SPEED);
                yield return null;
            }
            obj.transform.localPosition = endPos;
            
            if (!opening) SlideMenu.SetActive(false);

            sideMenuMoving = false;
            

		}


        public void removeAddCanvasButtons() {

            //turn off
            addCanvasButton.SetActive(false);
            //addCanvasToggle.SetActive(false);
            
            //shift
            canvasListParent.GetComponent<RectTransform>().sizeDelta += new Vector2(0, -60);
            canvasListParent.GetComponent<RectTransform>().localPosition += new Vector3(0, 30, 0);
            canvasListParent.GetComponent<BoxCollider>().size += new Vector3(0, -60, 0);
        }

        public void addCanvas(byte canvasId) {

           

            //make button
            GameObject buttonObj = GameObject.Instantiate(canvasButtonPrefab, canvasListParent.transform);
            buttonObj.GetComponent<RectTransform>().sizeDelta = new Vector2(200f, 50f);
            buttonObj.GetComponent<BoxCollider>().size = new Vector3(200, 50, 0.002f);
            canvasListParent.GetComponent<RectTransform>().sizeDelta += new Vector2(0,60);
            canvasListParent.GetComponent<RectTransform>().localPosition += new Vector3(0, -30, 0);
            canvasListParent.GetComponent<BoxCollider>().size += new Vector3(0,60, 0);
            buttonObj.transform.SetAsFirstSibling();
            

            //assign listener to button
            buttonObj.GetComponent<Button>().onClick.AddListener(() => display.swapCurrentCanvas(canvasId, true));

            //change text
            Text text = buttonObj.transform.GetChild(0).GetComponent<Text>();
            text.text = "Canvas " + canvasId;
            buttonObj.name = text.text;
            text.fontSize = 25;


          

        }

        public void queueState() {
            stateQueued = true;
        }
        

        void dequeueState() {
            stateQueued = false;
        }

        public void startPackingState(float period) {
            sendingStateOverNetwork = true;
            InvokeRepeating(nameof(packState), 1.0f, period);
        }

        public void packState() {
            
            //dont do anything if we dont wanna sync
            if (!display.syncDisplay) return;

            //make sure state is queued
            if (!stateQueued) return;
            
            //pack data
            List<byte> data = new List<byte>();


            //side menu
            data.Add(sideMenuOpen ? (byte)1 : (byte)0);
            
            //calc
            data.Add(calculatorParent.activeSelf ? (byte)1 : (byte)0);
            data.AddRange(BitConverter.GetBytes(calculatorGrabbable.x));
            data.AddRange(BitConverter.GetBytes(calculatorGrabbable.y));
            
            //canvas menu
            data.Add(canvasMenuParent.activeSelf ? (byte)1 : (byte)0);
            data.AddRange(BitConverter.GetBytes(canvasListGrabbable.x));
            data.AddRange(BitConverter.GetBytes(canvasListGrabbable.y));
            
            
            //stamp menu
            data.Add(stampExplorerParent.activeSelf ? (byte)1 : (byte)0);
            data.AddRange(BitConverter.GetBytes(stampExplorerGrabbable.x));
            data.AddRange(BitConverter.GetBytes(stampExplorerGrabbable.y));
            data.AddRange(BitConverter.GetBytes(stampExplorerSlider.slider.value));
            
            //clear menu
            data.Add(clearMenuParent.activeSelf ? (byte)1 : (byte)0);
            data.AddRange(BitConverter.GetBytes(clearMenuGrabbable.x));
            data.AddRange(BitConverter.GetBytes(clearMenuGrabbable.y));
            
            //moveable menu
            data.Add(movableMenuParent.activeSelf ? (byte)1 : (byte)0);
            data.AddRange(BitConverter.GetBytes(movableMenuGrabbable.x));
            data.AddRange(BitConverter.GetBytes(movableMenuGrabbable.y));

            //additional ui windows
            data.AddRange(BitConverter.GetBytes(additionalUIWindows.Count));
            for (int x = 0; x < additionalUIWindows.Count; x++) {
                data.Add(additionalUIWindows[x].parent.activeSelf ? (byte)1 : (byte)0);
                data.AddRange(BitConverter.GetBytes(additionalUIWindows[x].grabbable.x));
                data.AddRange(BitConverter.GetBytes(additionalUIWindows[x].grabbable.y));
            }

            //dont send if no data
            if (data.Count == 0) return;

            //send data
            NetworkManager.s_instance.sendUIState(display.uniqueIdentifier, data.ToArray());

            //get rid of state sync queue since it was just sent
            dequeueState();

            
        }

        public void unpackState(byte[] data) {

            //unpack
            int offset = 0;

            //side
            bool open = ReadByte(data, ref offset) == 1? true: false;
            if (open && !sideMenuOpen) {
                openSideMenu(false);
            }
            else if(!open && sideMenuOpen) {
                closeSideMenu(false);
            }
            
            //calc
            bool calcEn = ReadByte(data, ref offset) == 1 ? true : false;
            if (calcEn && !calculatorParent.activeSelf) {
                calculatorToggle(false);
            }
            else if (!calcEn && calculatorParent.activeSelf) {
                calculatorToggle(false);
            }
            Vector2 calcPos = new Vector2(ReadFloat(data, ref offset), ReadFloat(data, ref offset));
            calculatorGrabbable.setExactPos(calcPos.x, calcPos.y);
            
            //canvas
            bool canvasEn = ReadByte(data, ref offset) == 1 ? true : false;
            if (canvasEn && !canvasMenuParent.activeSelf) {
                canvasMenuToggle(false);
            }
            else if (!canvasEn && canvasMenuParent.activeSelf) {
                canvasMenuToggle(false);
            }
            Vector2 canvasPos = new Vector2(ReadFloat(data, ref offset), ReadFloat(data, ref offset));
            canvasListGrabbable.setExactPos(canvasPos.x, canvasPos.y);
            
            //stamp
            bool stampEn = ReadByte(data, ref offset) == 1 ? true : false;
            if (stampEn && !stampExplorerParent.activeSelf) {
                stampExplorerToggle(false);
            }
            else if (!stampEn && stampExplorerParent.activeSelf) {
                stampExplorerToggle(false);
            }
            Vector2 StampPos = new Vector2(ReadFloat(data, ref offset), ReadFloat(data, ref offset));
            stampExplorerGrabbable.setExactPos(StampPos.x, StampPos.y);
            stampExplorerSlider.setPos(ReadFloat(data, ref offset),false);
            
            //clear
            bool clearEn = ReadByte(data, ref offset) == 1 ? true : false;
            if (clearEn && !clearMenuParent.activeSelf) {
                clearMenuToggle(false);
            }
            else if (!clearEn && clearMenuParent.activeSelf) {
                clearMenuToggle(false);
            }
            Vector2 clearPos = new Vector2(ReadFloat(data, ref offset), ReadFloat(data, ref offset));
            clearMenuGrabbable.setExactPos(clearPos.x, clearPos.y);
            
            //movable menu
            bool menuEn = ReadByte(data, ref offset) == 1 ? true : false;
            if (menuEn && !movableMenuParent.activeSelf) {
                movableMenuParent.SetActive(true);
            }
            else if (!menuEn && movableMenuParent.activeSelf) {
                movableMenuParent.SetActive(false);
            }
            Vector2 menuPos = new Vector2(ReadFloat(data, ref offset), ReadFloat(data, ref offset));
            movableMenuGrabbable.setExactPos(menuPos.x, menuPos.y);
            
            //additional UI
            int additionalUICount = ReadInt(data, ref offset);
            for (int x = 0; x < additionalUICount; x++) {
                bool en = ReadByte(data, ref offset) == 1 ? true : false;
                if (en && !additionalUIWindows[x].parent.activeSelf) {
                    additionalUIWindows[x].setActive(true);
                }
                else if (!en && additionalUIWindows[x].parent.activeSelf) {
                    additionalUIWindows[x].setActive(false);
                }
                Vector2 pos = new Vector2(ReadFloat(data, ref offset), ReadFloat(data, ref offset));
                additionalUIWindows[x].grabbable.setExactPos(pos.x, pos.y);
            }
            

            //get rid of state sync queue since it is now outdated
            dequeueState();

        }
          
        #region Passthroughs

        public void addCanvasPassthrough() {
            display.addCanvasPassthrough();
        }

        public void undoPassthrough(VRPenInput input) {
            
            //do nothing if nothing to undo
            if (VectorDrawing.s_instance.undoStack.Count == 0) return;
            
            
            //undo
            VectorGraphic curr = VectorDrawing.s_instance.undoStack[VectorDrawing.s_instance.undoStack.Count - 1];
            VectorDrawing.s_instance.undo(curr.ownerId, curr.localIndex, curr.canvasId, true);
        }

        public AdditionalUIWindow addAdditionalUI() {
            GameObject obj = GameObject.Instantiate(additionalUIWindowPrefab, additionalUIWindowParent);
            AdditionalUIWindow additionalUIWindow = obj.GetComponent<AdditionalUIWindow>();
            additionalUIWindow.uiMan = this;
            additionalUIWindows.Add(additionalUIWindow);
            return additionalUIWindow;
        }

        public AdditionalUIWindow getAdditionalUI(int index) {
            return additionalUIWindows[index];
        }

        public void removeAdditionalUI(int index) {
            AdditionalUIWindow additionalUIWindow = additionalUIWindows[index];
            Destroy(additionalUIWindow.gameObject);
            additionalUIWindows.Remove(additionalUIWindow);
        }
        
        public void savePassthrough() {
            display.savePassthrough();
        }
        
        public void eyedropperPassthrough(VRPenInput input) {
            display.eyedropperPassthrough(input);

        }

        public void erasePassthrough(VRPenInput input) {
            display.erasePassthrough(input);
        }

        public void markerPassthrough(VRPenInput input) {
            display.markerPassthrough(input);
        }

        public void stampPassthrough(int stampId) {
            stampExplorerToggle(true);
            display.newStamp(StampType.image, stampUIParent, stampId, null, Color.black);
        }
        
        public void stampPassthrough(string text, Color textColor) {
            display.newStamp(StampType.text, stampUIParent, -1, text, textColor);
        }
        

        public void clearCanvasPassThrough() {
            display.clearCanvas();
        }
        
        #endregion

       

        void addFilesToStampExplorer() {

            
            int count = PersistantData.getStampFileNameCount();

			if (count == -1) {
				Debug.Log("Couldnt get stamp files due to persistant data not being instantiated");
			}
			else {
				Debug.Log(count + " file(s) added to stamp explorer.");

				for (int x = 0; x < count; x++) {

                    addFileToStampExplorer(x);

				}

			}

        }

        void addFileToStampExplorer(int index) {

            string str = PersistantData.getStampFileName(index);

            GameObject obj = Instantiate(stampResourcePrefab, stampResourceParent);
            obj.transform.GetChild(1).GetChild(1).GetComponent<Text>().text = str;

            obj.transform.GetChild(0).GetComponent<ButtonPassthrough>().UI = this;
            obj.transform.GetChild(0).GetComponent<ButtonPassthrough>().stampIndex = index;
            obj.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => highlightTimer(0.2f));
            obj.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => closeMenus(true));
        }

        
        int ReadInt(byte[] buf, ref int offset) {
            int val = BitConverter.ToInt32(buf, offset);
            offset += sizeof(Int32);
            return val;
        }
        byte ReadByte(byte[] buf, ref int offset) {
            byte val = buf[offset];
            offset += sizeof(byte);
            return val;
        }
        public static float ReadFloat(byte[] buf, ref int offset) {
            float val = BitConverter.ToSingle(buf, offset);
            offset += sizeof(float);
            return val;
        }

    }
}