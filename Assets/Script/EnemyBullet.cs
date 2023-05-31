using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class EnemyBullet : NetworkBehaviour
{
    public GameObject hitEffect;
    private void OnCollisionEnter2D(Collision2D collision)
    {
       GameObject effect =  Instantiate(hitEffect, transform.position, Quaternion.identity);
        Destroy(effect,1f);
        Destroy(gameObject);
    }
}

