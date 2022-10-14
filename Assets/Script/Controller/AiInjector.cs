using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiInjector : MonoBehaviour
{
    Controller controller;
    public Transform target;

    void Start()
    {
        controller=GetComponent<Controller>();
    }
    void getAi()
    {
        Vector3 dirToMove=(target.position-transform.position).normalized;
        float dotF=Vector3.Dot(transform.forward,dirToMove);
        float dotR=Vector3.Dot(transform.right,dirToMove);

        if(dotF>0)
            controller.inputData.y=1;
        else
            controller.inputData.y=-1;

        if(dotR>0)
            controller.inputData.x=1;
        else
            controller.inputData.x=-1;
    }
    void Update()
    {
        getAi();
    }
}
