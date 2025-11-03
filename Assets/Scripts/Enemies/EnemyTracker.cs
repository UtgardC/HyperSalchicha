using UnityEngine;

namespace HyperManzana.Enemies
{
    [DisallowMultipleComponent]
    [AddComponentMenu("HyperManzana/Enemies/Enemy Tracker (Marker)")]
    public class EnemyTracker : MonoBehaviour
    {
        // Sin lógica: el conteo lo hace EnemyCounter por hijos del padre "Enemigos".
        // Conservamos este componente para no romper prefabs/escenas existentes.
    }
}

