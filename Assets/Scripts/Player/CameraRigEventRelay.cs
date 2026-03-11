using UnityEngine;

[DisallowMultipleComponent]
[AddComponentMenu("HyperSalchicha/Player/Camera Rig Event Relay")]
public class CameraRigEventRelay : MonoBehaviour
{
    [SerializeField] private FirstPersonCameraRig rig;

    private void Awake()
    {
        if (rig == null)
            rig = GetComponentInParent<FirstPersonCameraRig>();
    }

    public void Event_PlayKick(int presetIndex)
    {
        if (rig != null)
            rig.Event_PlayKick(presetIndex);
    }
}
