using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skeleton : MonoBehaviour
{
    public float offset = 0;
    public Animator skeleton;
    public TriggerSet ts;
    public GameObject player, target;
    public SkinnedMeshRenderer skin;
    public GameObject root;
    public bool first = false;
    Vector3 home;
    RaycastHit hit = new RaycastHit();
    public CharacterController enemy;
    // Start is called before the first frame update
    void Start()
    {
        home = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        root.transform.localPosition = new Vector3(0, root.transform.localPosition.y, 0);
        Debug.DrawRay(transform.position, transform.TransformDirection(Vector3.down) * 10, Color.cyan);
        if (Vector3.Distance(transform.position, player.transform.position) < 30f && !first)
        {
            skin.enabled = true;
            skeleton.Play("comeOutOfTheGround2handed_B");
            first = true;
        }
        if (ts.fetch && Vector3.Distance(transform.position, player.transform.position) > 1.3f)
        {
            //Play the animation
            skeleton.Play("2handedRun");

            //LookAt the player
            Vector3 lookAtPosition = player.transform.position;
            lookAtPosition.y = transform.position.y;
            transform.LookAt(lookAtPosition);

            //Move Torwards the Player
           /* Vector3 forward = transform.TransformDirection(Vector3.forward);
            float speedey = 2 * .5f*Time.deltaTime; 
            enemy.SimpleMove(forward * speedey);
            */
            transform.position = Vector3.MoveTowards(transform.position, target.transform.position, 3 * Time.deltaTime);

            /*
              Vector3 lookAtPosition = player.transform.position;
            lookAtPosition.y = transform.position.y;
            transform.LookAt(lookAtPosition);
            if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.forward), out hit, 2f) && hit.transform.tag == "Enemy")
            {
                transform.position = Vector3.MoveTowards(transform.position, target.transform.position, 2 * Time.deltaTime);
            }
            else if (Physics.Raycast(transform.position, transform.TransformDirection(Vector3.down), out hit, 15f) && hit.transform.tag == "Enemy")
            {
                print("I am standing ON someone");
                transform.position = Vector3.MoveTowards(transform.position, home, 2 * Time.deltaTime);
            } 
            */
        }
        else if (Vector3.Distance(transform.position, player.transform.position) <= 1.3f)
        {
            skeleton.Play("2handedAttack1");         
        }
        else
        {
            if (Vector3.Distance(transform.position, home) > .001f)
            {
                transform.position = Vector3.MoveTowards(transform.position, home, 3 * Time.deltaTime);
                Vector3 lookAtPosition = home;
                lookAtPosition.y = transform.position.y;
                transform.LookAt(lookAtPosition);
                skeleton.Play("2handedRun");
            }
            else if (!skeleton.GetCurrentAnimatorStateInfo(0).IsName("comeOutOfTheGround2handed_B") && first)
            {
                skeleton.Play("idle2handed");
            }

        }
    }
    
}
