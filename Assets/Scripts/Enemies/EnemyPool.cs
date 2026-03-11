using System.Collections.Generic;
using UnityEngine;

namespace HyperSalchicha.Enemies
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HyperSalchicha/Enemies/Enemy Pool")]
    public class EnemyPool : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private EnemyBase normalPrefab;
        [SerializeField] private EnemyBase flyerPrefab;
        [SerializeField] private EnemyBase brutePrefab;

        [Header("Pooling")]
        [SerializeField] private Transform pooledRoot;
        [SerializeField] private bool autoAttachPlaceholderBehaviours = true;

        private readonly Dictionary<EnemyType, Queue<EnemyBase>> availableByType = new();
        private readonly Dictionary<EnemyType, int> totalCreatedByType = new();

        private void Awake()
        {
            EnsureCollections();
            if (pooledRoot == null)
                pooledRoot = transform;
        }

        public void Prewarm(int normalCount, int flyerCount, int bruteCount)
        {
            EnsureCapacity(EnemyType.Normal, normalCount);
            EnsureCapacity(EnemyType.Flyer, flyerCount);
            EnsureCapacity(EnemyType.Brute, bruteCount);
        }

        public EnemyBase Get(EnemyType type)
        {
            EnsureCollections();
            Queue<EnemyBase> queue = availableByType[type];

            while (queue.Count > 0)
            {
                EnemyBase instance = queue.Dequeue();
                if (instance == null)
                    continue;

                instance.gameObject.SetActive(true);
                EnsureTypeSpecificComponents(instance, type);
                return instance;
            }

            EnemyBase created = CreateInstance(type);
            if (created != null)
                created.gameObject.SetActive(true);
            return created;
        }

        public void Release(EnemyBase enemy)
        {
            if (enemy == null)
                return;

            EnsureCollections();
            EnemyType type = enemy.Type;
            enemy.transform.SetParent(pooledRoot, false);
            enemy.gameObject.SetActive(false);
            availableByType[type].Enqueue(enemy);
        }

        private void EnsureCapacity(EnemyType type, int targetCount)
        {
            int desired = Mathf.Max(0, targetCount);
            while (totalCreatedByType[type] < desired)
                CreateInstance(type);
        }

        private EnemyBase CreateInstance(EnemyType type)
        {
            EnemyBase prefab = ResolvePrefab(type);
            if (prefab == null)
            {
                Debug.LogWarning($"[{nameof(EnemyPool)}] No hay prefab asignado para {type}.", this);
                return null;
            }

            EnemyBase instance = Instantiate(prefab, pooledRoot);
            instance.gameObject.name = $"{prefab.gameObject.name}_{type}";
            EnsureTypeSpecificComponents(instance, type);
            instance.gameObject.SetActive(false);
            availableByType[type].Enqueue(instance);
            totalCreatedByType[type]++;
            return instance;
        }

        private EnemyBase ResolvePrefab(EnemyType type)
        {
            return type switch
            {
                EnemyType.Flyer => flyerPrefab != null ? flyerPrefab : normalPrefab,
                EnemyType.Brute => brutePrefab != null ? brutePrefab : normalPrefab,
                _ => normalPrefab,
            };
        }

        private void EnsureTypeSpecificComponents(EnemyBase enemy, EnemyType type)
        {
            if (!autoAttachPlaceholderBehaviours || enemy == null)
                return;

            if (type == EnemyType.Flyer && enemy.GetComponent<EnemyFlyerPlaceholder>() == null)
                enemy.gameObject.AddComponent<EnemyFlyerPlaceholder>();
            if (type == EnemyType.Brute && enemy.GetComponent<EnemyBrutePlaceholder>() == null)
                enemy.gameObject.AddComponent<EnemyBrutePlaceholder>();
        }

        private void EnsureCollections()
        {
            EnsureTypeQueue(EnemyType.Normal);
            EnsureTypeQueue(EnemyType.Flyer);
            EnsureTypeQueue(EnemyType.Brute);
        }

        private void EnsureTypeQueue(EnemyType type)
        {
            if (!availableByType.ContainsKey(type))
                availableByType[type] = new Queue<EnemyBase>();
            if (!totalCreatedByType.ContainsKey(type))
                totalCreatedByType[type] = 0;
        }
    }
}
