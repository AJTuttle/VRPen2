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
        float potentialMovement = (defaultSizeAboveScalingMenu + defaultSizeBelowScalingMenu + scalingParent.transform.childCount * sizePerScalingMenuItem) - maxSize;
        if (potentialMovement > 0) {

            //get movement
            float menuPos = pos * potentialMovement;

            //turn off culled menu items
            int topCulledCount = (int)((menuPos - defaultSizeAboveScalingMenu + 0.2f * sizePerScalingMenuItem) / sizePerScalingMenuItem);
            int nonCulledCount = (int)(maxSize - defaultSizeAboveScalingMenu - defaultSizeBelowScalingMenu + 0.8f * sizePerScalingMenuItem) / sizePerScalingMenuItem;

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
