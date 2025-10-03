using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ConveyorBelt2D : MonoBehaviour
{
    public enum ApplyMode { ImposeGroundVelocity, AddForce }
    public enum DirectionMode { Static, Alternating } // flip direction on a timer

    [Header("Belt Motion")]
    [Tooltip("Base belt speed (units/sec). Positive = right, Negative = left.")]
    public float speed = 3f;

    [Tooltip("How we apply motion. ImposeGroundVelocity is crisp; AddForce is softer.")]
    public ApplyMode applyMode = ApplyMode.ImposeGroundVelocity;

    [Header("Carry/Boost Tuning (ImposeGroundVelocity)")]
    [Tooltip("Extra speed when the body already moves in the belt direction.")]
    public float sameDirectionBonus = 2f;

    [Tooltip("How quickly we steer horizontal velocity toward the target (units/sec^2).")]
    public float targetAccel = 30f;

    [Header("Direction")]
    public DirectionMode directionMode = DirectionMode.Static;
    [Tooltip("Seconds between flips when Alternating.")]
    public float alternateInterval = 2.5f;

    [Header("Detection")]
    [Tooltip("Trigger slightly above the belt surface.")]
    public Collider2D topTrigger;
    [Tooltip("Vertical trigger hugging the LEFT edge.")]
    public Collider2D leftEdgeTrigger;
    [Tooltip("Vertical trigger hugging the RIGHT edge.")]
    public Collider2D rightEdgeTrigger;

    [Tooltip("Only affect these layers (e.g., Player).")]
    public LayerMask affectedLayers = ~0;

    [Tooltip("Only push objects whose center is within this many meters above the belt center.")]
    public float topTolerance = 0.75f;

    [Header("Force Mode (when AddForce)")]
    public float forceScale = 1.0f;   // multiply by rb.mass internally

    [Header("Edge Nudge")]
    [Tooltip("Vertical nudge at edges (units/sec if ImposeGroundVelocity, or scaled force if AddForce).")]
    public float edgeLift = 4f;

    // tracked bodies currently contacting zones
    private readonly HashSet<Rigidbody2D> _topBodies = new HashSet<Rigidbody2D>();
    private readonly HashSet<Rigidbody2D> _leftEdge = new HashSet<Rigidbody2D>();
    private readonly HashSet<Rigidbody2D> _rightEdge = new HashSet<Rigidbody2D>();

    private float _timer;
    private int _dirSign = 1; // +1 right, -1 left
    private float _beltY;

    void Reset()
    {
        if (!topTrigger || !leftEdgeTrigger || !rightEdgeTrigger)
        {
            foreach (var col in GetComponentsInChildren<Collider2D>())
            {
                if (!col.isTrigger) continue;
                if (!topTrigger) { topTrigger = col; continue; }
                if (!leftEdgeTrigger) { leftEdgeTrigger = col; continue; }
                if (!rightEdgeTrigger) { rightEdgeTrigger = col; continue; }
            }
        }
    }

    void Awake()
    {
        if (!topTrigger) Debug.LogWarning($"[{name}] Assign topTrigger.");
        if (!leftEdgeTrigger) Debug.LogWarning($"[{name}] Assign leftEdgeTrigger.");
        if (!rightEdgeTrigger) Debug.LogWarning($"[{name}] Assign rightEdgeTrigger.");
        _beltY = transform.position.y;
    }

    void OnEnable()
    {
        HookTrigger(topTrigger, ConveyorTriggerHelper.Zone.Top, true);
        HookTrigger(leftEdgeTrigger, ConveyorTriggerHelper.Zone.LeftEdge, true);
        HookTrigger(rightEdgeTrigger, ConveyorTriggerHelper.Zone.RightEdge, true);
    }

    void OnDisable()
    {
        HookTrigger(topTrigger, ConveyorTriggerHelper.Zone.Top, false);
        HookTrigger(leftEdgeTrigger, ConveyorTriggerHelper.Zone.LeftEdge, false);
        HookTrigger(rightEdgeTrigger, ConveyorTriggerHelper.Zone.RightEdge, false);

        // Clear belt effect if we disable mid-contact
        foreach (var rb in _topBodies)
        {
            var r = rb ? rb.GetComponent<ConveyorRider>() : null;
            if (r) r.groundVX = 0f;
        }
        _topBodies.Clear(); _leftEdge.Clear(); _rightEdge.Clear();
    }

    void Update()
    {
        if (directionMode == DirectionMode.Alternating)
        {
            _timer += Time.deltaTime;
            if (_timer >= alternateInterval)
            {
                _timer = 0f;
                _dirSign *= -1; // flip direction
            }
        }
    }

    void FixedUpdate()
    {
        float belt = _dirSign * Mathf.Abs(speed);

        // --- Top belt carry (continuous) ---
        if (_topBodies.Count > 0)
        {
            foreach (var rb in _topBodies)
            {
                if (!rb) continue;
                if (rb.worldCenterOfMass.y < _beltY - topTolerance) continue;

                if (applyMode == ApplyMode.ImposeGroundVelocity)
                {
                    // Preferred path: write into rider so the player code adds it.
                    var rider = rb.GetComponent<ConveyorRider>();
                    if (rider)
                    {
                        // Player's own x ≈ total - current belt contribution
                        float selfX = rb.linearVelocity.x - rider.groundVX;
                        bool sameDir = (selfX != 0f) && (Mathf.Sign(selfX) == Mathf.Sign(belt));
                        float target = belt + (sameDir ? Mathf.Abs(sameDirectionBonus) * Mathf.Sign(belt) : 0f);

                        rider.groundVX = Mathf.MoveTowards(rider.groundVX, target, targetAccel * Time.fixedDeltaTime);
                    }
                    else
                    {
                        // Fallback if no rider present: steer velocity directly
                        float curX = rb.linearVelocity.x;
                        bool sameDir = (curX != 0f) && (Mathf.Sign(curX) == Mathf.Sign(belt));
                        float targetX = belt + (sameDir ? Mathf.Abs(sameDirectionBonus) * Mathf.Sign(belt) : 0f);
                        float nextX = Mathf.MoveTowards(curX, targetX, targetAccel * Time.fixedDeltaTime);
                        rb.linearVelocity = new Vector2(nextX, rb.linearVelocity.y);
                    }
                }
                else // AddForce (gentler; keeps adding while on top)
                {
                    rb.AddForce(new Vector2(belt * rb.mass * forceScale, 0f), ForceMode2D.Force);
                }
            }
        }

        // --- Edge vertical nudge ---
        // Leading edge (direction of motion) nudges UP; trailing edge nudges DOWN.
        float up = Mathf.Abs(edgeLift);
        float down = -Mathf.Abs(edgeLift);

        if (applyMode == ApplyMode.ImposeGroundVelocity)
        {
            if (_dirSign > 0)
            {
                foreach (var rb in _rightEdge) if (rb) rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y + up);
                foreach (var rb in _leftEdge) if (rb) rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y + down);
            }
            else
            {
                foreach (var rb in _leftEdge) if (rb) rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y + up);
                foreach (var rb in _rightEdge) if (rb) rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y + down);
            }
        }
        else // AddForce
        {
            if (_dirSign > 0)
            {
                foreach (var rb in _rightEdge) if (rb) rb.AddForce(new Vector2(0f, up * rb.mass * forceScale), ForceMode2D.Force);
                foreach (var rb in _leftEdge) if (rb) rb.AddForce(new Vector2(0f, down * rb.mass * forceScale), ForceMode2D.Force);
            }
            else
            {
                foreach (var rb in _leftEdge) if (rb) rb.AddForce(new Vector2(0f, up * rb.mass * forceScale), ForceMode2D.Force);
                foreach (var rb in _rightEdge) if (rb) rb.AddForce(new Vector2(0f, down * rb.mass * forceScale), ForceMode2D.Force);
            }
        }
    }

    // --- trigger wiring ---
    void HookTrigger(Collider2D trigger, ConveyorTriggerHelper.Zone zone, bool subscribe)
    {
        if (!trigger) return;
        var helper = trigger.GetComponent<ConveyorTriggerHelper>();
        if (!helper) helper = trigger.gameObject.AddComponent<ConveyorTriggerHelper>();
        if (subscribe) helper.Setup(this, affectedLayers, zone);
        else helper.Setup(null, affectedLayers, zone);
    }

    public void HandleEnter(Collider2D other, ConveyorTriggerHelper.Zone zone)
    {
        var rb = other.attachedRigidbody;
        if (!rb || !IsLayerAffected(rb.gameObject.layer)) return;

        switch (zone)
        {
            case ConveyorTriggerHelper.Zone.Top: _topBodies.Add(rb); break;
            case ConveyorTriggerHelper.Zone.LeftEdge: _leftEdge.Add(rb); break;
            case ConveyorTriggerHelper.Zone.RightEdge: _rightEdge.Add(rb); break;
        }
    }

    public void HandleExit(Collider2D other, ConveyorTriggerHelper.Zone zone)
    {
        var rb = other.attachedRigidbody;
        if (!rb) return;

        switch (zone)
        {
            case ConveyorTriggerHelper.Zone.Top:
                _topBodies.Remove(rb);
                var rider = rb.GetComponent<ConveyorRider>();
                if (rider) rider.groundVX = 0f; // clear belt effect when leaving
                break;
            case ConveyorTriggerHelper.Zone.LeftEdge: _leftEdge.Remove(rb); break;
            case ConveyorTriggerHelper.Zone.RightEdge: _rightEdge.Remove(rb); break;
        }
    }

    bool IsLayerAffected(int layer) => (affectedLayers.value & (1 << layer)) != 0;

    // External controls
    public void SetDirectionRight(bool right)
    {
        _dirSign = right ? 1 : -1;
        directionMode = DirectionMode.Static;
    }

    public void StartAlternating(bool startRight)
    {
        _dirSign = startRight ? 1 : -1;
        _timer = 0f;
        directionMode = DirectionMode.Alternating;
    }
}

// Helper on each trigger to relay events with zone info
public class ConveyorTriggerHelper : MonoBehaviour
{
    public enum Zone { Top, LeftEdge, RightEdge }

    private ConveyorBelt2D _belt;
    private LayerMask _mask;
    private Zone _zone;

    public void Setup(ConveyorBelt2D belt, LayerMask mask, Zone zone)
    {
        _belt = belt;
        _mask = mask;
        _zone = zone;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!_belt) return;
        int layer = other.attachedRigidbody ? other.attachedRigidbody.gameObject.layer : other.gameObject.layer;
        if ((_mask.value & (1 << layer)) == 0) return;
        _belt.HandleEnter(other, _zone);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!_belt) return;
        int layer = other.attachedRigidbody ? other.attachedRigidbody.gameObject.layer : other.gameObject.layer;
        if ((_mask.value & (1 << layer)) == 0) return;
        _belt.HandleExit(other, _zone);
    }
}
