using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public sealed class PeckBeatInput : MonoBehaviour
{
    private const string ActionAName = "ActionA";
    private const string ActionBName = "ActionB";

    [SerializeField] private InputActionAsset inputActions;

    private InputAction actionA;
    private InputAction actionB;

    private void Awake()
    {
        if (inputActions == null)
        {
            Debug.LogWarning("PeckBeatInput: Input Actions asset is not assigned.", this);
            return;
        }

        actionA = inputActions.FindAction(ActionAName);
        actionB = inputActions.FindAction(ActionBName);

        if (actionA == null || actionB == null)
        {
            Debug.LogError("PeckBeatInput: ActionA or ActionB is missing from the Input Actions asset.", this);
        }
    }

    private void OnEnable()
    {
        actionA?.Enable();
        actionB?.Enable();
    }

    private void OnDisable()
    {
        actionA?.Disable();
        actionB?.Disable();
    }

    public bool ActionAPressedThisFrame()
    {
        return actionA != null && actionA.WasPressedThisFrame();
    }

    public bool ActionBPressedThisFrame()
    {
        return actionB != null && actionB.WasPressedThisFrame();
    }

    public bool ActionBIsPressed()
    {
        return actionB != null && actionB.IsPressed();
    }

    public bool ActionBReleasedThisFrame()
    {
        return actionB != null && actionB.WasReleasedThisFrame();
    }

    public bool ActionAKeyboardPressedThisFrame()
    {
        return WasPressedThisFrameByDevice<Keyboard>(actionA);
    }

    public bool ActionAGamepadPressedThisFrame()
    {
        return WasPressedThisFrameByDevice<Gamepad>(actionA);
    }

    public bool ActionBKeyboardPressedThisFrame()
    {
        return WasPressedThisFrameByDevice<Keyboard>(actionB);
    }

    public bool ActionBGamepadPressedThisFrame()
    {
        return WasPressedThisFrameByDevice<Gamepad>(actionB);
    }

    private static bool WasPressedThisFrameByDevice<TDevice>(InputAction action)
        where TDevice : InputDevice
    {
        if (action == null)
        {
            return false;
        }

        foreach (InputControl control in action.controls)
        {
            if (control.device is TDevice &&
                control is ButtonControl button &&
                button.wasPressedThisFrame)
            {
                return true;
            }
        }

        return false;
    }
}
