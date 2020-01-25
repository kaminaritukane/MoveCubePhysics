using BlankProject;
using Improbable.Gdk.Core;
using Improbable.Gdk.Subscriptions;
using UnityEngine;

public class MovementPackageReceiver : MonoBehaviour
{
    [Require] ServerUnitTransformReader serverTransformReader = null;
    [Require] EntityId entityId = default;

    [SerializeField] bool isSimulatedPlayer = false;

    void Start()
    {
        serverTransformReader.OnUnitsTransformSyncEvent += OnUnitsTransformSyncEvent;
    }

    private void OnDisable()
    {
        serverTransformReader.OnUnitsTransformSyncEvent -= OnUnitsTransformSyncEvent;
    }

    private void OnUnitsTransformSyncEvent(UnitsTransformPackage obj)
    {
        foreach( var info in obj.EntityTransformInfos )
        {
            var unitId = info.EntityId;
            if ( isSimulatedPlayer && unitId != entityId )
            {
                // only update transform for authority simulated player
                continue;
            }

            var transInterpolation = UnityClientConnector.Instance.
                TransformGameObjectCreator.GetClientTransInterp(unitId);
            if ( transInterpolation != null )
            {
                var trans = info.TransformInfo;
                transInterpolation.nextPosition = trans.Position.ToVector3();
                transInterpolation.nextRotation = Quaternion.Euler(trans.Rotation.ToVector3());
                transInterpolation.deltaTime = trans.DeltaTime.ToFloat100k();
            }

        }
    }
}
