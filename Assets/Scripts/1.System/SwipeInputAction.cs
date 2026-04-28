using UnityEngine;
using UnityEngine.InputSystem;

public class SwipeInputAction : MonoBehaviour
{
    [SerializeField] private float minSwipeDistance = 50f;

    private InputAction _pressAction;
    private InputAction _positionAction;

    private Vector2 _startScreenPos;
    private bool _isPressed;

    private void OnEnable()
    {
        _pressAction = new InputAction("Press", binding: "<Pointer>/press");
        _positionAction = new InputAction("Position", binding: "<Pointer>/position");

        _pressAction.started += OnPressStarted;
        _pressAction.canceled += OnPressCanceled;

        _pressAction.Enable();
        _positionAction.Enable();
    }

    private void OnDisable()
    {
        _pressAction.started -= OnPressStarted;
        _pressAction.canceled -= OnPressCanceled;

        _pressAction.Disable();
        _positionAction.Disable();

        _pressAction.Dispose();
        _positionAction.Dispose();
    }

    private void OnPressStarted(InputAction.CallbackContext context)
    {
        _startScreenPos = _positionAction.ReadValue<Vector2>();
        _isPressed = true;
    }

    private void OnPressCanceled(InputAction.CallbackContext context)
    {
        if (!_isPressed) return;
        _isPressed = false;

        Vector2 endScreenPos = _positionAction.ReadValue<Vector2>();
        Vector2 delta = endScreenPos - _startScreenPos;

        if (delta.magnitude < minSwipeDistance) return;

        Vector2Int direction;
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            direction = delta.x > 0 ? Vector2Int.right : Vector2Int.left;
        else
            direction = delta.y > 0 ? Vector2Int.up : Vector2Int.down;

        if (GameManager.Instance == null) return;
        GameManager.Instance.OnSwipe(direction);
    }
}
