using UnityEngine;
using UnityEngine.AI;
using System.Collections;

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

    // Combate
    private bool isAttacking = false;
    private Selectable currentTarget;
    private Unit attackTarget;

    protected override void Start()
    {
        base.Start();
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        rend = GetComponent<Renderer>();

        // 👇 Si no se ha inicializado explícitamente, buscar el Player usando ownerPlayerId
        if (Owner == null && PlayerManager.Instance != null)
        {
            Player possibleOwner = PlayerManager.Instance.GetPlayer(ownerPlayerId);
            if (possibleOwner != null)
            {
                Initialize(possibleOwner);
            }
        }
    }

    private void OnDestroy()
    {
        UnsubscribeFromOwner();
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

    private IEnumerator AttackRoutine()
    {
        while (attackTarget != null)
        {
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
        Debug.Log("OnAttackHit");
        if (attackTarget == null) return;

        float dist = Vector3.Distance(transform.position, attackTarget.transform.position);
        Debug.Log("OnDistance");
        if (dist > currentStats.alcance && dist > 2) return;
        Debug.Log("OnDamage");
        attackTarget.TakeDamage(currentStats.ataqueLigero, currentStats.ataquePesado);
    }

    public void TakeDamage(int dmgLigero, int dmgPesado)
    {
        int finalDamage = Mathf.Max(0, dmgLigero - currentStats.defensaLigera) +
                          Mathf.Max(0, dmgPesado - currentStats.defensaPesada);

        currentHealth -= finalDamage;

        if (currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        StopAllCoroutines();
        if (animator != null) animator.SetTrigger("die");
        if (agent != null) agent.isStopped = true;
        Destroy(gameObject, 3f);
    }
}
