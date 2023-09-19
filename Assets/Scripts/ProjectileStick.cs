using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileStick : MonoBehaviour
{
    private Rigidbody rb;
    private bool targetHit;
    
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(targetHit)
        {
            return;
        } else
        {
            targetHit = true;
        }

        rb.isKinematic = true;
        transform.SetParent(collision.transform);
    }
}
