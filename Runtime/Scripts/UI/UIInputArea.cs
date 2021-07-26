using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using Debug = VRPen.Debug;

public class UIInputArea : MonoBehaviour {

    
    //public vars
    [Tooltip("if no inputs were received, how many frames should it wait until up event (should be >0)")] 
    public int idleFramesUntilUPEvent;
    private int framesSinceLastUpdate = 0;
    
    
    //events
    public UnityEvent hoverEvent;
    public UnityEvent holdEvent;
    public UnityEvent downEvent;
    public UnityEvent upEvent;

    
    //data
    private float x = 0;
    private float y = 0;
    private float pressure = 0;
    
    private bool down = false;
    private bool up = false;
    private bool hold = false;
    private bool hover = false;
    
    
    public void input(float x, float y, float pressure) {
        
        
        //set new data
        this.x = x;
        this.y = y;
        this.pressure = pressure;
        framesSinceLastUpdate = 0;
        
        //hovering
        if (pressure == 0) {
            //release from last input
            if (hold) {
                down = false;
                up = true;
                hold = false;
                hover = true;
                upEvent.Invoke();
                hoverEvent.Invoke();
            }
            //hover
            else {
                down = false;
                up = false;
                hold = false;
                hover = true;
                hoverEvent.Invoke();
            }
        }

        //pressed
        else {
            //down event
            if (!hold) {
                down = true;
                up = false;
                hold = true;
                hover = false;
                downEvent.Invoke();
                holdEvent.Invoke();
            }
            //holding
            else {
                down = false;
                up = false;
                hold = true;
                hover = false;
                holdEvent.Invoke();
            }
        }
    }

    public float getPressure() {
        return pressure;
    }
    public Vector2 getPos() {
        return new Vector2(x,y);
    }

    public bool getDown() {
        return down;
    }
    
    public bool getUp() {
        return up;
    }
    
    public bool getHold() {
        return hold;
    }
    
    public bool getHover() {
        return hover;
    }

    private void Update() {
        
        //inc frame counter
        framesSinceLastUpdate++;

        //up event
        if (framesSinceLastUpdate > idleFramesUntilUPEvent) {
            
            //release from last input
            if (hold) {

                //set new data
                pressure = 0;
                framesSinceLastUpdate--; //decrement since next frame we want to turn off the upevent

                //set states
                down = false;
                up = true;
                hold = false;
                hover = false;
                upEvent.Invoke();
                
                
            }
            else {
                //set new data
                pressure = 0;
                framesSinceLastUpdate = 0;

                //set states
                down = false;
                up = false;
                hold = false;
                hover = false;
            }

        }

    }
}
