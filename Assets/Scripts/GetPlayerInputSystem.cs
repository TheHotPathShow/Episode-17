using Unity.Entities;

namespace TMG.BraiDOTS
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderFirst = true)]
    public partial class GetPlayerInputSystem : SystemBase
    {
        private BraidInputActions _inputActions;

        protected override void OnCreate()
        {
            _inputActions = new BraidInputActions();
            _inputActions.Enable();
        }

        protected override void OnUpdate()
        {
            var playerMoveInput = SystemAPI.GetSingletonRW<PlayerMoveInput>();
            var horizontalMovement = _inputActions.SideScrollMap.HorizontalMovement.ReadValue<float>();
            playerMoveInput.ValueRW.HorizontalMovement = horizontalMovement;
            playerMoveInput.ValueRW.PreviousJumpButtonState = playerMoveInput.ValueRO.JumpButtonHeld;
            playerMoveInput.ValueRW.JumpButtonHeld = _inputActions.SideScrollMap.Jump.IsPressed();

            var rewindData = SystemAPI.GetSingletonRW<RewindData>();
            rewindData.ValueRW.RewindInputPreviousState = rewindData.ValueRO.RewindInput;
            rewindData.ValueRW.RewindInput = _inputActions.SideScrollMap.Rewind.IsPressed();

            rewindData.ValueRW.IncreaseRewindSpeedPreviousState = rewindData.ValueRO.IncreaseRewindSpeedPressed;
            rewindData.ValueRW.IncreaseRewindSpeedPressed = _inputActions.SideScrollMap.IncreaseRewindSpeed.IsPressed();
            if (rewindData.ValueRO.IncreaseRewindSpeedPressedThisFrame)
            {
                rewindData.ValueRW.IncreaseRewindSpeed();
            }

            rewindData.ValueRW.DecreaseRewindSpeedPreviousState = rewindData.ValueRO.DecreaseRewindSpeedPressed;
            rewindData.ValueRW.DecreaseRewindSpeedPressed = _inputActions.SideScrollMap.DecreaseRewindSpeed1.IsPressed();
            if (rewindData.ValueRO.DecreaseRewindSpeedPressedThisFrame)
            {
                rewindData.ValueRW.DecreaseRewindSpeed();
            }
        }
    }

    public enum PlaybackSpeed : sbyte
    {
        Reverse8 = -8,
        Reverse4 = -4,
        Reverse2 = -2,
        Reverse1 = -1,
        Frozen   = 0,
        Forward1 = 1,
        Forward2 = 2,
        Forward4 = 4,
        Forward8 = 8
    }
    
    public struct RewindData : IComponentData
    {
        public PlaybackSpeed PlaybackSpeed;
        public uint MaxFrameIndex;
        public uint SeekIndex;
        public bool RewindInput;
        public bool RewindInputPreviousState;
        public bool RewindInputEndedThisFrame => RewindInput == false && RewindInputPreviousState;

        public bool IncreaseRewindSpeedPressed;
        public bool IncreaseRewindSpeedPreviousState;
        public bool IncreaseRewindSpeedPressedThisFrame => IncreaseRewindSpeedPressed && IncreaseRewindSpeedPreviousState == false;
        
        public bool DecreaseRewindSpeedPressed;
        public bool DecreaseRewindSpeedPreviousState;
        public bool DecreaseRewindSpeedPressedThisFrame => DecreaseRewindSpeedPressed && DecreaseRewindSpeedPreviousState == false;
                
        public void IncreaseRewindSpeed()
        {
            PlaybackSpeed = PlaybackSpeed switch
            {
                PlaybackSpeed.Forward8 => PlaybackSpeed.Forward4,
                PlaybackSpeed.Forward4 => PlaybackSpeed.Forward2,
                PlaybackSpeed.Forward2 => PlaybackSpeed.Forward1,
                PlaybackSpeed.Forward1 => PlaybackSpeed.Frozen,
                PlaybackSpeed.Frozen   => PlaybackSpeed.Reverse1,
                PlaybackSpeed.Reverse1 => PlaybackSpeed.Reverse2,
                PlaybackSpeed.Reverse2 => PlaybackSpeed.Reverse4,
                PlaybackSpeed.Reverse4 => PlaybackSpeed.Reverse8,
                _ => PlaybackSpeed
            };
        }

        public void DecreaseRewindSpeed()
        {
            PlaybackSpeed = PlaybackSpeed switch
            {
                PlaybackSpeed.Forward4 => PlaybackSpeed.Forward8,
                PlaybackSpeed.Forward2 => PlaybackSpeed.Forward4,
                PlaybackSpeed.Forward1 => PlaybackSpeed.Forward2,
                PlaybackSpeed.Frozen   => PlaybackSpeed.Forward1,
                PlaybackSpeed.Reverse1 => PlaybackSpeed.Frozen,
                PlaybackSpeed.Reverse2 => PlaybackSpeed.Reverse1,
                PlaybackSpeed.Reverse4 => PlaybackSpeed.Reverse2,
                PlaybackSpeed.Reverse8 => PlaybackSpeed.Reverse4,
                _ => PlaybackSpeed
            };
        }
    }
}