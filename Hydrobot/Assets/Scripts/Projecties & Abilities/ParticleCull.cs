using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleCull : MonoBehaviour
{
    private void Start()
    {
        // Destroy this game object after 0.15 seconds
        Destroy(gameObject, 0.5f);
    }
}
