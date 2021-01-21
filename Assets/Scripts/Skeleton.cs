using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skeleton : MonoBehaviour
{
    public Animator skeleton;
    public TriggerSet ts;
    public GameObject player;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
       if(ts.fetch)
        {
            skeleton.Play("Run2handed");
            transform.LookAt(player.transform);

        }
        else
        {
            skeleton.Play("idle2handed");
        }
    }
}
