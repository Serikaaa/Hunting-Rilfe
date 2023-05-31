using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
public class Shooting : NetworkBehaviour
{
    public Transform firePoint;
    public GameObject bulletPrefab;
    public float bulletForce = 10f;
    public float fireRate = 0.5F;
    private float nextFire = 0.0F;
    // Update is called once per frame
    void Update()
    {
        if (!IsOwner) return;
        if (Input.GetButtonDown("Fire1") && Time.time >nextFire)
        {
            nextFire = Time.time + fireRate;
            Shoot();
        }
    }
    void Shoot()
    {
        GameObject bullet = Instantiate(bulletPrefab, firePoint.position, firePoint.rotation);
        Rigidbody2D rb = bullet.GetComponent<Rigidbody2D>();
        rb.AddForce(firePoint.up * bulletForce, ForceMode2D.Impulse);
        bullet.GetComponent<NetworkObject>().Spawn();
        Destroy(bullet, 0.5f);
    }


}
