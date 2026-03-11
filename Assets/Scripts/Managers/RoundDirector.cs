using HyperSalchicha.Enemies;
using UnityEngine;

public class RoundDirector : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpawnDirector spawnDirector;
    [SerializeField] private GameManager gameManager;

    [Header("Flow")]
    [SerializeField] private int startingRound = 1;
    [SerializeField] private bool startWithInitialIntermission;
    [SerializeField] private float intermissionDuration = 18f;

    [Header("Normal Quota")]
    [SerializeField] private float normalQuotaBase = 6f;
    [SerializeField] private float normalQuotaLinear = 1.8f;
    [SerializeField] private float normalQuotaQuadratic = 0.06f;

    [Header("Flyer Quota")]
    [SerializeField] private int flyerIntroRound = 5;
    [SerializeField] private int flyerMinQuotaOnIntro = 2;
    [SerializeField] private float flyerRatioStart = 0.15f;
    [SerializeField] private float flyerRatioEnd = 0.35f;
    [SerializeField] private float flyerRatioMaxRound = 30f;

    [Header("Brute Quota")]
    [SerializeField] private int bruteIntroRound = 10;
    [SerializeField] private int bruteRoundCadence = 5;

    [Header("Alive Caps")]
    [SerializeField] private int aliveTotalCapBase = 6;
    [SerializeField] private float aliveTotalCapPerRound = 0.5f;
    [SerializeField] private int aliveTotalCapMax = 20;
    [SerializeField] private int flyerCapRound10 = 2;
    [SerializeField] private int flyerCapRound20 = 3;
    [SerializeField] private int flyerCapRound30 = 4;
    [SerializeField] private int flyerCapLate = 6;
    [SerializeField] private int bruteCapRound20 = 1;
    [SerializeField] private int bruteCapRound40 = 2;
    [SerializeField] private int bruteCapLate = 3;

    [Header("Spawn Timing")]
    [SerializeField] private float baseSpawnIntervalStart = 2.2f;
    [SerializeField] private float baseSpawnIntervalSlope = 0.05f;
    [SerializeField] private float baseSpawnIntervalMin = 0.75f;

    [Header("Normal Run Chance")]
    [SerializeField] private float runChanceStart = 0.15f;
    [SerializeField] private float runChancePerRound = 0.095f;

    [Header("Health Scaling")]
    [SerializeField] private AnimationCurve healthMultiplierByRound;
    [SerializeField] private float flyerHealthMultiplier = 0.8f;
    [SerializeField] private float bruteHealthMultiplier = 3.5f;

    [Header("Power-Up Drops")]
    [SerializeField] [Range(0f, 1f)] private float enemyPowerUpDropChance = 0.05f;
    [SerializeField] private GameObject powerUpPrefabA;
    [SerializeField] private GameObject powerUpPrefabB;

    private int currentRound;
    private int remainingNormals;
    private int remainingFlyers;
    private int remainingBrutes;
    private int aliveNormals;
    private int aliveFlyers;
    private int aliveBrutes;
    private int spawnedThisRound;
    private int totalToSpawnThisRound;
    private float intermissionTimer;

    public RoundState CurrentState { get; private set; } = RoundState.Intermission;
    public RoundConfig CurrentConfig { get; private set; }
    public int CurrentRound => currentRound;
    public int RemainingNormals => remainingNormals;
    public int RemainingFlyers => remainingFlyers;
    public int RemainingBrutes => remainingBrutes;
    public int RemainingTotal => remainingNormals + remainingFlyers + remainingBrutes;
    public int AliveNormals => aliveNormals;
    public int AliveFlyers => aliveFlyers;
    public int AliveBrutes => aliveBrutes;
    public int AliveTotal => aliveNormals + aliveFlyers + aliveBrutes;
    public int SpawnedThisRound => spawnedThisRound;
    public int TotalToSpawnThisRound => totalToSpawnThisRound;
    public float RoundProgress => totalToSpawnThisRound > 0 ? spawnedThisRound / (float)totalToSpawnThisRound : 1f;
    public float IntermissionRemainingSeconds => intermissionTimer;
    public float FlyerHealthMultiplier => flyerHealthMultiplier;
    public float BruteHealthMultiplier => bruteHealthMultiplier;
    public float EnemyPowerUpDropChance => Mathf.Clamp01(enemyPowerUpDropChance);

    private void Awake()
    {
        if (spawnDirector == null)
            spawnDirector = FindFirstObjectByType<SpawnDirector>();
        if (gameManager == null)
            gameManager = GameManager.Instance != null ? GameManager.Instance : FindFirstObjectByType<GameManager>();

        EnsureDefaultHealthCurve();
        if (spawnDirector != null)
            spawnDirector.SetRoundDirector(this);
    }

    private void Start()
    {
        StartRun();
    }

    private void Update()
    {
        if (CurrentState == RoundState.Intermission)
        {
            intermissionTimer -= Time.deltaTime;
            if (intermissionTimer <= 0f)
                BeginCurrentRound();
            return;
        }

        if (CurrentState == RoundState.Spawning && RemainingTotal <= 0)
            CurrentState = RoundState.Cleanup;

        if (RemainingTotal <= 0 && AliveTotal <= 0)
            CompleteRound();
    }

    public void SetSpawnDirector(SpawnDirector director)
    {
        spawnDirector = director;
        if (spawnDirector != null)
            spawnDirector.SetRoundDirector(this);
    }

    public RoundConfig BuildPreviewConfig(int round)
    {
        return BuildRoundConfig(Mathf.Max(1, round));
    }

    public bool CanSpawnNormal()
    {
        return remainingNormals > 0 && aliveNormals < CurrentConfig.normalAliveCap;
    }

    public bool CanSpawnFlyer()
    {
        return remainingFlyers > 0 && aliveFlyers < CurrentConfig.flyerAliveCap;
    }

    public bool CanSpawnBrute()
    {
        return remainingBrutes > 0 && aliveBrutes < CurrentConfig.bruteAliveCap;
    }

    public void NotifyEnemySpawned(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Normal:
                if (remainingNormals > 0)
                    remainingNormals--;
                aliveNormals++;
                break;
            case EnemyType.Flyer:
                if (remainingFlyers > 0)
                    remainingFlyers--;
                aliveFlyers++;
                break;
            case EnemyType.Brute:
                if (remainingBrutes > 0)
                    remainingBrutes--;
                aliveBrutes++;
                break;
        }

        spawnedThisRound = Mathf.Min(totalToSpawnThisRound, spawnedThisRound + 1);
        if (RemainingTotal <= 0 && CurrentState == RoundState.Spawning)
            CurrentState = RoundState.Cleanup;
    }

    public void NotifyEnemyKilled(EnemyType type)
    {
        switch (type)
        {
            case EnemyType.Normal:
                aliveNormals = Mathf.Max(0, aliveNormals - 1);
                break;
            case EnemyType.Flyer:
                aliveFlyers = Mathf.Max(0, aliveFlyers - 1);
                break;
            case EnemyType.Brute:
                aliveBrutes = Mathf.Max(0, aliveBrutes - 1);
                break;
        }
    }

    public void TrySpawnEnemyPowerUp(Vector3 position)
    {
        if (UnityEngine.Random.value > EnemyPowerUpDropChance)
            return;

        GameObject prefabToSpawn = SelectRandomPowerUpPrefab();
        if (prefabToSpawn == null)
            return;

        Instantiate(prefabToSpawn, position, prefabToSpawn.transform.rotation);
    }

    private void StartRun()
    {
        currentRound = Mathf.Max(1, startingRound);
        PrepareRound(currentRound);

        if (spawnDirector != null)
            spawnDirector.PrewarmPools();

        if (startWithInitialIntermission)
            EnterIntermission();
        else
            BeginCurrentRound();
    }

    private void PrepareRound(int round)
    {
        currentRound = Mathf.Max(1, round);
        CurrentConfig = BuildRoundConfig(currentRound);
        remainingNormals = CurrentConfig.normalQuota;
        remainingFlyers = CurrentConfig.flyerQuota;
        remainingBrutes = CurrentConfig.bruteQuota;
        aliveNormals = 0;
        aliveFlyers = 0;
        aliveBrutes = 0;
        spawnedThisRound = 0;
        totalToSpawnThisRound = CurrentConfig.totalQuota;

        if (gameManager != null)
            gameManager.SetRound(currentRound);
    }

    private void BeginCurrentRound()
    {
        CurrentState = RoundState.Spawning;
        if (spawnDirector != null)
            spawnDirector.BeginRound(CurrentConfig);
    }

    private void EnterIntermission()
    {
        CurrentState = RoundState.Intermission;
        intermissionTimer = Mathf.Max(0f, intermissionDuration);
        spawnDirector?.StopSpawning();
    }

    private void CompleteRound()
    {
        currentRound++;
        PrepareRound(currentRound);
        EnterIntermission();
    }

    private RoundConfig BuildRoundConfig(int round)
    {
        int safeRound = Mathf.Max(1, round);
        int normalQuota = Mathf.RoundToInt(
            normalQuotaBase +
            normalQuotaLinear * (safeRound - 1) +
            normalQuotaQuadratic * Mathf.Pow(safeRound - 1, 2f));

        int flyerQuota = 0;
        if (safeRound >= flyerIntroRound)
        {
            float t = Mathf.InverseLerp(flyerIntroRound, Mathf.Max(flyerIntroRound + 1, flyerRatioMaxRound), safeRound);
            float ratio = Mathf.Lerp(flyerRatioStart, flyerRatioEnd, t);
            flyerQuota = Mathf.Max(flyerMinQuotaOnIntro, Mathf.RoundToInt(normalQuota * ratio));
        }

        int bruteQuota = 0;
        if (safeRound >= bruteIntroRound && safeRound % Mathf.Max(1, bruteRoundCadence) == 0)
            bruteQuota = Mathf.FloorToInt(safeRound / 10f);

        int aliveTotalCap = Mathf.Min(
            aliveTotalCapMax,
            aliveTotalCapBase + Mathf.FloorToInt((safeRound - 1) * aliveTotalCapPerRound));

        int flyerAliveCap = 0;
        if (safeRound >= flyerIntroRound && safeRound < 10)
            flyerAliveCap = flyerCapRound10;
        else if (safeRound >= 10 && safeRound < 20)
            flyerAliveCap = flyerCapRound20;
        else if (safeRound >= 20 && safeRound < 30)
            flyerAliveCap = flyerCapRound30;
        else if (safeRound >= 30)
            flyerAliveCap = flyerCapLate;

        int bruteAliveCap = 0;
        if (safeRound >= bruteIntroRound && safeRound < 20)
            bruteAliveCap = bruteCapRound20;
        else if (safeRound >= 20 && safeRound < 40)
            bruteAliveCap = bruteCapRound40;
        else if (safeRound >= 40)
            bruteAliveCap = bruteCapLate;

        int normalAliveCap = Mathf.Max(1, aliveTotalCap - flyerAliveCap - bruteAliveCap);
        float baseSpawnInterval = Mathf.Max(baseSpawnIntervalMin, baseSpawnIntervalStart - baseSpawnIntervalSlope * (safeRound - 1));
        float runChance = Mathf.Clamp01(runChanceStart + runChancePerRound * (safeRound - 1));
        float healthMultiplier = Mathf.Max(0.1f, healthMultiplierByRound.Evaluate(safeRound));

        return new RoundConfig
        {
            roundNumber = safeRound,
            normalQuota = normalQuota,
            flyerQuota = flyerQuota,
            bruteQuota = bruteQuota,
            totalQuota = normalQuota + flyerQuota + bruteQuota,
            aliveTotalCap = aliveTotalCap,
            flyerAliveCap = flyerAliveCap,
            bruteAliveCap = bruteAliveCap,
            normalAliveCap = normalAliveCap,
            baseSpawnInterval = baseSpawnInterval,
            runChance = runChance,
            healthMultiplier = healthMultiplier,
        };
    }

    private void EnsureDefaultHealthCurve()
    {
        if (healthMultiplierByRound != null && healthMultiplierByRound.length > 0)
            return;

        healthMultiplierByRound = new AnimationCurve(
            new Keyframe(1f, 1f),
            new Keyframe(5f, 5f),
            new Keyframe(10f, 10f),
            new Keyframe(20f, 18f),
            new Keyframe(30f, 28f));
    }

    private GameObject SelectRandomPowerUpPrefab()
    {
        bool hasA = powerUpPrefabA != null;
        bool hasB = powerUpPrefabB != null;

        if (!hasA && !hasB)
            return null;
        if (hasA && !hasB)
            return powerUpPrefabA;
        if (!hasA && hasB)
            return powerUpPrefabB;

        return UnityEngine.Random.value < 0.5f ? powerUpPrefabA : powerUpPrefabB;
    }
}
