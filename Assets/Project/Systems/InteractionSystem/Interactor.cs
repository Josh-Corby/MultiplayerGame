using Project;
using Unity.Netcode;
using UnityEngine;
using Utilities;

public class Interactor : NetworkBehaviour
{
    [SerializeField] private Transform _detectionStartPoint;
    [SerializeField] private float _detectionDistance;
    [SerializeField] private LayerMask _interactableMask;
    [SerializeField] private float _detectionInterval = 0.1f;

    private CountdownTimer _detectionTimer;

    public IInteractable Interactable { get; private set; }

    protected void Awake()
    {
        _detectionTimer = new CountdownTimer(_detectionInterval);

        _detectionTimer.OnTimerStop += () =>
        {
            RunInteractionCheck();
            _detectionTimer.Start();
        };

        _detectionTimer.Start();
    }

    private void Update() => _detectionTimer.Tick(Time.deltaTime);

    private void RunInteractionCheck()
    {
        Debug.DrawRay(_detectionStartPoint.position, _detectionStartPoint.transform.forward, Color.red, _detectionDistance);

        if (Physics.Raycast(_detectionStartPoint.position, _detectionStartPoint.transform.forward, out RaycastHit hit, 
                            _detectionDistance, _interactableMask, QueryTriggerInteraction.Collide))
        {
            if (hit.collider.gameObject.TryGetComponent<IInteractable>(out var interactable))
            {
                Interactable = interactable;
            }
        }
        else
        {
            Interactable = null;
        }
    }

    public void Interact()
    {
        Interactable?.Interact(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent<Pickup>(out var pickup))
        {
            if(pickup.PickupOnContact)
                pickup.Interact(this);
        }
    }
}
