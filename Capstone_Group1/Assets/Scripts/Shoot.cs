using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shoot : MonoBehaviour
{
    public ProjectileBehaiour ProjectilePrefab;
    public Transform LaunchOffset;
    private bool canFire = true;

    public void FireButton()
    {
        if (canFire)
        {
            Instantiate(ProjectilePrefab, LaunchOffset.position, transform.rotation);
            canFire = false;
            StartCoroutine(ResetFireCooldown());
        }
    }

    private IEnumerator ResetFireCooldown()
    {
        yield return new WaitForSeconds(2f);
        canFire = true;
    }
}