using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISlider : MonoBehaviour
{

    public int defaultSizeAboveScalingMenu;
    public int defaultSizeBelowScalingMenu;
    public int sizePerScalingMenuItem;
    public int maxSize;
    

    public GameObject scalingParent;

    Slider slider;
    
    void Start(){
        slider = GetComponent<Slider>();

        //set pos to turn off culled objects
        setPos(0, false);
    }
    

    public void setPos(float pos, bool localInput) {

        //get scale distance
        int totalMenuSize = defaultSizeAboveScalingMenu + defaultSizeBelowScalingMenu + scalingParent.transform.childCount * sizePerScalingMenuItem;
        int potentialMovement = totalMenuSize- maxSize;

        if (potentialMovement > 0) {

            //get movement
            int menuPos = (int)(pos * potentialMovement);

            //get amount of empty space visable
            int aboveMenuVisable = defaultSizeAboveScalingMenu - menuPos;
            if (aboveMenuVisable < 0) aboveMenuVisable = 0;
            int belowMenuVisable = -totalMenuSize + defaultSizeBelowScalingMenu + menuPos + maxSize;
            if (belowMenuVisable < 0) belowMenuVisable = 0;

            Debug.Log(aboveMenuVisable + "  " + belowMenuVisable);

            //turn off culled menu items
            int topCulledCount = (int)((menuPos - defaultSizeAboveScalingMenu ) / sizePerScalingMenuItem);
            if (topCulledCount < 0) topCulledCount = 0;
            int nonCulledCount = (int)(maxSize - aboveMenuVisable - belowMenuVisable + sizePerScalingMenuItem) / sizePerScalingMenuItem;

            for (int x = 0; x < scalingParent.transform.childCount; x++) {
                if (x < topCulledCount) scalingParent.transform.GetChild(x).gameObject.SetActive(false);
                else if (x >= topCulledCount + nonCulledCount) scalingParent.transform.GetChild(x).gameObject.SetActive(false);
                else scalingParent.transform.GetChild(x).gameObject.SetActive(true);
            }

            //move scalingparent
            float menuPosWithDisabledObjects = menuPos - topCulledCount * sizePerScalingMenuItem;
            scalingParent.transform.localPosition = new Vector3(scalingParent.transform.localPosition.x, menuPosWithDisabledObjects , scalingParent.transform.localPosition.z);

        }
        
        //set slider
        slider.value = pos;

        if (localInput) {
            //TODO network
        }

    }
}
