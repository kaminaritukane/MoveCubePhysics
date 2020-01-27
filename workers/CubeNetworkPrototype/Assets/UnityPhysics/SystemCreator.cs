using Unity.Entities;
using UnityEngine;

public class SystemCreator : MonoBehaviour
{
    //private PhysicsBodiesMoveSystem moveSys;
    public int cubeCount = 1;

    static public SystemCreator Instance = null;

    private void Start()
    {
        Instance = this;

        var compSysGroup = World.Active.GetExistingSystem<SimulationSystemGroup>();

        var moveSys = World.Active.GetOrCreateSystem<PhysicsBodiesMoveSystem>();

        compSysGroup.AddSystemToUpdateList(moveSys);
    }
}
