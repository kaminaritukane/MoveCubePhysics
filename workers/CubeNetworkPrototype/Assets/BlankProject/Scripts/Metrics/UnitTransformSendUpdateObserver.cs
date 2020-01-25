using BlankProject;
using Improbable.Gdk.Subscriptions;
using UnityEngine;

namespace Pixelmatic.InfiniteFleet.SpatialOS.SimulatedPlayerMetrics
{
    [RequireComponent(typeof(TransformSender_Client))]
    public class UnitTransformSendUpdateObserver : MonoBehaviour
    {
        [SerializeField] bool IsSimulatedPlayer = false;

        private TransformSender_Client transSender;

        private void Start()
        {
            transSender = GetComponent<TransformSender_Client>();
            transSender.OnTransformSent += OnTransformSent;
        }

        private void OnDisable()
        {
            transSender.OnTransformSent -= OnTransformSent;
        }

        private void OnTransformSent()
        {
            if ( IsSimulatedPlayer )
            {
                SimulatedUnitMetrics.Instance?.CountUpdateSent();
            }
            //else
            //{
            //    ServerMetrics.Instance?.CountUpdateSent();
            //}
        }
    }
}
