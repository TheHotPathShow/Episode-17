using Unity.Entities;
using UnityEngine;

namespace TMG.BraiDOTS
{
    public class PhysicsWorldPropertiesAuthoring : MonoBehaviour
    {
        public float GravityForce;
        public float MaxFallSpeed;

        public class PhysicsWorldPropertiesBaker : Baker<PhysicsWorldPropertiesAuthoring>
        {
            public override void Bake(PhysicsWorldPropertiesAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent(entity, new PhysicsWorldProperties
                {
                    GravityForce = authoring.GravityForce, 
                    MaxFallSpeed = authoring.MaxFallSpeed
                });
            }
        }
    }
}