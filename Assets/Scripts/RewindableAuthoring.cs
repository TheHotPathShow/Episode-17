using Unity.Entities;
using UnityEngine;

namespace TMG.BraiDOTS
{
    public class RewindableAuthoring : MonoBehaviour
    {
        public class RewindableBaker : Baker<RewindableAuthoring>
        {
            public override void Bake(RewindableAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<RewindableTag>(entity);
            }
        }
    }

    public struct RewindableTag : IComponentData
    {
    }
}