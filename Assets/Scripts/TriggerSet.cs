using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TriggerSet : MonoBehaviour
{
    public bool fetch = false;
    private void OnTriggerStay(Collider other)
    {
        if(other.tag=="Player")
        {
            fetch = true;
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            fetch = false;
        }
    }
}
