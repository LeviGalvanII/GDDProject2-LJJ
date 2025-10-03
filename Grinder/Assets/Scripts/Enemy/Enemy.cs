using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))] // ensures a 2D collider exists on the prefab
public class Enemy : MonoBehaviour
{
    #region SerializedFields
    [Header("Lifetime")]
    [SerializeField, Tooltip("Seconds before this enemy despawns if not clicked.")]
    private float m_despawnTime = 3f;

    [Header("Spawn Area Mode")]
    [SerializeField, Tooltip("If true, spawn inside the camera's current visible area. If false, use manual half-extents below around the chosen center.")]
    private bool m_useCameraBounds = true;

    [SerializeField, Tooltip("Optional explicit camera. If null, uses Camera.main.")]
    private Camera m_camera;

    [SerializeField, Tooltip("Screen-edge padding (world units) to keep enemies away from the very edge). Only used when using camera bounds.")]
    private float m_screenPadding = 0.1f;

    [Header("Manual Bounds (if not using camera bounds)")]
    [SerializeField, Tooltip("Horizontal half-extent: spawns between center.x - X and +X.")]
    private float m_horizontalBound = 10f;

    [SerializeField, Tooltip("Vertical half-extent: spawns between center.y - Y and +Y.")]
    private float m_verticalBound = 5f;

    [Header("Spawn Separation")]
    [SerializeField, Tooltip("Extra padding (world units) to keep between enemies.")]
    private float m_padding = 0.15f;

    [SerializeField, Tooltip("Max attempts to find a clear spot before giving up.")]
    private int m_maxTries = 20;

    [SerializeField, Tooltip("Layers considered 'other enemies' when checking overlap. Auto-set in Awake if left empty.")]
    private LayerMask m_overlapMask;

    [Header("Optional Visuals")]
    [SerializeField, Tooltip("Optional SpriteRenderer cache (auto-filled if left empty).")]
    private SpriteRenderer m_spriteRenderer;
    #endregion

    #region PrivateState
    private float m_elapsedTime;
    private Vector2 m_startingPosition;
    private Collider2D m_col;
    #endregion

    #region UnityEvents
    private void Awake()
    {
        // Cache components once
        m_col = GetComponent<Collider2D>();
        if (m_spriteRenderer == null) m_spriteRenderer = GetComponent<SpriteRenderer>();

        // For click/overlap detection we usually want triggers, not physics collisions.
        if (m_col != null) m_col.isTrigger = true;

        // Auto-configure overlap mask if not set in Inspector
        if (m_overlapMask.value == 0)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            m_overlapMask = (enemyLayer >= 0) ? (1 << enemyLayer) : ~0; // ~0 == Everything
        }
    }

    private void OnEnable()
    {
        // 1) Compute spawn area center & half-extents
        Vector2 areaCenter;
        Vector2 areaHalf;
        GetSpawnArea(out areaCenter, out areaHalf);

        // 2) Shrink spawn area by this enemy's half-size so it never clips the edges
        Vector2 half = GetHalfExtentsWorld();
        Vector2 safeHalf = new Vector2(
            Mathf.Max(0f, areaHalf.x - half.x),
            Mathf.Max(0f, areaHalf.y - half.y)
        );

        float minX = areaCenter.x - safeHalf.x;
        float maxX = areaCenter.x + safeHalf.x;
        float minY = areaCenter.y - safeHalf.y;
        float maxY = areaCenter.y + safeHalf.y;

        // 3) Separation check box = sprite/collider size + padding all around
        Vector2 checkSize = (half * 2f) + new Vector2(m_padding * 2f, m_padding * 2f);

        bool placed = false;
        for (int attempt = 0; attempt < m_maxTries; attempt++)
        {
            float x = Random.Range(minX, maxX);
            float y = Random.Range(minY, maxY);
            Vector2 candidate = new Vector2(x, y);

            // Find anything else overlapping our test box
            Collider2D[] hits = Physics2D.OverlapBoxAll(candidate, checkSize, 0f, m_overlapMask);

            bool overlapsOther = false;
            foreach (var h in hits)
            {
                if (h == null || h == m_col) continue;
                // Only treat other ENEMIES as blocking (filter by tags)
                if (h.CompareTag("GoodEnemy") || h.CompareTag("BadEnemy"))
                {
                    overlapsOther = true;
                    break;
                }
            }

            if (!overlapsOther)
            {
                transform.position = candidate;
                placed = true;
                break;
            }
        }

        // 4) Fallback: clamp whatever position we currently have into the safe area
        if (!placed)
        {
            float cx = Mathf.Clamp(transform.position.x, minX, maxX);
            float cy = Mathf.Clamp(transform.position.y, minY, maxY);
            transform.position = new Vector2(cx, cy);
        }

        m_startingPosition = transform.position;
        m_elapsedTime = 0f;
    }

    private void Update()
    {
        // Use deltaTime for frame-rate–independent timers
        m_elapsedTime += Time.deltaTime;

        if (m_elapsedTime >= m_despawnTime)
        {
            // Preferred over Destroy so we can reuse pooled objects later
            gameObject.SetActive(false);
        }
    }
    #endregion

    #region Helpers
    private Vector2 GetHalfExtentsWorld()
    {
        // Prefer collider size; fallback to sprite size; else 0.
        if (m_col != null) return m_col.bounds.extents;
        if (m_spriteRenderer != null) return m_spriteRenderer.bounds.extents;
        return Vector2.zero;
    }

    /// <summary>
    /// Determines the spawn area. If m_useCameraBounds, uses the camera's visible rect.
    /// Otherwise, uses manual half-extents around a center (camera center if available, else (0,0)).
    /// Returns areaCenter and areaHalf (half-width, half-height).
    /// </summary>
    private void GetSpawnArea(out Vector2 areaCenter, out Vector2 areaHalf)
    {
        Camera cam = m_camera != null ? m_camera : Camera.main;

        if (m_useCameraBounds && cam != null)
        {
            if (cam.orthographic)
            {
                // Orthographic: half-height = orthographicSize, half-width = size * aspect
                float hh = cam.orthographicSize;
                float hw = hh * cam.aspect;
                areaCenter = (Vector2)cam.transform.position;
                areaHalf = new Vector2(hw, hh);

                // Apply screen padding
                areaHalf = new Vector2(
                    Mathf.Max(0f, areaHalf.x - m_screenPadding),
                    Mathf.Max(0f, areaHalf.y - m_screenPadding)
                );
            }
            else
            {
                // Perspective: project screen corners to world at the enemy's Z plane
                float zDist = Mathf.Abs(cam.transform.position.z - transform.position.z);
                Vector3 bl = cam.ScreenToWorldPoint(new Vector3(0f, 0f, zDist));
                Vector3 tr = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, zDist));

                Vector2 size = new Vector2(Mathf.Abs(tr.x - bl.x), Mathf.Abs(tr.y - bl.y));
                areaCenter = (Vector2)((bl + tr) * 0.5f);
                areaHalf = 0.5f * size;

                areaHalf = new Vector2(
                    Mathf.Max(0f, areaHalf.x - m_screenPadding),
                    Mathf.Max(0f, areaHalf.y - m_screenPadding)
                );
            }
        }
        else
        {
            // Manual mode: use half-extents around a center (camera center if available, else origin)
            Vector2 center = cam != null ? (Vector2)cam.transform.position : Vector2.zero;
            areaCenter = center;
            areaHalf = new Vector2(m_horizontalBound, m_verticalBound);
        }
    }
    #endregion

    #region PublicAPI
    public void Kill()
    {
        gameObject.SetActive(false);
    }

    public void SetTint(Color c)
    {
        if (m_spriteRenderer != null) m_spriteRenderer.color = c;
    }
    #endregion

    #region Gizmos
    // Visualize the spawn area used (camera rect or manual)
    private void OnDrawGizmosSelected()
    {
        Vector2 center, half;
        GetSpawnArea(out center, out half);

        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.35f);
        var size = new Vector3(half.x * 2f, half.y * 2f, 0f);
        Gizmos.DrawWireCube(new Vector3(center.x, center.y, 0f), size);
    }
    #endregion

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.I.Death();
            gameObject.SetActive(false);
        }
    }


}
