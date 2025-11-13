using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class WaterPickup : MonoBehaviour
{
    [SerializeField] private float waterAddAmount = 10f;
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float flickerDuration = 0.1f;
    [SerializeField] private ParticleSystem pickupEffect;
    public bool isObject = true;

    private void Start()
    {
        // Auto-snap to grid if marked as an object
        if (isObject)
        {
            GameObject tilemapObject = GameObject.Find("Main Tilemap");
            if (tilemapObject != null)
            {
                Tilemap tilemap = tilemapObject.GetComponent<Tilemap>();
                if (tilemap != null)
                {
                    Vector3Int cellPos = tilemap.WorldToCell(transform.position);
                    transform.position = tilemap.GetCellCenterWorld(cellPos);
                }
            }
        }

        // Auto-assign audio source if not set
        if (audioSource == null)
        {
            GameObject sfx = GameObject.Find("SFX");
            if (sfx != null)
            {
                audioSource = sfx.GetComponent<AudioSource>();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerRotation player = other.GetComponent<PlayerRotation>();
        if (player != null && player.waterTotal != null)
        {
            // Add water to player
            player.waterTotal.currentWater += waterAddAmount;

            // Play SFX
            if (pickupSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(pickupSound);
            }

            // Play VFX and flicker
            StartCoroutine(FlickerThenDestroy());
        }
    }

    private IEnumerator FlickerThenDestroy()
    {
        SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
        foreach (var r in renderers)
        {
            r.enabled = false;
        }

        if (pickupEffect != null)
        {
            Instantiate(pickupEffect, transform.position, Quaternion.identity);
        }

        yield return new WaitForSeconds(flickerDuration);

        Destroy(gameObject);
    }
}
