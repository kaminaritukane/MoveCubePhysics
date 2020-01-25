using Improbable.Gdk.Core;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public sealed class SimulatedUnitManager
{
    private static SimulatedUnitManager instance = null;

    private readonly Dictionary<WorkerInWorld, SimulatedUnitCreator> networkingSystemWrappers = new Dictionary<WorkerInWorld, SimulatedUnitCreator>();

    private readonly Dictionary<string, GameObject> cachedPrefabs = new Dictionary<string, GameObject>();
    private readonly Dictionary<EntityId, GameObject> entityIdToGameObject = new Dictionary<EntityId, GameObject>();

    public static SimulatedUnitManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new SimulatedUnitManager();
            }
            return instance;
        }
    }

    public GameObject GetPrefab(string prefabName, bool hasAuthority, string workerType)
    {
        if (!cachedPrefabs.TryGetValue(prefabName, out var prefab))
        {
            string workerSpecificPath = "";
            if (hasAuthority)
            {
                workerSpecificPath = Path.Combine("Prefabs", workerType, "Authority", prefabName);
            }
            else
            {
                workerSpecificPath = Path.Combine("Prefabs", workerType, prefabName);
            }
            prefab = Resources.Load<GameObject>(workerSpecificPath);
            cachedPrefabs[prefabName] = prefab;
        }

        return prefab;
    }

    public void RegisterEntityGO(EntityId entityId, GameObject go)
    {
        entityIdToGameObject[entityId] = go;
    }

    public void UnregisterEntityGO(EntityId entityId)
    {
        entityIdToGameObject.Remove(entityId);
    }

    public GameObject GetEntityGO(EntityId entityId)
    {
        GameObject value = null;
        entityIdToGameObject.TryGetValue(entityId, out value);
        return value;
    }

    public void RegisterWorkerEntityCreator(WorkerInWorld worker, SimulatedUnitCreator creator)
    {
        networkingSystemWrappers[worker] = creator;
    }

    public void UnregisterWorkerEntityCreator(WorkerInWorld worker)
    {
        if (worker != null)
        {
            networkingSystemWrappers?.Remove(worker);
        }
    }

    public List<GameObject> GetAllEntityGameObjects()
    {
        var gos = new List<GameObject>();
        foreach( var goc in entityIdToGameObject.Values )
        {
            gos.Add(goc);
        }
        return gos;
    }

    public int GetEntityAmount()
    {
        return entityIdToGameObject.Count;
    }
}
