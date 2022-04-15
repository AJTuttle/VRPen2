using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DebugUI : MonoBehaviour
{


    public GameObject obj;

    public Text errorText;

    public bool enabledInEditor;
    public bool enabledInBuild;

    public void display(string str) {
        #if UNITY_EDITOR
            if (!enabledInEditor) return; 
        #else
            if (!enabledInBuild) return;
        #endif
        obj.SetActive(true);
        errorText.text = str;
    }


}
