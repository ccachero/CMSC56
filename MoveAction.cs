using UnityEngine;
using System.Collections;

using UnityRTS;

/// <summary>
/// The MoveAction provides logic to move an object through
/// the world
/// </summary>
public class MoveAction : Action {

    // The speed at which to move
    public float Speed = 1;

    // The action is complete when it has reached its destination
    public override bool Complete {
        get { return query == null || Vector2.Distance(Request.GetTargetLocation(), entity.Position) < 0.5f; }
    }

    // A requst always contains a location, thus this action is always valid
    public override float ScoreRequest(ActionRequest request) { return 1; }

    private PathingQuery query;

    public override void Begin(ActionRequest request) {
        var pos = request.TargetLocation;
        query = new PathingQuery(
            GameObject.FindObjectOfType<PathingMap>(),
            new Rect(pos.x - 0.5f, pos.y - 0.5f, 1, 1), 1);
        base.Begin(request);
    }

    // Move the owning entity toward the target
    public override void Step(float dt) {
        var target = Request.GetTargetLocation();
        //entity.Position = Vector2.MoveTowards(entity.Position, target, dt * Speed);
        if (query != null) {
            var dir = query.GetDirectionFrom(entity.Position);
            entity.Position += dir * dt * Speed;
        }
        base.Step(dt);
    }

    public void OnDrawGizmos() {
        var mray = Camera.current.ScreenPointToRay(Input.mousePosition);
        var mpos = mray.origin + mray.direction * (1 - mray.origin.y) / mray.direction.y;
        if (mpos.x > 35 && mpos.x < 65 && mpos.z > 25 && mpos.z < 45 && query != null) {
            var dir = query.GetDirectionFrom(mpos.XZ(), true);
            Gizmos.DrawWireSphere(query.Destination.center.Y(0), 0.1f);
        }
    }

}
