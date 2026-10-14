using DG.Tweening;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class CloudSpawner : MonoBehaviour
{
    [Header("Cloud")]
    [SerializeField] private GameObject cloudPrefab;
    [SerializeField] private Sprite[] cloudSprites = new Sprite[2];
    [SerializeField] private Transform cloudContainer;
    [Min(0.01f)] [SerializeField] private float testCloudScaleMultiplier = 1.6f;
    [SerializeField] private bool logCloudScaleSelection;

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

        Sprite selectedSprite = cloudSprites[Random.Range(0, cloudSprites.Length)];
        visual.sprite = selectedSprite;

        Vector3 baseScale = visual.transform.localScale;
        float scaleMultiplier = IsTestCloudSprite(selectedSprite)
            ? Mathf.Max(0.01f, testCloudScaleMultiplier)
            : 1f;
        visual.transform.localScale = new Vector3(
            baseScale.x * scaleMultiplier,
            baseScale.y * scaleMultiplier,
            baseScale.z);

        if (logCloudScaleSelection)
        {
            string spriteName = selectedSprite != null ? selectedSprite.name : "<null>";
            Debug.Log(
                $"[CloudSpawner] Sprite={spriteName}, " +
                $"testMultiplier={testCloudScaleMultiplier:0.###}, " +
                $"finalScale={visual.transform.localScale}",
                cloud);
        }

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

    private static bool IsTestCloudSprite(Sprite sprite)
    {
        return sprite != null &&
               sprite.name.StartsWith("cloud-", System.StringComparison.OrdinalIgnoreCase);
    }
}
