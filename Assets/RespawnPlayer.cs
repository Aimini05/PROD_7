using UnityEngine;

public class RespawnPlayer : MonoBehaviour
{
    public Transform respawnPoint;

    public void Respawn()
    {
        transform.position = respawnPoint.position;
        transform.rotation = respawnPoint.rotation;

        Debug.Log("Player respawned!");
    }
}