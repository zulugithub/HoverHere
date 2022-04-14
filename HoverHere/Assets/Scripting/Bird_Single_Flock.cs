using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Parameter;

// code based on Holistic3d's "Flocking Fish in Unity 5: Creating Schooling behaviour with simple AI" tutorial:
// https://www.youtube.com/watch?v=eMpI1eCsIyM




// ##################################################################################
//                                                               dddddddd
//    BBBBBBBBBBBBBBBBB     iiii                                 d::::::d
//    B::::::::::::::::B   i::::i                                d::::::d
//    B::::::BBBBBB:::::B   iiii                                 d::::::d
//    BB:::::B     B:::::B                                       d:::::d 
//      B::::B     B:::::Biiiiiiirrrrr   rrrrrrrrr       ddddddddd:::::d 
//      B::::B     B:::::Bi:::::ir::::rrr:::::::::r    dd::::::::::::::d 
//      B::::BBBBBB:::::B  i::::ir:::::::::::::::::r  d::::::::::::::::d 
//      B:::::::::::::BB   i::::irr::::::rrrrr::::::rd:::::::ddddd:::::d 
//      B::::BBBBBB:::::B  i::::i r:::::r     r:::::rd::::::d    d:::::d 
//      B::::B     B:::::B i::::i r:::::r     rrrrrrrd:::::d     d:::::d 
//      B::::B     B:::::B i::::i r:::::r            d:::::d     d:::::d 
//      B::::B     B:::::B i::::i r:::::r            d:::::d     d:::::d 
//    BB:::::BBBBBB::::::Bi::::::ir:::::r            d::::::ddddd::::::dd
//    B:::::::::::::::::B i::::::ir:::::r             d:::::::::::::::::d
//    B::::::::::::::::B  i::::::ir:::::r              d:::::::::ddd::::d
//    BBBBBBBBBBBBBBBBB   iiiiiiiirrrrrrr               ddddddddd   ddddd
// ##################################################################################
public class Bird_Single_Flock : MonoBehaviour
{
    [HideInInspector]
    public List<GameObject> animals;  // set by global flock controller
    [HideInInspector]
    public stru_flock parameter;  // set by global flock controller 
    [HideInInspector]
    public Vector3 target_position;  // set by global flock controller
    [HideInInspector]
    public GameObject helicopter;  // set by global flock controller

    float bird_speed;
    bool bird_is_turning = false;




    // ##################################################################################
    // Start is called before the first frame update
    // ##################################################################################
    void Start()
    {
        bird_speed = Random.Range(parameter.animal_speed_min.val, parameter.animal_speed_max.val);
        gameObject.GetComponent<Animator>().speed = bird_speed / ((parameter.animal_speed_max.val + parameter.animal_speed_min.val) / 2.0f) * (parameter.animal_animation_speed.val / 100);
    }
    // ##################################################################################




    // ##################################################################################
    // Update is called once per frame
    // ##################################################################################
    void Update()
    {
        // limit position to box
        if (this.transform.position.x > parameter.area_size.val || this.transform.position.x < -parameter.area_size.val ||
            this.transform.position.y > parameter.area_height_max.val || this.transform.position.y < parameter.area_height_min.val ||
            this.transform.position.z > parameter.area_size.val || this.transform.position.z < -parameter.area_size.val)
            bird_is_turning = true;
        else
            bird_is_turning = false;
  
        // if bird is outside of box it is turning
        if(bird_is_turning)
        {
            Vector3 direction = new Vector3(0, (parameter.area_height_min.val + parameter.area_height_max.val) / 2, 0) - transform.position;
            transform.rotation = Quaternion.Slerp(transform.rotation,
                                                  Quaternion.LookRotation(direction),
                                                  parameter.animal_rotation_speed.val * Time.deltaTime);

            bird_speed = Random.Range(parameter.animal_speed_min.val, parameter.animal_speed_max.val);
        }
        else 
        {
            // else use swarm logic
            if (Random.Range(0.0f, parameter.apply_rules_value.val) < 1)
                Bird_Apply_Rules();
        }

        transform.Translate(0, 0, Time.deltaTime * bird_speed);
    }
    // ##################################################################################




    // ##################################################################################
    // swarm logic
    // ##################################################################################
    void Bird_Apply_Rules()
    {
        Vector3 vec_group_center = Vector3.zero;
        Vector3 vec_group_center_sum = Vector3.zero;
        Vector3 vec_group_avoid = Vector3.zero;

        float distance;

        int group_size = 0;
        
        foreach(GameObject each_animal in animals)
        {
            if(each_animal != this.gameObject)
            {
                distance = Vector3.Distance(each_animal.transform.position, this.transform.position);
                // build flocks
                if (distance <= parameter.animal_neighbour_distance.val)
                {
                    vec_group_center_sum += each_animal.transform.position;
                    group_size++;

                    // try to avoid each other
                    if (distance < parameter.avoid_animal_distance.val)
                        vec_group_avoid += (this.transform.position - each_animal.transform.position);
                }

                // try to avoid pilot
                if (Vector3.Distance(each_animal.transform.position, new Vector3(0.0f, 1.7f, 0.0f)) < 0.7f)
                    vec_group_avoid += (this.transform.position - each_animal.transform.position);

                // try to avoid helicopter
                distance = Vector3.Distance(helicopter.transform.position, this.transform.position);
                if (distance <= parameter.avoid_helicopter_distance.val)
                    vec_group_avoid += (this.transform.position - helicopter.transform.position);
            }


            if (group_size > 0)
            {
                vec_group_center = (vec_group_center_sum / group_size) + (target_position - this.transform.position);

                Vector3 direction = (vec_group_center + vec_group_avoid) - this.transform.position;
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation,
                                                          Quaternion.LookRotation(direction),
                                                          parameter.animal_rotation_speed.val * Time.deltaTime);
                }
            }
        }
    }
    // ##################################################################################



}
