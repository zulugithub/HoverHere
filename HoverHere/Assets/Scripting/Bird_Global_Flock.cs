using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Parameter;
using Common;
// code based on Holistic3d's "Flocking Fish in Unity 5: Creating Schooling behaviour with simple AI" tutorial:
// https://www.youtube.com/watch?v=eMpI1eCsIyM



class stru_animal_flock
{
    public string prefab_name { get; set; }
    public List<GameObject> animals { get; set; }
    public stru_flock parameter { get; set; }
    public Vector3 target_position { get; set; }
    public GameObject target_prefab_for_debug { get; set; }
};



// ##################################################################################
//    FFFFFFFFFFFFFFFFFFFFFFlllllll                                      kkkkkkkk           
//    F::::::::::::::::::::Fl:::::l                                      k::::::k           
//    F::::::::::::::::::::Fl:::::l                                      k::::::k           
//    FF::::::FFFFFFFFF::::Fl:::::l                                      k::::::k           
//      F:::::F       FFFFFF l::::l    ooooooooooo       cccccccccccccccc k:::::k    kkkkkkk
//      F:::::F              l::::l  oo:::::::::::oo   cc:::::::::::::::c k:::::k   k:::::k 
//      F::::::FFFFFFFFFF    l::::l o:::::::::::::::o c:::::::::::::::::c k:::::k  k:::::k  
//      F:::::::::::::::F    l::::l o:::::ooooo:::::oc:::::::cccccc:::::c k:::::k k:::::k   
//      F:::::::::::::::F    l::::l o::::o     o::::oc::::::c     ccccccc k::::::k:::::k    
//      F::::::FFFFFFFFFF    l::::l o::::o     o::::oc:::::c              k:::::::::::k     
//      F:::::F              l::::l o::::o     o::::oc:::::c              k:::::::::::k     
//      F:::::F              l::::l o::::o     o::::oc::::::c     ccccccc k::::::k:::::k    
//    FF:::::::FF           l::::::lo:::::ooooo:::::oc:::::::cccccc:::::ck::::::k k:::::k   
//    F::::::::FF           l::::::lo:::::::::::::::o c:::::::::::::::::ck::::::k  k:::::k  
//    F::::::::FF           l::::::l oo:::::::::::oo   cc:::::::::::::::ck::::::k   k:::::k 
//    FFFFFFFFFFF           llllllll   ooooooooooo       cccccccccccccccckkkkkkkk    kkkkkkk
// ##################################################################################
// bird and insect flocks / swarm
// ##################################################################################
public partial class Helicopter_Main : Helicopter_TimestepModel
{
    List<stru_animal_flock> all_animal_flocks = new List<stru_animal_flock>();

    // nedded to detect, if any of the parameter has changed. Only then the update should done.
    stru_animals animals_parameter_old = new stru_animals();


    // ##################################################################################
    // the following Flocks_Update function should not be called, if the parameter has not changed  
    // ##################################################################################
    bool Flocks_Check_If_Parameter_Has_Changed()
    {
        bool flag = false;
        if (!helicopter_ODE.par_temp.scenery.animals.Deep_Compare(animals_parameter_old))
            flag = true;
              
        animals_parameter_old = helicopter_ODE.par_temp.scenery.animals.Deep_Clone();
        return flag;
    }



    // ##################################################################################
    // all flocks are deleted and new are created with new parameter
    // ##################################################################################
    void Flocks_Update(ref List<stru_animal_flock> all_animal_flocks)
    {
        // destroy existing flocks
        Flocks_Destroy(ref all_animal_flocks);

        // parametrize new flocks for birds
        for (int i = 0; i< helicopter_ODE.par.scenery.animals.number_of_bird_flocks.val; i++)
        {
            all_animal_flocks.Add(new stru_animal_flock {
                prefab_name = "Birds Variant",
                animals = null,
                parameter = helicopter_ODE.par.scenery.animals.bird_flock,
                target_position = Vector3.zero,
                target_prefab_for_debug = GameObject.CreatePrimitive(PrimitiveType.Sphere)
            });
        }

        // parametrize new flocks for insects
        for (int i = 0; i< helicopter_ODE.par.scenery.animals.number_of_insect_flocks.val; i++)
        {
            all_animal_flocks.Add(new stru_animal_flock
            {
                prefab_name = "Mosquito Variant",
                animals = null,
                parameter = helicopter_ODE.par.scenery.animals.insect_flock,
                target_position = Vector3.zero,
                target_prefab_for_debug = GameObject.CreatePrimitive(PrimitiveType.Sphere)
            });
        }

        // create new flocks
        Flocks_Initialize(ref all_animal_flocks);
    }
    // ##################################################################################




    // ##################################################################################
    // instantiate new animals in every flock
    // ##################################################################################
    void Flocks_Initialize(ref List<stru_animal_flock> all_animal_flocks)
    {
        for (int flock_ID = 0; flock_ID < all_animal_flocks.Count; flock_ID++)
        { 
            GameObject animal_prefab = (GameObject)Resources.Load(all_animal_flocks[flock_ID].prefab_name, typeof(GameObject));

            all_animal_flocks[flock_ID].animals = new List<GameObject>(new GameObject[all_animal_flocks[flock_ID].parameter.number_of_animals.val]);

            all_animal_flocks[flock_ID].target_prefab_for_debug.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f); // only for debug

            all_animal_flocks[flock_ID].target_prefab_for_debug.SetActive( all_animal_flocks[flock_ID].parameter.show_flock_target.val ); // only for debug

            all_animal_flocks[flock_ID].target_prefab_for_debug.GetComponent<Collider>().enabled = false; // only for debug

            for (int i = 0; i < all_animal_flocks[flock_ID].parameter.number_of_animals.val; i++)
            {
                // initial position for animal
                Vector3 initial_position = Animal_Create_Random_Position_In_Flying_Box(flock_ID, ref all_animal_flocks);

                // instantiate animal
                all_animal_flocks[flock_ID].animals[i] = (GameObject)Instantiate(animal_prefab, initial_position, Quaternion.identity);

                // set animal scale variation
                all_animal_flocks[flock_ID].animals[i].transform.localScale *= Random.Range(1f - all_animal_flocks[flock_ID].parameter.animal_scale_variation.val / 100f, 1f + all_animal_flocks[flock_ID].parameter.animal_scale_variation.val / 100f);

                // setup parameter in each animals script
                (all_animal_flocks[flock_ID].animals[i].GetComponent<Bird_Single_Flock>()).parameter = all_animal_flocks[flock_ID].parameter;

                // each animal has to know about the position of the other animals in the flock
                (all_animal_flocks[flock_ID].animals[i].GetComponent<Bird_Single_Flock>()).animals = all_animal_flocks[flock_ID].animals;

                // each animal has to know about the position of the helicopter
                (all_animal_flocks[flock_ID].animals[i].GetComponent<Bird_Single_Flock>()).helicopter = helicopters_available;
                
            }
        }

        Flocks_Change_Target_Positions(ref all_animal_flocks, true);
    }
    // ##################################################################################

        


    // ##################################################################################
    // destroy all flocks
    // ##################################################################################
    void Flocks_Destroy(ref List<stru_animal_flock> all_animal_flocks)
    {
        for (int flock_ID = 0; flock_ID < all_animal_flocks.Count; flock_ID++)
        {
            // remove all animals
            foreach (GameObject each_animals in all_animal_flocks[flock_ID].animals)
                GameObject.Destroy(each_animals.gameObject);

            // remove their debug target sphere too
            GameObject.Destroy(all_animal_flocks[flock_ID].target_prefab_for_debug);
        }

        all_animal_flocks.Clear();
    }
    // ##################################################################################




    // ##################################################################################
    // give each flock a new random target position
    // ##################################################################################
    void Flocks_Change_Target_Positions(ref List<stru_animal_flock> all_animal_flocks, bool initializing)
    {
        for (int flock_ID = 0; flock_ID < all_animal_flocks.Count; flock_ID++)
        { 
            if (Random.Range(0, 100000) < all_animal_flocks[flock_ID].parameter.target_update_value.val || initializing == true)
            {
                // generate new target position
                Vector3 target_position = Animal_Create_Random_Position_In_Flying_Box(flock_ID, ref all_animal_flocks);

                // update all animal's target
                for (int i = 0; i < all_animal_flocks[flock_ID].parameter.number_of_animals.val; i++)
                    (all_animal_flocks[flock_ID].animals[i].GetComponent<Bird_Single_Flock>()).target_position = target_position;

                // for debug show the target position as sphere
                all_animal_flocks[flock_ID].target_prefab_for_debug.transform.position = target_position;
            }
        }
    }
    // ##################################################################################




    // ##################################################################################
    // animals live in a box, generate random position inside of it
    // ##################################################################################
    Vector3 Animal_Create_Random_Position_In_Flying_Box(int flock_ID, ref List<stru_animal_flock> all_animal_flocks)
    {
        return new Vector3( Random.Range(-all_animal_flocks[flock_ID].parameter.area_size.val, all_animal_flocks[flock_ID].parameter.area_size.val),
                            Random.Range(+all_animal_flocks[flock_ID].parameter.area_height_min.val, all_animal_flocks[flock_ID].parameter.area_height_max.val),
                            Random.Range(-all_animal_flocks[flock_ID].parameter.area_size.val, all_animal_flocks[flock_ID].parameter.area_size.val));
    }
    // ##################################################################################




}
