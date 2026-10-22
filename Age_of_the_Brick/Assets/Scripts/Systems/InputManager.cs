using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Global Singleton to manage the new Unity Input System instance.
/// Ensures only one instance of RTSInputActions exists and provides easy access.
/// </summary>
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    // Public reference to the generated input actions
    public RTSInputActions Actions { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Actions = new RTSInputActions();
            Actions.Enable();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        // Always clean up the input system to prevent memory leaks
        if (Instance == this)
        {
            Actions?.Disable();
            Actions?.Dispose();
        }
    }

    /// <summary>
    /// Helper method to quickly get the mouse position in screen space.
    /// </summary>
    public Vector2 GetMousePosition()
    {
        return Actions.Gameplay.MousePos.ReadValue<Vector2>();
    }
}