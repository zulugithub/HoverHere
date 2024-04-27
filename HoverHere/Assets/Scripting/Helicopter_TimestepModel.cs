// ##################################################################################
// Free RC helicopter Simulator
// 20.01.2020 
// Copyright (c) zulu
// Source: https://joinerda.github.io/Threading-In-Unity/
//
// Unity c# code
// ##################################################################################
//        Frame 1                                  Frame 2                                  Frame 3
// -------|--x--------------x----------------------|--x-------------------x-----------------|--x--------x------------->  time [s]                
//         FU()           Up()                      FU()                Up()                 FU()     Up()               FixedUpdate(), Update()
//                          |                                              |                            |              
//                          v                                              v                            v               
//                          [TS][TS][TS][TS][TS][TS]                       [TS][TS][TS][TS][TS][TS]     [TS][TS][TS][TS][TS][TS]   TakeStep 
//
// One frame is divided in a number of TakeStep ODE-calculations. If the Frame-time would be at 60Hz 0.01666 sec and 10 TakeSteps
// would be calculated, then each takeStep would have the physical time step of 0.001666 sec.
// ##################################################################################
using UnityEngine;
using System.Collections;
using System.Threading;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;


public abstract class Helicopter_TimestepModel : MonoBehaviour
{
    [HideInInspector]
    //public float thread_ODE_deltat; // = 0.001f;
    public Thread thread_ODE;
    public static AutoResetEvent thread_pause_signal = new AutoResetEvent(false);
    public float max_physics_calc_rate_per_frame { get; private set; }
    public int physics_takestep_calculations_per_frame { get; private set; }
    public float screen_refreshrate_fps = 60.0f;
    public float realtime_per_step_sec = 0.001f;
    private const float max_calculation_frequency_hz = 1000; 
    Stopwatch sw = new Stopwatch();

    static readonly object _locker = new object();

    ~Helicopter_TimestepModel()
    {
        Thread.Sleep(100);
        thread_pause_signal.Set();
        thread_ODE.Abort(1000);

        thread_ODE.Join(100);
    }


    // Use this for initialization
    public void Simulation_Thread_Start()
    {
        thread_ODE = new Thread(this.ThreadedActions);
        thread_ODE.IsBackground = true;
        thread_ODE.Start();
        thread_ODE.Priority = System.Threading.ThreadPriority.Highest;
        sw.Start();
    }

    public void ThreadedActions()
    {
        while (true)
        {
            try
            {
                Thread.Sleep(0); //Make sure Unity does not freeze

                thread_pause_signal.WaitOne(); // Blockiert den aktuellen thread, bis das aktuelle WaitHandle ein signal empfängt.

                float screen_refresh_rate_sec = 1.0f / screen_refreshrate_fps;
                max_physics_calc_rate_per_frame = screen_refresh_rate_sec / realtime_per_step_sec;
                //float max_physics_calc_rate_per_sec       = screen_refreshrate_fps * (screen_refresh_rate_sec / realtime_per_step_sec);
                physics_takestep_calculations_per_frame = (int)(max_physics_calc_rate_per_frame * 0.700f); // use 60% of time per frame for ODE takesteps calculations
                if ((physics_takestep_calculations_per_frame * screen_refreshrate_fps) > max_calculation_frequency_hz)
                    physics_takestep_calculations_per_frame = (int)(max_calculation_frequency_hz / screen_refreshrate_fps); //[] limit maximal calculation frequency
                float physics_timestep = screen_refresh_rate_sec / physics_takestep_calculations_per_frame;

                for (int i = 0; i < physics_takestep_calculations_per_frame; i++)
                {
                    bool last_takestep_in_frame = (i == (physics_takestep_calculations_per_frame - 1)); // mark the last TakeStep claculation per frame, so i.e. debbugging can only made in this last TakeStep calculation

                    TakeStep(physics_timestep, last_takestep_in_frame); // [s] 
                }
            }
            catch (ThreadAbortException ex)
            {
                UnityEngine.Debug.Log("Thread is aborted and the code is " + ex.ExceptionState);
            }
        }
    }

    public abstract void TakeStep(float dt, bool last_takestep_in_frame);
}