using UnityEngine;
using UnityEngine.AI;

namespace HyperSalchicha.Enemies
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HyperSalchicha/Enemies/Spawn Point")]
    public class SpawnPoint : MonoBehaviour
    {
        [Header("Type Support")]
        [SerializeField] private bool supportsGround = true;
        [SerializeField] private bool supportsAir;
        [SerializeField] private bool supportsBrute;

        [Header("Distance")]
        [SerializeField] private float minDistanceToPlayer = 10f;
        [SerializeField] private float maxDistanceToPlayer = 60f;

        [Header("Space")]
        [SerializeField] private float minOpenRadius;
        [SerializeField] private float minCeilingHeight;
        [SerializeField] private LayerMask obstructionMask = ~0;
        [SerializeField] private float groundProbeDistance = 3f;
        [SerializeField] private float navMeshProbeDistance = 2f;

        [Header("Zone")]
        [SerializeField] private int zoneId = -1;
        [SerializeField] private float weight = 1f;

        public int ZoneId => zoneId;
        public float Weight => Mathf.Max(0.01f, weight);

        public bool SupportsType(EnemyType type)
        {
            return type switch
            {
                EnemyType.Normal => supportsGround,
                EnemyType.Flyer => supportsAir,
                EnemyType.Brute => supportsBrute,
                _ => false,
            };
        }

        public bool IsValidFor(EnemyType type, Transform playerTarget)
        {
            if (!SupportsType(type))
                return false;
            if (!IsWithinDistance(playerTarget))
                return false;
            if (!HasRequiredOpenArea())
                return false;
            if (!HasRequiredCeiling())
                return false;
            if ((type == EnemyType.Normal || type == EnemyType.Brute) && !HasGroundBelow())
                return false;
            if ((type == EnemyType.Normal || type == EnemyType.Brute) && !HasNavigablePosition())
                return false;

            return true;
        }

        private bool IsWithinDistance(Transform playerTarget)
        {
            if (playerTarget == null)
                return true;

            float distance = Vector3.Distance(playerTarget.position, transform.position);
            if (minDistanceToPlayer > 0f && distance < minDistanceToPlayer)
                return false;
            if (maxDistanceToPlayer > 0f && distance > maxDistanceToPlayer)
                return false;

            return true;
        }

        private bool HasRequiredOpenArea()
        {
            if (minOpenRadius <= 0f)
                return true;

            Collider[] hits = Physics.OverlapSphere(
                transform.position,
                minOpenRadius,
                obstructionMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hits.Length; i++)
            {
                Collider hit = hits[i];
                if (hit == null)
                    continue;
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;

                return false;
            }

            return true;
        }

        private bool HasRequiredCeiling()
        {
            if (minCeilingHeight <= 0f)
                return true;

            return !Physics.Raycast(
                transform.position,
                Vector3.up,
                minCeilingHeight,
                obstructionMask,
                QueryTriggerInteraction.Ignore);
        }

        private bool HasGroundBelow()
        {
            return Physics.Raycast(
                transform.position + Vector3.up * 0.25f,
                Vector3.down,
                groundProbeDistance,
                ~0,
                QueryTriggerInteraction.Ignore);
        }

        private bool HasNavigablePosition()
        {
            return NavMesh.SamplePosition(
                transform.position,
                out _,
                Mathf.Max(0.1f, navMeshProbeDistance),
                NavMesh.AllAreas);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = supportsGround ? new Color(0.3f, 1f, 0.3f, 0.9f) : new Color(0.4f, 0.4f, 0.4f, 0.4f);
            Gizmos.DrawWireSphere(transform.position, 0.4f);

            if (minDistanceToPlayer > 0f)
            {
                Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.25f);
                Gizmos.DrawWireSphere(transform.position, minDistanceToPlayer);
            }

            if (maxDistanceToPlayer > 0f)
            {
                Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.2f);
                Gizmos.DrawWireSphere(transform.position, maxDistanceToPlayer);
            }

            if (minOpenRadius > 0f)
            {
                Gizmos.color = new Color(1f, 0.4f, 0.25f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, minOpenRadius);
            }

            if (minCeilingHeight > 0f)
            {
                Gizmos.color = new Color(0.6f, 0.9f, 1f, 0.5f);
                Gizmos.DrawLine(transform.position, transform.position + Vector3.up * minCeilingHeight);
            }
        }
    }
}
