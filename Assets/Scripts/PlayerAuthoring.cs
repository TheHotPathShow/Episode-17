using Unity.Entities;
using UnityEngine;

namespace TMG.BraiDOTS
{
    public class PlayerAuthoring : MonoBehaviour
    {
        public float MoveSpeedAcceleration;
        public float MaxMoveSpeed;
        public float JumpForce;
        
        public class PlayerBaker : Baker<PlayerAuthoring>
        {
            public override void Bake(PlayerAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);
                AddComponent<PlayerMoveInput>(entity);
                AddComponent(entity, new PlayerMoveProperties
                {
                    MoveSpeedAcceleration = authoring.MoveSpeedAcceleration,
                    MaxMoveSpeed = authoring.MaxMoveSpeed, 
                    JumpForce = authoring.JumpForce
                });
                AddComponent(entity, new RewindData
                {
                    PlaybackSpeed = PlaybackSpeed.Reverse1
                });
            }
        }
    }
}