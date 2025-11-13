using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectiveActivation : MonoBehaviour
{
    [SerializeField] private List<GameObject> objectsToActivate;   // Objects to turn ON
    [SerializeField] private List<GameObject> objectsToDeactivate; // Objects to turn OFF

    public void nextObjective()
    {
        foreach (GameObject obj in objectsToActivate)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }

        foreach (GameObject obj in objectsToDeactivate)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }

        DestroyThisObject();
    }

    public void DestroyThisObject()
    {
        Destroy(gameObject);
    }
}
