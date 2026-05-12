using UnityEngine;

// ============================================================
// MPProjectileSpawner.cs
// Attach to Player prefab alongside MPPlayerMovement
// Spawns MPProjectile with correct owner role
// ============================================================

public class MPProjectileSpawner : MonoBehaviour
{
    [HideInInspector] public string ownerRole = "p1";

    public MPProjectile mpProjectilePrefab;
    public Transform launchOffset;

    public void SpawnProjectile()
    {
        if (mpProjectilePrefab == null || launchOffset == null) return;

        MPProjectile proj = Instantiate(mpProjectilePrefab,
            launchOffset.position, transform.rotation);
        proj.ownerRole = ownerRole;
    }
}