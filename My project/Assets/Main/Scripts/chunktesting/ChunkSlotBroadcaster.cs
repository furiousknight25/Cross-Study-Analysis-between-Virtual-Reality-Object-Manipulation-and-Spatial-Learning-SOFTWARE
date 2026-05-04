using UnityEngine;
using UnityEngine.InputSystem;

public class ChunkSlotBroadcaster : MonoBehaviour
{
    [Tooltip("The channel to broadcast the slot index to.")]
    public IntEventChannelSO slotSelectedChannel;
    
    [Tooltip("The Kintschian chunk index this slot represents (e.g., 0, 1, 2).")]
    public int slotIndex;

    void Update()
    {
        if (Keyboard.current == null) return;

        bool isPressed = false;
        switch (slotIndex)
        {
            case 0: isPressed = Keyboard.current.digit0Key.wasPressedThisFrame; break;
            case 1: isPressed = Keyboard.current.digit1Key.wasPressedThisFrame; break;
            case 2: isPressed = Keyboard.current.digit2Key.wasPressedThisFrame; break;
            case 3: isPressed = Keyboard.current.digit3Key.wasPressedThisFrame; break;
            case 4: isPressed = Keyboard.current.digit4Key.wasPressedThisFrame; break;
            case 5: isPressed = Keyboard.current.digit5Key.wasPressedThisFrame; break;
        }

        if (isPressed) BroadcastSlot();
    }

    /// <summary>
    /// Map this method to your Ultraleap button's OnPress UnityEvent.
    /// </summary>
    public void BroadcastSlot()
    {
        if (slotSelectedChannel != null)
        {
            slotSelectedChannel.RaiseEvent(slotIndex);
        }
    }
}