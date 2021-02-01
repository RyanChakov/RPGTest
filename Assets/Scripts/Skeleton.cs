using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skeleton : MonoBehaviour
{
    public Animator skeleton;
    public TriggerSet ts;
    public GameObject player;
    public SkinnedMeshRenderer skin;
    public GameObject root;
    public bool first = false;
    public CharacterController me;
    Vector3 home;
    // Start is called before the first frame update
    void Start()
    {
        home = transform.position;
        print(home.ToString());
    }

    // Update is called once per frame
    void Update()
    {
        root.transform.localPosition = new Vector3(0,root.transform.localPosition.y,0);
        if(Input.GetKeyDown(KeyCode.E))
        {
            print(transform.position.ToString());
        }
        if (Vector3.Distance(transform.position, player.transform.position) < 30f && !first)
        {
            skin.enabled = true;
            skeleton.Play("comeOutOfTheGround2handed_B");
            first = true;
        }
        if (ts.fetch && Vector3.Distance(transform.position,player.transform.position)>1.3f)
        {
            //Play the animation
            skeleton.Play("2handedRun");

            //LookAt the player
            Vector3 lookAtPosition = player.transform.position;
            lookAtPosition.y = transform.position.y;
            transform.LookAt(lookAtPosition);

            //Move Torwards the Player

            // transform.position = Vector3.MoveTowards(transform.position,player.transform.position,2*Time.deltaTime);
            Vector3 stop = transform.position-Vector3.MoveTowards(transform.position, player.transform.position, 2 * Time.deltaTime);
            me.SimpleMove(stop);
        }
        else if (Vector3.Distance(transform.position, player.transform.position) <= 1.3f)
        {
            skeleton.Play("2handedAttack1");
        }
        else
        {
            if (Vector3.Distance(transform.position,home) > .001f)
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
