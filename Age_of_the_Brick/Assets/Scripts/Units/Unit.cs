using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.UI;

public enum UnitBehaviourState
{
    Idle,
    Moving,
    Attacking,
    Waiting
}


/// <summary>
/// Clase base de las unidades en el juego.
/// Hereda de Selectable, lo que permite interacción del jugador (selección, comandos).
/// Controla movimiento, combate, animaciones y UI de salud.
/// </summary>
public class Unit : Selectable
{
    // ============================================================
    // CONFIGURACIÓN GENERAL
    // ============================================================

    [Header("Configuración General")]
    public UnitStats stats;                 // ScriptableObject con las estadísticas por era.
    public EraStats currentStats;           // Stats activas (dependen de la era).
    private Animator animator;              // Controlador de animaciones.
    private NavMeshAgent agent;             // Controlador de movimiento.
    private Renderer rend;                  // Render del modelo principal.

    // Estado de vida en tiempo de ejecución.
    public float currentHealth;

    // ============================================================
    // OWNER / PLAYER
    // ============================================================

    /// <summary>
    /// Jugador propietario de la unidad. Controla acceso, registro y cambios de era.
    /// </summary>
    private Player Owner;

    // ============================================================
    // UI DE VIDA (World Space)
    // ============================================================

    [Header("Health UI (World)")]
    [Tooltip("Canvas en modo World Space que contiene la barra de vida (Slider).")]
    [SerializeField] private Canvas healthCanvas;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Vector3 healthCanvasOffset = new Vector3(0f, 4.2f, 0f);
    private static Camera mainCamCached; // Cache de cámara principal para evitar llamadas costosas.

    // ============================================================
    // VISIBILIDAD DE BARRA DE VIDA
    // ============================================================

    [Header("Health UI Behavior")]
    [SerializeField] private float showHealthDuration = 3f;  // Tiempo visible tras daño/curación.
    private float lastHealthChangeTime = -999f;
    private bool isHealthVisible = false;

    [Header("Health Bar Colors")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color dangerColor = Color.red;

    // ============================================================
    // COMBATE Y ESTADO
    // ============================================================

    private bool isAttacking = false;
    private Selectable currentTarget;
    private Unit attackTarget;

    [Header("Death Behavior")]
    public bool fightsAfterDeath = false;       // Permite atacar unos segundos después de morir.
    public float postDeathFightDuration = 2f;   // Duración del estado “post muerte”.

    // ============================================================
    // State machine
    // ============================================================

    [Header("Estados de la unidad")]

    private UnitBehaviourState behaviourState = UnitBehaviourState.Idle;
    public UnitBehaviourState BehaviourState => behaviourState;


    // ============================================================
    // MÉTODOS DE CICLO DE VIDA
    // ============================================================

    protected override void Start()
    {
        base.Start();

        TryGetComponent(out animator);
        TryGetComponent(out agent);
        TryGetComponent(out rend);

        // Cache de cámara
        if (mainCamCached == null)
            mainCamCached = Camera.main;

        // Intentar asignar dueño automáticamente
        if (Owner == null && PlayerManager.Instance != null)
        {
            Player possibleOwner = PlayerManager.Instance.GetPlayer(ownerPlayerId);
            if (possibleOwner != null)
                Initialize(possibleOwner);
        }

        // Ocultar barra de vida al inicio si está llena
        healthCanvas?.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        UnsubscribeFromOwner();
        Owner?.Registry.UnregisterUnit(this);
    }

    // ============================================================
    // INICIALIZACIÓN Y RELACIÓN CON PLAYER
    // ============================================================

    /// <summary>
    /// Inicializa la unidad con el propietario y la registra en su sistema.
    /// </summary>
    public void Initialize(Player owner)
    {
        Owner = owner;
        ownerPlayerId = owner.playerId;
        SubscribeToOwner();
        UpdateStats(owner.CurrentEra);

        if (currentHealth <= 0)
            currentHealth = currentStats.vida;

        Owner.Registry.RegisterUnit(this);

        InitHealthUI();
        UpdateHealthUI();
    }

    private void SubscribeToOwner()
    {
        if (Owner != null)
            Owner.OnEraChanged += HandleEraChange;
    }

    private void UnsubscribeFromOwner()
    {
        if (Owner != null)
            Owner.OnEraChanged -= HandleEraChange;
    }

    // ============================================================
    // CAMBIO DE ERA Y EVOLUCIÓN
    // ============================================================

    /// <summary>
    /// Cambia los stats o modelo de la unidad según la nueva era.
    /// </summary>
    private void HandleEraChange(int nuevaEra)
    {
        if (stats.evolucionaVisual)
        {
            GameObject nuevoGO = Instantiate(stats.prefabsPorEra[nuevaEra], transform.position, transform.rotation);
            Unit nuevaUnidad = nuevoGO.GetComponent<Unit>();
            nuevaUnidad.Initialize(Owner);
            nuevaUnidad.InheritFrom(this, nuevaEra);
            Destroy(gameObject);
        }
        else
        {
            UpdateStats(nuevaEra);
        }
    }

    // ============================================================
    // STATS Y VIDA
    // ============================================================

    /// <summary>
    /// Actualiza las estadísticas según la era y conserva la proporción de vida.
    /// </summary>
    public void UpdateStats(int era)
    {
        EraStats oldStats = currentStats;
        currentStats = stats.statsPorEra[era];

        if (oldStats != null && oldStats.vida > 0)
        {
            float porcentajeVida = currentHealth / oldStats.vida;
            currentHealth = Mathf.Clamp(currentStats.vida * porcentajeVida, 0.0f, currentStats.vida);
        }
        else currentHealth = currentStats.vida;

        if (agent != null)
            agent.speed = currentStats.velocidadMovimiento;

        if (animator != null)
        {
            animator.SetFloat("moveSpeedMultiplier", currentStats.velocidadMovimiento);
            animator.SetFloat("attackSpeedMultiplier", currentStats.velocidadAtaque);
        }

        if (healthSlider != null)
        {
            healthSlider.maxValue = currentStats.vida;
            healthSlider.value = currentHealth;
        }
    }

    /// <summary>
    /// Transfiere estado entre versiones de unidad (al evolucionar visualmente).
    /// </summary>
    public void InheritFrom(Unit oldUnit, int era)
    {
        currentHealth = Mathf.Min(oldUnit.currentHealth, stats.statsPorEra[era].vida);
        UpdateStats(era);

        if (oldUnit.attackTarget != null)
        {
            attackTarget = oldUnit.attackTarget;
            if (oldUnit.isAttacking)
                StartCoroutine(AttackRoutine());
        }

        if (oldUnit.agent != null && oldUnit.agent.hasPath)
            MoveTo(oldUnit.agent.destination);
    }

    // ============================================================
    // UPDATE
    // ============================================================

    private void Update()
    {
        if (state == SelectableState.Dead) return; // ya no hace nada si está muerta

        UpdateHealthCanvasTransform();
        UpdateHealthVisibility();

        if (agent == null || agent.pathPending) return;

        float speed = agent.velocity.sqrMagnitude;

        if (speed > 0.01f && behaviourState != UnitBehaviourState.Moving && !isAttacking)
            SetBehaviourState(UnitBehaviourState.Moving);
        else if (speed <= 0.01f && !isAttacking && behaviourState != UnitBehaviourState.Idle)
            SetBehaviourState(UnitBehaviourState.Idle);
    }

    // ============================================================
    // State Machine
    // ============================================================

    private void SetBehaviourState(UnitBehaviourState newState)
    {
        if (behaviourState == newState) return;

        behaviourState = newState;

        switch (newState)
        {
            case UnitBehaviourState.Idle:
                PlayIdleAnimation();
                break;
            case UnitBehaviourState.Moving:
                PlayWalkAnimation();
                break;
            case UnitBehaviourState.Attacking:
                PlayAttackAnimation();
                break;
        }
    }


    // ============================================================
    // UI DE VIDA
    // ============================================================

    private void InitHealthUI()
    {
        if (healthCanvas == null || healthSlider == null) return;

        if (healthCanvas.renderMode != RenderMode.WorldSpace)
            healthCanvas.renderMode = RenderMode.WorldSpace;

        healthCanvas.transform.SetParent(transform, false);
        healthCanvas.transform.localPosition = healthCanvasOffset;

        healthSlider.minValue = 0f;
        healthSlider.maxValue = (currentStats != null) ? currentStats.vida : 1f;
        healthSlider.value = currentHealth;

        UpdateHealthBarColor();
        healthCanvas.gameObject.SetActive(false);
    }

    private void UpdateHealthUI()
    {
        if (healthSlider == null) return;

        healthSlider.value = Mathf.Clamp(currentHealth, 0f, (currentStats != null) ? currentStats.vida : currentHealth);
        lastHealthChangeTime = Time.time;
        SetHealthVisibility(true);
        UpdateHealthBarColor();
    }

    private void UpdateHealthBarColor()
    {
        if (healthFillImage == null || currentStats == null) return;

        float porcentaje = currentHealth / currentStats.vida;
        if (porcentaje > 0.6f)
            healthFillImage.color = healthyColor;
        else if (porcentaje > 0.3f)
            healthFillImage.color = warningColor;
        else
            healthFillImage.color = dangerColor;
    }

    private void UpdateHealthCanvasTransform()
    {
        if (healthCanvas == null || mainCamCached == null) return;

        healthCanvas.transform.position = transform.position + healthCanvasOffset;
        healthCanvas.transform.rotation = Quaternion.LookRotation(mainCamCached.transform.forward);
    }

    private void UpdateHealthVisibility()
    {
        if (!isHealthVisible) return;
        if (Time.time - lastHealthChangeTime > showHealthDuration)
            SetHealthVisibility(false);
    }

    private void SetHealthVisibility(bool visible)
    {
        if (healthCanvas != null && visible != isHealthVisible)
        {
            healthCanvas.gameObject.SetActive(visible);
            isHealthVisible = visible;
        }
    }

    // ============================================================
    // ANIMACIONES
    // ============================================================

    public virtual void PlayIdleAnimation()
    {
        animator?.SetBool("isMoving", false);
    }

    public void PlayWalkAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("attack", false);
            animator.SetBool("isMoving", true);
        }
    }

    protected virtual void PlayAttackAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("attack", true);
            animator.SetBool("isMoving", false);
        }
    }

    // ============================================================
    // SELECCIÓN VISUAL
    // ============================================================

    public void SetSelected(bool selected)
    {
        if (rend != null)
            rend.material.color = selected ? Color.yellow : Color.white;
    }

    // ============================================================
    // MOVIMIENTO Y COMBATE
    // ============================================================

    public void Wait()
    {
        StopAllCoroutines();
        attackTarget = null;
        isAttacking = false;
        if (agent != null) agent.isStopped = true;
        SetBehaviourState(UnitBehaviourState.Idle);
    }

    public void MoveTo(Vector3 destination)
    {
        StopAllCoroutines();
        currentTarget = null;
        attackTarget = null;
        isAttacking = false;

        if (agent != null)
        {
            agent.isStopped = false;
            agent.SetDestination(destination);
        }

        SetBehaviourState(UnitBehaviourState.Moving);
    }

    public void SetTarget(Selectable target)
    {
        if (target == null) return;

        StopAllCoroutines();
        currentTarget = target;
        attackTarget = target.GetComponent<Unit>();
        isAttacking = false;

        if (attackTarget != null && attackTarget.Owner != Owner)
        {
            isAttacking = true;
            StartCoroutine(AttackRoutine());
        }
        else if (target.CompareTag("Resource"))
        {
            MoveTo(target.transform.position);
        }
        else if (target.CompareTag("Building"))
        {
            // Pendiente: reparar o atacar edificio
        }
    }

    public void Attack(Unit target)
    {
        if (target == null) return;

        StopAllCoroutines();
        currentTarget = target;
        attackTarget = target;
        isAttacking = true;
        StartCoroutine(AttackRoutine());
    }

    // ============================================================
    // COMBATE Y RUTINAS
    // ============================================================

    /// <summary>
    /// Busca el enemigo más cercano dentro del radio de visión.
    /// </summary>
    private Unit FindNearestEnemy()
    {
        float visionRadius = (currentStats != null && currentStats.vision * 2 > 0) ? currentStats.vision * 2 : 12f;
        Collider[] hits = Physics.OverlapSphere(transform.position, visionRadius);

        Unit nearestEnemy = null;
        float minDist = Mathf.Infinity;

        foreach (var hit in hits)
        {
            Unit candidate = hit.GetComponent<Unit>();
            if (candidate == null || candidate == this) continue;
            if (candidate.state != SelectableState.Alive) continue;
            if (!candidate.IsEnemyTo(ownerPlayerId)) continue;

            float dist = Vector3.Distance(transform.position, candidate.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearestEnemy = candidate;
            }
        }
        return nearestEnemy;
    }

    private void StopAttacking()
    {
        isAttacking = false;
        attackTarget = null;

        if (agent != null)
            agent.isStopped = true;

        if (animator != null)
        {
            animator.SetBool("attack", false);
            animator.SetBool("isMoving", false);
        }
    }

    /// <summary>
    /// Corrutina principal de ataque cuerpo a cuerpo o a distancia.
    /// Controla el seguimiento del objetivo y la transición entre combate y reposo.
    /// </summary>
    private IEnumerator AttackRoutine()
    {
        while (attackTarget != null)
        {
            // Si el objetivo muere, buscar reemplazo o detener ataque.
            if (attackTarget.state == SelectableState.Dead)
            {
                yield return new WaitForSeconds(0.1f);
                Unit newTarget = FindNearestEnemy();

                if (newTarget != null)
                {
                    attackTarget = newTarget;
                    continue;
                }
                else
                {
                    StopAttacking();
                    yield break;
                }
            }

            float dist = Vector3.Distance(transform.position, attackTarget.transform.position);
            if (dist <= currentStats.alcance || dist <= 2)
            {
                agent.isStopped = true;

                // Mirar hacia el objetivo
                Vector3 lookDir = attackTarget.transform.position - transform.position;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(lookDir);

                SetBehaviourState(UnitBehaviourState.Attacking);
                yield return new WaitForSeconds(1f / currentStats.velocidadAtaque);
            }
            else
            {
                agent.isStopped = false;
                agent.SetDestination(attackTarget.transform.position);
                SetBehaviourState(UnitBehaviourState.Moving);
            }

            yield return null;
        }

        StopAttacking();
    }

    public virtual void OnAttackHit()
    {
        if (attackTarget == null) return;
        if (attackTarget.state == SelectableState.Dead) return;

        float dist = Vector3.Distance(transform.position, attackTarget.transform.position);
        if (dist > currentStats.alcance && dist > 2) return;
        attackTarget.TakeDamage(currentStats.ataqueLigero, currentStats.ataquePesado);
    }

    public void TakeDamage(int dmgLigero, int dmgPesado)
    {
        if (state == SelectableState.Dead) return;

        int finalDamage = Mathf.Max(0, dmgLigero - currentStats.defensaLigera) +
                          Mathf.Max(0, dmgPesado - currentStats.defensaPesada);

        currentHealth -= finalDamage;
        UpdateHealthUI();

        if (currentHealth <= 0f && state == SelectableState.Alive)
        {
            state = SelectableState.Dying;
            StartCoroutine(DyingRoutine());
        }
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentStats.vida, currentHealth + amount);
        UpdateHealthUI();
    }

    private IEnumerator DyingRoutine()
    {
        if (fightsAfterDeath)
        {
            yield return new WaitForSeconds(postDeathFightDuration);
        }
        Die();
    }

    private void Die()
    {
        if (state == SelectableState.Dead) return;

        state = SelectableState.Dead;
        isAttacking = false;
        healthCanvas?.gameObject.SetActive(false);

        StopAllCoroutines();
        animator?.SetTrigger("die");

        Destroy(gameObject, 3f);
    }

    private void OnDrawGizmosSelected()
    {
        if (currentStats != null)
        {
            Gizmos.color = Color.red;
            float visionRadius = (currentStats.vision > 0) ? currentStats.vision : 12f;
            Gizmos.DrawWireSphere(transform.position, visionRadius);
        }
    }
}
