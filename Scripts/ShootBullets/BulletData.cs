using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BulletData : NetworkBehaviour
{
    private NetworkVariable<ulong> owner = new(999);
    private NetworkVariable<bool> isActiveSelf = new(true);

    public static event Action<(ulong from, ulong to)> OnHitPlayer; 

    private const int MAX_FLY_TIME = 3;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            DeactivateSelfDelay();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetOwnershipServerRpc(ulong id)
    {
        this.owner.Value = id;
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetBulletIsActiveServerRpc(bool isActive)
    {
        var networkObject = GetComponent<NetworkObject>();
        if (networkObject == null)
        {
            Debug.LogError("NetworkObject component is missing.");
            return;
        }

        if (!networkObject.IsSpawned)
        {
            Debug.LogError("NetworkObject is not spawned. Cannot call RPC.");
            return;
        }

        isActiveSelf.Value = isActive;

        if (isActive == false)
        {
            Debug.Log("Despawning bullet.");
            networkObject.Despawn();
        }
        else
        {
            Debug.Log("Spawning bullet.");
            networkObject.Spawn();
        }
    }

    public void DeactivateSelfDelay()
    {
        StartCoroutine(DeactivateSelfDelayCoroutine());
    }

    private IEnumerator DeactivateSelfDelayCoroutine()
    {
        yield return new WaitForSeconds(MAX_FLY_TIME);
        SetBulletIsActiveServerRpc(false);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (IsServer)
        {
            if (collision.transform.TryGetComponent(out NetworkObject networkObject))
            {
                if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
                {
                    Debug.Log("Bullet has Collision to player");
                    (ulong, ulong) fromShooterToHit = new(owner.Value, networkObject.OwnerClientId);
                    OnHitPlayer?.Invoke(fromShooterToHit);
                    SetBulletIsActiveServerRpc(false);
                    return;
                }
            }
            else
            {
                Debug.Log("Collision with non-networked object.");
                SetBulletIsActiveServerRpc(false);
            }
        }
    }
}
