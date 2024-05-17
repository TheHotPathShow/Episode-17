using Unity.Entities;
using UnityEngine;

namespace TMG.BraiDOTS
{
    public class FallGuyAuthoring : MonoBehaviour
    {
        public float GroundDistance;
        
        public class FallGuyBaker : Baker<FallGuyAuthoring>
        {
            public override void Bake(FallGuyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new GroundDistance { Value = authoring.GroundDistance });
                AddComponent<CharacterVelocity>(entity);
            }
        }
    }

    public struct GroundDistance : IComponentData
    {
        public float Value;
        public float ValueWithExtra => Value + 0.1f;
    }
}