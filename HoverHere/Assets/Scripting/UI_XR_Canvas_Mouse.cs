using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR;


public class UI_XR_Canvas_Mouse : MonoBehaviour
{
    public GameObject Arrow3D = null;

    private Vector2 mouse_position = new Vector2();
    private float mousePosX_min;
    private float mousePosX_max;
    private float mousePosY_min;
    private float mousePosY_max;
    Vector2 canvas_size;
    Vector2 screen_size;
    RectTransform canvas_rt;

    void Start()
    {
        XR_mouse_enable();
    }

    void Update()
    {
        if (XRSettings.enabled)
        {
            XR_mouse_enable(); 
            XR_mouse_move();
        }
        else
        {

        }
    }



    private void XR_mouse_enable()
    {
        // limit mouse range to screen
        //Cursor.lockState = CursorLockMode.Confined;

        canvas_rt = GameObject.Find("Canvas").GetComponent<RectTransform>();
        canvas_size = new Vector2(canvas_rt.rect.width, canvas_rt.rect.height);
        screen_size = new Vector2(Screen.width, Screen.height);
        mousePosX_min = -canvas_size.x / 2;
        mousePosX_max = canvas_size.x / 2; // Screen.width;
        mousePosY_min = -canvas_size.y / 2;
        mousePosY_max = canvas_size.y / 2; // Screen.height;
    }




    private void XR_mouse_move()
    {
        // get mouse from current monitor
        mouse_position = UnityEngine.InputSystem.Mouse.current.position.ReadValue();

        // correct center position. canvas center is at (0,0) the screens center is i.e. (1960/2, 1080/2)
        mouse_position -= screen_size / 2;

        // scale mouse position from screen to canvas
        float mouse_sensitivity_factor = 1.0f;
        mouse_position.x *= (canvas_size.x / screen_size.x) * mouse_sensitivity_factor;
        mouse_position.y *= (canvas_size.y / screen_size.y) * mouse_sensitivity_factor;

        // limit mouse position to canvas area
        mouse_position.x = Mathf.Clamp(mouse_position.x, mousePosX_min, mousePosX_max);
        mouse_position.y = Mathf.Clamp(mouse_position.y, mousePosY_min, mousePosY_max);

        // move "LeftHand Controller"'s parent object
        transform.localPosition = new Vector3(mouse_position.x, mouse_position.y, -5f);
        // move arrow
        Arrow3D.transform.localPosition = new Vector3(mouse_position.x, mouse_position.y, -2f);


        //UnityEngine.Debug.Log((screen_size.x / canvas_size.x) + "  mouse_position: " + UnityEngine.InputSystem.Mouse.current.position.ReadValue() + "   mouse_position*: " + mouse_position);

    }
}