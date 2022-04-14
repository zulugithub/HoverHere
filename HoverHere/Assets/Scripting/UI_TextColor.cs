using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UI_TextColor : MonoBehaviour
{
    Text text;
    public float factor = 0.5f;

    // Start is called before the first frame update
    void Start()
    {
        text = gameObject.GetComponent<Text>();
    }

    // Update is called once per frame
    void Update()
    {

        // pick a random color
        Color newColor = new Color( (Time.time/factor) % 1.0f, (Time.time / factor) % 1.0f, (Time.time / factor) % 1.0f, 1.0f);
        // apply it on current object's material
        text.color = newColor;

    }
}
