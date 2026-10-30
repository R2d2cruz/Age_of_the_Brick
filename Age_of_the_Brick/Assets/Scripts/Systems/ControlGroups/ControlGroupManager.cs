using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Saves and recalls control groups 0–9 (Ctrl+digit save, digit recall, double-tap camera focus).
/// </summary>
public class ControlGroupManager : MonoBehaviour
{
    public static ControlGroupManager Instance { get; private set; }

    [SerializeField] private float doubleTapSeconds = 0.35f;
    [SerializeField] private PlayerCameraController cameraController;

    private readonly List<Selectable>[] slots = new List<Selectable>[10];
    private readonly float[] lastTapTime = new float[10];

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
        for (int i = 0; i < 10; i++)
        {
            slots[i] = new List<Selectable>();
            lastTapTime[i] = float.NegativeInfinity;
        }
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        bool ctrl = Keyboard.current.leftCtrlKey.isPressed
                    || Keyboard.current.rightCtrlKey.isPressed;

        for (int digit = 0; digit <= 9; digit++)
        {
            if (!WasDigitPressedThisFrame(digit)) continue;

            if (ctrl)
            {
                SaveGroup(digit);
            }
            else
            {
                float now = Time.unscaledTime;
                bool doubleTap = (now - lastTapTime[digit]) <= doubleTapSeconds;
                lastTapTime[digit] = now;

                RecallGroup(digit);
                if (doubleTap)
                    FocusGroup(digit);
            }
        }
    }

    private static bool WasDigitPressedThisFrame(int digit)
    {
        switch (digit)
        {
            case 0: return Keyboard.current.digit0Key.wasPressedThisFrame
                           || Keyboard.current.numpad0Key.wasPressedThisFrame;
            case 1: return Keyboard.current.digit1Key.wasPressedThisFrame
                           || Keyboard.current.numpad1Key.wasPressedThisFrame;
            case 2: return Keyboard.current.digit2Key.wasPressedThisFrame
                           || Keyboard.current.numpad2Key.wasPressedThisFrame;
            case 3: return Keyboard.current.digit3Key.wasPressedThisFrame
                           || Keyboard.current.numpad3Key.wasPressedThisFrame;
            case 4: return Keyboard.current.digit4Key.wasPressedThisFrame
                           || Keyboard.current.numpad4Key.wasPressedThisFrame;
            case 5: return Keyboard.current.digit5Key.wasPressedThisFrame
                           || Keyboard.current.numpad5Key.wasPressedThisFrame;
            case 6: return Keyboard.current.digit6Key.wasPressedThisFrame
                           || Keyboard.current.numpad6Key.wasPressedThisFrame;
            case 7: return Keyboard.current.digit7Key.wasPressedThisFrame
                           || Keyboard.current.numpad7Key.wasPressedThisFrame;
            case 8: return Keyboard.current.digit8Key.wasPressedThisFrame
                           || Keyboard.current.numpad8Key.wasPressedThisFrame;
            case 9: return Keyboard.current.digit9Key.wasPressedThisFrame
                           || Keyboard.current.numpad9Key.wasPressedThisFrame;
            default: return false;
        }
    }

    private void SaveGroup(int index)
    {
        if (SelectionHandler.Instance == null) return;
        slots[index].Clear();
        foreach (Selectable s in SelectionHandler.Instance.SelectedObjects)
        {
            if (s != null)
                slots[index].Add(s);
        }
    }

    private void RecallGroup(int index)
    {
        if (SelectionHandler.Instance == null) return;
        var alive = new List<Selectable>();
        for (int i = 0; i < slots[index].Count; i++)
        {
            Selectable s = slots[index][i];
            if (s != null && s.state == SelectableState.Alive)
                alive.Add(s);
        }

        slots[index] = alive;
        if (alive.Count == 0) return;
        SelectionHandler.Instance.SetSelection(alive);
    }

    private void FocusGroup(int index)
    {
        if (cameraController == null) return;
        Vector3 sum = Vector3.zero;
        int count = 0;
        for (int i = 0; i < slots[index].Count; i++)
        {
            Selectable s = slots[index][i];
            if (s == null || s.state != SelectableState.Alive) continue;
            sum += s.transform.position;
            count++;
        }

        if (count == 0) return;
        cameraController.FocusOnWorldPoint(sum / count);
    }
}
