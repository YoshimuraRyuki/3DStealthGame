using UnityEngine;
using UnityEngine.EventSystems;

// UIの選択・解除イベントを検知するコンポーネント
public class UISelectionEffect : MonoBehaviour, ISelectHandler, IDeselectHandler
{
    // 選択された時に表示したいオブジェクト（矢印や枠線など）
    [SerializeField] private GameObject selectionVisual;

    void Awake()
    {
        // 最初は非表示にしておく
        if (selectionVisual != null)
        {
            selectionVisual.SetActive(false);
        }
    }

    // スティック等でこのUIが選ばれた時に実行される
    public void OnSelect(BaseEventData eventData)
    {
        print("呼ばれた");
        if (selectionVisual != null)
        {
            selectionVisual.SetActive(true);
        }
    }

    // 別のUIに選択が移った（解除された）時に実行される
    public void OnDeselect(BaseEventData eventData)
    {
        if (selectionVisual != null)
        {
        print("削除された");
            selectionVisual.SetActive(false);
        }
    }

    // ゲームオブジェクトが非アクティブになった際も非表示にする安全処理
    void OnDisable()
    {
        if (selectionVisual != null)
        {
            selectionVisual.SetActive(false);
        }
    }
}