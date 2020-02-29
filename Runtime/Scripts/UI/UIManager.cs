using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;
using System;

namespace VRPen {

	public class UIManager : MonoBehaviour {
        
        public enum PacketHeader : int {
            Slide,
            Calc,
            Canvas,
            Stamp,
            Clear,
            MovableMenu
        }
        bool[] packetHeaderToSync;
        UIGrabbable[] packetHeaderGrabbables;

		[Header("Will autofill")]
		public VectorDrawing vectorMan;
		public NetworkManager network;


        [Header("Will not autofill")]

        public List<UIGrabbable> grabbablesAddedOnStart;
        public Toggle newCanvasIsPrivateToggle;
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
		public Display display;
		
        public Transform stampUIParent;

		public GameObject canvasButtonPrefab;


		public bool sideMenuOpen = false;
		private bool sideMenuMoving = false;

		const float RESIZE_SPEED = .1f;

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


		private void Awake() {
            
            
            //grab scripts if not in prefab
            if (vectorMan == null) vectorMan = FindObjectOfType<VectorDrawing>();
			if (network == null) network = FindObjectOfType<NetworkManager>();

            //create arrays for syncing
            packetHeaderToSync = new bool[Enum.GetValues(typeof(PacketHeader)).Length];
            packetHeaderGrabbables = new UIGrabbable[Enum.GetValues(typeof(PacketHeader)).Length];
            foreach (UIGrabbable grabbable in grabbablesAddedOnStart) {

                grabbable.initializePos();

                int index = (int)grabbable.type;
                if (packetHeaderGrabbables[index] != null) {
                    Debug.LogError("Multiple grabbables were added to uiMan with the same type (not allowed)");
                }
                else {
                    packetHeaderGrabbables[index] = grabbable;
                }
            }


            //grab stamp file names to put in explorer
            addFilesToStampExplorer();

            //set default stamp size
            //stampSizeSlider.value = DEFAULT_STAMP_SIZE;
            //stampSliderPassthrough()

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

			if (localInput) queueState(PacketHeader.Slide);

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

            if (localInput) queueState(PacketHeader.Slide);


        }

		public void calculatorToggle(bool localInput) {

			calculatorParent.SetActive(!calculatorParent.activeSelf);
			canvasMenuParent.SetActive(false);
			clearMenuParent.SetActive(false);
            stampExplorerParent.SetActive(false);



            if (localInput) queueState(PacketHeader.Calc);

		}

		public void canvasMenuToggle(bool localInput) {

			canvasMenuParent.SetActive(!canvasMenuParent.activeSelf);
			calculatorParent.SetActive(false);
			clearMenuParent.SetActive(false);
            stampExplorerParent.SetActive(false);


            if (localInput) queueState(PacketHeader.Canvas);

        }
        public void clearMenuToggle(bool localInput) {

			clearMenuParent.SetActive(!clearMenuParent.activeSelf);
			calculatorParent.SetActive(false);
			canvasMenuParent.SetActive(false);
            stampExplorerParent.SetActive(false);
            

            if (localInput) queueState(PacketHeader.Clear);

        }

        public void stampExplorerToggle(bool localInput) {

            clearMenuParent.SetActive(false);
            calculatorParent.SetActive(false);
            canvasMenuParent.SetActive(false);
            stampExplorerParent.SetActive(!stampExplorerParent.activeSelf);



            if (localInput) queueState(PacketHeader.Stamp);
        }

		public void closeMenus(bool localInput) {

            if (clearMenuParent.activeSelf) {

			    clearMenuParent.SetActive(false);
                if (localInput) queueState(PacketHeader.Clear);
            }
            if (calculatorParent.activeSelf) {

                calculatorParent.SetActive(false);
                if (localInput) queueState(PacketHeader.Calc);
            }
            if (canvasMenuParent.activeSelf) {

                canvasMenuParent.SetActive(false);
                if (localInput) queueState(PacketHeader.Canvas);
            }
            if (stampExplorerParent.activeSelf) {

                stampExplorerParent.SetActive(false);
                if (localInput) queueState(PacketHeader.Stamp);
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

        public void queueState(PacketHeader head) {
            packetHeaderToSync[(int)head] = true;
        }
        

        void dequeueState() {
            for (int x = 0; x < packetHeaderToSync.Length; x++) {
                packetHeaderToSync[x] = false;
            }
        }

        public void startPackingState(float period) {
            InvokeRepeating(nameof(packState), 1.0f, period);
        }

        public void packState() {

            //dont do anything if we dont wanna sync
            if (!network.syncDisplayUIs) return;

            //pack data
            List<byte> data = new List<byte>();

            for (int x = 0; x < packetHeaderToSync.Length; x++) {
                if (packetHeaderToSync[x]) {

                    //header
                    data.AddRange(BitConverter.GetBytes(x));

                    switch ((PacketHeader)x) {
                        case PacketHeader.Slide:
                            data.Add(sideMenuOpen ? (byte)1 : (byte)0);
                            break;
                        case PacketHeader.Calc:
                            data.Add(calculatorParent.activeSelf ? (byte)1 : (byte)0);
                            data.AddRange(BitConverter.GetBytes(packetHeaderGrabbables[x].x));
                            data.AddRange(BitConverter.GetBytes(packetHeaderGrabbables[x].y));
                            break;
                        case PacketHeader.Canvas:
                            data.Add(canvasMenuParent.activeSelf ? (byte)1 : (byte)0);
                            data.AddRange(BitConverter.GetBytes(packetHeaderGrabbables[x].x));
                            data.AddRange(BitConverter.GetBytes(packetHeaderGrabbables[x].y));
                            break;
                        case PacketHeader.Stamp:
                            data.Add(stampExplorerParent.activeSelf ? (byte)1 : (byte)0);
                            data.AddRange(BitConverter.GetBytes(packetHeaderGrabbables[x].x));
                            data.AddRange(BitConverter.GetBytes(packetHeaderGrabbables[x].y));
                            break;
                        case PacketHeader.Clear:
                            data.Add(clearMenuParent.activeSelf ? (byte)1 : (byte)0);
                            data.AddRange(BitConverter.GetBytes(packetHeaderGrabbables[x].x));
                            data.AddRange(BitConverter.GetBytes(packetHeaderGrabbables[x].y));
                            break;
                        case PacketHeader.MovableMenu:
                            data.Add(clearMenuParent.activeSelf ? (byte)1 : (byte)0);
                            data.AddRange(BitConverter.GetBytes(packetHeaderGrabbables[x].x));
                            data.AddRange(BitConverter.GetBytes(packetHeaderGrabbables[x].y));
                            break;

                    }
                }

            }

            //dont send if no data
            if (data.Count == 0) return;

            //send data
            network.sendUIState(display.DisplayId, data.ToArray());

            //get rid of state sync queue since it was just sent
            dequeueState();

            
        }

        public void unpackState(byte[] data) {

            //unpack
            int offset = 0;
            while (offset < data.Length) {
                PacketHeader head = (PacketHeader)ReadInt(data, ref offset);
                switch (head) {

                    case PacketHeader.Slide:
                        bool open = ReadByte(data, ref offset) == 1? true: false;
                        if (open && !sideMenuOpen) {
                            openSideMenu(false);
                        }
                        else if(!open && sideMenuOpen) {
                            closeSideMenu(false);
                        }
                        
                        break;
                    case PacketHeader.Calc:
                        bool calcEn = ReadByte(data, ref offset) == 1 ? true : false;
                        if (calcEn && !calculatorParent.activeSelf) {
                            calculatorToggle(false);
                        }
                        else if (!calcEn && calculatorParent.activeSelf) {
                            calculatorToggle(false);
                        }
                        Vector2 calcPos = new Vector2(ReadFloat(data, ref offset), ReadFloat(data, ref offset));
                        packetHeaderGrabbables[(int)PacketHeader.Calc].setExactPos(calcPos.x, calcPos.y);
                        break;
                    case PacketHeader.Canvas:
                        bool canvasEn = ReadByte(data, ref offset) == 1 ? true : false;
                        if (canvasEn && !canvasMenuParent.activeSelf) {
                            canvasMenuToggle(false);
                        }
                        else if (!canvasEn && canvasMenuParent.activeSelf) {
                            canvasMenuToggle(false);
                        }
                        Vector2 canvasPos = new Vector2(ReadFloat(data, ref offset), ReadFloat(data, ref offset));
                        packetHeaderGrabbables[(int)PacketHeader.Canvas].setExactPos(canvasPos.x, canvasPos.y);
                        break;
                    case PacketHeader.Stamp:
                        bool stampEn = ReadByte(data, ref offset) == 1 ? true : false;
                        if (stampEn && !stampExplorerParent.activeSelf) {
                            stampExplorerToggle(false);
                        }
                        else if (!stampEn && stampExplorerParent.activeSelf) {
                            stampExplorerToggle(false);
                        }
                        Vector2 StampPos = new Vector2(ReadFloat(data, ref offset), ReadFloat(data, ref offset));
                        packetHeaderGrabbables[(int)PacketHeader.Stamp].setExactPos(StampPos.x, StampPos.y);
                        break;
                    case PacketHeader.Clear:
                        bool clearEn = ReadByte(data, ref offset) == 1 ? true : false;
                        if (clearEn && !clearMenuParent.activeSelf) {
                            clearMenuToggle(false);
                        }
                        else if (!clearEn && clearMenuParent.activeSelf) {
                            clearMenuToggle(false);
                        }
                        Vector2 clearPos = new Vector2(ReadFloat(data, ref offset), ReadFloat(data, ref offset));
                        packetHeaderGrabbables[(int)PacketHeader.Clear].setExactPos(clearPos.x, clearPos.y);
                        break;
                    case PacketHeader.MovableMenu:
                        bool menuEn = ReadByte(data, ref offset) == 1 ? true : false;
                        if (menuEn && !movableMenuParent.activeSelf) {
                            movableMenuParent.SetActive(true);
                        }
                        else if (!menuEn && movableMenuParent.activeSelf) {
                            movableMenuParent.SetActive(false);
                        }
                        Vector2 menuPos = new Vector2(ReadFloat(data, ref offset), ReadFloat(data, ref offset));
                        packetHeaderGrabbables[(int)PacketHeader.MovableMenu].setExactPos(menuPos.x, menuPos.y);
                        break;

                }
                
            }
            //get rid of state sync queue since it is now outdated
            dequeueState();

        }
            #region Passthroughs

        public void addCanvasPassthrough() {
            display.addCanvasPassthrough(!newCanvasIsPrivateToggle.isOn);
        }

        public void undoPassthrough() {
            display.undoPassthrough();
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

        public void stampPassthrough(VRPenInput input, int stampId) {
            display.stampPassthrough(input, stampUIParent, stampId);
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

					string str = PersistantData.getStampFileName(x);

					GameObject obj = Instantiate(stampResourcePrefab, stampResourceParent);
					obj.transform.GetChild(1).GetChild(1).GetComponent<Text>().text = str;

					obj.transform.GetChild(0).GetComponent<ButtonPassthrough>().UI = this;
					obj.transform.GetChild(0).GetComponent<ButtonPassthrough>().stampIndex = x;
					obj.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => highlightTimer(0.2f));
					obj.transform.GetChild(0).GetComponent<Button>().onClick.AddListener(() => closeMenus(true));

				}

			}

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