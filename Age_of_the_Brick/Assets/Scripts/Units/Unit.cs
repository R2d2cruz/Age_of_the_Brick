using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using UnityEngine.UI;

public class Unit : Selectable
{
    [Header("Config")]
    public UnitStats stats; // ScriptableObject (tiene statsPorEra[], evolucionaVisual, prefabsPorEra)

    public EraStats currentStats;
    private Animator animator;
    private NavMeshAgent agent;

    // Vida runtime
    public float currentHealth;

    // Owner del objeto (jugador que posee la unidad)
    private Player Owner;
    private Renderer rend;

    // -------------------------
    // Health bar UI (WORLD SPACE)
    // -------------------------
    [Header("Health UI (World)")]
    [Tooltip("Canvas (World Space) que contiene el Slider")]
    [SerializeField] private Canvas healthCanvas;
    [SerializeField] private Slider healthSlider;
    [SerializeField] private Vector3 healthCanvasOffset = new Vector3(0f, 4.2f, 0f); // ajustar alto sobre la unidad
    private static Camera mainCamCached; // cache para evitar Camera.main cada frame

    // 👇 Nueva sección
    [Header("Health UI Behavior")]
    [SerializeField] private float showHealthDuration = 3f; // segundos visible tras recibir daño
    private float lastHealthChangeTime = -999f; // tiempo del último cambio de vida
    private bool isHealthVisible = false;       // estado actual de visibilidad

    [Header("Health Bar Colors")]
    [SerializeField] private Image healthFillImage;
    [SerializeField] private Color healthyColor = Color.green;
    [SerializeField] private Color warningColor = Color.yellow;
    [SerializeField] private Color dangerColor = Color.red;


    // Combate
    private bool isAttacking = false;
    private Selectable currentTarget;
    private Unit attackTarget;

    [Header("Death Behavior")]
    public bool fightsAfterDeath = false; // si puede seguir peleando tras llegar a 0
    public float postDeathFightDuration = 2f; // segundos que sigue peleando


    protected override void Start()
    {
        base.Start();
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        rend = GetComponent<Renderer>();

        // Cache camera
        if (mainCamCached == null)
            mainCamCached = Camera.main;

        // 👇 Si no se ha inicializado explícitamente, buscar el Player usando ownerPlayerId
        if (Owner == null && PlayerManager.Instance != null)
        {
            Player possibleOwner = PlayerManager.Instance.GetPlayer(ownerPlayerId);
            if (possibleOwner != null)
            {
                Initialize(possibleOwner);
            }
        }

        // Ocultar la barra si la vida está completa
        if (healthCanvas != null)
            healthCanvas.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        UnsubscribeFromOwner();
        if (Owner != null)
            Owner.Registry.UnregisterUnit(this);
    }

    // -----------------------
    // Inicialización / Owner
    // -----------------------
    public void Initialize(Player owner)
    {
        Owner = owner;
        ownerPlayerId = owner.playerId; // 👈 sincroniza el ID con el Player asignado
        SubscribeToOwner();
        UpdateStats(owner.CurrentEra);

        if (currentHealth <= 0)
            currentHealth = currentStats.vida;

        // 👇 Registrarse en el jugador
        owner.Registry.RegisterUnit(this);

        // Health UI (en caso de que Initialize se llame después de Start)
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

    // -----------------------
    // Manejo de era
    // -----------------------
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

    // -----------------------
    // Stats y vida
    // -----------------------
    public void UpdateStats(int era)
    {
        EraStats oldStats = currentStats;
        currentStats = stats.statsPorEra[era];

        if (oldStats != null && oldStats.vida > 0)
        {
            float porcentajeVida = currentHealth / oldStats.vida;
            currentHealth = Mathf.Clamp(currentStats.vida * porcentajeVida, 0.0f, currentStats.vida);
        }
        else
        {
            currentHealth = currentStats.vida;
        }

        if (agent != null)
            agent.speed = currentStats.velocidadMovimiento;

        if (animator != null)
        {
            animator.SetFloat("moveSpeedMultiplier", currentStats.velocidadMovimiento);
            animator.SetFloat("attackSpeedMultiplier", currentStats.velocidadAtaque);
        }

        // Actualizar máximos del UI
        if (healthSlider != null)
        {
            healthSlider.maxValue = currentStats.vida;
            healthSlider.value = currentHealth;
        }
    }

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

    // -----------------------
    // Manejo update
    // -----------------------
    private void Update()
    {
        // billboard health canvas y actualización del valor (si hay cambios)
        UpdateHealthCanvasTransform();
        UpdateHealthVisibility();

        if (agent == null || agent.pathPending) return;

        // Magnitud de velocidad (cuán rápido se está moviendo)
        float speed = agent.velocity.magnitude;

        // 🔧 Actualiza el parámetro "isMoving" según si hay velocidad
        if (speed > 0.1f) // se está moviendo
        {
            PlayWalkAnimation();
        }
        else
        {
            PlayIdleAnimation();
        }
    }

    // -----------------------
    // Health UI helpers
    // -----------------------
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

        // Mostrar temporalmente si hay daño o curación
        lastHealthChangeTime = Time.time;
        SetHealthVisibility(true);

        // Actualizar color visual
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
        if (healthCanvas == null) return;

        // Si cache de camera no existe, intentar recuperarla
        if (mainCamCached == null) mainCamCached = Camera.main;
        if (mainCamCached == null) return;

        // Mantener el canvas encima del unit (si quieres fijar el offset dinámicamente)
        healthCanvas.transform.position = transform.position + healthCanvasOffset;

        // Rotar para mirar a la cámara (mirar hacia la cámara)
        Vector3 dir = mainCamCached.transform.position - healthCanvas.transform.position;
        // si quieres que la barra también se incline según cámara, usa sin zero Y; aquí la dejamos mirando plano completo
        healthCanvas.transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    // 👇 Nueva función de control de visibilidad
    private void UpdateHealthVisibility()
    {
        if (!isHealthVisible) return;

        if (Time.time - lastHealthChangeTime > showHealthDuration)
        {
            SetHealthVisibility(false);
        }
    }

    private void SetHealthVisibility(bool visible)
    {
        if (healthCanvas != null && visible != isHealthVisible)
        {
            healthCanvas.gameObject.SetActive(visible);
            isHealthVisible = visible;
        }
    }

    // -----------------------
    // Animaciones comunes
    // -----------------------
    public virtual void PlayIdleAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("isMoving", false);
            //animator.ResetTrigger("attack");
        }
    }

    public void PlayWalkAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("attack", false);
            animator.SetBool("isMoving", true); // por si estaba caminando
        }
    }

    protected virtual void PlayAttackAnimation()
    {
        if (animator != null)
        {
            animator.SetBool("attack", true);
            animator.SetBool("isMoving", false); // por si estaba caminando
        }
    }

    // -----------------------
    // Selección
    // -----------------------
    public void SetSelected(bool selected)
    {
        if (rend != null)
            rend.material.color = selected ? Color.yellow : Color.white;
    }

    // -----------------------
    // Acciones
    // -----------------------
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

        PlayWalkAnimation();
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
            // Pendiente: reparar/atacar edificio
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

    // 🔹 Añade esto dentro de Unit.cs (después de OnAttackHit y antes de TakeDamage)
    private Unit FindNearestEnemy()
    {
        // Usa el radio de visión definido en EraStats (si no está, usa un valor por defecto)
        float visionRadius = (currentStats != null && currentStats.vision * 2 > 0) ? currentStats.vision * 2 : 12f;
        Collider[] hits = Physics.OverlapSphere(transform.position, visionRadius);

        Debug.Log(visionRadius);

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

        if (agent != null)
            agent.isStopped = true;

        if (animator != null)
        {
            animator.SetBool("attack", false);
            animator.SetBool("isMoving", false);
        }

        attackTarget = null;
    }

    private IEnumerator AttackRoutine()
    {
        while (attackTarget != null)
        {
            // 👇 Verificar si el objetivo está muerto
            if (attackTarget.state == SelectableState.Dead)
            {
                // Pequeña pausa para permitir que el estado se actualice completamente
                yield return new WaitForSeconds(0.1f);

                // Buscar nuevo objetivo automáticamente
                Unit newTarget = FindNearestEnemy();

                if (newTarget != null)
                {
                    attackTarget = newTarget;
                    Debug.Log($"{name} cambió de objetivo a {newTarget.name}");
                    continue; // sigue el bucle sin detener el ataque
                }
                else
                {
                    attackTarget = null;
                    isAttacking = false;

                    // Detener el movimiento y animaciones de ataque
                    if (agent != null) agent.isStopped = true;
                    if (animator != null)
                    {
                        animator.SetBool("attack", false);
                        animator.SetBool("isMoving", false);
                    }

                    yield break; // salir del coroutine si no hay nuevos enemigos
                }
            }

            float dist = Vector3.Distance(transform.position, attackTarget.transform.position);

            if (dist <= currentStats.alcance || dist <= 2)
            {
                agent.isStopped = true;

                Vector3 lookDir = attackTarget.transform.position - transform.position;
                lookDir.y = 0;
                if (lookDir.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(lookDir);

                PlayAttackAnimation();

                yield return new WaitForSeconds(1f / currentStats.velocidadAtaque);
            }
            else
            {
                agent.isStopped = false;
                agent.SetDestination(attackTarget.transform.position);
                PlayWalkAnimation();
            }

            yield return null;
        }

        // 👇 Cuando el objetivo muere o ya no es válido
        attackTarget = null;
        isAttacking = false;

        agent.isStopped = true;


        // 🔧 Resetea la animación de ataque
        if (animator != null)
        {
            animator.SetBool("attack", false);
            animator.SetBool("isMoving", false);
        }
    }

    public void Wait()
    {
        StopAllCoroutines();
        attackTarget = null;
        isAttacking = false;

        if (agent != null) agent.isStopped = true;
        PlayIdleAnimation();
    }

    // -----------------------
    // Combate
    // -----------------------
    public virtual void OnAttackHit()
    {
        if (attackTarget == null) return;

        float dist = Vector3.Distance(transform.position, attackTarget.transform.position);
        if (dist > currentStats.alcance && dist > 2) return;
        attackTarget.TakeDamage(currentStats.ataqueLigero, currentStats.ataquePesado);
    }

    public void TakeDamage(int dmgLigero, int dmgPesado)
    {
        int finalDamage = Mathf.Max(0, dmgLigero - currentStats.defensaLigera) +
                          Mathf.Max(0, dmgPesado - currentStats.defensaPesada);

        currentHealth -= finalDamage;

        // 👇 Actualiza el valor visual del slider
        UpdateHealthUI();

        if (currentHealth <= 0f && state == SelectableState.Alive)
        {
            state = SelectableState.Dying;
            StartCoroutine(DyingRoutine());
        }
    }

    // 👇 Método opcional si agregas curación
    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentStats.vida, currentHealth + amount);
        UpdateHealthUI();
    }
    private IEnumerator DyingRoutine()
    {
        if (fightsAfterDeath)
        {
            float startTime = Time.time;

            /* // Opcional: animación o efecto especial
            if (animator != null)
                animator.SetBool("isBerserk", true); */

            while (Time.time - startTime < postDeathFightDuration)
            {
                // Permite seguir atacando si ya estaba en combate
                if (isAttacking && attackTarget != null)
                {
                    // Evita moverse, pero puede seguir atacando
                    agent.isStopped = true;
                }

                yield return null;
            }
        }

        die();
    }


    private void die()
    {
        if (state == SelectableState.Dead) return; // prevenir doble muerte
        state = SelectableState.Dead;
        // esconder UI al morir
        if (healthCanvas != null)
            healthCanvas.gameObject.SetActive(false);

        StopAllCoroutines();
        if (animator != null) animator.SetTrigger("die");
        if (agent != null) agent.isStopped = true;
        Destroy(gameObject, 1f);
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
