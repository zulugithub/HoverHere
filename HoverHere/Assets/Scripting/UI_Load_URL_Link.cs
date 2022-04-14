using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_Load_URL_Link : MonoBehaviour
{
    public float factor = 2 * Mathf.PI;
    public float offset = 1.0f;

    public void OpenGitHub() 
    {
        Application.OpenURL("https://github.com/zulugithub/HoverHere");
    }

    public void OpenBlender()
    {
        Application.OpenURL("https://www.blender.org/");
    }

    public void OpenUnity()
    {
        Application.OpenURL("https://unity.com/");
    }

    public void OpenYouTube()
    {
        Application.OpenURL("https://www.youtube.com/channel/UCS6mk9Hsd6h49sqQiX_3l0A");
    }


    // Update is called once per frame
    void Update()
    {
        float f = (Mathf.Sin((Time.time + offset) * factor ) + 1) /2 +0.5f;

        // pick a random color
        Color newColor = new Color(f ,f ,f , 1.0f);
        // apply it on current object's material
        gameObject.GetComponent<Image>().color = newColor;

    }
}
