using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BillBoard : MonoBehaviour
{
    private Camera cam;

    private void Start()
    {
        cam = Camera.main;
        transform.rotation = cam.transform.rotation;
    }
}