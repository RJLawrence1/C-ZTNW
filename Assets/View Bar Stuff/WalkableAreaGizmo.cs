using UnityEngine;

public class WalkableAreaGizmo : MonoBehaviour
{
    public Color gizmoColor = new Color(0, 1, 0, 0.2f);

    void OnDrawGizmos()
    {
        PolygonCollider2D poly = GetComponent<PolygonCollider2D>();
        if (poly == null) return;

        Gizmos.color = gizmoColor;
        Vector2[] points = poly.points;

        for (int i = 0; i < points.Length; i++)
        {
            Vector2 current = transform.TransformPoint(points[i]);
            Vector2 next = transform.TransformPoint(points[(i + 1) % points.Length]);
            Gizmos.DrawLine(current, next);
        }
    }
}