using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyLauncher2D : MonoBehaviour
{
    public enum SideMode { Left, Right, BothRandom }

    #region SerializedFields
    [Header("Camera / Spawn Lanes")]
    [SerializeField] private Camera m_camera;
    [SerializeField] private float m_screenPadding = 0.15f;   // keep away from very edges
    [SerializeField] private float m_sideSpawnOffset = 0.5f;  // how far off-screen to spawn

    [Header("Which side(s) do we shoot from?")]
    [SerializeField] private SideMode m_sideMode = SideMode.BothRandom;

    [Header("Launch Tuning (side-shot)")]
    [SerializeField] private float m_minLaunchSpeed = 8f;
    [SerializeField] private float m_maxLaunchSpeed = 12f;

    [SerializeField, Tooltip("Angle jitter (deg) around horizontal. 0 = straight; 10–20 gives slight arc.")]
    private float m_angleJitter = 12f;

    [SerializeField] private float m_gravityScale = 0f; // 0 for true horizontal shots; >0 for arcing
    [SerializeField] private float m_airDrag = 0.0f;
    [SerializeField] private float m_spinMin = -200f, m_spinMax = 200f;

    [Header("Spawn Separation (at spawn moment)")]
    [SerializeField] private float m_padding = 0.15f;
    [SerializeField] private int m_maxTries = 20;
    [SerializeField] private LayerMask m_overlapMask;

    [Header("Despawn")]
    [SerializeField] private float m_timeout = 6f;
    #endregion

    #region Private
    private Collider2D m_col;
    private Rigidbody2D m_rb;
    private SpriteRenderer m_sr;
    private float m_elapsed;
    private bool m_seen;
    #endregion

    private void Awake()
    {
        m_col = GetComponent<Collider2D>();
        m_rb = GetComponent<Rigidbody2D>();
        m_sr = GetComponent<SpriteRenderer>();

        if (m_col) m_col.isTrigger = true;

        if (m_overlapMask.value == 0)
        {
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            m_overlapMask = (enemyLayer >= 0) ? (1 << enemyLayer) : ~0;
        }
    }

    private void Start()
    {
        LaunchFromSide();
    }

    private void Update()
    {
        m_elapsed += Time.deltaTime;
        if (m_elapsed >= m_timeout)
            gameObject.SetActive(false);
    }

    private void OnBecameVisible() => m_seen = true;
    private void OnBecameInvisible() { if (m_seen) gameObject.SetActive(false); }

    public void SetSide(SideMode mode) => m_sideMode = mode;

    private void LaunchFromSide()
    {
        var cam = m_camera ? m_camera : Camera.main;

        // Visible rect center & half-extents
        Vector2 center, half;
        if (cam && cam.orthographic)
        {
            float hh = cam.orthographicSize;
            float hw = hh * cam.aspect;
            center = (Vector2)cam.transform.position;
            half = new Vector2(hw, hh);
        }
        else if (cam)
        {
            float z = Mathf.Abs(cam.transform.position.z - transform.position.z);
            Vector3 bl = cam.ScreenToWorldPoint(new Vector3(0, 0, z));
            Vector3 tr = cam.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, z));
            center = (Vector2)((bl + tr) * 0.5f);
            half = 0.5f * new Vector2(Mathf.Abs(tr.x - bl.x), Mathf.Abs(tr.y - bl.y));
        }
        else
        {
            center = Vector2.zero;
            half = new Vector2(10, 5);
        }

        // edge padding
        half = new Vector2(Mathf.Max(0f, half.x - m_screenPadding), Mathf.Max(0f, half.y - m_screenPadding));

        // choose side
        bool fromRight = m_sideMode switch
        {
            SideMode.Left => false,
            SideMode.Right => true,
            _ => (Random.value < 0.5f) // BothRandom
        };

        // compute spawn pos
        Vector2 eh = GetHalfExtentsWorld();
        float sideX = fromRight
            ? (center.x + half.x + eh.x + m_sideSpawnOffset)
            : (center.x - half.x - eh.x - m_sideSpawnOffset);

        float minY = center.y - (half.y - eh.y);
        float maxY = center.y + (half.y - eh.y);

        // separation check box
        Vector2 checkSize = (eh * 2f) + new Vector2(m_padding * 2f, m_padding * 2f);

        bool placed = false;
        for (int i = 0; i < m_maxTries; i++)
        {
            float y = Random.Range(minY, maxY);
            Vector2 candidate = new Vector2(sideX, y);

            var hits = Physics2D.OverlapBoxAll(candidate, checkSize, 0f, m_overlapMask);
            bool overlaps = false;
            foreach (var h in hits)
            {
                if (h && h != m_col && (h.CompareTag("GoodEnemy") || h.CompareTag("BadEnemy")))
                { overlaps = true; break; }
            }
            if (!overlaps)
            {
                transform.position = candidate;
                placed = true;
                break;
            }
        }
        if (!placed)
        {
            float y = Mathf.Clamp(transform.position.y, minY, maxY);
            transform.position = new Vector2(sideX, y);
        }

        // rigidbody config
        m_rb.simulated = true;
        m_rb.gravityScale = m_gravityScale;
        m_rb.linearDamping = m_airDrag;
        m_rb.angularDamping = 0.05f;
        m_rb.linearVelocity = Vector2.zero;
        m_rb.angularVelocity = 0f;

        // mostly horizontal shot
        float baseDeg = fromRight ? 180f : 0f; // 180 = left, 0 = right
        float jitter = Random.Range(-m_angleJitter, m_angleJitter);
        float angle = baseDeg + jitter;

        float speed = Random.Range(m_minLaunchSpeed, m_maxLaunchSpeed);
        float rad = angle * Mathf.Deg2Rad;
        Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        m_rb.linearVelocity = dir * speed;
        m_rb.angularVelocity = Random.Range(m_spinMin, m_spinMax);
    }

    private Vector2 GetHalfExtentsWorld()
    {
        if (m_col) return m_col.bounds.extents;
        if (m_sr) return m_sr.bounds.extents;
        return Vector2.zero;
    }
}
