using UnityEngine;
using System.Collections.Generic;

public class SimpleSpinBlur : MonoBehaviour
{
    private Quaternion _rotationPrevious = Quaternion.identity;
    private Vector3 rotDelta_Prev = Vector3.one;

    

    Mesh SSB_Mesh;
    [Range(1, 1000)][Tooltip("RPM Range")]
    public float rpm = 1000;
    [Range(1, 180)][Tooltip("Motion Blur Amount")]
    public float shutterSpeed = 4;
    
    [Range(1, 360)][Tooltip("Motion Blur Samples")]
    public int Samples = 360;
    Queue<Quaternion> rotationQueue = new Queue<Quaternion>();
    public Material SSB_Material;
    [Range(-0.1f, 0.1f)][Tooltip("Motion Blur Opacity")]
    public float alphaOffset;
    public AdvancedSettings advancedSettings;

    Material[] SSB_Materials;

    public Camera camera_;


    void Start()
    {
        SSB_Mesh = GetComponent<MeshFilter>().mesh;
        SSB_Material.enableInstancing = advancedSettings.enableGPUInstancing;

        SSB_Materials = new Material[Samples+1];
        for (int i = 0; i <= Samples; i++)
        {
            SSB_Materials[i] = new Material(SSB_Material);
        }


    }



    private void Update()
    {
        //float angle = Vector3.Angle(camera.transform.forward, transform.up) / 90f; // 0...1
        //print("angle: " + angle);


        //////////////////////////////////////////////////////////////////////////
        /// at flat angles decrease transparncy
        //////////////////////////////////////////////////////////////////////////
        // https://mathinsight.org/distance_point_plane
        Vector3 v = (camera_.transform.position - transform.position); 
        float d = Vector3.Dot(v, transform.up); // [distance camera poition to rotor's plane] 
        float a = (camera_.transform.position - transform.position).magnitude; // [m] camera to rotor
        float angle = Mathf.Abs(Mathf.Asin( d / a ) / (Mathf.PI/2)); // [0...1] angle 

        if (angle < 0.03f)
        {
            angle = angle/0.03f;
        }
        else
        {
            angle = 1;
        }
        //print("d: " + d + " angle: " + angle);
        //////////////////////////////////////////////////////////////////////////




        // set first object transparency to 0
        Color tempColor_0;
        tempColor_0 = new Color(SSB_Material.color.r, SSB_Material.color.g, SSB_Material.color.b, 0);
        SSB_Material.color = tempColor_0;



        //// set first object transparency to 0
        //Color tempColor_0;
        //tempColor_0 = new Color(SSB_Material.color.r, SSB_Material.color.g, SSB_Material.color.b, Mathf.Abs((2 / (float)Samples) + alphaOffset) * Mathf.Sin(((float)0 / (float)Samples) * 3.1415f));
        //SSB_Material.color = tempColor_0;


        //float shutterSpeed_ = rpm / shutterSpeed;
        float shutterSpeed_ = shutterSpeed;

        for (int i = 0; i <= Samples; i++)
        {
            Color tempColor;
            //////////////////////////////////////////////////////////////////////////
            //float alpha = (1 - angle) * (10f / (float)Samples)    +     Mathf.Abs((2 / (float)Samples) + alphaOffset) * Mathf.Pow(Mathf.Sin(((float)i / (float)Samples) * 3.1415f), 2);
            //////////////////////////////////////////////////////////////////////////
            //float alpha = Mathf.Abs((2 / (float)Samples) + alphaOffset) * Mathf.Pow(Mathf.Sin(((float)i / (float)Samples) * 2.00000000000f * 3.1415f), 2);
            float alpha = (1 - angle) * (10f / (float)Samples)     +      Mathf.Abs((2 / (float)Samples) + alphaOffset) * Mathf.Pow(Mathf.Sin(((float)i / (float)Samples) * 2.00000000000f * 3.1415f), 2);

            tempColor = new Color(SSB_Material.color.r, SSB_Material.color.g, SSB_Material.color.b, alpha);
            SSB_Materials[i].color = tempColor;

            float cyclic_angle = advancedSettings.AngularVelocityCutoff; // [deg]
            //Quaternion q = Quaternion.Lerp(transform.parent.rotation * Quaternion.Euler(cyclic_angle, -shutterSpeed_, 0), transform.parent.rotation * Quaternion.Euler(cyclic_angle, shutterSpeed_, 0), (float)i / (float)Samples);
            //Quaternion q = Quaternion.Lerp(transform.parent.rotation * Quaternion.Euler(cyclic_angle, 0, 0), transform.parent.rotation * Quaternion.Euler(cyclic_angle, 270, 0), (float)i / (float)Samples);
            Quaternion q = transform.parent.rotation * Quaternion.Euler(-cyclic_angle, ((float)i / (float)Samples)*360f, 0);
            Graphics.DrawMesh(SSB_Mesh, transform.position, q, SSB_Materials[i], 0, null, advancedSettings.subMaterialIndex);
        }
    }



    /*
    private void Update()
    {
       float shutterSpeed__ =   rpm; // shutterSpeed

        if (rotationQueue.Count >= shutterSpeed__)
        {
            rotationQueue.Dequeue();
            ////Second Dequeue to reduce queue size
            //if (rotationQueue.Count >= shutterSpeed__)
            //{
            //    rotationQueue.Dequeue();
            //}
        }

        //Queue<string> numbers = new Queue<string>();
        //numbers.Enqueue("one");
        //numbers.Enqueue("two");
        //numbers.Enqueue("three");
        //numbers.Enqueue("four");
        //numbers.Enqueue("five");
        //string ttt = numbers.Peek();
        //numbers.Dequeue();
        //ttt = numbers.Peek();
        //numbers.Enqueue("xxxxx");
        //ttt = numbers.Peek();
        //numbers.Dequeue();

        rotationQueue.Enqueue(transform.rotation);
        if (Quaternion.Angle(transform.rotation, rotationQueue.Peek()) / shutterSpeed__ >= advancedSettings.AngularVelocityCutoff)
        {
            // set first object transparency to 0
            Color tempColor_0;
            tempColor_0 = new Color(SSB_Material.color.r, SSB_Material.color.g, SSB_Material.color.b, Mathf.Abs((2 / (float)Samples) + alphaOffset) * Mathf.Sin(((float)0 / (float)Samples) * 3.1415f));
            SSB_Material.color = tempColor_0;


            if (advancedSettings.unitLocalScale)
            {
                for (int i = 0; i <= Samples; i++)
                {
                    Graphics.DrawMesh(SSB_Mesh, transform.position, Quaternion.Lerp(rotationQueue.Peek(), transform.rotation, (float)i / (float)Samples), SSB_Material, 0, null, advancedSettings.subMaterialIndex);
                }
            }
            else
            {
                for (int i = 0; i <= Samples; i++) 
                {
                    Color tempColor;
                    tempColor = new Color(SSB_Material.color.r, SSB_Material.color.g, SSB_Material.color.b, Mathf.Abs((2 / (float)Samples) + alphaOffset) * Mathf.Pow(Mathf.Sin(((float)i / (float)Samples)*3.1415f) ,2));
                    SSB_Materials[i].color = tempColor;

                    Matrix4x4 matrix = Matrix4x4.TRS(transform.position, Quaternion.Lerp(rotationQueue.Peek(), transform.rotation, (float)i / (float)Samples), transform.localScale);
                    Graphics.DrawMesh(SSB_Mesh, matrix, SSB_Materials[i], 0, null, advancedSettings.subMaterialIndex);
                }

            }

            //Color tempColor;
            //tempColor = new Color(SSB_Material.color.r, SSB_Material.color.g, SSB_Material.color.b, Mathf.Abs((2 / (float)Samples) + alphaOffset));
            //SSB_Material.color = tempColor;
        }
        else
        {
            if (SSB_Material.color.a < 1)
            {
                Color tempColor;
                tempColor = new Color(SSB_Material.color.r, SSB_Material.color.g, SSB_Material.color.b, 1);
                SSB_Material.color = tempColor;
            }
        }
    }
    */


}
[System.Serializable]
public class AdvancedSettings
{
    [Tooltip("[Optimization] Enables material's GPU Instancing property")]
    public bool enableGPUInstancing;
    [Tooltip("Index for objects with multiple materials")]
    public int subMaterialIndex = 0;
    [Tooltip("[Optimization] Check this box if the scale of your model is globalScale (1,1,1)")]
    public bool unitLocalScale = false;
    [Tooltip("[Optimization] Angular velocity threshold value before which the effects will not be rendered.")]
    public float AngularVelocityCutoff;
}