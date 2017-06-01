using UnityEngine;
using System.Collections;

public class Obstacle : MonoBehaviour, IDamagable {

	public void TakeDamage(float time)
    {
        Destroy(gameObject, time);
    }
}
