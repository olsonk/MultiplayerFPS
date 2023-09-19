using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowingScript : MonoBehaviour
{
    public Transform cam;
    public Transform attackPoint;
    public GameObject objectToThrow;

    public int totalThrows;
    public float throwCooldown;

    public KeyCode throwKey = KeyCode.Mouse0;
    public float throwForce;
    public float throwUpwardForce;

    bool readyToThrow;
    
    // Start is called before the first frame update
    void Start()
    {
        ResetThrow();
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKey(throwKey) && readyToThrow && totalThrows > 0)
        {
            Throw();
        }
    }

    private void Throw()
    {
        readyToThrow = false;

        GameObject projectile = Instantiate(objectToThrow, attackPoint.position, cam.rotation);

        Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();

        Vector3 forceDirection = cam.transform.forward;

        RaycastHit hit;

        if(Physics.Raycast(cam.position, cam.forward, out hit, 500f))
        {
            forceDirection = (hit.point - attackPoint.position).normalized;
        }

        Vector3 forceToAdd = forceDirection * throwForce + transform.up * throwUpwardForce;

        projectileRb.AddForce(forceToAdd, ForceMode.Impulse);

        totalThrows--;

        Invoke(nameof(ResetThrow), throwCooldown); 
    }

    private void ResetThrow()
    {
        readyToThrow = true;
    }
}
