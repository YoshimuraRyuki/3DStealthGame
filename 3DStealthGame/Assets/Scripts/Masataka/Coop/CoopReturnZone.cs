using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(BoxCollider))]
public sealed class CoopReturnZone : MonoBehaviour
{
    public UnityEvent onCleared;
    private void Awake() { GetComponent<BoxCollider>().isTrigger = true; }
    public bool Contains(Vector3 position)
    {
        BoxCollider box = GetComponent<BoxCollider>();
        Vector3 p = transform.InverseTransformPoint(position) - box.center;
        Vector3 half = box.size * 0.5f;
        return Mathf.Abs(p.x) <= half.x && Mathf.Abs(p.y) <= half.y && Mathf.Abs(p.z) <= half.z;
    }
    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>(); if (box == null) return;
        Gizmos.color = Color.green; Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
