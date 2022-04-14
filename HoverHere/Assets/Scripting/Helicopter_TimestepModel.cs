// ##################################################################################
// Free RC helicopter Simulator
// 20.01.2020 
// Copyright (c) zulu
// Source: https://joinerda.github.io/Threading-In-Unity/
//
// Unity c# code
// ##################################################################################
using UnityEngine;
using System.Collections;
using System.Threading;
using System;
using System.Diagnostics;


public abstract class Helicopter_TimestepModel : MonoBehaviour
{
    [HideInInspector]
    public float thread_ODE_deltat; // = 0.001f;
    public Thread thread_ODE;

    public readonly bool threadRunning = true;
    //bool stepFree = true;
    bool stepRunning = false;
    //public float model_t = 0.0f;

    //public bool threaded = true;
    //bool fastrun = true;
    bool paused = false;

    bool paused_flag_old = false;

    Stopwatch sw = new Stopwatch();

    static readonly object _locker = new object();



    ~Helicopter_TimestepModel()
    {
        Thread.Sleep(0);
        if(thread_ODE != null)
            thread_ODE.Abort();
    }

   // public bool GetThreaded()
    //{
   //     return threaded;
   // }

    // Use this for initialization
    public void Simulation_Thread_Start()
    {
        //model_t = 0.0f;
        // create thread before finishing Start
       // if (threaded)
       // {
            thread_ODE = new Thread(this.ThreadedActions);

            thread_ODE.Start();
            //thread_ODE.Priority = System.Threading.ThreadPriority.Lowest;
            sw.Start();
            //UnityEngine.Debug.Log("GetHashCode " + thread_ODE.GetHashCode() + "   ManagedThreadId " + thread_ODE.ManagedThreadId);
       // }

    }

    public void ThreadedActions()
    {
        //long oldTime = DateTime.Now.Ticks;
        long oldTime = sw.Elapsed.Ticks;
        long currentTime;
        float waitTime;
        float deltaTime;


        while (threadRunning)
        {
            Thread.Sleep(0); //Make sure Unity does not freeze
            waitTime = thread_ODE_deltat*1000; // [ms]

            //currentTime = DateTime.Now.Ticks;
            currentTime = sw.Elapsed.Ticks;
            deltaTime = (float)TimeSpan.FromTicks(currentTime - oldTime).TotalMilliseconds; // [ms]

            // timescale setting (slow motion)       
            //deltaTime *= thread_ODE_timescale;


            if (deltaTime < 0.01f) // [ms]
                deltaTime = 0.01f;

            if (deltaTime >= waitTime)
            {
                oldTime = currentTime; //Store current Time
                //UnityEngine.Debug.Log("deltaTime: " + deltaTime + "  waitTime: " + waitTime + "  model_dt: " + model_dt);

                stepRunning = true;
                // TakeStep(model_dt);
                // model_t += model_dt; //am I doing this twice?
                if (!paused)
                {
                    TakeStep(deltaTime * 0.001f); // [s]
                }
                //model_t += deltaTime; 
                stepRunning = false;
            }
           

            /*
            try
            {
                if (stepFree || fastrun)
                {
                    stepRunning = true;
                    TakeStep(model_dt);
                    model_t += model_dt;//am I doing this twice?
                    stepRunning = false;
                }
                if (!fastrun)
                    lock (_locker)
                    {
                        stepFree = false;
                    }
            }
            //(ThreadAbortException ex) 
            catch
            {
                threadRunning = false;
            }
            */
        }
    }

    // Frame-rate independent message for physics calculations
   /* void FixedUpdate()
    {
        if (threaded)
        {
            lock (_locker)
            {
                stepFree = true;
            }
        }
        else
        {
            stepRunning = true;
            TakeStep(model_dt);
            model_t += model_dt;
            stepRunning = false;
        }
    }*/

    public void Pause_ODE(bool paused_flag)
    {
        if(paused_flag != paused_flag_old) // change pause state only if setting has changed
        { 
            lock (_locker)
            {
                //stepFree = false;
                // would a lock be more efficient here?
                while (stepRunning) { } // wait for step to finish to avoid race condition
                                        // grow array if needed
                paused = paused_flag;
            }
            paused_flag_old = paused_flag;
        }
    }

    public abstract void TakeStep(float dt);
}