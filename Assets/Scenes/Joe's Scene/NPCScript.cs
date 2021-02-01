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


    public GameObject player;
    public GameObject NPC;




    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //drawing the raycast coming out of my guy

        Debug.DrawRay(cam.transform.position, cam.transform.TransformDirection(Vector3.forward) * 10f, Color.blue);

        //IF The camera is within 10 feet of a Game Object with the hit collider NPC. then the "Press E to talk text appears"

        
        if (Physics.Raycast(cam.transform.position, cam.transform.TransformDirection(Vector3.forward), out hit, 10f))
        {

            if (hit.collider.tag == "NPC")
            {


                text.SetActive(true);
               
            }
           


        }
        else
        {
            text.SetActive(false);
        }


        // If you press E on a unit with a NPC tag then the Ryan is dumb thing should appear



        if (Input.GetKeyDown(KeyCode.E) && Physics.Raycast(cam.transform.position, cam.transform.TransformDirection(Vector3.forward), out hit, 10f))
        {

            if (hit.collider.tag == "NPC")
            {
                NPCline.text = "Hello, Ryan is dumb if he ever reviews this code or looks at my project. :)";



            }

        }

        if (Input.GetKeyDown(KeyCode.Escape)){

            NPCline.text = "";
              

        }



    }
}
