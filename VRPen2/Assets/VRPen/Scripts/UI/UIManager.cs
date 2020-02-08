using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;
using System;

namespace VRPen {

	public class UIManager : MonoBehaviour {

        public bool networkUI;
        
        enum PacketHeader : int {
            Slide,
            Calc,
            Canvas,
            Stamp,
        }
        bool[] packetHeaderToSync;

		[Header("Will autofill")]
		public VectorDrawing vectorMan;
		public NetworkManager network;


		[Header("Will not autofill")]
		public GameObject SideMenuParent;
		public GameObject calculatorParent;
		public GameObject canvasMenuParent;
		public GameObject canvasListParent;
		public GameObject stampExplorerParent;
		public GameObject clearMenuParent;
		public GameObject menuArrow;
		public Display display;
		
        public Transform stampUIParent;

		public GameObject canvasButtonPrefab;


		public bool sideMenuOpen = false;
		private bool sideMenuMoving = false;

		const float RESIZE_TIME = .2f;

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


		private void Start() {
            
            
            //grab scripts if not in prefab
            if (vectorMan == null) vectorMan = FindObjectOfType<VectorDrawing>();
			if (network == null) network = FindObjectOfType<NetworkManager>();

            //create array
            packetHeaderToSync = new bool[Enum.GetValues(typeof(PacketHeader)).Length];

            //grab stamp file names to put in explorer
            addFilesToStampExplorer();

            //set default stamp size
            //stampSizeSlider.value = DEFAULT_STAMP_SIZE;
            //stampSliderPassthrough()

		}

        private void Update() {
            //packState();
        }


        public void openSideMenu(bool localInput) {
			if (sideMenuMoving) return;
			sideMenuMoving = true;

			// uncull things that will be visible
			SideMenuParent.SetActive(true);

			StartCoroutine(resizeMenu(SideMenuParent, SideMenuParent.transform.localScale,
				SideMenuParent.transform.localScale, SideMenuParent.transform.localPosition,
				SideMenuParent.transform.localPosition + new Vector3(200, 0, 0), false, localInput));
			StartCoroutine(resizeMenu(menuArrow, menuArrow.transform.localScale, menuArrow.transform.localScale,
				menuArrow.transform.localPosition, menuArrow.transform.localPosition + new Vector3(200f, 0, 0), false, localInput));
			StartCoroutine(resizeMenu(calculatorParent, calculatorParent.transform.localScale, calculatorParent.transform.localScale,
				calculatorParent.transform.localPosition, calculatorParent.transform.localPosition + new Vector3(200f, 0, 0), false, localInput));
			StartCoroutine(resizeMenu(canvasMenuParent, canvasMenuParent.transform.localScale, canvasMenuParent.transform.localScale,
				canvasMenuParent.transform.localPosition, canvasMenuParent.transform.localPosition + new Vector3(200f, 0, 0), false, localInput));
			StartCoroutine(resizeMenu(clearMenuParent, clearMenuParent.transform.localScale, clearMenuParent.transform.localScale,
				clearMenuParent.transform.localPosition, clearMenuParent.transform.localPosition + new Vector3(200f, 0, 0), false, localInput));
            StartCoroutine(resizeMenu(stampExplorerParent, stampExplorerParent.transform.localScale, stampExplorerParent.transform.localScale,
                stampExplorerParent.transform.localPosition, stampExplorerParent.transform.localPosition + new Vector3(200f, 0, 0), false, localInput));
            sideMenuOpen = true;
			menuArrow.transform.GetChild(0).gameObject.SetActive(false);
			menuArrow.transform.GetChild(1).gameObject.SetActive(true);

			if (localInput) queueState(PacketHeader.Slide);

        }

		public void closeSideMenu(bool localInput) {
			if (sideMenuMoving) return;
			sideMenuMoving = true;

			StartCoroutine(resizeMenu(SideMenuParent, SideMenuParent.transform.localScale,
				SideMenuParent.transform.localScale, SideMenuParent.transform.localPosition,
				SideMenuParent.transform.localPosition - new Vector3(200, 0, 0), true, localInput));
			StartCoroutine(resizeMenu(menuArrow, menuArrow.transform.localScale, menuArrow.transform.localScale,
				menuArrow.transform.localPosition, menuArrow.transform.localPosition - new Vector3(200f, 0, 0), false, localInput));
			StartCoroutine(resizeMenu(calculatorParent, calculatorParent.transform.localScale, calculatorParent.transform.localScale,
				calculatorParent.transform.localPosition, calculatorParent.transform.localPosition - new Vector3(200f, 0, 0), false, localInput));
			StartCoroutine(resizeMenu(canvasMenuParent, canvasMenuParent.transform.localScale, canvasMenuParent.transform.localScale,
				canvasMenuParent.transform.localPosition, canvasMenuParent.transform.localPosition - new Vector3(200f, 0, 0), false, localInput));
			StartCoroutine(resizeMenu(clearMenuParent, clearMenuParent.transform.localScale, clearMenuParent.transform.localScale,
				clearMenuParent.transform.localPosition, clearMenuParent.transform.localPosition - new Vector3(200f, 0, 0), false, localInput));
            StartCoroutine(resizeMenu(stampExplorerParent, stampExplorerParent.transform.localScale, stampExplorerParent.transform.localScale,
                stampExplorerParent.transform.localPosition, stampExplorerParent.transform.localPosition - new Vector3(200f, 0, 0), false, localInput));
            sideMenuOpen = false;
			menuArrow.transform.GetChild(0).gameObject.SetActive(true);
			menuArrow.transform.GetChild(1).gameObject.SetActive(false);


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




            if (localInput) ;//queueState(PacketHeader.);

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
                //if (localInput) queueState(PacketHeader.Slide);
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

        IEnumerator resizeMenu(GameObject obj, Vector3 startScale, Vector3 endScale, Vector3 startPos, Vector3 endPos, bool setInactive, bool localInput) {
            Vector3 scaleChange = endScale - startScale;
            Vector3 posChange = endPos - startPos;
            float startTime = Time.time;
            while (startTime + RESIZE_TIME > Time.time) {
                obj.transform.localScale += scaleChange * Time.deltaTime / RESIZE_TIME;
                obj.transform.localPosition += posChange * Time.deltaTime / RESIZE_TIME;
                yield return null;
            }
            obj.transform.localPosition = endPos;
            obj.transform.localScale = endScale;

			sideMenuMoving = false;

			if (setInactive) {
				obj.SetActive(false);
				if (localInput) packState();
			}

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
            buttonObj.GetComponent<Button>().onClick.AddListener(() => display.swapCurrentCanvas(canvasId));

            //change text
            Text text = buttonObj.transform.GetChild(0).GetComponent<Text>();
            text.text = "Canvas " + canvasId;
            buttonObj.name = text.text;
            text.fontSize = 25;


          

        }

        void queueState(PacketHeader head) {
            packetHeaderToSync[(int)head] = true;
        }

        void dequeueState() {
            for (int x = 0; x < packetHeaderToSync.Length; x++) {
                packetHeaderToSync[x] = false;
            }
        }

        void packState() {

            //dont do anything if we dont wanna sync
            if (!networkUI) return;

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
                            break;
                        case PacketHeader.Canvas:
                            data.Add(canvasMenuParent.activeSelf ? (byte)1 : (byte)0);
                            break;
                        case PacketHeader.Stamp:
                            data.Add(stampExplorerParent.activeSelf ? (byte)1 : (byte)0);
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
                        break;
                    case PacketHeader.Canvas:
                        bool canvasEn = ReadByte(data, ref offset) == 1 ? true : false;
                        if (canvasEn && !canvasMenuParent.activeSelf) {
                            canvasMenuToggle(false);
                        }
                        else if (!canvasEn && canvasMenuParent.activeSelf) {
                            canvasMenuToggle(false);
                        }
                        break;
                    case PacketHeader.Stamp:
                        bool stampEn = ReadByte(data, ref offset) == 1 ? true : false;
                        if (stampEn && !stampExplorerParent.activeSelf) {
                            stampExplorerToggle(false);
                        }
                        else if (!stampEn && stampExplorerParent.activeSelf) {
                            stampExplorerToggle(false);
                        }
                        break;

                }
                
            }
            //get rid of state sync queue since it is now outdated
            dequeueState();

        }
            #region Passthroughs

            public void addCanvasPassthrough() {
            display.addCanvasPassthrough();
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


    }
}