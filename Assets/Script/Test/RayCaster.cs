using UnityEngine;

public class RayCaster : MonoBehaviour
{
    void OnDrawGizmos()
    {
        RaycastHit hit;

        Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity);

        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, transform.forward * 10);
    }
}