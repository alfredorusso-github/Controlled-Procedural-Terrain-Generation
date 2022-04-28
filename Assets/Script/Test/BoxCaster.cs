using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxCaster : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDrawGizmos(){
        RaycastHit hit; 

        Physics.BoxCast(transform.position, transform.lossyScale/20, transform.forward, out hit, Quaternion.identity, Mathf.Infinity);

        if(hit.collider.name == "Cube"){
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.forward * hit.distance);
            Gizmos.DrawWireCube(transform.position+transform.forward * hit.distance, transform.lossyScale);
        }
        else{
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, transform.forward * hit.distance);
            Gizmos.DrawWireCube(transform.position+transform.forward * hit.distance, transform.lossyScale);
        }
    }
}
