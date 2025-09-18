using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class BuildingConstruction : MonoBehaviour
{
    public Building building;
    private Animator animator;

    [Header("Animaciones")]
    [SerializeField] private string constructionAnim = "Construccion";
    [SerializeField] private string idleAnim = "Idle";
    [SerializeField] private string destructionAnim = "Destruccion";

    private bool finished;

    [Header("Optimización")]
    [SerializeField] private GameObject optimizedMesh; // opcional: mesh único para idle
    [SerializeField] private GameObject blockMeshesRoot; // raíz de bloques individuales

    private void Awake()
    {
        animator = GetComponent<Animator>();
        building.currentHealth = 0;
        PlayConstructionAnimation(0f);
    }

    public void Construir(float hpPorSegundo)
    {
        if (finished) return;

        building.currentHealth += hpPorSegundo * Time.deltaTime;
        float progreso = Mathf.Clamp01(building.currentHealth / building.GetMaxVida());

        PlayConstructionAnimation(progreso);

        if (progreso >= 1f)
        {
            FinishConstruction();
        }
    }

    private void PlayConstructionAnimation(float progreso)
    {
        animator.Play(constructionAnim, 0, progreso);
        animator.speed = 0; // Detener animación, la controlamos manualmente
    }

    private void FinishConstruction()
    {
        finished = true;
        animator.speed = 1;
        animator.Play(idleAnim);

        if (optimizedMesh != null)
        {
            if (blockMeshesRoot != null) blockMeshesRoot.SetActive(false);
            optimizedMesh.SetActive(true);
        }
    }

    public void Destruir()
    {
        StartCoroutine(PlayDestruction());
    }

    private IEnumerator PlayDestruction()
    {
        animator.speed = 1;
        animator.Play(destructionAnim);

        float animLength = animator.GetCurrentAnimatorStateInfo(0).length;
        yield return new WaitForSeconds(animLength);

        // Desactivar bloques al final de la animación
        if (blockMeshesRoot != null)
            blockMeshesRoot.SetActive(false);

        Destroy(gameObject);
    }
}
