using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class SpawnManager : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The different types of enemies that should be spawned and their corresponding spawn information.")]
    private EnemySpawnInfo[] m_EnemyTypes;

    private float[] m_EnemySpawnTimers;

    private void Awake()
    {
        int len = (m_EnemyTypes == null) ? 0 : m_EnemyTypes.Length;
        m_EnemySpawnTimers = new float[len];

        for (int i = 0; i < len; i++)
            m_EnemySpawnTimers[i] = Mathf.Max(0f, m_EnemyTypes[i].FirstSpawnTime);
    }

    private void Update()
    {
        if (m_EnemySpawnTimers == null || m_EnemySpawnTimers.Length == 0) return;

        for (int i = 0; i < m_EnemySpawnTimers.Length; i++)
        {
            var info = m_EnemyTypes[i];
            if (info.EnemyPrefab == null) continue;

            if (m_EnemySpawnTimers[i] <= 0f)
            {
                // Spawn
                var go = Instantiate(info.EnemyPrefab);

                // If it has a side-launcher, set its side mode (Left/Right/Both)
                var launcher = go.GetComponent<EnemyLauncher2D>();
                if (launcher) launcher.SetSide(info.Side);

                // Reset timer
                float period = (info.SpawnRate > 0f) ? (1f / info.SpawnRate) : Mathf.Infinity;
                m_EnemySpawnTimers[i] = period;
            }
            else
            {
                m_EnemySpawnTimers[i] -= Time.deltaTime;
            }
        }
    }
}

[System.Serializable]
public struct EnemySpawnInfo
{
    [SerializeField]
    [Tooltip("The enemy prefab to spawn.")]
    private GameObject m_EnemyPrefab;

    [SerializeField]
    [Tooltip("The time we should wait before the first enemy is spawned.")]
    private float m_FirstSpawnTime;

    [SerializeField, Range(0, 100)]
    [Tooltip("How many enemies should spawn per second.")]
    private float m_SpawnRate;

    [SerializeField]
    [Tooltip("Which side(s) this enemy type should come from.")]
    private EnemyLauncher2D.SideMode m_Side;

    public GameObject EnemyPrefab => m_EnemyPrefab;
    public float FirstSpawnTime => m_FirstSpawnTime;
    public float SpawnRate => m_SpawnRate;
    public EnemyLauncher2D.SideMode Side => m_Side;
}
