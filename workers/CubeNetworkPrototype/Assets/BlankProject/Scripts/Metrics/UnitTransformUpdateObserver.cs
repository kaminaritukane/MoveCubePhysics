using BlankProject;
using Improbable.Gdk.Subscriptions;
using UnityEngine;

namespace Pixelmatic.InfiniteFleet.SpatialOS.SimulatedPlayerMetrics
{
    public class UnitTransformUpdateObserver : MonoBehaviour
    {
        [Require] private ClientUnitTransformReader unitTransformReader = null;

        [SerializeField] bool IsSimulatedPlayer = false;

        private void Start()
        {
            unitTransformReader.OnUpdate += OnUpdate;   
        }

        private void OnDisable() 
        {
            unitTransformReader.OnUpdate -= OnUpdate;
        }

        private void OnUpdate(ClientUnitTransform.Update obj)
        {
            if (IsSimulatedPlayer)
            {
                SimulatedUnitMetrics.Instance?.CountUpdateReceived();
            }
            //else
            //{
            //    ServerMetrics.Instance?.CountUpdateReceived();
            //}
        }
    }
}
