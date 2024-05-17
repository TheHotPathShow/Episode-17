using Unity.Entities;
using Unity.Mathematics;

namespace TMG.BraiDOTS
{
    public struct PlayerMoveProperties : IComponentData
    {
        public float MoveSpeedAcceleration;
        public float MaxMoveSpeed;
        public float JumpForce;
    }

    public struct PhysicsWorldProperties : IComponentData
    {
        public float GravityForce;
        public float MaxFallSpeed;
    }

    public struct CharacterVelocity : IComponentData
    {
        public float2 Value;
    }
    
    public struct PlayerMoveInput : IComponentData
    {
        public float HorizontalMovement;
        public bool JumpButtonHeld;
        public bool PreviousJumpButtonState;

        public bool JumpPressedThisFrame => JumpButtonHeld && PreviousJumpButtonState == false;
    }
}