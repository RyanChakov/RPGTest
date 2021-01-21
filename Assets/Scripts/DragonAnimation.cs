using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragonAnimation : MonoBehaviour
{
    public Animator dragon;
    public Transform start, end;
    Transform temp;
    public float journeyTime = 10.0f;
    private float startTime;
    // Start is called before the first frame update
    void Start()
    {
        startTime = Time.time;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 center = (start.position + end.position) * 0.5F;
        
        center -= new Vector3(1, 0, 0);

        Vector3 riseRelCenter = start.position - center;
        Vector3 setRelCenter = end.position - center;
        float fracComplete = (Time.time - startTime) / journeyTime;

        transform.position = Vector3.Slerp(riseRelCenter, setRelCenter, fracComplete);
        transform.position += center;
        transform.LookAt(end);
      
        if(Vector3.Distance(transform.position,end.position)<=2)
        {
            print("STOP");
            temp = start;
            start = end;
            end = temp;
            startTime = Time.time;
        }
    }
}
