using BlankProject;
using Improbable;
using Improbable.Gdk.Core;
using Improbable.Gdk.GameObjectCreation;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.TransformSynchronization;
using System;
using UnityEngine;

public class SimulatedUnitCreator : IEntityGameObjectCreator
{
    protected readonly WorkerInWorld worker;
    protected readonly string workerType;

    private readonly Vector3 workerOrigin;

    private EntityId authorityEntityId = new EntityId(0);

    private readonly Type[] componentsToAdd =
    {
        typeof(Transform),
        typeof(MeshRenderer)
    };

    public SimulatedUnitCreator(WorkerInWorld worker, string workerType, Vector3 workerOrigin)
    {
        this.worker = worker;
        this.workerType = workerType;
        this.workerOrigin = workerOrigin;
    }

    public void OnEntityCreated(SpatialOSEntity entity, EntityGameObjectLinker linker)
    {
        var hasAuthority = PlayerLifecycleHelper.IsOwningWorker(entity.SpatialOSEntityId, worker.World);
        if (!hasAuthority)
        {
            return;
        }

        if (!entity.TryGetComponent<Metadata.Component>(out var metadata) ||
                !entity.TryGetComponent<TransformInternal.Component>(out var transformInternal))
        {
            return;
        }

        authorityEntityId = entity.SpatialOSEntityId;

        var prefabName = metadata.EntityType;

        var prefab = SimulatedUnitManager.Instance.GetPrefab(prefabName, hasAuthority, workerType);
        if ( prefab != null )
        {
            var position = transformInternal.Location.ToUnityVector() + workerOrigin;
            var rotation = transformInternal.Rotation.ToUnityQuaternion();

            var goName = prefab.name;
            if (entity.TryGetComponent<
                
                PlayerState.Component>(out var ps)
                && !string.IsNullOrEmpty(ps.Name) )
            {
                goName = ps.Name;
            }

            var go = UnityEngine.Object.Instantiate(prefab, position, rotation);
            go.name = $"{goName}({entity.SpatialOSEntityId}, Sim_{workerType})";

            SimulatedUnitManager.Instance.RegisterEntityGO(entity.SpatialOSEntityId, go);

            linker.LinkGameObjectToSpatialOSEntity(entity.SpatialOSEntityId, go, componentsToAdd);
        }
    }

    public void OnEntityRemoved(EntityId entityId)
    {
        if (authorityEntityId != entityId)
        {
            return;
        }

        var go = SimulatedUnitManager.Instance.GetEntityGO(entityId);
        if ( go != null )
        {
            SimulatedUnitManager.Instance.UnregisterEntityGO(entityId);
            UnityEngine.Object.Destroy(go);
        }

        authorityEntityId = new EntityId(0);
    }
}
