#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[CustomEditor(typeof(CoreCarryController))]
public sealed class CoreCarrySetupEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox(
            "両手IKを動かすには、Animator ControllerのBase LayerでIK Passを有効にします。",
            MessageType.Info);

        if (GUILayout.Button("AnimatorのIK Passを有効化"))
        {
            EnableIkPass((CoreCarryController)target);
        }
    }

    private static void EnableIkPass(CoreCarryController controller)
    {
        Animator animator = controller.GetComponentInChildren<Animator>();
        if (animator == null)
        {
            Debug.LogError("プレイヤー配下にAnimatorが見つかりません。");
            return;
        }

        var animatorController = animator.runtimeAnimatorController as AnimatorController;
        if (animatorController == null || animatorController.layers.Length == 0)
        {
            Debug.LogError("Animator ControllerまたはBase Layerが見つかりません。");
            return;
        }

        AnimatorControllerLayer[] layers = animatorController.layers;
        layers[0].iKPass = true;
        animatorController.layers = layers;

        EditorUtility.SetDirty(animatorController);
        AssetDatabase.SaveAssets();
        Debug.Log("Base LayerのIK Passを有効にしました。");
    }
}
#endif
