using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;
using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Controls;
using Common;
using UnityEditor;
using UnityEngine.InputSystem.HID;
using System.IO;
// RX2SIM Game Controller has two frames. --> workaround needed
// https://forum.unity.com/threads/new-input-system-problem-rx2sim-game-controller-every-second-frame-is-zero.871330/#post-5749141




// ##################################################################################
//    IIIIIIIIII                                                             tttt                  SSSSSSSSSSSSSSS                                            tttt                                                      
//    I::::::::I                                                          ttt:::t                SS:::::::::::::::S                                        ttt:::t                                                      
//    I::::::::I                                                          t:::::t               S:::::SSSSSS::::::S                                        t:::::t                                                      
//    II::::::II                                                          t:::::t               S:::::S     SSSSSSS                                        t:::::t                                                      
//      I::::Innnn  nnnnnnnn    ppppp   ppppppppp   uuuuuu    uuuuuuttttttt:::::ttttttt         S:::::S      yyyyyyy           yyyyyyy  ssssssssss   ttttttt:::::ttttttt        eeeeeeeeeeee       mmmmmmm    mmmmmmm   
//      I::::In:::nn::::::::nn  p::::ppp:::::::::p  u::::u    u::::ut:::::::::::::::::t         S:::::S       y:::::y         y:::::y ss::::::::::s  t:::::::::::::::::t      ee::::::::::::ee   mm:::::::m  m:::::::mm 
//      I::::In::::::::::::::nn p:::::::::::::::::p u::::u    u::::ut:::::::::::::::::t          S::::SSSS     y:::::y       y:::::yss:::::::::::::s t:::::::::::::::::t     e::::::eeeee:::::eem::::::::::mm::::::::::m
//      I::::Inn:::::::::::::::npp::::::ppppp::::::pu::::u    u::::utttttt:::::::tttttt           SS::::::SSSSS y:::::y     y:::::y s::::::ssss:::::stttttt:::::::tttttt    e::::::e     e:::::em::::::::::::::::::::::m
//      I::::I  n:::::nnnn:::::n p:::::p     p:::::pu::::u    u::::u      t:::::t                   SSS::::::::SSy:::::y   y:::::y   s:::::s  ssssss       t:::::t          e:::::::eeeee::::::em:::::mmm::::::mmm:::::m
//      I::::I  n::::n    n::::n p:::::p     p:::::pu::::u    u::::u      t:::::t                      SSSSSS::::Sy:::::y y:::::y      s::::::s            t:::::t          e:::::::::::::::::e m::::m   m::::m   m::::m
//      I::::I  n::::n    n::::n p:::::p     p:::::pu::::u    u::::u      t:::::t                           S:::::Sy:::::y:::::y          s::::::s         t:::::t          e::::::eeeeeeeeeee  m::::m   m::::m   m::::m
//      I::::I  n::::n    n::::n p:::::p    p::::::pu:::::uuuu:::::u      t:::::t    tttttt                 S:::::S y:::::::::y     ssssss   s:::::s       t:::::t    tttttte:::::::e           m::::m   m::::m   m::::m
//    II::::::IIn::::n    n::::n p:::::ppppp:::::::pu:::::::::::::::uu    t::::::tttt:::::t     SSSSSSS     S:::::S  y:::::::y      s:::::ssss::::::s      t::::::tttt:::::te::::::::e          m::::m   m::::m   m::::m
//    I::::::::In::::n    n::::n p::::::::::::::::p  u:::::::::::::::u    tt::::::::::::::t     S::::::SSSSSS:::::S   y:::::y       s::::::::::::::s       tt::::::::::::::t e::::::::eeeeeeee  m::::m   m::::m   m::::m
//    I::::::::In::::n    n::::n p::::::::::::::pp    uu::::::::uu:::u      tt:::::::::::tt     S:::::::::::::::SS   y:::::y         s:::::::::::ss          tt:::::::::::tt  ee:::::::::::::e  m::::m   m::::m   m::::m
//    IIIIIIIIIInnnnnn    nnnnnn p::::::pppppppp        uuuuuuuu  uuuu        ttttttttttt        SSSSSSSSSSSSSSS    y:::::y           sssssssssss              ttttttttttt      eeeeeeeeeeeeee  mmmmmm   mmmmmm   mmmmmm
//                               p:::::p                                                                           y:::::y                                                                                              
//                               p:::::p                                                                          y:::::y                                                                                               
//                              p:::::::p                                                                        y:::::y                                                                                                
//                              p:::::::p                                                                       y:::::y                                                                                                 
//                              p:::::::p                                                                      yyyyyyy                                                                                                  
//                              ppppppppp
// ##################################################################################  
// Unity's new Input System
// ##################################################################################   
public partial class Helicopter_Main : Helicopter_TimestepModel
{
    // channel variables
    float[] input_channel_used_in_game = new float[8];
    float[] input_channel_from_event_proccessing = new float[8];

    // custom devices
    public RX2SIM_Game_Controller RX2SIM_game_controller;
    public DSMX_Game_Controller DSMX_game_controller;
    public FRSKY_Game_Controller FRSKY_Game_Controller;



    // ##################################################################################
    // user input action
    // ##################################################################################
    private void OnEnable()
    {
        InputSystem.onEvent += (eventPtr, device) => IO_Proccess_Input_System_Event(eventPtr, device);         
    }
    private void OnDisable()
    {
        InputSystem.onEvent -= (eventPtr, device) => IO_Proccess_Input_System_Event(eventPtr, device);
    }
    // ##################################################################################



   


    // ##################################################################################
    // check if / which devices are connected
    // ##################################################################################
    void IO_Get_Connected_Gamepads_And_Joysticks()
    {
        // get names of connected gamepads and joysticks
        connected_input_devices_count = 0;
        connected_input_devices_id.Clear();
        connected_input_devices_names.Clear();
        connected_input_devices_type.Clear();
        foreach (var each in Gamepad.all)
        {
            connected_input_devices_id.Add(each.deviceId);
            connected_input_devices_names.Add(each.name);
            connected_input_devices_type.Add("Gamepad");
            connected_input_devices_count++;
            //UnityEngine.Debug.Log("Gamepad " + each.name + "    deviceId " + each.deviceId.ToString());
        }
        foreach (var each in Joystick.all)
        {
            connected_input_devices_id.Add(each.deviceId);
            connected_input_devices_names.Add(each.name);
            connected_input_devices_type.Add("Joystick");
            connected_input_devices_count++;
            //UnityEngine.Debug.Log("Joystick " + each.name + "    deviceId " + each.deviceId.ToString());
        }
        //UnityEngine.Debug.Log("selected_input_device_id "  + selected_input_device_id);
    }
    // ##################################################################################




    // ##################################################################################
    // Input System event handling
    // ##################################################################################
    // https://learn.unity.com/tutorial/events-uh#
    void IO_Proccess_Input_System_Event(InputEventPtr eventPtr, InputDevice device)
    {
        // Ignore anything that isn't a state event.
        if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
            return;

        //UnityEngine.Debug.Log("connected_input_devices_names " + device.name);
        //foreach (var cdn in connected_input_devices_names)
        //    UnityEngine.Debug.Log("connected_input_devices_names " + cdn);

        IO_Get_Connected_Gamepads_And_Joysticks();

        if (selected_input_device_id >= (connected_input_devices_names.Count))
            selected_input_device_id = 0;

        // check if at least one controller is available
        if (connected_input_devices_count > 0)
        {
            // proccess selected device
            if (device.name.Contains(connected_input_devices_names[selected_input_device_id]))
            {
                // if device is a gamepad
                if (connected_input_devices_type[selected_input_device_id].Contains("Gamepad"))
                {
                    //UnityEngine.Debug.Log(connected_input_devices_names[selected_input_device_id] + " is a Gamepad");
                    if (device.description.product.Contains("USB dsmX HID"))
                    {
                        for (int i = 0; i < 8; i++) input_channel_from_event_proccessing[i] = 0;

                        foreach (var each_control in ((DSMX_Game_Controller)device).allControls)
                        {
                            for (int i = 0; i < 8; i++)
                            {
                                if (each_control.name.Contains("axis" + i.ToString()))
                                {
                                    ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[i]);
                                    input_channel_from_event_proccessing[i] = input_channel_from_event_proccessing[i] * 2.0f - 1.0f; // formatting if neccessary
                                    //UnityEngine.Debug.Log("Control___axis" + i + ": " + axis[i]);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (device.description.product.Contains("FrSky Simulator"))
                        {
                            for (int i = 0; i < 8; i++) input_channel_from_event_proccessing[i] = 0;

                            foreach (var each_control in ((FRSKY_Game_Controller)device).allControls)
                            {
                                //UnityEngine.Debug.Log("Control___name: " + each_control.name);
                                for (int i = 0; i < 8; i++)
                                {
                                    if (each_control.name.Contains("axis" + i.ToString()))
                                    {
                                        ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[i]);

                                        // due to an overflow problem with values in range of -127...127: they are interpreted as uint and not as int, 
                                        // therefore the result has an overflow problem. (maybe there is a better solution by editing FRSKY_Game_Controller_Device_State())
                                        if (input_channel_from_event_proccessing[i] > 0.5f) input_channel_from_event_proccessing[i] -= 1.0f;

                                        input_channel_from_event_proccessing[i] = input_channel_from_event_proccessing[i] * 2.0f - 1.0f; // formatting if neccessary
                                    }
                                }
                            }
                        }
                        else // device is a standard gamepad
                        {
                            for (int i = 0; i < 8; i++) input_channel_from_event_proccessing[i] = 0;

                            foreach (var each_control in ((Gamepad)device).allControls)
                            {
                                //UnityEngine.Debug.Log("Control___name " + each_control.name);

                                if (each_control.displayName.Equals("Left Stick X"))
                                    ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[0]);
                                if (each_control.displayName.Equals("Left Stick Y"))
                                    ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[1]);
                                if (each_control.displayName.Equals("Right Stick X"))
                                    ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[2]);
                                if (each_control.displayName.Equals("Right Stick Y"))
                                    ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[3]);
                                if (each_control.displayName.Equals("Left Trigger"))
                                    ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[4]);
                                if (each_control.displayName.Equals("Right Trigger"))
                                    ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[5]);
                                if (each_control.displayName.Equals("X"))
                                    ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[6]);
                                if (each_control.displayName.Equals("A"))
                                    ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[7]);
                            }
                        }
                    }

                    // Can handle events yourself, for example, and then stop them
                    // from further processing by marking them as handled.
                    eventPtr.handled = true;

                } // if device is a joystick
                else if (connected_input_devices_type[selected_input_device_id].Contains("Joystick"))   
                {
                    //UnityEngine.Debug.Log(connected_input_devices_names[selected_input_device_id] + " is a Joystick");

                    // workaround for not correct supported "RX2SIM Game Controller" device ( due to its data packets seperated into two events )
                    if (device.description.product.Contains("RX2SIM Game Controller"))
                    {
                        int reportId_workaround_as_integer = 0;

                        foreach (var each_control in ((RX2SIM_Game_Controller)device).allControls)
                        {
                            //UnityEngine.Debug.Log("Control___name: " + each_control.name);

                            if (each_control.name.Contains("reportId_workaround_as_integer"))
                            {
                                ((IntegerControl)each_control).ReadValueFromEvent(eventPtr, out reportId_workaround_as_integer);
                                //UnityEngine.Debug.Log("Control___reportId1: " + reportId_workaround_as_botton);
                            }

                            // process first data frame wih axes 0..3
                            if (reportId_workaround_as_integer == 1)
                            {
                                for (int i = 0; i < 4; i++)
                                {
                                    if (each_control.name.Contains("axis" + i.ToString()))
                                    {
                                        ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[i]);
                                        input_channel_from_event_proccessing[i] = (input_channel_from_event_proccessing[i] - 0.5f) * 2f;
                                        //UnityEngine.Debug.Log("Control___axis" + i + ": " + axis[i]);
                                    }
                                }
                            }
                            // process second data frame wih axes 4..7 (following 8 buttons are not considered yet)
                            if (reportId_workaround_as_integer == 2)
                            {
                                for (int i = 4; i < 8; i++)
                                {
                                    if (each_control.name.Contains("axis" + (i - 4).ToString()))
                                    {
                                        ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[i]);
                                        input_channel_from_event_proccessing[i] = (input_channel_from_event_proccessing[i] - 0.5f) * 2f;
                                        //UnityEngine.Debug.Log("Control___axis" + i + ": " + axis[i]);
                                    }
                                }
                            }
                        }

                        //UnityEngine.Debug.Log("input_channel_from_event_proccessing       C0: " + Helper.FormatNumber(input_channel_from_event_proccessing[0], "0.000") +
                        //                                                                " C1: " + Helper.FormatNumber(input_channel_from_event_proccessing[1], "0.000") +
                        //                                                                " C2: " + Helper.FormatNumber(input_channel_from_event_proccessing[2], "0.000") +
                        //                                                                " C3: " + Helper.FormatNumber(input_channel_from_event_proccessing[3], "0.000") +
                        //                                                                " C4: " + Helper.FormatNumber(input_channel_from_event_proccessing[4], "0.000") +
                        //                                                                " C5: " + Helper.FormatNumber(input_channel_from_event_proccessing[5], "0.000") +
                        //                                                                " C6: " + Helper.FormatNumber(input_channel_from_event_proccessing[6], "0.000") +
                        //                                                                " C7: " + Helper.FormatNumber(input_channel_from_event_proccessing[7], "0.000"));


                    }
                    else  // standard Joystick
                    {
                        for (int i = 0; i < 8; i++) input_channel_from_event_proccessing[i] = 0;

                        foreach (var each_control in ((Joystick)device).allControls)
                        {
                            //UnityEngine.Debug.Log("Control___name " + each_control.name);

                            if (each_control.displayName.Equals("Stick X"))
                                ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[0]);
                            if (each_control.displayName.Equals("Stick Y"))
                                ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[1]);
                            if (each_control.displayName.Equals("Rz"))
                                ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[2]);
                            if (each_control.displayName.Equals("Z"))
                                ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[3]);
                            if (each_control.displayName.Equals("Rx"))
                                ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[4]);
                            if (each_control.displayName.Equals("Ry"))
                                ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[5]);
                            if (each_control.displayName.Equals("Trigger"))
                                ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[6]);
                            if (each_control.displayName.Equals("Button 2"))
                                ((AxisControl)each_control).ReadValueFromEvent(eventPtr, out input_channel_from_event_proccessing[7]);
                        }
              
                    }

                    // Can handle events yourself, for example, and then stop them
                    // from further processing by marking them as handled.
                    eventPtr.handled = true;
                }
            }
        }

 

    }
    // ##################################################################################





    // during Awake
    void IO_InputSystem_Initialize()
    {


        // ##################################################################################
        // InputSystem
        // ##################################################################################
        //InputSystem.onDeviceChange +=
        //   (device, change) =>
        //   {
        //       switch (change)
        //       {
        //           case InputDeviceChange.Added:
        //               // New Device.
        //               break;
        //           case InputDeviceChange.Disconnected:
        //               // Device got unplugged.
        //               break;
        //           case InputDeviceChange.Enabled:
        //               // Plugged back in.
        //               break;
        //           case InputDeviceChange.Removed:
        //               // Remove from Input System entirely; by default, Devices stay in the system once discovered.
        //               break;
        //           default:
        //               // See InputDeviceChange reference for other event types.
        //               break;
        //       }
        //   };



        // ##################################################################################
        // InputSystem
        // ##################################################################################
        RX2SIM_game_controller = new RX2SIM_Game_Controller();  // call constructor
        DSMX_game_controller = new DSMX_Game_Controller();  // call constructor
        FRSKY_Game_Controller = new FRSKY_Game_Controller();  // call constructor


        // ##################################################################################
        // Init: input actions , InputSystem
        // ##################################################################################
        //input_action = new PlayerInputActions();
        //input_action.PlayerControls.Channel0.performed += ctx => input_channel_from_input_action[0] = ctx.ReadValue<float>();
        //input_action.PlayerControls.Channel1.performed += ctx => input_channel_from_input_action[1] = ctx.ReadValue<float>();
        //input_action.PlayerControls.Channel2.performed += ctx => input_channel_from_input_action[2] = ctx.ReadValue<float>();
        //input_action.PlayerControls.Channel3.performed += ctx => input_channel_from_input_action[3] = ctx.ReadValue<float>();
        //input_action.PlayerControls.Channel4.performed += ctx => input_channel_from_input_action[4] = ctx.ReadValue<float>();
        //input_action.PlayerControls.Channel5.performed += ctx => input_channel_from_input_action[5] = ctx.ReadValue<float>();
        //input_action.PlayerControls.Channel6.performed += ctx => input_channel_from_input_action[6] = ctx.ReadValue<float>();
        //input_action.PlayerControls.Channel7.performed += ctx => input_channel_from_input_action[7] = ctx.ReadValue<float>();
        // ##################################################################################

    }




    // ##################################################################################
    // Input System diagnostics - write all device info to  files
    // ##################################################################################
    void Input_System_Diagnostics()
    {
        int i = 0;
        foreach (var each_device in InputSystem.devices)
        {
            // get description
            using (StreamWriter stream = new StreamWriter("device_" + i + "_description.txt", true))
                stream.Write(each_device.description.ToJson());


            // get layout 
            var layout = InputSystem.LoadLayout(each_device.layout);
            if (layout != null)
            { 
                using (StreamWriter stream = new StreamWriter("device_" + i + "_layout.txt", true))
                    stream.Write(layout.ToJson());
            }

            // get hid descriptor (part of description)
            if (each_device.description.interfaceName == "HID")
            {
                using (StreamWriter stream = new StreamWriter("device_" + i + "_hidDescriptor.txt", true))
                    stream.Write(each_device.description.capabilities);               
            }

            i++;
        }
    }
    // ##################################################################################



}










// ##################################################################################
//  RRRRRRRRRRRRRRRRR   XXXXXXX       XXXXXXX 222222222222222       SSSSSSSSSSSSSSS IIIIIIIIIIMMMMMMMM               MMMMMMMM
//  R::::::::::::::::R  X:::::X       X:::::X2:::::::::::::::22   SS:::::::::::::::SI::::::::IM:::::::M             M:::::::M
//  R::::::RRRRRR:::::R X:::::X       X:::::X2::::::222222:::::2 S:::::SSSSSS::::::SI::::::::IM::::::::M           M::::::::M
//  RR:::::R     R:::::RX::::::X     X::::::X2222222     2:::::2 S:::::S     SSSSSSSII::::::IIM:::::::::M         M:::::::::M
//    R::::R     R:::::RXXX:::::X   X:::::XXX            2:::::2 S:::::S              I::::I  M::::::::::M       M::::::::::M
//    R::::R     R:::::R   X:::::X X:::::X               2:::::2 S:::::S              I::::I  M:::::::::::M     M:::::::::::M
//    R::::RRRRRR:::::R     X:::::X:::::X             2222::::2   S::::SSSS           I::::I  M:::::::M::::M   M::::M:::::::M
//    R:::::::::::::RR       X:::::::::X         22222::::::22     SS::::::SSSSS      I::::I  M::::::M M::::M M::::M M::::::M
//    R::::RRRRRR:::::R      X:::::::::X       22::::::::222         SSS::::::::SS    I::::I  M::::::M  M::::M::::M  M::::::M
//    R::::R     R:::::R    X:::::X:::::X     2:::::22222               SSSSSS::::S   I::::I  M::::::M   M:::::::M   M::::::M
//    R::::R     R:::::R   X:::::X X:::::X   2:::::2                         S:::::S  I::::I  M::::::M    M:::::M    M::::::M
//    R::::R     R:::::RXXX:::::X   X:::::XXX2:::::2                         S:::::S  I::::I  M::::::M     MMMMM     M::::::M
//  RR:::::R     R:::::RX::::::X     X::::::X2:::::2       222222SSSSSSS     S:::::SII::::::IIM::::::M               M::::::M
//  R::::::R     R:::::RX:::::X       X:::::X2::::::2222222:::::2S::::::SSSSSS:::::SI::::::::IM::::::M               M::::::M
//  R::::::R     R:::::RX:::::X       X:::::X2::::::::::::::::::2S:::::::::::::::SS I::::::::IM::::::M               M::::::M
//  RRRRRRRR     RRRRRRRXXXXXXX       XXXXXXX22222222222222222222 SSSSSSSSSSSSSSS   IIIIIIIIIIMMMMMMMM               MMMMMMMM
// ##################################################################################
// RX2SIM Game Controller - Workaround 
// ##################################################################################
// https://forum.unity.com/threads/new-input-system-problem-rx2sim-game-controller-every-second-frame-is-zero.871330/
// The "RX2SIM Game Controller" sends its data in two frames marked by different 
// "Report ID"s. Unity's InputSystem V1.0.0. cant handel this correctly. Therefore
// a workaourind is user, where "Report ID" state is attached to a Control-Layer of 
// type "Integer"  ("reportId_workaround_as_integer"). The InputSystem.onEvent is used 
// to catch the data receive event an process the data manually.
//
// ##################################################################################
// We receive data as raw HID input reports. This struct
// describes the raw binary format of such a report.
//[StructLayout(LayoutKind.Explicit, Size = 15)]
public struct RX2SIM_Game_Controller_Device_State : IInputStateTypeInfo
{
    // Because all HID input reports are tagged with the 'HID ' FourCC,
    // this is the format we need to use for this state struct.
    public FourCC format => new FourCC('H', 'I', 'D');

    // HID input reports can start with an 8-bit report ID. It depends on the device
    // whether this is present or not. On the RX2SIM Game Controller, it is
    // present and in the two frames it has the value 1 or 2.
    [InputControl(name = "reportId_workaround_as_integer", format = "BIT", layout = "Integer", displayName = "Button reportId1 workaround", bit = 0, offset = 0, sizeInBits = 2)]
    //[FieldOffset(0)]
    public byte reportId_workaround_as_integer;

    // https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html
    [InputControl(name = "axis0", format = "BIT", layout = "Axis", displayName = "Axis 0", bit = 0, offset = 1, sizeInBits = 12)] // axis-X
    //[FieldOffset(1)]
    public float axis0;

    [InputControl(name = "axis1", format = "BIT", layout = "Axis", displayName = "Axis 1", bit = 4, offset = 2, sizeInBits = 12)] // axis-Y
    //[FieldOffset(2)]
    public float axis1;

    [InputControl(name = "axis2", format = "BIT", layout = "Axis", displayName = "Axis 2", bit = 0, offset = 4, sizeInBits = 12)] // axis-Z
    //[FieldOffset(3)]
    public float axis2;

    [InputControl(name = "axis3", format = "BIT", layout = "Axis", displayName = "Axis 3", bit = 4, offset = 5, sizeInBits = 12)] // axis-Rx
    //[FieldOffset(4)]
    public float axis3;


    //[InputControl(name = "axis4", format = "BIT", layout = "Axis", displayName = "Axis 4", bit = 0, offset = 8, sizeInBits = 12)] // axis-Ry
    //[FieldOffset(5)]
    //public float axis4;

    //[InputControl(name = "axis5", format = "BIT", layout = "Axis", displayName = "Axis 5", bit = 4, offset = 9, sizeInBits = 12)] // axis-Rz
    //[FieldOffset(6)]
    //public float axis5;

    //[InputControl(name = "axis6", format = "BIT", layout = "Axis", displayName = "Axis 6", bit = 0, offset = 11, sizeInBits = 12)] // Slider
    //[FieldOffset(7)]
    //public float axis6;

    //[InputControl(name = "axis7", format = "BIT", layout = "Axis", displayName = "Axis 7", bit = 4, offset = 12, sizeInBits = 12)] // Dial
    //[FieldOffset(8)]
    //public float axis7;


    //[InputControl(name = "button1", format = "BIT", layout = "Button", displayName = "Button 1", bit = 0, offset = 14, sizeInBits = 1)]
    //[InputControl(name = "button2", format = "BIT", layout = "Button", displayName = "Button 2", bit = 1, offset = 14, sizeInBits = 1)]
    //[InputControl(name = "button3", format = "BIT", layout = "Button", displayName = "Button 3", bit = 2, offset = 14, sizeInBits = 1)]
    //[InputControl(name = "button4", format = "BIT", layout = "Button", displayName = "Button 4", bit = 3, offset = 14, sizeInBits = 1)]
    //[InputControl(name = "button5", format = "BIT", layout = "Button", displayName = "Button 5", bit = 4, offset = 14, sizeInBits = 1)]
    //[InputControl(name = "button6", format = "BIT", layout = "Button", displayName = "Button 6", bit = 5, offset = 14, sizeInBits = 1)]
    //[InputControl(name = "button7", format = "BIT", layout = "Button", displayName = "Button 7", bit = 6, offset = 14, sizeInBits = 1)]
    //[InputControl(name = "button8", format = "BIT", layout = "Button", displayName = "Button 8", bit = 7, offset = 14, sizeInBits = 1)]
    //[FieldOffset(10)] 
    //public byte buttons1;
}
// ##################################################################################



// ##################################################################################
// Using InputControlLayoutAttribute, we tell the system about the state
// struct we created, which includes where to find all the InputControl
// attributes that we placed on there.This is how the Input System knows
// what controls to create and how to configure them.
// ##################################################################################
[InputControlLayout(stateType = typeof(RX2SIM_Game_Controller_Device_State))]
#if UNITY_EDITOR
[InitializeOnLoad] // Make sure static constructor is called during startup.
#endif
public class RX2SIM_Game_Controller : Joystick // Gamepad // Joystick // InputDevice 
{
    ////public override int valueSizeInBytes { get; }

    //UnityEngine.InputSystem.PlayerInput
    static RX2SIM_Game_Controller()
    {
        //// This is one way to match the Device.
        InputSystem.RegisterLayout<RX2SIM_Game_Controller>(
            matches: new InputDeviceMatcher()
                .WithInterface("HID")
                .WithManufacturer("RCWARE")
                .WithProduct("RX2SIM Game Controller"));

        //// Alternatively, you can also match by PID and VID, which is generally
        //// more reliable for HIDs.
        //InputSystem.RegisterLayout<RX2SIM_Game_Controller>(
        //    matches: new InputDeviceMatcher()
        //        .WithInterface("HID")
        //        .WithCapability("vendorId", 1155) // RCWARE
        //        .WithCapability("productId", 41195)); // RX2SIM Game Controller

    }

    //// In the Player, to trigger the calling of the static constructor,
    //// create an empty method annotated with RuntimeInitializeOnLoadMethod.
    [RuntimeInitializeOnLoadMethod]
    static void Init() { }
}
// ##################################################################################









// ##################################################################################
//  DDDDDDDDDDDDD           SSSSSSSSSSSSSSS MMMMMMMM               MMMMMMMMXXXXXXX       XXXXXXX               ///////DDDDDDDDDDDDD           SSSSSSSSSSSSSSS MMMMMMMM               MMMMMMMM 222222222222222    
//  D::::::::::::DDD      SS:::::::::::::::SM:::::::M             M:::::::MX:::::X       X:::::X              /:::::/ D::::::::::::DDD      SS:::::::::::::::SM:::::::M             M:::::::M2:::::::::::::::22  
//  D:::::::::::::::DD   S:::::SSSSSS::::::SM::::::::M           M::::::::MX:::::X       X:::::X             /:::::/  D:::::::::::::::DD   S:::::SSSSSS::::::SM::::::::M           M::::::::M2::::::222222:::::2 
//  DDD:::::DDDDD:::::D  S:::::S     SSSSSSSM:::::::::M         M:::::::::MX::::::X     X::::::X            /:::::/   DDD:::::DDDDD:::::D  S:::::S     SSSSSSSM:::::::::M         M:::::::::M2222222     2:::::2 
//    D:::::D    D:::::D S:::::S            M::::::::::M       M::::::::::MXXX:::::X   X:::::XXX           /:::::/      D:::::D    D:::::D S:::::S            M::::::::::M       M::::::::::M            2:::::2 
//    D:::::D     D:::::DS:::::S            M:::::::::::M     M:::::::::::M   X:::::X X:::::X             /:::::/       D:::::D     D:::::DS:::::S            M:::::::::::M     M:::::::::::M            2:::::2 
//    D:::::D     D:::::D S::::SSSS         M:::::::M::::M   M::::M:::::::M    X:::::X:::::X             /:::::/        D:::::D     D:::::D S::::SSSS         M:::::::M::::M   M::::M:::::::M         2222::::2  
//    D:::::D     D:::::D  SS::::::SSSSS    M::::::M M::::M M::::M M::::::M     X:::::::::X             /:::::/         D:::::D     D:::::D  SS::::::SSSSS    M::::::M M::::M M::::M M::::::M    22222::::::22   
//    D:::::D     D:::::D    SSS::::::::SS  M::::::M  M::::M::::M  M::::::M     X:::::::::X            /:::::/          D:::::D     D:::::D    SSS::::::::SS  M::::::M  M::::M::::M  M::::::M  22::::::::222     
//    D:::::D     D:::::D       SSSSSS::::S M::::::M   M:::::::M   M::::::M    X:::::X:::::X          /:::::/           D:::::D     D:::::D       SSSSSS::::S M::::::M   M:::::::M   M::::::M 2:::::22222        
//    D:::::D     D:::::D            S:::::SM::::::M    M:::::M    M::::::M   X:::::X X:::::X        /:::::/            D:::::D     D:::::D            S:::::SM::::::M    M:::::M    M::::::M2:::::2             
//    D:::::D    D:::::D             S:::::SM::::::M     MMMMM     M::::::MXXX:::::X   X:::::XXX    /:::::/             D:::::D    D:::::D             S:::::SM::::::M     MMMMM     M::::::M2:::::2             
//  DDD:::::DDDDD:::::D  SSSSSSS     S:::::SM::::::M               M::::::MX::::::X     X::::::X   /:::::/            DDD:::::DDDDD:::::D  SSSSSSS     S:::::SM::::::M               M::::::M2:::::2       222222
//  D:::::::::::::::DD   S::::::SSSSSS:::::SM::::::M               M::::::MX:::::X       X:::::X  /:::::/             D:::::::::::::::DD   S::::::SSSSSS:::::SM::::::M               M::::::M2::::::2222222:::::2
//  D::::::::::::DDD     S:::::::::::::::SS M::::::M               M::::::MX:::::X       X:::::X /:::::/              D::::::::::::DDD     S:::::::::::::::SS M::::::M               M::::::M2::::::::::::::::::2
//  DDDDDDDDDDDDD         SSSSSSSSSSSSSSS   MMMMMMMM               MMMMMMMMXXXXXXX       XXXXXXX///////               DDDDDDDDDDDDD         SSSSSSSSSSSSSSS   MMMMMMMM               MMMMMMMM22222222222222222222
// ##################################################################################
// DSMXGame Controller - Orange Rx DSMX/DSM2 USB Dongle 
// ##################################################################################
// jstest return :
// sudo jstest /dev/input/js0
// Driver version is 2.1.0.
// Joystick(Cypress USB dsmX HID) has 8 axes(X, Y, Z, Rx, Ry, Rz, Throttle, Rudder) and 0 buttons().
//
// sudo evdev-joystick --s /dev/input/event21
// Supported Absolute axes:
// Absolute axis 0x00 (0) (X Axis) (value: 127, min: 0, max: 255, flatness: 15 (=5.88%), fuzz: 0)
// Absolute axis 0x01 (1) (Y Axis) (value: 127, min: 0, max: 255, flatness: 15 (=5.88%), fuzz: 0)
// Absolute axis 0x02 (2) (Z Axis) (value: 103, min: 0, max: 255, flatness: 15 (=5.88%), fuzz: 0)
// Absolute axis 0x03 (3) (X Rate Axis) (value: 127, min: 0, max: 255, flatness: 15 (=5.88%), fuzz: 0)
// Absolute axis 0x04 (4) (Y Rate Axis) (value: 212, min: 0, max: 255, flatness: 15 (=5.88%), fuzz: 0)
// Absolute axis 0x05 (5) (Z Rate Axis) (value: 103, min: 0, max: 255, flatness: 15 (=5.88%), fuzz: 0)
// Absolute axis 0x06 (6) (Throttle) (value: 212, min: 0, max: 255, flatness: 15 (=5.88%), fuzz: 0)
// Absolute axis 0x07 (7) (Rudder) (value: 69, min: 0, max: 255, flatness: 15 (=5.88%), fuzz: 0)
// ##################################################################################
// We receive data as raw HID input reports. This struct
// describes the raw binary format of such a report.
//[StructLayout(LayoutKind.Explicit, Size = 15)]
public struct DSMX_Game_Controller_Device_State : IInputStateTypeInfo
{
    // Because all HID input reports are tagged with the 'HID ' FourCC,
    // this is the format we need to use for this state struct.
    public FourCC format => new FourCC('H', 'I', 'D');

    // HID input reports can start with an 8-bit report ID. It depends on the device
    // whether this is present or not. On the RX2SIM Game Controller, it is
    // present and in the two frames it has the value 1 or 2.
    [InputControl(name = "reportId", format = "BIT", layout = "Integer", displayName = "reportId1", bit = 0, offset = 0, sizeInBits = 8)]
    public byte reportId_;

    // https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html
    [InputControl(name = "axis0", format = "BIT", layout = "Axis", displayName = "X Axis", bit = 0, offset = 1, sizeInBits = 8)] // axis-X Axis
    public float axis0;

    [InputControl(name = "axis1", format = "BIT", layout = "Axis", displayName = "Y Axis", bit = 0, offset = 2, sizeInBits = 8)] // axis-Y Axis
    public float axis1;

    [InputControl(name = "axis2", format = "BIT", layout = "Axis", displayName = "Z Axis", bit = 0, offset = 3, sizeInBits = 8)] // axis-Z Axis
    public float axis2;

    [InputControl(name = "axis3", format = "BIT", layout = "Axis", displayName = "X Rate Axis", bit = 0, offset = 4, sizeInBits = 8)] // axis-X Rate Axis
    public float axis3;

    [InputControl(name = "axis4", format = "BIT", layout = "Axis", displayName = "Y Rate Axis", bit = 0, offset = 5, sizeInBits = 8)] // axis-Y Rate Axis
    public float axis4;

    [InputControl(name = "axis5", format = "BIT", layout = "Axis", displayName = "Z Rate Axis", bit = 0, offset = 6, sizeInBits = 8)] // axis-Z Rate Axis
    public float axis5;

    [InputControl(name = "axis6", format = "BIT", layout = "Axis", displayName = "Throttle", bit = 0, offset = 7, sizeInBits = 8)] // axis-Throttle
    public float axis6;

    [InputControl(name = "axis7", format = "BIT", layout = "Axis", displayName = "Rudder", bit = 0, offset = 8, sizeInBits = 8)] // axis-Rudder
    public float axis7;

}
// ##################################################################################



// ##################################################################################
// Using InputControlLayoutAttribute, we tell the system about the state
// struct we created, which includes where to find all the InputControl
// attributes that we placed on there.This is how the Input System knows
// what controls to create and how to configure them.
// ##################################################################################
[InputControlLayout(stateType = typeof(DSMX_Game_Controller_Device_State))]
#if UNITY_EDITOR
[InitializeOnLoad] // Make sure static constructor is called during startup.
#endif
public class DSMX_Game_Controller : Gamepad // Gamepad // Joystick // InputDevice 
{
    ////public override int valueSizeInBytes { get; }

    //UnityEngine.InputSystem.PlayerInput
    static DSMX_Game_Controller()
    {
        //// This is one way to match the Device.
        InputSystem.RegisterLayout<DSMX_Game_Controller>(
            matches: new InputDeviceMatcher()
                .WithInterface("HID")
                .WithManufacturer("Cypress")
                .WithProduct("USB dsmX HID"));

        //// Alternatively, you can also match by PID and VID, which is generally
        //// more reliable for HIDs.
        //InputSystem.RegisterLayout<RX2SIM_Game_Controller>(
        //    matches: new InputDeviceMatcher()
        //        .WithInterface("HID")
        //        .WithCapability("vendorId", 1155)
        //        .WithCapability("productId", 41195)); 

    }

    //// In the Player, to trigger the calling of the static constructor,
    //// create an empty method annotated with RuntimeInitializeOnLoadMethod.
    [RuntimeInitializeOnLoadMethod]
    static void Init() { }
}
// ##################################################################################









//// ##################################################################################                                                                                                                                                                                                      
//    FFFFFFFFFFFFFFFFFFFFFF                     SSSSSSSSSSSSSSS kkkkkkkk                                    
//    F::::::::::::::::::::F                   SS:::::::::::::::Sk::::::k                                    
//    F::::::::::::::::::::F                  S:::::SSSSSS::::::Sk::::::k                                    
//    FF::::::FFFFFFFFF::::F                  S:::::S     SSSSSSSk::::::k                                    
//      F:::::F       FFFFFFrrrrr   rrrrrrrrr S:::::S             k:::::k    kkkkkkkyyyyyyy           yyyyyyy
//      F:::::F             r::::rrr:::::::::rS:::::S             k:::::k   k:::::k  y:::::y         y:::::y 
//      F::::::FFFFFFFFFF   r:::::::::::::::::rS::::SSSS          k:::::k  k:::::k    y:::::y       y:::::y  
//      F:::::::::::::::F   rr::::::rrrrr::::::rSS::::::SSSSS     k:::::k k:::::k      y:::::y     y:::::y   
//      F:::::::::::::::F    r:::::r     r:::::r  SSS::::::::SS   k::::::k:::::k        y:::::y   y:::::y    
//      F::::::FFFFFFFFFF    r:::::r     rrrrrrr     SSSSSS::::S  k:::::::::::k          y:::::y y:::::y     
//      F:::::F              r:::::r                      S:::::S k:::::::::::k           y:::::y:::::y      
//      F:::::F              r:::::r                      S:::::S k::::::k:::::k           y:::::::::y       
//    FF:::::::FF            r:::::r          SSSSSSS     S:::::Sk::::::k k:::::k           y:::::::y        
//    F::::::::FF            r:::::r          S::::::SSSSSS:::::Sk::::::k  k:::::k           y:::::y         
//    F::::::::FF            r:::::r          S:::::::::::::::SS k::::::k   k:::::k         y:::::y          
//    FFFFFFFFFFF            rrrrrrr           SSSSSSSSSSSSSSS   kkkkkkkk    kkkkkkk       y:::::y           
//                                                                                        y:::::y            
//                                                                                       y:::::y             
//                                                                                      y:::::y              
//                                                                                     y:::::y               
//                                                                                    yyyyyyy 
// ##################################################################################
//// FrSky FrSky Simulator
// ##################################################################################
// Free RC helicopter Simulator Input Devices Tester --> device_description_x.txt
// - only one reportID(=0) --> all informations are delivered in one event
// - axes start at reportOffsetInBits = 32 --> "offset = 4" (bytes)
// - all 8 axes have 8bit (-127...+127)  --> "sizeInBits = 8"
// - last two axes seam to have additional reportOffsetInBits
// ##################################################################################
// We receive data as raw HID input reports. This struct
// describes the raw binary format of such a report.
//[StructLayout(LayoutKind.Explicit, Size = 15)]
public struct FRSKY_Game_Controller_Device_State : IInputStateTypeInfo
{
    // Because all HID input reports are tagged with the 'HID ' FourCC,
    // this is the format we need to use for this state struct.
    public FourCC format => new FourCC('H', 'I', 'D');

    // HID input reports can start with an 8-bit report ID. It depends on the device
    // whether this is present or not. 
    [InputControl(name = "reportId", format = "BIT", layout = "Integer", displayName = "reportId1", bit = 0, offset = 0, sizeInBits = 8)]
    //[FieldOffset(0)]
    public byte reportId_;

    // https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/api/UnityEngine.InputSystem.Layouts.InputControlAttribute.html
    [InputControl(name = "axis0", format = "BIT", layout = "Axis", displayName = "X Axis", bit = 0, offset = 4, sizeInBits = 8)] // axis-X Axis     , parameters = "normalize,normalizeMin=-127,normalizeMax=127")
    public float axis0;

    [InputControl(name = "axis1", format = "BIT", layout = "Axis", displayName = "Y Axis", bit = 0, offset = 5, sizeInBits = 8)] // axis-Y Axis
    public float axis1;

    [InputControl(name = "axis2", format = "BIT", layout = "Axis", displayName = "Z Axis", bit = 0, offset = 6, sizeInBits = 8)] // axis-Z Axis
    public float axis2;

    [InputControl(name = "axis3", format = "BIT", layout = "Axis", displayName = "X Rate Axis", bit = 0, offset = 7, sizeInBits = 8)] // axis-X Rate Axis
    public float axis3;

    [InputControl(name = "axis4", format = "BIT", layout = "Axis", displayName = "Y Rate Axis", bit = 0, offset = 8, sizeInBits = 8)] // axis-Y Rate Axis
    public float axis4;

    [InputControl(name = "axis5", format = "BIT", layout = "Axis", displayName = "Z Rate Axis", bit = 0, offset = 9, sizeInBits = 8)] // axis-Z Rate Axis
    public float axis5;

    [InputControl(name = "axis6", format = "BIT", layout = "Axis", displayName = "Throttle", bit = 0, offset = 11, sizeInBits = 8)] // axis-Throttle
    public float axis6;

    [InputControl(name = "axis7", format = "BIT", layout = "Axis", displayName = "Rudder", bit = 0, offset = 11, sizeInBits = 8)] // axis-Rudder
    public float axis7;

}
// ##################################################################################



// ##################################################################################
// Using InputControlLayoutAttribute, we tell the system about the state
// struct we created, which includes where to find all the InputControl
// attributes that we placed on there.This is how the Input System knows
// what controls to create and how to configure them.
// ##################################################################################
[InputControlLayout(stateType = typeof(FRSKY_Game_Controller_Device_State))]
#if UNITY_EDITOR
[InitializeOnLoad] // Make sure static constructor is called during startup.
#endif
public class FRSKY_Game_Controller : Gamepad // Gamepad // Joystick // InputDevice 
{
    ////public override int valueSizeInBytes { get; }

    //UnityEngine.InputSystem.PlayerInput
    static FRSKY_Game_Controller()
    {
        //// This is one way to match the Device.
        InputSystem.RegisterLayout<FRSKY_Game_Controller>(
            matches: new InputDeviceMatcher()
                .WithInterface("HID")
                .WithManufacturer("FrSky")
                .WithProduct("FrSky Simulator"));

        //// Alternatively, you can also match by PID and VID, which is generally
        //// more reliable for HIDs.
        //InputSystem.RegisterLayout<RX2SIM_Game_Controller>(
        //    matches: new InputDeviceMatcher()
        //        .WithInterface("HID")
        //        .WithCapability("vendorId", 1155)
        //        .WithCapability("productId", 41195)); 

    }

    //// In the Player, to trigger the calling of the static constructor,
    //// create an empty method annotated with RuntimeInitializeOnLoadMethod.
    [RuntimeInitializeOnLoadMethod]
    static void Init() { }
}
// ##################################################################################