using BlankProject;
using Improbable.Gdk.Core;
using Improbable.Gdk.PlayerLifecycle;
using Improbable.Gdk.Subscriptions;
using Improbable.Gdk.TransformSynchronization;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Improbable.Gdk.GameObjectCreation
{
    public class ServerGameObjectCreator : IEntityGameObjectCreator
    {
        private const string PlayerEntityType = "Player";

        private readonly Dictionary<string, GameObject> cachedPrefabs
            = new Dictionary<string, GameObject>();

        protected readonly WorkerInWorld worker;
        private readonly string workerType;
        private readonly Vector3 workerOrigin;

        //private readonly GameObjectCreatorFromMetadata fallback;

        public readonly Dictionary<EntityId, GameObject> entityIdToGameObject = new Dictionary<EntityId, GameObject>();
        public readonly Dictionary<EntityId, SpatialOSEntity> entityIdToSpatialEntity = new Dictionary<EntityId, SpatialOSEntity>();

        public readonly Dictionary<EntityId, ClientTransInterpolation> entityIdToClientTransInterp = new Dictionary<EntityId, ClientTransInterpolation>();

        private readonly Type[] componentsToAdd =
        {
            typeof(Transform),
            typeof(MeshRenderer)
        };

        public ServerGameObjectCreator(WorkerInWorld worker, string workerType, Vector3 workerOrigin)
        {
            this.worker = worker;
            this.workerType = workerType;
            this.workerOrigin = workerOrigin;

            //fallback = new GameObjectCreatorFromMetadata(workerType, workerOrigin, worker.LogDispatcher);
        }

        public GameObject GetGameObject(EntityId id)
        {
            GameObject ret = null;
            entityIdToGameObject.TryGetValue(id, out ret);
            return ret;
        }

        public ClientTransInterpolation GetClientTransInterp(EntityId id)
        {
            entityIdToClientTransInterp.TryGetValue(id, out var comp);
            return comp;
        }

        public void OnEntityCreated(SpatialOSEntity entity, EntityGameObjectLinker linker)
        {
            if (!entity.TryGetComponent<Metadata.Component>(out var metadata))
            {
                return;
            }

            entityIdToSpatialEntity.Add(entity.SpatialOSEntityId, entity);

            var prefabName = metadata.EntityType;
            if ( prefabName == PlayerEntityType )
            {
                CreatePlayerGameObject(prefabName, entity, linker);
            }
            else
            {
                var hasAuthority = PlayerLifecycleHelper.IsOwningWorker(entity.SpatialOSEntityId, worker.World); ;

                worker.LogDispatcher.HandleLog(LogType.Log,
                    new LogEvent($"GameObjectCreatorFromTransform.OnEntityCreated:{entity.SpatialOSEntityId.Id}," +
                    $" {metadata.EntityType}, hasAuthority: {hasAuthority}"));

                //fallback.OnEntityCreated(entity, linker);
                CreateLinkedGameObject(entity, linker, hasAuthority, prefabName);
            }
        }

        public void OnEntityRemoved(EntityId entityId)
        {
            entityIdToSpatialEntity.Remove(entityId);

            //fallback.OnEntityRemoved(entityId);

            if (!entityIdToGameObject.TryGetValue(entityId, out var go))
            {
                return;
            }

            Object.Destroy(go);
            entityIdToGameObject.Remove(entityId);
            entityIdToClientTransInterp.Remove(entityId);
        }

        public int GetEntityAmount()
        {
            return entityIdToSpatialEntity.Count;
        }

        public int GetEntityCubeAmount()
        {
            int count = 0;
            foreach( var kv in entityIdToSpatialEntity )
            {
                if (kv.Value.HasComponent<MoveCube.Component>())
                {
                    count++;
                }
            }

            return count;
        }

        private void CreatePlayerGameObject(string prefabName, SpatialOSEntity entity, EntityGameObjectLinker linker)
        {
            if (!entity.TryGetComponent<TransformInternal.Component>(out var transformInternal))
            {
                throw new InvalidOperationException("Player entity does not have the TransformInternal component");
            }

            if (!entity.TryGetComponent<OwningWorker.Component>(out var owningWorker))
            {
                throw new InvalidOperationException("Player entity does not have the OwningWorker component");
            }

            var position = transformInternal.Location.ToUnityVector() + workerOrigin;
            var rotation = transformInternal.Rotation.ToUnityQuaternion();

            var hasAuthority = owningWorker.WorkerId == worker.WorkerId;//PlayerLifecycleHelper.IsOwningWorker(entity.SpatialOSEntityId, worker.World);
            worker.LogDispatcher.HandleLog(LogType.Log,
                new LogEvent($"GameObjectCreatorFromTransform.CreatePlayerGameObject" +
                $" EntityId:{entity.SpatialOSEntityId.Id}, " +
                $" owningWorker:{owningWorker.WorkerId}, currentWorker:{worker.WorkerId}," +
                $" hasAuthority:{hasAuthority}"));

            if ( hasAuthority )
            {
                prefabName = $"Camera{prefabName}";
            }

            if (!cachedPrefabs.TryGetValue(prefabName, out var prefab))
            {
                var commonPath = Path.Combine("Prefabs", "Common", prefabName);

                var workerSpecificPath = "";
                if (hasAuthority)
                {
                    workerSpecificPath = Path.Combine("Prefabs", workerType, "Authority", prefabName);
                }
                else
                {
                    workerSpecificPath = Path.Combine("Prefabs", workerType, prefabName);
                }

                prefab = Resources.Load<GameObject>(workerSpecificPath) ?? Resources.Load<GameObject>(commonPath);

                cachedPrefabs[prefabName] = prefab;
            }

            if (prefab == null)
            {
                return;
            }

            var goName = prefab.name;
            if (entity.TryGetComponent<PlayerState.Component>(out var ps)
                && !string.IsNullOrEmpty(ps.Name))
            {
                goName = ps.Name;
            }

            var gameObject = Object.Instantiate(prefab, position, rotation);
            gameObject.name = $"{goName}({entity.SpatialOSEntityId}, {workerType})";

            entityIdToGameObject.Add(entity.SpatialOSEntityId, gameObject);
            var transInterpComp = gameObject.GetComponent<ClientTransInterpolation>();
            if ( transInterpComp != null )
            {
                entityIdToClientTransInterp.Add(entity.SpatialOSEntityId, transInterpComp);
            }

            linker.LinkGameObjectToSpatialOSEntity(entity.SpatialOSEntityId, gameObject, componentsToAdd);
        }

        protected GameObject CreateLinkedGameObject(
            SpatialOSEntity entity,
            EntityGameObjectLinker linker,
            bool hasAuthority,
            string prefabName)
        {
            Vector3 position = Vector3.zero;
            if (entity.TryGetComponent<Position.Component>(out var posComp))
            {
                position = posComp.Coords.ToUnityVector() + workerOrigin;
            }

            string workerSpecificPath = prefabName;
            if (hasAuthority)
            {
                workerSpecificPath = Path.Combine("Prefabs", workerType, "Authority", prefabName);
            }
            else
            {
                workerSpecificPath = Path.Combine("Prefabs", workerType, prefabName);
            }

            GameObject gameObject = null;
            if (!cachedPrefabs.TryGetValue(workerSpecificPath, out var prefab))
            {
                prefab = Resources.Load<GameObject>(workerSpecificPath);
                cachedPrefabs[workerSpecificPath] = prefab;
            }

            if (prefab == null)
            {
                //Debug.LogWarning($"Creating GameObject of entity failed: no prefab found at {workerSpecificPath}, " +
                //                 $"or the snapshot doesn't have a prefab associated.");
                return null;
            }

            gameObject = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);

            gameObject.name = $"{prefab.name}({entity.SpatialOSEntityId}, {workerType})";

            entityIdToGameObject.Add(entity.SpatialOSEntityId, gameObject);

            var transInterpComp = gameObject.GetComponent<ClientTransInterpolation>();
            if (transInterpComp != null)
            {
                entityIdToClientTransInterp.Add(entity.SpatialOSEntityId, transInterpComp);
            }

            linker.LinkGameObjectToSpatialOSEntity(entity.SpatialOSEntityId, gameObject, componentsToAdd);

            return gameObject;
        }
    }
}
