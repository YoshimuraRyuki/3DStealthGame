using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// コア運搬表示だけを確認するための仮入力。
/// 本実装で取得・通信処理ができたら、このコンポーネントは外す。
/// </summary>
public sealed class CoreCarryDebugInput : MonoBehaviour
{
    [SerializeField] private CoreCarryController carryController;
    [SerializeField] private GameObject testCore;
    [SerializeField] private float releaseDistance = 1.2f;

    private void Awake()
    {
        if (carryController == null)
        {
            carryController = GetComponent<CoreCarryController>();
        }
    }

    private void Update()
    {
        if (Keyboard.current == null || !Keyboard.current.cKey.wasPressedThisFrame) return;

        if (carryController.IsCarrying)
        {
            Vector3 releasePosition = transform.position + transform.forward * releaseDistance;
            carryController.ReleaseCore(releasePosition);
        }
        else if (testCore != null)
        {
            carryController.BeginCarry(testCore);
        }
    }
}
