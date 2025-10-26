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
    private Selectable currentTarget;   // objetivo genérico (puede ser resource/building/unit)
    private Unit attackTarget;          // objetivo si es unidad enemiga

    [Header("Death Behavior")]
    public bool fightsAfterDeath = false;       // Permite atacar unos segundos después de morir.
    public float postDeathFightDuration = 2f;   // Duración del estado “post muerte”.

    // ============================================================
    // State machine (instancia simple por unidad)
    // ============================================================

    [Header("Estados de la unidad")]
    private UnitBehaviourState behaviourState = UnitBehaviourState.Idle;
    public UnitBehaviourState BehaviourState => behaviourState;

    private UnitState currentState; // instancia actual del estado

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

        // Inicializar estado por defecto
        ChangeState(new IdleState(this));
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
                ChangeState(new AttackingState(this)); // reiniciar estado de ataque en la nueva unidad
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

        // Tick del estado actual (si existe)
        currentState?.Tick();
    }

    // ============================================================
    // State machine helpers
    // ============================================================

    private void SetBehaviourState(UnitBehaviourState newState)
    {
        if (behaviourState == newState) return;

        behaviourState = newState;

        // Solo cambia animación (asegura compatibilidad con tus setters de animator)
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

    private void ChangeState(UnitState newState)
    {
        // Exit estado anterior
        currentState?.Exit();
        currentState = newState;
        // Enter nuevo estado
        currentState?.Enter();
        // Mantener flag de comportamiento (visibilidad externa)
        SetBehaviourState(currentState.GetBehaviourState());
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
    // MOVIMIENTO Y COMBATE (API pública intacta)
    // ============================================================

    public void Wait()
    {
        StopAllCoroutines();
        attackTarget = null;
        isAttacking = false;
        if (agent != null) agent.isStopped = true;
        ChangeState(new IdleState(this));
    }

    public void MoveTo(Vector3 destination)
    {
        currentTarget = null;
        attackTarget = null;
        isAttacking = false;

        if (agent != null)
        {
            agent.isStopped = false;
            agent.SetDestination(destination);
        }

        ChangeState(new MovingState(this, destination));
    }

    public void SetTarget(Selectable target)
    {
        if (target == null) return;

        currentTarget = target;
        attackTarget = target.GetComponent<Unit>();
        isAttacking = false;

        if (attackTarget != null && attackTarget.Owner != Owner)
        {
            isAttacking = true;
            ChangeState(new AttackingState(this));
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

        currentTarget = target;
        attackTarget = target;
        isAttacking = true;
        ChangeState(new AttackingState(this));
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
    /// Método llamado por el estado de ataque cuando corresponde.
    /// Conserva la misma firma y función que antes para compatibilidad con overrides.
    /// </summary>
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

    // ============================================================
    // === Unit State classes (internas, acceden a 'this' unidad)
    // ============================================================

    private abstract class UnitState
    {
        protected Unit unit;
        public UnitState(Unit u) { unit = u; }
        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void Tick() { }
        public virtual UnitBehaviourState GetBehaviourState() { return UnitBehaviourState.Idle; }
    }

    private class IdleState : UnitState
    {
        public IdleState(Unit u) : base(u) { }
        public override void Enter()
        {
            // detener movimiento y animaciones si corresponde
            if (unit.agent != null)
            {
                unit.agent.ResetPath();
                unit.agent.isStopped = true;
            }
            unit.SetBehaviourState(UnitBehaviourState.Idle);
        }
        public override UnitBehaviourState GetBehaviourState() => UnitBehaviourState.Idle;
    }

    private class MovingState : UnitState
    {
        private Vector3 destination;
        public MovingState(Unit u, Vector3 dest) : base(u) { destination = dest; }
        public override void Enter()
        {
            if (unit.agent != null)
            {
                unit.agent.isStopped = false;
                unit.agent.SetDestination(destination);
            }
            unit.SetBehaviourState(UnitBehaviourState.Moving);
        }

        public override void Tick()
        {
            // Si se convirtió en target de ataque (target válido), cambiar a Attacking
            if (unit.attackTarget != null && unit.attackTarget != unit && unit.attackTarget.state == SelectableState.Alive)
            {
                unit.ChangeState(new AttackingState(unit));
                return;
            }

            // si se llegó (o el agente no puede llegar) -> idle
            if (unit.agent == null) return;

            // Si todavía está ajustando el camino, mantener animación de movimiento
            if (unit.agent.pathPending)
            {
                unit.PlayWalkAnimation();
                return;
            }

            // Si todavía no ha llegado, pero se está moviendo o rotando hacia el destino
            if (unit.agent.remainingDistance > unit.agent.stoppingDistance)
            {
                unit.PlayWalkAnimation(); // asegura animación activa
            }
            else
            {
                // Si ya llegó completamente, cambiar a Idle
                unit.ChangeState(new IdleState(unit));
            }
        }

        public override UnitBehaviourState GetBehaviourState() => UnitBehaviourState.Moving;
    }

    private class AttackingState : UnitState
    {
        private float attackCooldown = 0;
        public AttackingState(Unit u) : base(u) { }

        public override void Enter()
        {
            unit.isAttacking = true;
            unit.SetBehaviourState(UnitBehaviourState.Attacking);
            attackCooldown = 1f / Mathf.Max(0.0001f, unit.currentStats.velocidadAtaque);
            // detener NavMeshAgent si está dentro de rango gestionado por Tick
            // no tocar animators aquí más que a través de SetBehaviourState -> PlayAttackAnimation
        }

        public override void Tick()
        {
            // Si objetivo null o muerto -> buscar otro o ir a Idle
            if (unit.attackTarget == null || unit.attackTarget.state != SelectableState.Alive)
            {
                // buscar reemplazo
                Unit newTarget = unit.FindNearestEnemy();
                if (newTarget != null)
                {
                    unit.attackTarget = newTarget;
                    // seguir atacando al nuevo objetivo
                }
                else
                {
                    unit.StopAttacking();
                    unit.ChangeState(new IdleState(unit));
                    return;
                }
            }

            // Si tenemos objetivo válido
            if (unit.attackTarget != null)
            {
                // Distancia
                float dist = Vector3.Distance(unit.transform.position, unit.attackTarget.transform.position);

                // Si fuera de rango, mover hacia objetivo
                if (dist > unit.currentStats.alcance && dist > 2f)
                {
                    if (unit.agent != null)
                    {
                        unit.agent.isStopped = false;
                        unit.agent.SetDestination(unit.attackTarget.transform.position);
                    }
                    unit.SetBehaviourState(UnitBehaviourState.Moving);
                }
                else
                {
                    // En rango: detenerse, rotar y atacar según cooldown
                    if (unit.agent != null) unit.agent.isStopped = true;

                    Vector3 lookDir = unit.attackTarget.transform.position - unit.transform.position;
                    lookDir.y = 0;
                    if (lookDir.sqrMagnitude > 0.001f)
                        unit.transform.rotation = Quaternion.LookRotation(lookDir);

                    unit.SetBehaviourState(UnitBehaviourState.Attacking);

                    attackCooldown -= Time.deltaTime;
                    if (attackCooldown <= 0f)
                    {
                        // Reproduce la animación, y el evento dentro de ella llamará a OnAttackHit()
                        unit.PlayAttackAnimation();

                        // Reinicia cooldown basado en velocidad de ataque
                        attackCooldown = 1f / Mathf.Max(0.0001f, unit.currentStats.velocidadAtaque);
                    }
                }
            }
        }

        public override void Exit()
        {
            unit.isAttacking = false;
            // no detener agente ni animaciones aquí: StopAttacking hace eso si se quiere
        }

        public override UnitBehaviourState GetBehaviourState() => UnitBehaviourState.Attacking;
    }
}
