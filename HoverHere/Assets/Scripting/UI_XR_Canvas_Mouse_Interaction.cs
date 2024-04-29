using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
//using System.Linq;
using Common;
using UnityEngine.XR;



public class UI_XR_Canvas_Mouse_Interaction : MonoBehaviour
{
    //public RectTransform rt = null;
    public GameObject CursorObject = null;


    //public Canvas Canvas = null;
    private Vector2 mousePosition = new Vector2();
    //public GameObject arrow3Dobject = null;

    EventSystem m_EventSystem;
    //private Vector2 monitor_size = new Vector2(1920, 1080);
    //public Component[] Images;





    // Start is called before the first frame update
    void Start()
    {
        m_EventSystem = EventSystem.current;

        //Cursor.lockState = CursorLockMode.Confined;
        //monitor_size = new Vector2(Screen.width, Screen.height);
    }

    // Update is called once per frame
    void Update()
    {
        if (XRSettings.enabled)
        {
            CursorObject.SetActive(true);

            //Debug.Log("x: " + UnityEngine.InputSystem.Mouse.current.position.ReadValue().x + "  y: " + UnityEngine.InputSystem.Mouse.current.position.ReadValue().y);
            mousePosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();

            var canvas_rt = GameObject.Find("Canvas").GetComponent<RectTransform>();
            Vector2 can_size = new Vector2(canvas_rt.rect.width / 2f, canvas_rt.rect.height / 2f);
            mousePosition -= can_size;

            CursorObject.GetComponent<RectTransform>().localPosition = mousePosition;
            //arrow3Dobject.transform.position = rt.transform.TransformPoint(mousePosition); // local to world

            // mouse pressed
            if (UnityEngine.InputSystem.Mouse.current.leftButton.wasPressedThisFrame)
            {
                // search ALL UI canvas children (recursively) and check, if mouse is hovering over RectTransform
                foreach (Transform child in canvas_rt.transform.GetComponentsInChildren<Transform>())
                {
                    if (Processing(child, m_EventSystem))
                    {
                        break;
                    }
                }
            }
        }
        else
        {
            CursorObject.SetActive(false);
        }
    }





    public static bool Processing(Transform trans, EventSystem m_EventSystem)
    {
        if (Helper.HasComponent<InputField>(trans.gameObject) ||
            Helper.HasComponent<Dropdown>(trans.gameObject) ||
            Helper.HasComponent<Toggle>(trans.gameObject) ||
            Helper.HasComponent<Scrollbar>(trans.gameObject) ||
            Helper.HasComponent<Button>(trans.gameObject)
            )
        {
            // mouse position
            Vector2 mousePosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
            var canvas_rt = GameObject.Find("Canvas").GetComponent<RectTransform>();
            Vector2 can_size = new Vector2(canvas_rt.rect.width / 2f, canvas_rt.rect.height / 2f);
            mousePosition -= can_size;

            // RectTransform's corner relative to Canvas
            RectTransform rt = trans.GetComponent<RectTransform>();
            Vector3[] corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            for (var i = 0; i < 4; i++)
                corners[i] = canvas_rt.InverseTransformPoint(corners[i]); // Transforms position from world space to local space.}
            Rect newRect = new Rect(corners[0], corners[2] - corners[0]);

            // check, if mouse is hovering over RectTransform
            if (newRect.Contains(mousePosition)) //-(Vector2)rt.localPosition
            {
                //Debug.Log(trans);
                ExecuteEvents.Execute(trans.gameObject, new BaseEventData(m_EventSystem), ExecuteEvents.submitHandler);
                return true;
            }
            else
            {
                return false;
            }
        }
        else
        {
            return false;
        }

    }



    //public static Transform RecursiveProcessChild(Transform parent, EventSystem m_EventSystem)
    //{
    //    Transform child = null;
    //    for (int i = 0; i < parent.childCount; i++)
    //    {
    //        child = parent.GetChild(i);
    //        if (Processing(child, m_EventSystem))
    //        {
    //            break;
    //        }
    //        else
    //        {
    //            child = RecursiveProcessChild(child, m_EventSystem);
    //            if (child != null)
    //            {
    //                break;
    //            }
    //        }
    //    }
    //    return child;
    //}


}






//public static class Helper
//{

//    public static bool HasComponent<T>(this GameObject obj) where T : Component
//    {
//        return obj.GetComponent(typeof(T)) != null;
//    }

//}



