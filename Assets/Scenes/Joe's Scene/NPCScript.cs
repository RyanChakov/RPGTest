using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class NPCScript : MonoBehaviour
{
    public GameObject text;
    RaycastHit hit = new RaycastHit();
    public GameObject cam;
    public Text NPCline;


    public GameObject player = // player capsule variable;
    public GameObject NPC; = // npc capsule 




    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

        if (Physics.Raycast(cam.transform.position, cam.transform.TransformDirection(Vector3.forward), out hit, 10f))
        {

            if(hit.collider.tag == "NPC")
            {



                if (Vector3.Distance( // distance between variables of player capsule and npc capsule is <5 unity units){


                          text.SetActive(true);




            }


              



                



            }

        }


        

    // If you press E on a unit with a NPC tag then the Ryan is dumb thing should appear



        if (Input.GetKeyDown(KeyCode.E) && Physics.Raycast(cam.transform.position, cam.transform.TransformDirection(Vector3.forward), out hit, 10f) )
        {

            if (hit.collider.tag == "NPC")
            {
                NPCline.text = "Hello, Ryan is dumb if he ever reviews this code or looks at my project. :)";



            }




        }




        }
}
