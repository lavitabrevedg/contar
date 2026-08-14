using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SwipeInputAction : MonoBehaviour
{
    [SerializeField] private float minSwipeDistance = 50f;

    private InputAction pressAction;
    private InputAction positionAction;

    private Vector2 startScreenPos;
    private bool isPressed;

    public event Action<Vector2Int> SwipeDetected;

    private void OnEnable()
    {
        pressAction = new InputAction("Press", binding: "<Pointer>/press");
        positionAction = new InputAction("Position", binding: "<Pointer>/position");

        pressAction.started += OnPressStarted;
        pressAction.canceled += OnPressCanceled;

        pressAction.Enable();
        positionAction.Enable();
    }

    private void OnDisable()
    {
        pressAction.started -= OnPressStarted;
        pressAction.canceled -= OnPressCanceled;

        pressAction.Disable();
        positionAction.Disable();

        pressAction.Dispose();
        positionAction.Dispose();
    }

    private void OnPressStarted(InputAction.CallbackContext context)
    {
        startScreenPos = positionAction.ReadValue<Vector2>();
        isPressed = true;
    }

    private void OnPressCanceled(InputAction.CallbackContext context)
    {
        if (!isPressed) return;
        isPressed = false;

        Vector2 endScreenPos = positionAction.ReadValue<Vector2>();
        Vector2 delta = endScreenPos - startScreenPos;

        if (delta.magnitude < minSwipeDistance) return;

        Vector2Int direction;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            direction = delta.x > 0 ? Vector2Int.right : Vector2Int.left;
        else
            direction = delta.y > 0 ? Vector2Int.up : Vector2Int.down;

        SwipeDetected?.Invoke(direction);

        if (GameManager.Instance == null) return;
        GameManager.Instance.OnSwipe(direction);
    }
}
