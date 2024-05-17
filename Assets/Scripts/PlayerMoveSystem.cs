using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace TMG.BraiDOTS
{
    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    [DisableAutoCreation]
    public partial struct PlayerMoveSystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            
            foreach (var (transform, velocity, moveInput, moveProperties, mass) in SystemAPI.Query<RefRW<LocalTransform>, RefRW<PhysicsVelocity>, PlayerMoveInput, PlayerMoveProperties, PhysicsMass>())
            {
                transform.ValueRW.Position.x += moveInput.HorizontalMovement * moveProperties.MaxMoveSpeed * deltaTime;

                if (moveInput.JumpPressedThisFrame)
                {
                    var movePropertiesJumpForce = math.up() * moveProperties.JumpForce;
                    velocity.ValueRW.ApplyLinearImpulse(in mass, in movePropertiesJumpForce);
                }
            }
        }
    }

    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial struct TurboPhysicsSystem : ISystem, ISystemStartStop
    {
        private PhysicsWorldProperties _physicsProperties;
        private CollisionFilter _groundCastFilter;
        
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<PhysicsWorldProperties>();
        }

        public void OnStartRunning(ref SystemState state)
        {
            _physicsProperties = SystemAPI.GetSingleton<PhysicsWorldProperties>();
            _groundCastFilter = new CollisionFilter
            {
                BelongsTo = 1 << 0,
                CollidesWith = 1 << 1
            };
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var deltaTime = SystemAPI.Time.DeltaTime;
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

            foreach (var (characterVelocity, transform, groundDistance) in SystemAPI.Query<RefRW<CharacterVelocity>, RefRW<LocalTransform>, GroundDistance>())
            {
                characterVelocity.ValueRW.Value += _physicsProperties.GravityForce * deltaTime * math.down().xy;
                transform.ValueRW.Position.xy += characterVelocity.ValueRO.Value * deltaTime;
                
                var raycastInput = new RaycastInput
                {
                    Start = transform.ValueRO.Position,
                    End = transform.ValueRO.Position + math.down() * groundDistance.Value,
                    Filter = _groundCastFilter
                };

                if (collisionWorld.CastRay(raycastInput, out var hit))
                {
                    characterVelocity.ValueRW.Value.y = 0f;
                    transform.ValueRW.Position.y = hit.Position.y + groundDistance.Value;
                }
            }

            foreach (var (characterVelocity, playerMoveInput, moveProperties, transform, groundDistance) in SystemAPI.Query<RefRW<CharacterVelocity>, PlayerMoveInput, PlayerMoveProperties, LocalTransform, GroundDistance>())
            {
                
                var raycastInput = new RaycastInput
                {
                    Start = transform.Position,
                    End = transform.Position + math.down() * groundDistance.ValueWithExtra,
                    Filter = _groundCastFilter
                };
                
                if (collisionWorld.CastRay(raycastInput, out var hit))
                {
                    if (playerMoveInput.JumpPressedThisFrame)
                    {
                        characterVelocity.ValueRW.Value.y = moveProperties.JumpForce;
                    }
                    characterVelocity.ValueRW.Value.x = playerMoveInput.HorizontalMovement * moveProperties.MaxMoveSpeed;
                }
                else
                {
                    characterVelocity.ValueRW.Value.x += playerMoveInput.HorizontalMovement * moveProperties.MoveSpeedAcceleration * deltaTime;
                    characterVelocity.ValueRW.Value.x = math.clamp(characterVelocity.ValueRO.Value.x, -1f * moveProperties.MaxMoveSpeed, moveProperties.MaxMoveSpeed);
                }
            }
        }

        public void OnStopRunning(ref SystemState state)
        {
        }
    }
}