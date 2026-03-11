using System;

namespace HyperSalchicha.Enemies
{
    public enum EnemyType
    {
        Normal = 0,
        Flyer = 1,
        Brute = 2,
    }

    public enum RoundState
    {
        Intermission = 0,
        Spawning = 1,
        Cleanup = 2,
    }

    [Serializable]
    public struct RoundConfig
    {
        public int roundNumber;
        public int normalQuota;
        public int flyerQuota;
        public int bruteQuota;
        public int totalQuota;
        public int aliveTotalCap;
        public int flyerAliveCap;
        public int bruteAliveCap;
        public int normalAliveCap;
        public float baseSpawnInterval;
        public float runChance;
        public float healthMultiplier;
    }

    [Serializable]
    public struct EnemySpawnData
    {
        public EnemyType type;
        public int round;
        public float maxHealth;
        public float currentHealth;
        public bool startsRunning;
        public bool isRecycled;
    }

    [Serializable]
    public struct EnemyRecycleState
    {
        public EnemyType type;
        public int round;
        public float maxHealth;
        public float currentHealth;
        public bool startsRunning;
        public bool isRecycled;

        public EnemySpawnData ToSpawnData()
        {
            return new EnemySpawnData
            {
                type = type,
                round = round,
                maxHealth = maxHealth,
                currentHealth = currentHealth,
                startsRunning = startsRunning,
                isRecycled = isRecycled,
            };
        }
    }
}
