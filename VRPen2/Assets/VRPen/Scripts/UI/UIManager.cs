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

			if (localInput) packState();

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



            if (localInput) packState();

		}

		public void canvasMenuToggle(bool localInput) {

			canvasMenuParent.SetActive(!canvasMenuParent.activeSelf);
			calculatorParent.SetActive(false);
			clearMenuParent.SetActive(false);
            stampExplorerParent.SetActive(false);


            if (localInput) packState();

		}public void clearMenuToggle(bool localInput) {

			clearMenuParent.SetActive(!clearMenuParent.activeSelf);
			calculatorParent.SetActive(false);
			canvasMenuParent.SetActive(false);
            stampExplorerParent.SetActive(false);




            if (localInput) packState();

		}

        public void stampExplorerToggle(bool localInput) {

            clearMenuParent.SetActive(false);
            calculatorParent.SetActive(false);
            canvasMenuParent.SetActive(false);
            stampExplorerParent.SetActive(!stampExplorerParent.activeSelf);



            if (localInput) packState();
        }

		public void closeMenus(bool localInput) {
			clearMenuParent.SetActive(false);
			calculatorParent.SetActive(false);
			canvasMenuParent.SetActive(false);
            stampExplorerParent.SetActive(false);




            if (localInput) packState();
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

		public void packState() {

            if (!networkUI) return;

			byte stateMask = 0;

			stateMask += (byte)((SideMenuParent.activeSelf ? 1 : 0) << 0);
			stateMask += (byte)((calculatorParent.activeSelf ? 1 : 0) << 1);
			stateMask += (byte)((canvasMenuParent.activeSelf ? 1 : 0) << 2);
			stateMask += (byte)((clearMenuParent.activeSelf ? 1 : 0) << 3);

			network.sendUIState(display.DisplayId, stateMask);

		}

		public void unpackState(byte stateMask) {

			//side menu
			if (!SideMenuParent.activeInHierarchy && ((stateMask & (1 << 0)) > 0)) {
				openSideMenu(false);
			}
			else if (SideMenuParent.activeInHierarchy && !((stateMask & (1 << 0)) > 0)) {
				closeSideMenu(false);
			}

			//menu toggles
			if (calculatorParent.activeSelf ^ ((stateMask & (1 << 1)) > 0)) calculatorToggle(false);
			if (canvasMenuParent.activeSelf ^ ((stateMask & (1 << 2)) > 0)) canvasMenuToggle(false);
			if (clearMenuParent.activeSelf ^ ((stateMask & (1 << 3)) > 0)) clearMenuToggle(false);


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


    }

}
