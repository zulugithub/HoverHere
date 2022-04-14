using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Missile_CollisionDetection : MonoBehaviour
{
    public GameObject missile;
    public GameObject missile_body;
    public GameObject missile_fire;
    public GameObject missile_explosion;

    private Rigidbody rigid_body;
    private RaycastHit hit;
    private Vector3 target_position;
    private bool target_found =false;

    public float propulsion_force = 300f;
    public float torque_stiffness = 0.5f;
    public float torque_damping = 0.1f;
    private const float max_distance = 300; // [m]

    private Vector3 rotate_amount_relative_old;
    private Vector3 rotate_amount_relative_velocity;



    void Start()
    {
        rigid_body = gameObject.GetComponent<Rigidbody>();
  
        if (Physics.Raycast(transform.position, transform.right, out hit, max_distance))
        {
            Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.right) * hit.distance, Color.yellow,5);
            //UnityEngine.Debug.Log("hit " + hit);

            target_position = hit.point;
            target_found = true;
        }
    }

    void FixedUpdate()
    {
        if(target_found)
        {
            // aiming torque
            Vector3 direction_to_target = target_position - transform.position;
            direction_to_target.Normalize();     

            Vector3 rotate_amount = Vector3.Cross(direction_to_target, transform.right);
            
            Vector3 rotate_amount_relative = transform.InverseTransformDirection(rotate_amount);
            rotate_amount_relative_velocity = (rotate_amount_relative - rotate_amount_relative_old) / Time.fixedDeltaTime;

            rigid_body.AddRelativeTorque(- torque_stiffness * rotate_amount_relative - torque_damping * rotate_amount_relative_velocity );
            rotate_amount_relative_old = rotate_amount_relative;
            
            // propulsion force
            rigid_body.AddRelativeForce(Vector3.right * propulsion_force, ForceMode.Force);

            // debug lines
#if UNITY_EDITOR
            Debug.DrawRay(transform.position, direction_to_target, Color.blue, 2);
            Debug.DrawRay(transform.position, rotate_amount, Color.red, 2);
            Debug.DrawRay(transform.position, rotate_amount_relative_velocity, Color.green, 2);
#endif
        }
    }

    void OnCollisionEnter(Collision collision)
    {

        missile_body.gameObject.SetActive(false);
        missile_fire.gameObject.SetActive(false);
        rigid_body.velocity = Vector3.zero;

        Destroy(missile,10);

        GameObject GameObjectclone = (GameObject)Instantiate(missile_explosion, transform.position, transform.rotation);
        Destroy(GameObjectclone, 1.8f);
    }


}
