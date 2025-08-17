using UnityEngine;

public class RockExplosion : MonoBehaviour
{
    public float explosionForce = 500f;
    public float explosionRadius = 5f;
    public float upwardsModifier = 0.5f;

    private bool exploded = false;

    private void OnEnable()
    {
        Explode();
    }

    private void Explode()
    {
        if (exploded) return;
        exploded = true;

        foreach (Transform piece in transform)
        {
            Rigidbody rb = piece.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = piece.gameObject.AddComponent<Rigidbody>();
            }

            rb.AddExplosionForce(
                explosionForce,
                transform.position,
                explosionRadius,
                upwardsModifier,
                ForceMode.Impulse
            );

            piece.SetParent(null);
        }

        Destroy(gameObject);
    }
}
