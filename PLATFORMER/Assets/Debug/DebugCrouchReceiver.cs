using UnityEngine;
using UnityEngine.InputSystem;

public class DebugCrouchReceiver : MonoBehaviour
{
    public void OnCrouch(InputValue value)
    {
        Debug.Log($"🚨 [DEBUG TEST] OnCrouch rebut directament: {value.isPressed}");
    }
}
