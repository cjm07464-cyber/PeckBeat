using UnityEngine;
using UnityEngine.InputSystem;

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
}
