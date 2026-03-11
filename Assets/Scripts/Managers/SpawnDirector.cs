using System;
using System.Collections.Generic;
using HyperSalchicha.Enemies;
using UnityEngine;
using UnityEngine.AI;

public class SpawnDirector : MonoBehaviour
{
    [Serializable]
    private class ZoneAdjacency
    {
        public int zoneId = -1;
        public int[] adjacentZoneIds = Array.Empty<int>();
    }

    private struct TrackedEnemyState
    {
        public Vector3 lastPosition;
        public float farTimer;
        public float stuckTimer;
    }

    [Header("References")]
    [SerializeField] private RoundDirector roundDirector;
    [SerializeField] private EnemyPool enemyPool;
    [SerializeField] private Transform playerTarget;

    [Header("Scene Data")]
    [SerializeField] private bool autoFindSpawnPoints = true;
    [SerializeField] private List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

    [Header("Spawn Timing")]
    [SerializeField] private float failedSpawnRetryDelay = 0.25f;
    [SerializeField] private float minBruteSpawnSpacing = 10f;

    [Header("Prewarm")]
    [SerializeField] private int prewarmReferenceRound = 30;
    [SerializeField] private int prewarmPoolMargin = 2;

    [Header("Recycling")]
    [SerializeField] private float normalRecycleDistance = 35f;
    [SerializeField] private float normalRecycleTime = 4f;
    [SerializeField] private float flyerRecycleDistance = 42f;
    [SerializeField] private float flyerRecycleTime = 3f;
    [SerializeField] private float bruteRecycleDistance = 55f;
    [SerializeField] private float bruteRecycleTime = 6f;
    [SerializeField] private float stuckMovementThreshold = 0.15f;
    [SerializeField] private float stuckCheckDelay = 3f;

    [Header("Zones")]
    [SerializeField] private int currentPlayerZoneId = -1;
    [SerializeField] private ZoneAdjacency[] zoneAdjacency = Array.Empty<ZoneAdjacency>();

    private readonly List<EnemyBase> activeEnemies = new();
    private readonly Dictionary<EnemyBase, TrackedEnemyState> trackedEnemies = new();
    private float spawnTimer;
    private float timeSinceLastBruteSpawn = float.MaxValue;
    private bool roundSpawningActive;

    private void Awake()
    {
        if (roundDirector == null)
            roundDirector = FindFirstObjectByType<RoundDirector>();
        if (enemyPool == null)
            enemyPool = FindFirstObjectByType<EnemyPool>();
        ResolvePlayerTarget();
        RefreshSpawnPoints();
    }

    private void Start()
    {
        if (roundDirector != null)
            roundDirector.SetSpawnDirector(this);
    }

    private void Update()
    {
        ResolvePlayerTarget();
        timeSinceLastBruteSpawn += Time.deltaTime;
        UpdateRecycling();

        if (!roundSpawningActive || roundDirector == null || roundDirector.CurrentState != RoundState.Spawning)
            return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f)
            return;

        TrySpawnNextEnemy();
    }

    public void SetRoundDirector(RoundDirector director)
    {
        roundDirector = director;
    }

    public void SetCurrentPlayerZone(int zoneId)
    {
        currentPlayerZoneId = zoneId;
    }

    public void BeginRound(RoundConfig config)
    {
        roundSpawningActive = true;
        spawnTimer = 0f;
        if (timeSinceLastBruteSpawn > minBruteSpawnSpacing)
            timeSinceLastBruteSpawn = minBruteSpawnSpacing;
    }

    public void StopSpawning()
    {
        roundSpawningActive = false;
    }

    public void PrewarmPools()
    {
        if (enemyPool == null || roundDirector == null)
            return;

        RoundConfig config = roundDirector.BuildPreviewConfig(prewarmReferenceRound);
        enemyPool.Prewarm(
            config.normalAliveCap + prewarmPoolMargin,
            config.flyerAliveCap + prewarmPoolMargin,
            config.bruteAliveCap + prewarmPoolMargin);
    }

    public void RefreshSpawnPoints()
    {
        if (!autoFindSpawnPoints)
            return;

        spawnPoints.Clear();
        spawnPoints.AddRange(FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None));
    }

    private void TrySpawnNextEnemy()
    {
        if (roundDirector.RemainingTotal <= 0)
            return;
        if (roundDirector.AliveTotal >= roundDirector.CurrentConfig.aliveTotalCap)
        {
            spawnTimer = failedSpawnRetryDelay;
            return;
        }

        EnemyType? nextType = SelectNextType();
        if (nextType == null)
        {
            spawnTimer = failedSpawnRetryDelay;
            return;
        }

        SpawnPoint point = FindValidSpawnPoint(nextType.Value);
        if (point == null)
        {
            spawnTimer = failedSpawnRetryDelay;
            return;
        }

        if (!SpawnEnemy(nextType.Value, point))
        {
            spawnTimer = failedSpawnRetryDelay;
            return;
        }

        spawnTimer = GetAdaptiveSpawnInterval();
    }

    private bool SpawnEnemy(EnemyType type, SpawnPoint point)
    {
        if (enemyPool == null)
            return false;

        EnemyBase enemy = enemyPool.Get(type);
        if (enemy == null)
            return false;

        PlaceEnemyAtSpawn(enemy, point);
        EnemySpawnData data = BuildSpawnData(type, enemy.BaseMaxHealth, false, 0f);
        enemy.Initialize(data, enemyPool, point);
        TrackEnemy(enemy);
        roundDirector.NotifyEnemySpawned(type);

        if (type == EnemyType.Brute)
            timeSinceLastBruteSpawn = 0f;

        return true;
    }

    private EnemySpawnData BuildSpawnData(EnemyType type, float baseHealth, bool recycled, float currentHealth)
    {
        float typeHealthMultiplier = 1f;
        if (type == EnemyType.Flyer)
            typeHealthMultiplier = roundDirector.FlyerHealthMultiplier;
        else if (type == EnemyType.Brute)
            typeHealthMultiplier = roundDirector.BruteHealthMultiplier;

        float maxHealth = Mathf.Max(1f, baseHealth * roundDirector.CurrentConfig.healthMultiplier * typeHealthMultiplier);
        bool startsRunning = type == EnemyType.Normal && UnityEngine.Random.value <= roundDirector.CurrentConfig.runChance;

        return new EnemySpawnData
        {
            type = type,
            round = roundDirector.CurrentRound,
            maxHealth = maxHealth,
            currentHealth = recycled && currentHealth > 0f ? currentHealth : maxHealth,
            startsRunning = startsRunning,
            isRecycled = recycled,
        };
    }

    private float GetAdaptiveSpawnInterval()
    {
        float interval = roundDirector.CurrentConfig.baseSpawnInterval;
        if (roundDirector.AliveTotal < roundDirector.CurrentConfig.aliveTotalCap * 0.6f)
            interval *= 0.85f;

        return Mathf.Max(0.05f, interval);
    }

    private EnemyType? SelectNextType()
    {
        float progress = roundDirector.RoundProgress;

        bool canNormal = roundDirector.CanSpawnNormal();
        bool canFlyer = roundDirector.CanSpawnFlyer();
        bool canBrute = roundDirector.CanSpawnBrute()
            && progress >= 0.35f
            && timeSinceLastBruteSpawn >= minBruteSpawnSpacing;

        List<(EnemyType type, float weight)> candidates = new();
        if (canNormal)
            candidates.Add((EnemyType.Normal, 1f));

        if (canFlyer)
        {
            float flyerWeight = progress < 0.2f ? 0.35f : 0.75f;
            if (roundDirector.CurrentRound >= 10 &&
                roundDirector.AliveFlyers == 0 &&
                roundDirector.RemainingFlyers > 0 &&
                progress >= 0.2f)
            {
                flyerWeight *= 3f;
            }

            candidates.Add((EnemyType.Flyer, flyerWeight));
        }

        if (canBrute)
            candidates.Add((EnemyType.Brute, 0.25f));

        if (candidates.Count == 0)
            return null;

        if (roundDirector.CurrentRound >= 20 &&
            roundDirector.AliveFlyers == 0 &&
            roundDirector.RemainingFlyers > 0 &&
            roundDirector.CanSpawnFlyer() &&
            progress >= 0.2f)
        {
            return EnemyType.Flyer;
        }

        return PickWeightedRandom(candidates);
    }

    private SpawnPoint FindValidSpawnPoint(EnemyType type)
    {
        if (spawnPoints.Count == 0)
            RefreshSpawnPoints();

        List<SpawnPoint> candidates = new();
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            SpawnPoint point = spawnPoints[i];
            if (point == null)
                continue;
            if (!point.IsValidFor(type, playerTarget))
                continue;
            candidates.Add(point);
        }

        if (candidates.Count == 0)
            return null;

        List<SpawnPoint> exactZone = GetZoneFiltered(candidates, currentPlayerZoneId);
        if (exactZone.Count > 0)
            return PickWeightedRandom(exactZone);

        List<SpawnPoint> adjacent = GetAdjacentZoneFiltered(candidates, currentPlayerZoneId);
        if (adjacent.Count > 0)
            return PickWeightedRandom(adjacent);

        return PickWeightedRandom(candidates);
    }

    private void TrackEnemy(EnemyBase enemy)
    {
        enemy.OnKilled -= HandleEnemyKilled;
        enemy.OnKilled += HandleEnemyKilled;

        if (!activeEnemies.Contains(enemy))
            activeEnemies.Add(enemy);

        trackedEnemies[enemy] = new TrackedEnemyState
        {
            lastPosition = enemy.transform.position,
            farTimer = 0f,
            stuckTimer = 0f,
        };
    }

    private void HandleEnemyKilled(EnemyBase enemy)
    {
        if (enemy == null)
            return;

        roundDirector?.NotifyEnemyKilled(enemy.Type);
        activeEnemies.Remove(enemy);
        trackedEnemies.Remove(enemy);
    }

    private void UpdateRecycling()
    {
        if (playerTarget == null || activeEnemies.Count == 0)
            return;

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            EnemyBase enemy = activeEnemies[i];
            if (enemy == null || enemy.IsDead || !enemy.gameObject.activeInHierarchy)
            {
                activeEnemies.RemoveAt(i);
                trackedEnemies.Remove(enemy);
                continue;
            }

            if (!trackedEnemies.TryGetValue(enemy, out TrackedEnemyState state))
                state = new TrackedEnemyState { lastPosition = enemy.transform.position };

            float distance = Vector3.Distance(enemy.transform.position, playerTarget.position);
            float farDistance = GetRecycleDistance(enemy.Type);
            float farTime = GetRecycleTime(enemy.Type);

            if (distance > farDistance)
                state.farTimer += Time.deltaTime;
            else
                state.farTimer = 0f;

            float moved = Vector3.Distance(enemy.transform.position, state.lastPosition);
            if (moved <= stuckMovementThreshold)
                state.stuckTimer += Time.deltaTime;
            else
                state.stuckTimer = 0f;

            state.lastPosition = enemy.transform.position;
            trackedEnemies[enemy] = state;

            bool shouldRecycle =
                state.farTimer >= farTime ||
                state.stuckTimer >= stuckCheckDelay ||
                HasInvalidPath(enemy);

            if (shouldRecycle)
                TryRecycleEnemy(enemy);
        }
    }

    private bool TryRecycleEnemy(EnemyBase enemy)
    {
        SpawnPoint point = FindValidSpawnPoint(enemy.Type);
        if (point == null)
            return false;

        EnemyRecycleState recycleState = enemy.CaptureRecycleState();
        recycleState.isRecycled = true;
        PlaceEnemyAtSpawn(enemy, point);
        enemy.Initialize(recycleState.ToSpawnData(), enemyPool, point);
        trackedEnemies[enemy] = new TrackedEnemyState
        {
            lastPosition = enemy.transform.position,
            farTimer = 0f,
            stuckTimer = 0f,
        };
        return true;
    }

    private void PlaceEnemyAtSpawn(EnemyBase enemy, SpawnPoint point)
    {
        if (enemy == null || point == null)
            return;

        if (enemy.TryGetNavMeshAgent(out NavMeshAgent agent) && agent != null && agent.enabled && agent.isOnNavMesh)
        {
            agent.Warp(point.transform.position);
        }
        else
        {
            enemy.transform.SetPositionAndRotation(point.transform.position, point.transform.rotation);
        }
    }

    private bool HasInvalidPath(EnemyBase enemy)
    {
        if (enemy.Type == EnemyType.Flyer)
            return false;

        if (!enemy.TryGetNavMeshAgent(out NavMeshAgent agent) || agent == null || !agent.enabled)
            return false;
        if (!agent.isOnNavMesh)
            return true;

        return agent.pathStatus == NavMeshPathStatus.PathInvalid;
    }

    private void ResolvePlayerTarget()
    {
        if (playerTarget != null)
            return;

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            playerTarget = player.transform;
    }

    private float GetRecycleDistance(EnemyType type)
    {
        return type switch
        {
            EnemyType.Flyer => flyerRecycleDistance,
            EnemyType.Brute => bruteRecycleDistance,
            _ => normalRecycleDistance,
        };
    }

    private float GetRecycleTime(EnemyType type)
    {
        return type switch
        {
            EnemyType.Flyer => flyerRecycleTime,
            EnemyType.Brute => bruteRecycleTime,
            _ => normalRecycleTime,
        };
    }

    private List<SpawnPoint> GetZoneFiltered(List<SpawnPoint> candidates, int zoneId)
    {
        List<SpawnPoint> filtered = new();
        if (zoneId < 0)
            return filtered;

        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].ZoneId == zoneId)
                filtered.Add(candidates[i]);
        }

        return filtered;
    }

    private List<SpawnPoint> GetAdjacentZoneFiltered(List<SpawnPoint> candidates, int zoneId)
    {
        List<SpawnPoint> filtered = new();
        if (zoneId < 0)
            return filtered;

        int[] adjacent = Array.Empty<int>();
        for (int i = 0; i < zoneAdjacency.Length; i++)
        {
            if (zoneAdjacency[i].zoneId == zoneId)
            {
                adjacent = zoneAdjacency[i].adjacentZoneIds ?? Array.Empty<int>();
                break;
            }
        }

        if (adjacent.Length == 0)
            return filtered;

        for (int i = 0; i < candidates.Count; i++)
        {
            for (int j = 0; j < adjacent.Length; j++)
            {
                if (candidates[i].ZoneId == adjacent[j])
                {
                    filtered.Add(candidates[i]);
                    break;
                }
            }
        }

        return filtered;
    }

    private static EnemyType PickWeightedRandom(List<(EnemyType type, float weight)> candidates)
    {
        float totalWeight = 0f;
        for (int i = 0; i < candidates.Count; i++)
            totalWeight += Mathf.Max(0.01f, candidates[i].weight);

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            cumulative += Mathf.Max(0.01f, candidates[i].weight);
            if (roll <= cumulative)
                return candidates[i].type;
        }

        return candidates[candidates.Count - 1].type;
    }

    private static SpawnPoint PickWeightedRandom(List<SpawnPoint> candidates)
    {
        float totalWeight = 0f;
        for (int i = 0; i < candidates.Count; i++)
            totalWeight += candidates[i].Weight;

        float roll = UnityEngine.Random.Range(0f, totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < candidates.Count; i++)
        {
            cumulative += candidates[i].Weight;
            if (roll <= cumulative)
                return candidates[i];
        }

        return candidates[candidates.Count - 1];
    }
}
