using BlankProject;
using Unity.Entities;

namespace ServerCommon
{
    public struct MovementComponent : IComponentData
    {
        public TransformInfo info;
    }
}
