using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCameraController : MonoBehaviour
{
    [Header("Config")]
    public float moveSpeed = 10f;
    public float edgeScrollSpeed = 15f;
    public float edgeSize = 20f; // en píxeles, tamaño del borde para mover cámara

    private Camera cam;

    private void Awake()
    {
        cam = Camera.main;
    }

    void Update()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        Vector3 direction = Vector3.zero;

        // Teclado
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
            direction += Vector3.forward;
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
            direction += Vector3.back;
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
            direction += Vector3.left;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
            direction += Vector3.right;

        // Bordes de pantalla
        Vector3 mousePos = Mouse.current.position.ReadValue();

        if (mousePos.x <= edgeSize)
            direction += Vector3.left;
        else if (mousePos.x >= Screen.width - edgeSize)
            direction += Vector3.right;

        if (mousePos.y <= edgeSize)
            direction += Vector3.back;
        else if (mousePos.y >= Screen.height - edgeSize)
            direction += Vector3.forward;

        // --- 🔥 Transformar la dirección al espacio relativo de la cámara ---
        Vector3 camForward = cam.transform.forward;
        Vector3 camRight = cam.transform.right;

        camForward.y = 0; // ignorar inclinación vertical
        camRight.y = 0;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDir = (camForward * direction.z + camRight * direction.x).normalized;

        transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);
    }

}
