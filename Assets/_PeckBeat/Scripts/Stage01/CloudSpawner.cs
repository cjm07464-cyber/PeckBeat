using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CloudSpawner : MonoBehaviour
{
    [Header("Cloud")]
    [SerializeField] private GameObject cloudPrefab;
    [SerializeField] private Sprite[] cloudSprites = new Sprite[2];
    [SerializeField] private Transform cloudContainer;

    [Header("Lanes")]
    [SerializeField] private Transform[] spawnPoints = new Transform[2];
    [SerializeField] private Transform[] exitPoints = new Transform[2];

    [Header("Random Speed")]
    [Min(0.01f)] [SerializeField] private float minSpeed = 1f;
    [Min(0.01f)] [SerializeField] private float maxSpeed = 2f;

    private bool[] laneOccupied;

    private void OnEnable()
    {
        int laneCount = Mathf.Min(spawnPoints?.Length ?? 0, exitPoints?.Length ?? 0);
        laneOccupied = new bool[laneCount];
    }

    private void Update()
    {
        if (laneOccupied == null)
        {
            return;
        }

        for (int lane = 0; lane < laneOccupied.Length; lane++)
        {
            if (!laneOccupied[lane])
            {
                SpawnCloud(lane);
            }
        }
    }

    private void SpawnCloud(int lane)
    {
        if (cloudPrefab == null || cloudContainer == null || cloudSprites == null ||
            cloudSprites.Length == 0)
        {
            return;
        }

        Transform spawnPoint = spawnPoints[lane];
        Transform exitPoint = exitPoints[lane];
        if (spawnPoint == null || exitPoint == null)
        {
            return;
        }

        GameObject cloud = Instantiate(
            cloudPrefab,
            spawnPoint.position,
            Quaternion.identity,
            cloudContainer);

        SpriteRenderer visual = cloud.GetComponentInChildren<SpriteRenderer>();
        if (visual == null)
        {
            Destroy(cloud);
            return;
        }

        visual.sprite = cloudSprites[Random.Range(0, cloudSprites.Length)];

        float speedMin = Mathf.Max(0.01f, Mathf.Min(minSpeed, maxSpeed));
        float speedMax = Mathf.Max(speedMin, Mathf.Max(minSpeed, maxSpeed));
        float speed = Random.Range(speedMin, speedMax);
        float duration = Vector3.Distance(spawnPoint.position, exitPoint.position) / speed;

        laneOccupied[lane] = true;
        cloud.transform.DOMove(exitPoint.position, duration)
            .SetEase(Ease.Linear)
            .SetLink(cloud, LinkBehaviour.KillOnDestroy)
            .OnComplete(() =>
            {
                Destroy(cloud);
                laneOccupied[lane] = false;
            });
    }
}
