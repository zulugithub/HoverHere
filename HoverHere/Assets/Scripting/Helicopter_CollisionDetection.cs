// ############################################################################
// Free RC helicopter Simulator
// 20.01.2020 
// Copyright (c) zulu
//
// Unity c# code
// ############################################################################
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Helicopter_CollisionDetection : MonoBehaviour
{
    private Helicopter_Main Helicopter_Main;

    void Awake()
    {
        Helicopter_Main = GameObject.Find("Game_Controller").GetComponent<Helicopter_Main>();
    }

    // OnCollisionEnter is called when this collider/rigidbody has begun touching another rigidbody/collider.
    // https://docs.unity3d.com/ScriptReference/Collider.OnCollisionEnter.html
    void OnTriggerEnter(Collider other) //Collision collision
    {
        //Debug.Log("Collision!!!");
        if(Helicopter_Main.gl_pause_flag == false)
        {
            Helicopter_Main.Pause_ODE(true);
            Helicopter_Main.Crash_Play_Audio(Application.streamingAssetsPath + "/Audio/crash_audio_001.wav");
            Helicopter_Main.Reset_Simulation_States();
            Helicopter_Main.Pause_ODE(false);
        }
    }

}
