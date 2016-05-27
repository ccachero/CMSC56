using UnityEngine;
using System.Collections;

public class PathEntity : MonoBehaviour {

    public Vector2 BaseSize = Vector2.one;

    private PathingMap map;
    public PathingMap Map {
        get {
            if (map == null) map = GameObject.FindObjectOfType<PathingMap>();
            return map;
        }
    }

    public Rect Bounds {
        get {
            var fwd = transform.forward;
            var rgt = transform.right;
            var aabbSize = new Vector2(
                Mathf.Abs(rgt.x) * BaseSize.x + Mathf.Abs(fwd.x) * BaseSize.y,
                Mathf.Abs(rgt.z) * BaseSize.x + Mathf.Abs(fwd.z) * BaseSize.y
            );
            var bounds = new Rect(
                transform.position.x - aabbSize.x / 2,
                transform.position.z - aabbSize.y / 2,
                aabbSize.x, aabbSize.y);
            return bounds;
        }
    }

    public void OnEnable() {
        if (Map != null) Map.AddCost(Bounds);
    }

    public void OnDisable() {
        if (Map != null) Map.RemoveCost(Bounds);
    }

}
