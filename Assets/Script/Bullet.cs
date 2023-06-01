using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class Bullet : NetworkBehaviour
{
    public Shooting parent;
    [SerializeField] public GameObject hitEffect;
  /*  private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!IsOwner) return;
        GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
        Destroy(effect, 0.2f);
        Destroy(gameObject);
    }
  */


    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner) return;
        GameObject effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
        Destroy(effect, 0.2f);
        Destroy(gameObject);
    }

}

