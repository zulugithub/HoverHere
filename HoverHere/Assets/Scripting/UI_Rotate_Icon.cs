using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_Rotate_Icon : MonoBehaviour
{
    RectTransform rt = null;

    // Start is called before the first frame update
    void Start()
    {
        rt = gameObject.GetComponent<RectTransform>();
    }

    // Update is called once per frame
    void Update()
    {
        rt.rotation = Quaternion.Euler(0, Time.time*100, 0);
    }
}
