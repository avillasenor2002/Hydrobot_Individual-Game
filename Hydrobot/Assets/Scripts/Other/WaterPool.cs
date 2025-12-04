using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterPool : MonoBehaviour
{
    public WaterTank WaterTank;
    public bool WaterRetore;
    public float WaterRestoreTotal;
    public Rigidbody2D playerRB;
    public float BoyancyRise;
    public float Boyancygrav;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (WaterTank.currentWater <= WaterTank.maxWater && WaterRetore == true)
        {
            WaterTank.currentWater += WaterRestoreTotal;
        }

        if (WaterRetore == true)
        {
            playerRB.AddForce(transform.up * BoyancyRise, ForceMode2D.Force);
            //playerRB.gravityScale = Boyancygrav;
        }
        else
        {
            //playerRB.gravityScale = 0.45f; 
            //playerRB.AddForce(-transform.up * Boyancygrav, ForceMode2D.Force);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Player collision (optional)
        if (other.CompareTag("Player"))
        {
                WaterRetore= true;
            playerRB = other.attachedRigidbody;
        }
        if (other.CompareTag("Projectile"))
        {
            WaterProjectile Wa = other.gameObject.GetComponent<WaterProjectile>();
            Wa.Death();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        // Player collision (optional)
        if (other.CompareTag("Player"))
        {
            WaterRetore = false;
            playerRB.AddForce(-transform.up * Boyancygrav, ForceMode2D.Force);
        }
    }
}
