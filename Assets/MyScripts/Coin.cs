using UnityEngine;

public class Coin : MonoBehaviour
{
    public int value = 1;
    public float spinSpeed = 140f;
    public float bobHeight = 0.18f;
    public float bobSpeed = 3f;

    Vector3 spawnLocalPos;
    Vector3 spawnScale;
    Vector3 baseLocalPos;
    float bobOffset;
    bool collected;
    bool spawnSaved;

    public bool IsCollected => collected;

    void Awake()
    {
        SaveSpawnPose();
    }

    void OnEnable()
    {
        collected = false;
        if (!spawnSaved)
            SaveSpawnPose();
        transform.localScale = spawnScale;
        baseLocalPos = spawnLocalPos;
        bobOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    void SaveSpawnPose()
    {
        spawnLocalPos = transform.localPosition;
        spawnScale = transform.localScale;
        spawnSaved = true;
    }

    public void ResetForReuse()
    {
        collected = false;
        if (!spawnSaved)
            SaveSpawnPose();
        transform.localScale = spawnScale;
        transform.localPosition = spawnLocalPos;
        baseLocalPos = spawnLocalPos;
    }

    void Update()
    {
        if (collected) return;

        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);
        float bob = Mathf.Sin(Time.time * bobSpeed + bobOffset) * bobHeight;
        transform.localPosition = baseLocalPos + Vector3.up * bob;
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (other.GetComponentInParent<PlayerRunner>() == null) return;

        collected = true;
        if (GameManager.Instance != null)
            GameManager.Instance.CollectCoin(this);
    }
}
