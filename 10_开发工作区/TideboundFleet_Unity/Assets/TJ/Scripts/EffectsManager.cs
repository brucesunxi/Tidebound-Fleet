using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EffectsManager : MonoBehaviour
{
     public static EffectsManager instance;

     public GameObject hitEffect;
     private void Awake()
     {
          instance = this;
     }

     public void PlayEffect(GameObject effect, Vector3 pos, Quaternion rot)
     {
          Instantiate(effect, pos, rot);
     }
}
