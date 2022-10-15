using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiInjector : MonoBehaviour
{
    ControllerClient controller;
    public Transform target;

    void Start()
    {
        controller=GetComponent<ControllerClient>();
    }
    void getAi()
    {
        Vector3 dirToMove=(target.position-transform.position).normalized;
        float dotF=Vector3.Dot(transform.forward,dirToMove);
        float dotR=Vector3.Dot(transform.right,dirToMove);

        if(dotF>0)
            controller.inputData.input.y=1;
        else
            controller.inputData.input.y=-1;

        if(dotR>0)
            controller.inputData.input.x=1;
        else
            controller.inputData.input.x=-1;
    }
    void Update()
    {
        getAi();
    }
}
