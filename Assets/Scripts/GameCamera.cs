using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameCamera : MonoBehaviour {

    public Vector3 kDir;
    public Vector3 kUP;
    public Vector3 kTargetPos;
    public Vector3 kLookAtPos;
    public Vector3 kModifyer;

    private Vector3 kModifyerBase;
   
    public float ScrollWheelInput;
    public float ScrollWhellModify;
    private float ScrollWhellModifyD;

    public float Ho;
    public float Ve;

    private void Update()
    {
        
    }

    private void LateUpdate()
    {

        UpdateTime();

        UpdateInput();

        UpdatePos();
    }

    private void UpdateTime()
    {

    }

    private void UpdateInput()
    {
        ScrollWheelInput = -Input.GetAxis("Mouse ScrollWheel");

        Ho = Input.GetAxis("Horizontal");
        Ve = Input.GetAxis("Vertical");

        ScrollWhellModifyD += ScrollWheelInput;
        ScrollWhellModifyD /= 2;

        kModifyerBase = kModifyer + Mathf.Abs(ScrollWhellModifyD) * kModifyer /2;

        kTargetPos.y += kModifyerBase.y * ScrollWheelInput;
        kTargetPos.x += kModifyerBase.x * Ho;
        kTargetPos.z += kModifyerBase.z * Ve;

    }

    private void UpdatePos()
    {
        transform.position = kTargetPos;
        kLookAtPos = kTargetPos + kDir;

        transform.LookAt(kLookAtPos, kUP);


    }
}
