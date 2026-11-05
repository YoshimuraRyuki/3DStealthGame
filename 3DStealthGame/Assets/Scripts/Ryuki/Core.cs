using UnityEngine;

public class Core : MonoBehaviour
{
    public bool isCarried { get; private set; } = false;
    public Transform carrier { get; private set; } = null;

    /// <summary>
    /// 敵がCoreを狙う・確保するときに呼ぶ
    /// </summary>
    public bool ClaimCore(Transform requester)
    {
        // 誰かに確保されている場合は失敗
        if (isCarried && carrier != null && carrier.GetComponent<EnemyManager>() != null)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// コアが誰の手にも渡っていない自由な状態に戻す
    /// </summary>
    public void ResetCore()
    {
        isCarried = false;
        carrier = null;

        // 親要素（敵やプレイヤーの手元）から切り離す
        transform.SetParent(null);

        // コライダーを再有効化
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = true;
            col.isTrigger = false; // 必要に応じて
        }

        // 物理挙動を復元
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        Debug.Log("Coreの状態がリセットされました。再び誰でも確保可能です。");
    }

    public void AttachToEnemy(Transform attachPoint)
    {
        isCarried = true;
        carrier = attachPoint.root;

        // アタッチ前の元の見た目の大きさを記憶
        Vector3 originalLossyScale = transform.lossyScale;

        transform.SetParent(attachPoint);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // 敵のスケールに左右されないよう、元の大きさに補正してセット
        if (attachPoint.lossyScale.x != 0 && attachPoint.lossyScale.y != 0 && attachPoint.lossyScale.z != 0)
        {
            transform.localScale = new Vector3(
                originalLossyScale.x / attachPoint.lossyScale.x,
                originalLossyScale.y / attachPoint.lossyScale.y,
                originalLossyScale.z / attachPoint.lossyScale.z
            );
        }

        // 物理挙動を完全に停止・Kinematic化する
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.isKinematic = true;
        }

        // 敵のColliderとの物理衝突を防ぐため、コライダーを無効化する
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }
    }

    public void ReleaseCore(Transform enemy)
    {
        if (carrier == enemy)
        {
            carrier = null;
            isCarried = false;
        }
    }
}
