using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputInjector : MonoBehaviour
{
    // Start is called before the first frame update
    public Joystick joystick;
    ControllerClient controller;

    Vector2 savedSkew = Vector2.zero;
    float skew;
    
    public void Start()
    {
        controller=GetComponent<ControllerClient>();
        skew=0;
    }
    public void getInput()
    {
        float skewRaw=((Vector2)Input.acceleration - savedSkew).x;
        skew=skewRaw*1.2f;
        skew=Mathf.Clamp(skew,-1,1);
        if (skew < 0.05 && skew > -0.05)
        {
            skew = 0.0f;
        }
    }
    void Update()
    {
        getInput();
        controller.inputData.input.y=joystick.v;
        controller.inputData.input.x=skew;
    }
}
