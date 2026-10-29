using System.Collections;
using UnityEngine;

/// <summary>
/// Shared ballistic projectile used by ranged units (and future building shooters).
/// Homes toward a target for a track window, then completes a parabola to the last known position.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Projectile : MonoBehaviour
{
    private const float FadeDurationSeconds = 5f;
    private const int GroundMaskLayer = 6; // TagManager: GroundMask

    [SerializeField]
    [Tooltip("Arc height as a fraction of horizontal distance to the aim point.")]
    private float arcHeightFactor = 0.25f;

    private int ownerPlayerId;
    private Selectable shooter;
    private Selectable target;
    private int lightAttack;
    private int heavyAttack;
    private float speed;
    private float trackTime;

    private Vector3 startPosition;
    private Vector3 endPosition;
    private float flightProgress;
    private float elapsedTrackTime;
    private bool isLocked;
    private bool isFading;
    private bool hasInitialized;

    private Renderer[] cachedRenderers;
    private MaterialPropertyBlock propertyBlock;

    /// <summary>
    /// Configures and arms the projectile for flight.
    /// </summary>
    public void Initialize(
        int ownerPlayerId,
        Selectable shooter,
        Selectable target,
        int lightAttack,
        int heavyAttack,
        float speed,
        float trackTime)
    {
        this.ownerPlayerId = ownerPlayerId;
        this.shooter = shooter;
        this.target = target;
        this.lightAttack = lightAttack;
        this.heavyAttack = heavyAttack;
        this.speed = Mathf.Max(0.01f, speed);
        this.trackTime = Mathf.Max(0f, trackTime);

        startPosition = transform.position;
        endPosition = ResolveAimPoint(target);
        flightProgress = 0f;
        elapsedTrackTime = 0f;
        isLocked = false;
        isFading = false;
        hasInitialized = true;

        cachedRenderers = GetComponentsInChildren<Renderer>();
        propertyBlock = new MaterialPropertyBlock();

        EnsureKinematicBody();
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    private void Update()
    {
        if (!hasInitialized || isFading) return;

        if (!isLocked)
        {
            elapsedTrackTime += Time.deltaTime;
            bool targetAlive = target != null && target.state == SelectableState.Alive;
            if (targetAlive && elapsedTrackTime < trackTime)
            {
                endPosition = ResolveAimPoint(target);
            }
            else
            {
                if (targetAlive)
                {
                    endPosition = ResolveAimPoint(target);
                }
                isLocked = true;
            }
        }

        float horizontalDistance = HorizontalDistance(startPosition, endPosition);
        float duration = Mathf.Max(0.05f, horizontalDistance / speed);
        flightProgress += Time.deltaTime / duration;

        Vector3 previousPosition = transform.position;
        Vector3 nextPosition = EvaluateParabola(startPosition, endPosition, Mathf.Clamp01(flightProgress));
        transform.position = nextPosition;

        Vector3 velocity = nextPosition - previousPosition;
        if (velocity.sqrMagnitude > 0.0001f)
        {
            transform.rotation = Quaternion.LookRotation(velocity.normalized, Vector3.up);
        }

        if (flightProgress >= 1f)
        {
            BeginFade();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasInitialized || isFading || other == null) return;

        if (other.gameObject.layer == GroundMaskLayer)
        {
            BeginFade();
            return;
        }

        Selectable hitSelectable = other.GetComponentInParent<Selectable>();
        if (hitSelectable == null) return;
        if (shooter != null && hitSelectable == shooter) return;
        if (!hitSelectable.IsEnemyTo(ownerPlayerId)) return;
        if (hitSelectable.state != SelectableState.Alive) return;

        if (hitSelectable is Unit unitTarget)
        {
            unitTarget.TakeDamage(lightAttack, heavyAttack);
            Destroy(gameObject);
            return;
        }

        if (hitSelectable is Building buildingTarget)
        {
            buildingTarget.TakeDamage(lightAttack, heavyAttack);
            Destroy(gameObject);
        }
    }

    private void BeginFade()
    {
        if (isFading) return;
        isFading = true;
        StartCoroutine(FadeAndDestroy());
    }

    private IEnumerator FadeAndDestroy()
    {
        float elapsed = 0f;
        Vector3 initialScale = transform.localScale;

        while (elapsed < FadeDurationSeconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / FadeDurationSeconds);
            float alpha = 1f - t;
            transform.localScale = Vector3.Lerp(initialScale, Vector3.zero, t);
            ApplyFadeAlpha(alpha);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void ApplyFadeAlpha(float alpha)
    {
        if (cachedRenderers == null) return;

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer renderer = cachedRenderers[i];
            if (renderer == null) continue;

            renderer.GetPropertyBlock(propertyBlock);
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_BaseColor"))
            {
                Color color = renderer.sharedMaterial.GetColor("_BaseColor");
                color.a = alpha;
                propertyBlock.SetColor("_BaseColor", color);
            }
            else if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_Color"))
            {
                Color color = renderer.sharedMaterial.GetColor("_Color");
                color.a = alpha;
                propertyBlock.SetColor("_Color", color);
            }

            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void EnsureKinematicBody()
    {
        Rigidbody body = GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }

        body.isKinematic = true;
        body.useGravity = false;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    private static Vector3 ResolveAimPoint(Selectable aimTarget)
    {
        if (aimTarget == null) return Vector3.zero;

        Collider targetCollider = aimTarget.GetComponentInChildren<Collider>();
        if (targetCollider != null)
        {
            return targetCollider.bounds.center;
        }

        return aimTarget.transform.position;
    }

    private Vector3 EvaluateParabola(Vector3 start, Vector3 end, float t)
    {
        Vector3 flat = Vector3.Lerp(start, end, t);
        float horizontal = HorizontalDistance(start, end);
        float arcHeight = horizontal * Mathf.Max(0f, arcHeightFactor);
        flat.y += arcHeight * 4f * t * (1f - t);
        return flat;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
