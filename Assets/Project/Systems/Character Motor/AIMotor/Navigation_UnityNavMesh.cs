using UnityEngine;
using UnityEngine.AI;

namespace Project
{
    [RequireComponent(typeof(NavMeshAgent), 
        typeof(AICharacterMotor))]
    public class Navigation_UnityNavMesh : BaseNavigation
    {
        private NavMeshAgent _linkedAgent;
        private AICharacterMotor _aiMotor;

        private Vector3[] _currentPath;
        private int _targetPoint = -1;
        protected override void Initialise()
        {
            _linkedAgent = GetComponent<NavMeshAgent>();
            _aiMotor = GetComponent<AICharacterMotor>();

            _linkedAgent.updatePosition = false;
            _linkedAgent.updateRotation = false;
        }

        private void LateUpdate()
        {
            _linkedAgent.nextPosition = transform.position;
        }

        protected override bool RequestPath()
        {
            _linkedAgent.speed = _maxMoveSpeed;
            _linkedAgent.angularSpeed = _rotationSpeed;
            _linkedAgent.stoppingDistance = _destinationReachedThreshold;

            _linkedAgent.SetDestination(Destination);

            OnBeganPathFinding();

            return true;
        } 

        protected override void Tick_Default()
        {

        }

        protected override void Tick_Pathfinding()
        {
            // no pathfinding in progress?
            if(!_linkedAgent.pathPending)
            {
                if(_linkedAgent.pathStatus == NavMeshPathStatus.PathComplete)
                {
                    _currentPath = _linkedAgent.path.corners;
                    _targetPoint = 0;
                    OnPathFound();
                }
                else
                    OnFailedToFindPath();
            }
        }

        protected override void Tick_PathFollowing()
        {
            // get the 2D vector to the target
            Vector3 targetPosition = _currentPath[_targetPoint];
            Vector3 vectorToTarget = targetPosition - transform.position;
            vectorToTarget.y = 0;

            // reached the target point?
            if(vectorToTarget.magnitude <= _destinationReachedThreshold)
            {
                // advance to next point
                ++_targetPoint;

                // reached destination?
                if(_targetPoint == _currentPath.Length)
                {
                    _aiMotor.Stop();

                    OnReachedDestination();
                    return;
                }

                // refresh target information
                targetPosition = _currentPath[_targetPoint];
            }

            _aiMotor.SteerTowards(targetPosition, _rotationSpeed, _destinationReachedThreshold, _maxMoveSpeed);

            if (DEBUG_ShowHeading)
                Debug.DrawLine(transform.position + Vector3.up, _linkedAgent.steeringTarget, Color.green);
        }

        protected override void Tick_OrientingAtEndOfPath()
        {
            if(_aiMotor.LookTowards(LookTarget, _rotationSpeed)) 
                OnFacingLookTarget();
        }

        public override void StopMovement()
        {
            _linkedAgent.ResetPath();
            _currentPath = null;
            _targetPoint = -1;

            _aiMotor.Stop();
        }

        public override bool FindNearestPoint(Vector3 searchPos, float range, out Vector3 foundPos)
        {
            NavMeshHit hitResult;
            if(NavMesh.SamplePosition(searchPos, out hitResult, range, NavMesh.AllAreas))
            {
                foundPos = hitResult.position; 
                return true;
            }

            foundPos = searchPos;
            return false;
        }     
    }
}