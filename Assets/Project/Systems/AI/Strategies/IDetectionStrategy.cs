using UnityEngine;
using Utilities;

namespace Project
{
    public interface IDetectionStrategy
    {
        bool Execute(Transform target, Transform detector, CountdownTimer timer);
    }
}
