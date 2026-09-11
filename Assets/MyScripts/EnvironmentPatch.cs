using UnityEngine;

public class EnvironmentPatch : MonoBehaviour
{
    [Header("Patch Settings")]

    [Tooltip("Forward length of this patch. Keep the same on all 3 patches.")]
    public float length = 40f;


    [Header("Player Spawn")]

    [Tooltip("Spawn point for the player on this patch.")]
    public Transform spawnPoint;


    // =========================================================
    // RESET PATCH CONTENTS
    // =========================================================

    public void ResetContents()
    {
        // -----------------------------------------------------
        // RESET COINS
        // -----------------------------------------------------

        Coin[] coins =
            GetComponentsInChildren<Coin>(true);


        for (int i = 0; i < coins.Length; i++)
        {
            coins[i].ResetForReuse();

            coins[i].gameObject.SetActive(true);
        }


        // -----------------------------------------------------
        // RESET OBSTACLES
        // -----------------------------------------------------

        Transform[] children =
            GetComponentsInChildren<Transform>(true);


        for (int i = 0; i < children.Length; i++)
        {
            GameObject go =
                children[i].gameObject;


            // Don't process the patch itself
            if (go == gameObject)
                continue;


            // Reactivate disabled obstacles
            if (
                go.CompareTag("Obstacle") &&
                !go.activeSelf
            )
            {
                go.SetActive(true);
            }
        }
    }


    // =========================================================
    // GET PLAYER SPAWN POINT
    // =========================================================

    public Transform GetSpawnPoint()
    {
        return spawnPoint;
    }
}
