// ##################################################################################
// Free RC Helicopter Simulator
// 20.01.2020 
// Copyright (c) zulu
//
// Unity c# code
// ##################################################################################

// ##################################################################################
// anti stuttering measures
// ##################################################################################
// 
// Basics: The ODE-thread is running with high frequency (ie.e 200 Hz). The framerate is abot 30 or 60, or 75 Hz, thus the call 
// frequency of Fixedupdate() or Update() are lower, then the ODE-thread freuqency.
//
// Inter- or extrapolation is used to smooth out the motion (translation and rotation) of the helicopter model.
// Therefore two successive results from the ODE-thread are used. The time-ticks iniside a frame, at which the interpolation 
// should take place, is set to be at the beginning of the FixedUpdate() function call. The reason is, that FixedUpdate() is
// called almost of the beginning of every new frame. Update() may be delayed in each frame. The code handles also the case, 
// if FixedUpdate() is not called during a frame, or if it is called multiple times. That depends on the Time.fixedDeltaTime 
// setting, in the Helicopter_Main.cs script (it overwrites the unity editor setting!).
// 
// To know which results should be used, the last 20 positon and rotation results are stored in a array 
// (size should be > ceil(f_ODE_Hz / f_FPS_Hz)), together with a precise timetick information. As mentioned also the timetick 
// of the beginning of the FixedUpdate() is measured. In the Update() fuction then the closest ODE-thread results to the 
// FixedUpdate()-timetick are searched and with the two successive ODE-thread results the inter- or extrapolation is computed.
//
//        Frame 1                                  Frame 2                                  Frame 3
// -------|--x--------------x----------------------|--x-------------------x-----------------|--x-----x------------->  time [s]                
//         FU()           Up()                      FU()                Up()                 FU()  Up()               FixedUpdate(), Update()
//           |                                        |                                        |                     
//           v                                        v                                        v                      
// Th         Th         Th         Th         Th         Th         Th         Th         Th         Th             ODE-Thread, store every position/orienatation in buffer              
// ^          ^                                ^          ^                     ^          ^                         use position/orienatation from here from buffer for ... 
// |          |                                |          |                     |          |                         
// o---------oo                                o------o---o                     o----------o---o                                           
//          interpolate                               interpolate                              extrapolate           ... interpolate/extrapolate processed in Update() 
// 
//      
// ##################################################################################


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Diagnostics;
using System;
using System.Reflection;
using UnityEditor;
using UnityEngine.Profiling;
using UnityEngine.UI;
using Common;
using UnityEngine.Rendering.PostProcessing;
using System.Runtime.Serialization;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using System.Xml.Linq;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Controls;
using System.Threading;



// ################################################################################## 
//               AAA                                        tttt            iiii     SSSSSSSSSSSSSSS      tttt                                   tttt               tttt                                                  
//              A:::A                                    ttt:::t           i::::i  SS:::::::::::::::S  ttt:::t                                ttt:::t            ttt:::t                                                  
//             A:::::A                                   t:::::t            iiii  S:::::SSSSSS::::::S  t:::::t                                t:::::t            t:::::t                                                  
//            A:::::::A                                  t:::::t                  S:::::S     SSSSSSS  t:::::t                                t:::::t            t:::::t                                                  
//           A:::::::::A         nnnn  nnnnnnnn    ttttttt:::::ttttttt    iiiiiii S:::::S        ttttttt:::::ttttttt    uuuuuu    uuuuuuttttttt:::::tttttttttttttt:::::ttttttt        eeeeeeeeeeee    rrrrr   rrrrrrrrr   
//          A:::::A:::::A        n:::nn::::::::nn  t:::::::::::::::::t    i:::::i S:::::S        t:::::::::::::::::t    u::::u    u::::ut:::::::::::::::::tt:::::::::::::::::t      ee::::::::::::ee  r::::rrr:::::::::r  
//         A:::::A A:::::A       n::::::::::::::nn t:::::::::::::::::t     i::::i  S::::SSSS     t:::::::::::::::::t    u::::u    u::::ut:::::::::::::::::tt:::::::::::::::::t     e::::::eeeee:::::eer:::::::::::::::::r 
//        A:::::A   A:::::A      nn:::::::::::::::ntttttt:::::::tttttt     i::::i   SS::::::SSSSStttttt:::::::tttttt    u::::u    u::::utttttt:::::::tttttttttttt:::::::tttttt    e::::::e     e:::::err::::::rrrrr::::::r
//       A:::::A     A:::::A       n:::::nnnn:::::n      t:::::t           i::::i     SSS::::::::SS    t:::::t          u::::u    u::::u      t:::::t            t:::::t          e:::::::eeeee::::::e r:::::r     r:::::r
//      A:::::AAAAAAAAA:::::A      n::::n    n::::n      t:::::t           i::::i        SSSSSS::::S   t:::::t          u::::u    u::::u      t:::::t            t:::::t          e:::::::::::::::::e  r:::::r     rrrrrrr
//     A:::::::::::::::::::::A     n::::n    n::::n      t:::::t           i::::i             S:::::S  t:::::t          u::::u    u::::u      t:::::t            t:::::t          e::::::eeeeeeeeeee   r:::::r            
//    A:::::AAAAAAAAAAAAA:::::A    n::::n    n::::n      t:::::t    tttttt i::::i             S:::::S  t:::::t    ttttttu:::::uuuu:::::u      t:::::t    tttttt  t:::::t    tttttte:::::::e            r:::::r            
//   A:::::A             A:::::A   n::::n    n::::n      t::::::tttt:::::ti::::::iSSSSSSS     S:::::S  t::::::tttt:::::tu:::::::::::::::uu    t::::::tttt:::::t  t::::::tttt:::::te::::::::e           r:::::r            
//  A:::::A               A:::::A  n::::n    n::::n      tt::::::::::::::ti::::::iS::::::SSSSSS:::::S  tt::::::::::::::t u:::::::::::::::u    tt::::::::::::::t  tt::::::::::::::t e::::::::eeeeeeee   r:::::r            
// A:::::A                 A:::::A n::::n    n::::n        tt:::::::::::tti::::::iS:::::::::::::::SS     tt:::::::::::tt  uu::::::::uu:::u      tt:::::::::::tt    tt:::::::::::tt  ee:::::::::::::e   r:::::r            
//AAAAAAA                   AAAAAAAnnnnnn    nnnnnn          ttttttttttt  iiiiiiii SSSSSSSSSSSSSSS         ttttttttttt      uuuuuuuu  uuuu        ttttttttttt        ttttttttttt      eeeeeeeeeeeeee   rrrrrrr 
// ################################################################################## 
// 
// ##################################################################################   
public partial class Helicopter_Main : Helicopter_TimestepModel
{



    // ##################################################################################
    //  PPPPPPPPPPPPPPPPP                                                                                                           tttt                                                      
    //  P::::::::::::::::P                                                                                                       ttt:::t                                                      
    //  P::::::PPPPPP:::::P                                                                                                      t:::::t                                                      
    //  PP:::::P     P:::::P                                                                                                     t:::::t                                                      
    //    P::::P     P:::::Paaaaaaaaaaaaa  rrrrr   rrrrrrrrr   aaaaaaaaaaaaa      mmmmmmm    mmmmmmm       eeeeeeeeeeee    ttttttt:::::ttttttt        eeeeeeeeeeee    rrrrr   rrrrrrrrr       
    //    P::::P     P:::::Pa::::::::::::a r::::rrr:::::::::r  a::::::::::::a   mm:::::::m  m:::::::mm   ee::::::::::::ee  t:::::::::::::::::t      ee::::::::::::ee  r::::rrr:::::::::r      
    //    P::::PPPPPP:::::P aaaaaaaaa:::::ar:::::::::::::::::r aaaaaaaaa:::::a m::::::::::mm::::::::::m e::::::eeeee:::::eet:::::::::::::::::t     e::::::eeeee:::::eer:::::::::::::::::r     
    //    P:::::::::::::PP           a::::arr::::::rrrrr::::::r         a::::a m::::::::::::::::::::::me::::::e     e:::::etttttt:::::::tttttt    e::::::e     e:::::err::::::rrrrr::::::r    
    //    P::::PPPPPPPPP      aaaaaaa:::::a r:::::r     r:::::r  aaaaaaa:::::a m:::::mmm::::::mmm:::::me:::::::eeeee::::::e      t:::::t          e:::::::eeeee::::::e r:::::r     r:::::r    
    //    P::::P            aa::::::::::::a r:::::r     rrrrrrraa::::::::::::a m::::m   m::::m   m::::me:::::::::::::::::e       t:::::t          e:::::::::::::::::e  r:::::r     rrrrrrr    
    //    P::::P           a::::aaaa::::::a r:::::r           a::::aaaa::::::a m::::m   m::::m   m::::me::::::eeeeeeeeeee        t:::::t          e::::::eeeeeeeeeee   r:::::r                
    //    P::::P          a::::a    a:::::a r:::::r          a::::a    a:::::a m::::m   m::::m   m::::me:::::::e                 t:::::t    tttttte:::::::e            r:::::r                
    //  PP::::::PP        a::::a    a:::::a r:::::r          a::::a    a:::::a m::::m   m::::m   m::::me::::::::e                t::::::tttt:::::te::::::::e           r:::::r                
    //  P::::::::P        a:::::aaaa::::::a r:::::r          a:::::aaaa::::::a m::::m   m::::m   m::::m e::::::::eeeeeeee        tt::::::::::::::t e::::::::eeeeeeee   r:::::r                
    //  P::::::::P         a::::::::::aa:::ar:::::r           a::::::::::aa:::am::::m   m::::m   m::::m  ee:::::::::::::e          tt:::::::::::tt  ee:::::::::::::e   r:::::r                
    //  PPPPPPPPPP          aaaaaaaaaa  aaaarrrrrrr            aaaaaaaaaa  aaaammmmmm   mmmmmm   mmmmmm    eeeeeeeeeeeeee            ttttttttttt      eeeeeeeeeeeeee   rrrrrrr                  
    // ##################################################################################
    #region parameter
    // ##################################################################################
    // variables, objects
    // ##################################################################################

    // anti stutter measures
    Stopwatch stopwatch_antistutter = new Stopwatch();

    int io_antistutter__fixedupdate_calls_in_this_frame_counter = 1;

    private struct transform_data
    {
        public long ticks; // for time measurement
        public Vector3 position;
        public Quaternion rotation;

        public transform_data(long ticks, Vector3 position, Quaternion rotation)
        {
            this.ticks = ticks;
            this.position = position;
            this.rotation = rotation;
        }
    }

    private transform_data[] list_last_ODE_thread_transforms = new transform_data[20]; //  size = ceil(f_ODE_Hz / f_FPS_Hz)
    private int ODE_thread_transforms_index = 0;


    //int UnitySelectMonitor = 0;
    Helper.Exponential_Moving_Average_Filter exponential_moving_average_filter_for_refresh_rate_sec = new Helper.Exponential_Moving_Average_Filter();


    //long fixedupdate_ticks;
    //long fixedupdate_ticks_old;
    //long time_ticks_at_beginning_of_ODE_calculation;

    float refresh_rate_hz;
    float refresh_rate_sec;
    float refresh_rate_sec_old=0;
    bool refresh_rate_sec_found_flag = false;
    int refresh_rate_sec_found_flag_cntr = 0;

    private struct time_ticks
    {
        public long one_frame;
        public long beginning_of_ODE_calculation;
        public long fixedupdate;
        //public long fixedupdate_old;
        public long calculated;
        public long calculated_old;

        //public time_ticks(long beginning_of_ODE_calculation, long fixedupdate, long fixedupdate_old)
        //{
        //    this.beginning_of_ODE_calculation = beginning_of_ODE_calculation;
        //    this.fixedupdate = fixedupdate;
        //    this.fixedupdate_old = fixedupdate_old;
        //}
    }

    time_ticks ticks = new time_ticks();




#if DEBUG_LOG
    long my_debug_time_array_ID_cntr;
    enum enum_ID
    {
        // 0 should not be used
        t_fixed = 1,
        t_main = 2,
        t_ode = 3,
        t_ode2 = 4,
        t_dt = 5,
        t_factor = 6,
        t_x = 7,
        t_y = 8,
        t_z = 9,
        t_last_ticks = 10,
        t_prev_ticks = 11,
        t_x2 = 12,
        t_y2 = 13,
        t_z2 = 14,
        t_update = 15,
        t_tick_correction = 16,
        t_tick_calculated = 17,
        t_tick_calculated_old = 18,
        t_one_frame = 19,
        t_key_pressed = 20
    }
    const int range = 100000;
    enum_ID[] my_debug_time_array_ID = new enum_ID[range];
    long[] my_debug_time_array_VALUE = new long[range];
    //Stopwatch stopwatch_debug = new Stopwatch();
#endif
    // ##################################################################################
    #endregion









    // ##################################################################################
    //                                                                                                                              dddddddd                 
    //    MMMMMMMM               MMMMMMMM                             tttt         hhhhhhh                                          d::::::d                 
    //    M:::::::M             M:::::::M                          ttt:::t         h:::::h                                          d::::::d                 
    //    M::::::::M           M::::::::M                          t:::::t         h:::::h                                          d::::::d                 
    //    M:::::::::M         M:::::::::M                          t:::::t         h:::::h                                          d:::::d                  
    //    M::::::::::M       M::::::::::M    eeeeeeeeeeee    ttttttt:::::ttttttt    h::::h hhhhh          ooooooooooo       ddddddddd:::::d     ssssssssss   
    //    M:::::::::::M     M:::::::::::M  ee::::::::::::ee  t:::::::::::::::::t    h::::hh:::::hhh     oo:::::::::::oo   dd::::::::::::::d   ss::::::::::s  
    //    M:::::::M::::M   M::::M:::::::M e::::::eeeee:::::eet:::::::::::::::::t    h::::::::::::::hh  o:::::::::::::::o d::::::::::::::::d ss:::::::::::::s 
    //    M::::::M M::::M M::::M M::::::Me::::::e     e:::::etttttt:::::::tttttt    h:::::::hhh::::::h o:::::ooooo:::::od:::::::ddddd:::::d s::::::ssss:::::s
    //    M::::::M  M::::M::::M  M::::::Me:::::::eeeee::::::e      t:::::t          h::::::h   h::::::ho::::o     o::::od::::::d    d:::::d  s:::::s  ssssss 
    //    M::::::M   M:::::::M   M::::::Me:::::::::::::::::e       t:::::t          h:::::h     h:::::ho::::o     o::::od:::::d     d:::::d    s::::::s      
    //    M::::::M    M:::::M    M::::::Me::::::eeeeeeeeeee        t:::::t          h:::::h     h:::::ho::::o     o::::od:::::d     d:::::d       s::::::s   
    //    M::::::M     MMMMM     M::::::Me:::::::e                 t:::::t    tttttth:::::h     h:::::ho::::o     o::::od:::::d     d:::::d ssssss   s:::::s 
    //    M::::::M               M::::::Me::::::::e                t::::::tttt:::::th:::::h     h:::::ho:::::ooooo:::::od::::::ddddd::::::dds:::::ssss::::::s
    //    M::::::M               M::::::M e::::::::eeeeeeee        tt::::::::::::::th:::::h     h:::::ho:::::::::::::::o d:::::::::::::::::ds::::::::::::::s 
    //    M::::::M               M::::::M  ee:::::::::::::e          tt:::::::::::tth:::::h     h:::::h oo:::::::::::oo   d:::::::::ddd::::d s:::::::::::ss  
    //    MMMMMMMM               MMMMMMMM    eeeeeeeeeeeeee            ttttttttttt  hhhhhhh     hhhhhhh   ooooooooooo      ddddddddd   ddddd  sssssssssss   
    // ##################################################################################
    //
    // ##################################################################################
    private void IO_AntiStutter__Init()
    {
        // convert sec to tics
        ticks.one_frame = (long)(((double)refresh_rate_sec) * TimeSpan.TicksPerSecond); 

        // init timer
        stopwatch_antistutter.Reset();
        stopwatch_antistutter.Start();

        //
        IO_AntiStutter__Preset_ODE_Thread_Transforms();

        // start value
        ticks.calculated = stopwatch_antistutter.Elapsed.Ticks;
    }
    // ##################################################################################




    // ##################################################################################
    // set the complete buffer to the initial conditions position and rotation
    // ##################################################################################
    private void IO_AntiStutter__Preset_ODE_Thread_Transforms()
    {
        //stopwatch_antistutter.Reset();

        for (int i = 0; i < list_last_ODE_thread_transforms.Length; i++)
        {
            //list_last_ODE_thread_transforms[i].ticks = stopwatch_antistutter.Elapsed.Ticks+i; // just something
            //list_last_ODE_thread_transforms[i].position = helicopter_ODE.par_temp.transmitter_and_helicopter.helicopter.initial_conditions.position.vect3;
            //list_last_ODE_thread_transforms[i].rotation = Quaternion.Euler(helicopter_ODE.par_temp.transmitter_and_helicopter.helicopter.initial_conditions.orientation.vect3);


            helicopter_ODE.Initial_Position_And_Orientation(out position, out rotation); // righthanded, [m], []
            position = Helper.ConvertRightHandedToLeftHandedVector(position); // left handed, [m]
            rotation = Helper.ConvertRightHandedToLeftHandedQuaternion(rotation); // left handed, []

            list_last_ODE_thread_transforms[i].ticks = stopwatch_antistutter.Elapsed.Ticks + i; // just something
            list_last_ODE_thread_transforms[i].position = position;
            list_last_ODE_thread_transforms[i].rotation = rotation;
        }
    }
    // ##################################################################################



    // ##################################################################################
    //
    // ##################################################################################
    void IO_AntiStutter__Get_ODE_Transform_Before_Calculation(float dt)
    {
#if DEBUG_LOG 
        // debug time ticks to file
        Debug_Collect_Time_Ticks(enum_ID.t_ode, stopwatch_antistutter.Elapsed.Ticks);
        Debug_Collect_Time_Ticks(enum_ID.t_dt, (long)(dt * 10000000));
#endif

        ticks.beginning_of_ODE_calculation = stopwatch_antistutter.Elapsed.Ticks;
    }
    // ##################################################################################



    // ##################################################################################
    // get transform from ode thread and put into a list (buffer)
    // we have to later decide, which two results we use for inter/extrapolation
    // ##################################################################################
    void IO_AntiStutter__Get_ODE_Transform_After_Calculation()
    {
        // anti stutter -> save last ode result for extrapolation of position and orientation 
        ODE_thread_transforms_index++;
        // if at end of list go back to beginning
        ODE_thread_transforms_index %= list_last_ODE_thread_transforms.Length;

        IO_AntiStutter__Fill_List_Transform(ticks.beginning_of_ODE_calculation);


#if DEBUG_LOG 
        // debug time ticks to file
        Debug_Collect_Time_Ticks(enum_ID.t_ode2, stopwatch_antistutter.Elapsed.Ticks);
        Debug_Collect_Time_Ticks(enum_ID.t_x, (long)(helicopter_ODE.x_states[0] * 10000000));
        Debug_Collect_Time_Ticks(enum_ID.t_y, (long)(helicopter_ODE.x_states[1] * 10000000));
        Debug_Collect_Time_Ticks(enum_ID.t_z, (long)(helicopter_ODE.x_states[2] * 10000000));
#endif
    }
    // ##################################################################################



    // ##################################################################################
    // anti stutter
    // ##################################################################################
    private void IO_AntiStutter__Fill_List_Transform(long time_ticks)
    {
        // anti stutter -> save two last ode results and interpolate/extrapolate actual value

        position.x = (float)helicopter_ODE.x_states[0]; // [m] x in reference frame
        position.y = (float)helicopter_ODE.x_states[1]; // [m] y in reference frame
        position.z = (float)helicopter_ODE.x_states[2]; // [m] z in reference frame

        rotation.w = (float)helicopter_ODE.x_states[3]; // [-] w
        rotation.x = (float)helicopter_ODE.x_states[4]; // [-] x
        rotation.y = (float)helicopter_ODE.x_states[5]; // [-] y
        rotation.z = (float)helicopter_ODE.x_states[6]; // [-] z

        list_last_ODE_thread_transforms[ODE_thread_transforms_index].ticks = time_ticks;
        list_last_ODE_thread_transforms[ODE_thread_transforms_index].position = (position = Helper.ConvertRightHandedToLeftHandedVector(position));
        list_last_ODE_thread_transforms[ODE_thread_transforms_index].rotation = (rotation = Helper.ConvertRightHandedToLeftHandedQuaternion(rotation));
    }
    // ##################################################################################


    // ##################################################################################
    // anti stutter: set transforms to helicopter
    // ##################################################################################
    private void IO_AntiStutter__Set_Transform()
    {
        // value used to slowly align the ticks.calculated value to the ticks.fixedupdate value
        long tick_correction = 0;

        // update the ticks.one_frame, because the excat refresh_rate_sec has to be determined at beginning of the sim
        ticks.one_frame = (long)(((double)refresh_rate_sec * 0.9999) * TimeSpan.TicksPerSecond);



        // check if the last ticks.calculated_old value was more than two frames ago. then wee need a new ticks.calculated_old value...
        if ( (ticks.calculated_old + ticks.one_frame) < (stopwatch_antistutter.Elapsed.Ticks - ticks.one_frame) )
        {
            //UnityEngine.Debug.Log("ticks.calculated_old+ticks.one_frame: " + (ticks.calculated_old+ticks.one_frame) / 10E6 + "     stopwatch_antistutter.Elapsed.Ticks: " + stopwatch_antistutter.Elapsed.Ticks / 10E6 + "  === " + ((ticks.calculated_old + one_frame) - (stopwatch_antistutter.Elapsed.Ticks - one_frame)));

            // ... if a ticks.fixedupdate is inside the last frame, then use this as new ticks.calculated_old value,
            if (ticks.fixedupdate > (stopwatch_antistutter.Elapsed.Ticks - ticks.one_frame))
            {
                ticks.calculated_old = ticks.fixedupdate - ticks.one_frame;
            }
            else // othervise create one relative to the actual Update() time.
            {
                ticks.calculated_old = stopwatch_antistutter.Elapsed.Ticks - ticks.one_frame;
            }
        }
        else
        {
            // if there is a fixedupdate tick close to the actual update tick, then use the fixedupdate-tick to 
            // synchronize the calculated-tick. The calculated-tick should align with the fixedupdate-tick.
            if (ticks.fixedupdate > (stopwatch_antistutter.Elapsed.Ticks - ticks.one_frame*0.5) )
            {
                long delta_ticks = (ticks.fixedupdate - (ticks.calculated_old + ticks.one_frame));
                tick_correction = (long)((double)delta_ticks * 0.1000000); // slowly adjust the correction value

                // if the ticks.one_frame isn't exactly the frame rate tick (because it is derived 
                // from an integer "Screen.currentResolution.refreshRate") then the ticks.calculated can't
                // excactly hit the fixedupdate-tick. This is because this inexact ticks.one_frame introduces in 
                // every frame an error. 
                // The code above behaves like a P-Controller. To elimate the error a PI-controller should be designed.
                // The error means, that the physical calculation do not precise align with the beginning of each frame, 
                // what leads to larger extrapolation values

                //UnityEngine.Debug.Log("delta_ticks: " + delta_ticks / 10E6);
            }
        }

        // create the ticks.calculated to be used in the position/rotation interpolation/extrapolation
        ticks.calculated = ticks.calculated_old + ticks.one_frame + tick_correction;






        // get the "last" and "prev" ODE-transform positions by searching the closest time ticks in the list to ticks.calculated
        long delta_min = 100000000;
        int index_min = 0;
        for (int i = 0; i < list_last_ODE_thread_transforms.Length; i++)
        {
            long delta = ticks.calculated - list_last_ODE_thread_transforms[i].ticks;

            if (Mathf.Abs(delta) < Mathf.Abs(delta_min))
            {
                delta_min = delta;
                index_min = i;
            }
        }

        // find which field holds the prev and last results
        int ODE_thread_transforms_index_last = index_min;
        int ODE_thread_transforms_index_prev = ((index_min - 1 + list_last_ODE_thread_transforms.Length) % list_last_ODE_thread_transforms.Length);

        // 
        //long x = fixedupdate_ticks - fixedupdate_ticks_delta + fixedupdate_ticks_delta_filtered;
        long x = ticks.calculated;

        // find inter/extrapolation factor (with linear equation)
        //  y = ((y2 - y1)/(x2 - x1)) * (x - x1) + y1; // https://de.wikipedia.org/wiki/Geradengleichung
        float factor = (float)(((1.0 - 0.0) / 
            (double)(list_last_ODE_thread_transforms[ODE_thread_transforms_index_last].ticks - list_last_ODE_thread_transforms[ODE_thread_transforms_index_prev].ticks)) * 
            (double)(x - list_last_ODE_thread_transforms[ODE_thread_transforms_index_prev].ticks) + 0f);

        //UnityEngine.Debug.Log("factor: " + factor + "     ticks.calculated: " + ticks.calculated / 10E6 +
        //     "     last: " + ODE_thread_transforms_index_last +
        //     "     prev: " + ODE_thread_transforms_index_prev +
        //     "     last_: " + list_last_ODE_thread_transforms[ODE_thread_transforms_index_last].ticks / 10E6 +
        //     "     prev_: " + list_last_ODE_thread_transforms[ODE_thread_transforms_index_prev].ticks / 10E6);

        // do the inter/extrapolation
        helicopters_available.transform.position = Vector3.LerpUnclamped(list_last_ODE_thread_transforms[ODE_thread_transforms_index_prev].position, list_last_ODE_thread_transforms[ODE_thread_transforms_index_last].position, factor);
        helicopters_available.transform.rotation = Quaternion.SlerpUnclamped(list_last_ODE_thread_transforms[ODE_thread_transforms_index_prev].rotation, list_last_ODE_thread_transforms[ODE_thread_transforms_index_last].rotation, factor);




#if DEBUG_LOG
        // debug time ticks to file
        Debug_Collect_Time_Ticks(enum_ID.t_main, stopwatch_antistutter.Elapsed.Ticks);
        Debug_Collect_Time_Ticks(enum_ID.t_fixed, ticks.fixedupdate);
        Debug_Collect_Time_Ticks(enum_ID.t_factor, (long)(factor * 10000000));
        Debug_Collect_Time_Ticks(enum_ID.t_tick_correction, tick_correction );
        Debug_Collect_Time_Ticks(enum_ID.t_tick_calculated, ticks.calculated);
        Debug_Collect_Time_Ticks(enum_ID.t_tick_calculated_old, ticks.calculated_old);
        Debug_Collect_Time_Ticks(enum_ID.t_last_ticks, list_last_ODE_thread_transforms[ODE_thread_transforms_index_last].ticks );
        Debug_Collect_Time_Ticks(enum_ID.t_prev_ticks, list_last_ODE_thread_transforms[ODE_thread_transforms_index_prev].ticks );
        Debug_Collect_Time_Ticks(enum_ID.t_one_frame, ticks.one_frame);
        Debug_Collect_Time_Ticks(enum_ID.t_key_pressed, Keyboard.current.enterKey.IsPressed() ? 1 : 0);


        Debug_Collect_Time_Ticks(enum_ID.t_x2, (long)(helicopters_available.transform.position.x * 10000000));
        Debug_Collect_Time_Ticks(enum_ID.t_y2, (long)(helicopters_available.transform.position.y * 10000000));
        Debug_Collect_Time_Ticks(enum_ID.t_z2, (long)(helicopters_available.transform.position.z * 10000000));
#endif

        ticks.calculated_old = ticks.calculated;
    }
    // ##################################################################################





    // ##################################################################################
    //
    // ##################################################################################
    private void IO_AntiStutter__Get_FixedUpdate_TimeTick()
    {
        // ##################################################################################
        // anti stuttering measures
        // ##################################################################################
        if (QualitySettings.vSyncCount > 0)
        {
            if (io_antistutter__fixedupdate_calls_in_this_frame_counter++ == 0)
            {
                if (gl_pause_flag == false)
                {
                    ticks.fixedupdate = stopwatch_antistutter.Elapsed.Ticks;
                }
            }
        }
    }
    // ##################################################################################





    // Start is called before the first frame update
    void Start_IO_AntiStutter()
    {
        IO_AntiStutter__Init();
    }

    // Update is called once per frame
    void Update_IO_AntiStutter()
    {
        
    }


}
