using System;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace TMG.BraiDOTS
{
    [Flags]
    public enum ChangeBits : byte
    {
        None = 0,
        XPosition = 1 << 0,
        YPosition = 1 << 1,
        AnimationFrame = 1 << 2,
        AnimationTimer = 1 << 3,
        CharacterVelocity = 1 << 4,
    }

    public unsafe interface IRewindableComponent<T> : IComponentData where T : unmanaged
    {
        public UnsafeList<T>* BaseFrames { get; set; }
        public UnsafeList<T>* TweenFrames { get; set; }
        public int TweenFrameSeekIndex { get; set; }
    }

    public unsafe struct ChangeBitList: IComponentData
    {
        public UnsafeList<ChangeBits>* Value;
    }
    
    public unsafe struct XPositionLists : IRewindableComponent<float>
    {
        public UnsafeList<float>* BaseFrames { get; set; }
        public UnsafeList<float>* TweenFrames { get; set; }
        public int TweenFrameSeekIndex { get; set; }
    }

    public unsafe struct YPositionLists : IRewindableComponent<float>
    {
        public UnsafeList<float>* BaseFrames { get; set; }
        public UnsafeList<float>* TweenFrames { get; set; }
        public int TweenFrameSeekIndex { get; set; }
    }

    public unsafe struct VelocityListPtr : IRewindableComponent<float2> 
    {
        public UnsafeList<float2>* BaseFrames { get; set; }
        public UnsafeList<float2>* TweenFrames { get; set; }
        public int TweenFrameSeekIndex { get; set; }
    }
    
    public partial struct InitializeRewindSystem : ISystem
    {
        public unsafe void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);

            foreach (var (transform, entity) in SystemAPI.Query<LocalTransform>().WithAll<RewindableTag>().WithNone<ChangeBitList>().WithEntityAccess())
            {
                var changeBitList = new NativeList<ChangeBits>(Allocator.Persistent);
                ecb.AddComponent(entity, new ChangeBitList
                {
                    Value = changeBitList.GetUnsafeList()
                });

                var xPositionBaseList = new NativeList<float>(Allocator.Persistent);
                var yPositionBaseList = new NativeList<float>(Allocator.Persistent);
                var xPositionList = new NativeList<float>(Allocator.Persistent);
                var yPositionList = new NativeList<float>(Allocator.Persistent);

                ecb.AddComponent(entity, new XPositionLists
                {
                    BaseFrames = xPositionBaseList.GetUnsafeList(),
                    TweenFrames = xPositionList.GetUnsafeList(),
                    TweenFrameSeekIndex = -1
                });
                
                ecb.AddComponent(entity, new YPositionLists
                {
                    BaseFrames = yPositionBaseList.GetUnsafeList(),
                    TweenFrames = yPositionList.GetUnsafeList(),
                    TweenFrameSeekIndex = -1
                });

                var velocityBaseList = new NativeList<float2>(Allocator.Persistent);
                var velocityList = new NativeList<float2>(Allocator.Persistent);
                ecb.AddComponent(entity, new VelocityListPtr
                {
                    BaseFrames = velocityBaseList.GetUnsafeList(),
                    TweenFrames = velocityList.GetUnsafeList()
                });
            }

            ecb.Playback(state.EntityManager);
        }
    }
    
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderLast = true)]
    public partial class RewindSystem : SystemBase
    {
        private BraidInputActions _inputActions;

        protected override void OnCreate()
        {
            _inputActions = new BraidInputActions();
            _inputActions.Enable();
        }

        protected override unsafe void OnUpdate()
        {
            var rewindData = SystemAPI.GetSingletonRW<RewindData>();
            var isBaseFrame = (rewindData.ValueRO.SeekIndex - 1) % 120 == 0;

            // Is Rewinding
            if (rewindData.ValueRO.RewindInput)
            {
                for (var i = 0; i < math.max(1, math.abs((sbyte)rewindData.ValueRO.PlaybackSpeed)); i++)
                {
                    var shouldApplyRewind = i == math.max(1, math.abs((sbyte)rewindData.ValueRO.PlaybackSpeed)) - 1;
                    rewindData.ValueRW.SeekIndex = (uint)math.clamp(rewindData.ValueRO.SeekIndex + math.sign((sbyte)rewindData.ValueRO.PlaybackSpeed), 1, rewindData.ValueRO.MaxFrameIndex - 1);
                    var seekIndex = (int)rewindData.ValueRO.SeekIndex - 1;

                    foreach (var (changeBitListPtr, entity) in SystemAPI.Query<ChangeBitList>().WithEntityAccess())
                    {
                        ref var cbList = ref *changeBitListPtr.Value;
                        var curChangeBits = cbList[seekIndex];
                        var baseFrameIndex = (int)math.floor((float)rewindData.ValueRO.SeekIndex / 120);
                        var baseChangeBitIndex = baseFrameIndex * 120;
                        var baseChangeBits = cbList[baseChangeBitIndex];
                             
                        if (SystemAPI.HasComponent<LocalTransform>(entity))
                        {
                            var xPositionLists = SystemAPI.GetComponentRW<XPositionLists>(entity);
                            float xPosition;
                            var xPositionTweenList = *xPositionLists.ValueRO.TweenFrames;
                            if (!isBaseFrame && !xPositionTweenList.IsEmpty && (curChangeBits & ChangeBits.XPosition) == ChangeBits.XPosition)
                            {
                                xPositionLists.ValueRW.TweenFrameSeekIndex = math.clamp(math.sign((sbyte)rewindData.ValueRO.PlaybackSpeed) + xPositionLists.ValueRO.TweenFrameSeekIndex, 0, xPositionTweenList.Length - 1);
                                xPosition = xPositionTweenList[xPositionLists.ValueRO.TweenFrameSeekIndex];
                            }
                            else
                            {
                                var baseList = *xPositionLists.ValueRO.BaseFrames;
                                if ((baseChangeBits & ChangeBits.XPosition) == ChangeBits.XPosition)
                                {
                                    xPosition = baseList[baseFrameIndex];
                                }
                                else
                                {
                                    xPosition = baseList[0];
                                }
                            }

                            var yPositionLists = SystemAPI.GetComponentRW<YPositionLists>(entity);
                            var yPositionTweenList = *yPositionLists.ValueRO.TweenFrames;
                            float yPosition;
                            if (!isBaseFrame && !yPositionTweenList.IsEmpty && (curChangeBits & ChangeBits.YPosition) == ChangeBits.YPosition)
                            {
                                yPositionLists.ValueRW.TweenFrameSeekIndex = math.clamp(math.sign((sbyte)rewindData.ValueRO.PlaybackSpeed) + yPositionLists.ValueRO.TweenFrameSeekIndex, 0, yPositionTweenList.Length - 1);
                                yPosition = yPositionTweenList[yPositionLists.ValueRO.TweenFrameSeekIndex];
                            }
                            else
                            {
                                var baseList = *yPositionLists.ValueRO.BaseFrames;
                                if ((baseChangeBits & ChangeBits.YPosition) == ChangeBits.YPosition)
                                {
                                    yPosition = baseList[baseFrameIndex];
                                }
                                else
                                {
                                    yPosition = baseList[0];
                                }
                            }

                            if (shouldApplyRewind)
                            {
                                var transform = SystemAPI.GetComponentRW<LocalTransform>(entity);
                                transform.ValueRW.Position = new float3(xPosition, yPosition, 0f);
                            }
                        }

                        if (SystemAPI.HasComponent<CharacterVelocity>(entity))
                        {
                            var velocityLists = SystemAPI.GetComponentRW<VelocityListPtr>(entity);
                            var velocityTweenList = *velocityLists.ValueRO.TweenFrames;
                            float2 velocity;
                            if (!isBaseFrame && !velocityTweenList.IsEmpty && (curChangeBits & ChangeBits.CharacterVelocity) == ChangeBits.CharacterVelocity)
                            {
                                velocityLists.ValueRW.TweenFrameSeekIndex = math.clamp(math.sign((sbyte)rewindData.ValueRO.PlaybackSpeed) + velocityLists.ValueRO.TweenFrameSeekIndex, 0, velocityTweenList.Length - 1);
                                velocity = velocityTweenList[velocityLists.ValueRO.TweenFrameSeekIndex];
                            }
                            else
                            {
                                var baseList = *velocityLists.ValueRO.BaseFrames;
                                if ((baseChangeBits & ChangeBits.CharacterVelocity) == ChangeBits.CharacterVelocity)
                                {
                                    velocity = baseList[baseFrameIndex];
                                }
                                else
                                {
                                    velocity = baseList[0];
                                }
                            }

                            if (shouldApplyRewind)
                            {
                                var characterVelocity = SystemAPI.GetComponentRW<CharacterVelocity>(entity);
                                characterVelocity.ValueRW.Value = velocity;
                            }
                        }
                    }
                }
            }
            else 
            {
                // Stop rewinding, destroy all data in rewind buffers AHEAD of seek index
                if (rewindData.ValueRO.RewindInputEndedThisFrame)
                {
                    var seekIndex = (int)rewindData.ValueRO.SeekIndex - 1;
                    rewindData.ValueRW.MaxFrameIndex = rewindData.ValueRO.SeekIndex;
                    rewindData.ValueRW.SeekIndex = rewindData.ValueRO.MaxFrameIndex;
                    rewindData.ValueRW.PlaybackSpeed = PlaybackSpeed.Reverse1;
                    
                    foreach (var (changeBitLists, entity) in SystemAPI.Query<RefRW<ChangeBitList>>().WithEntityAccess())
                    {
                        ref var cbList = ref *changeBitLists.ValueRO.Value;
                        cbList.RemoveRange(seekIndex, cbList.Length - seekIndex);
                        var baseFrameToDestroyIndex = (int)math.floor((float)rewindData.ValueRO.SeekIndex / 120);

                        if (SystemAPI.HasComponent<LocalTransform>(entity))
                        {
                            var xPositionLists = SystemAPI.GetComponent<XPositionLists>(entity);
                            ref var xPositionTweenList = ref *xPositionLists.TweenFrames;
                            if (!xPositionTweenList.IsEmpty && xPositionLists.TweenFrameSeekIndex >= 0)
                            {
                                xPositionTweenList.RemoveRange(xPositionLists.TweenFrameSeekIndex, xPositionTweenList.Length - xPositionLists.TweenFrameSeekIndex);
                            }
                            
                            var yPositionLists = SystemAPI.GetComponent<YPositionLists>(entity);
                            ref var yPositionTweenList = ref *yPositionLists.TweenFrames;
                            if (!yPositionTweenList.IsEmpty && yPositionLists.TweenFrameSeekIndex >= 0)
                            {
                                yPositionTweenList.RemoveRange(yPositionLists.TweenFrameSeekIndex, yPositionTweenList.Length - yPositionLists.TweenFrameSeekIndex);
                            }

                            ref var xPositionBaseList = ref *xPositionLists.BaseFrames;
                            if (baseFrameToDestroyIndex < xPositionBaseList.Length)
                            {
                                xPositionBaseList.RemoveRange(baseFrameToDestroyIndex, xPositionBaseList.Length - baseFrameToDestroyIndex);
                            }
                            ref var yPositionBaseList = ref *yPositionLists.BaseFrames;
                            if (baseFrameToDestroyIndex < yPositionBaseList.Length)
                            {
                                yPositionBaseList.RemoveRange(baseFrameToDestroyIndex, yPositionBaseList.Length - baseFrameToDestroyIndex);
                            }
                        }

                        if (SystemAPI.HasComponent<CharacterVelocity>(entity))
                        {
                            var velocityLists = SystemAPI.GetComponent<VelocityListPtr>(entity);
                            ref var velocityBaseList = ref *velocityLists.BaseFrames;
                            velocityBaseList.RemoveRange(baseFrameToDestroyIndex, velocityBaseList.Length - baseFrameToDestroyIndex);
                            ref var velocityTweenList = ref *velocityLists.TweenFrames;
                            velocityTweenList.RemoveRange(velocityLists.TweenFrameSeekIndex, velocityTweenList.Length - velocityLists.TweenFrameSeekIndex);
                        }
                    }
                }

                // Append new data to rewind buffers
                foreach (var (changeBitLists, entity) in SystemAPI.Query<RefRW<ChangeBitList>>().WithEntityAccess())
                {
                    ref var cbList = ref *changeBitLists.ValueRO.Value;
                    var curChangeBits = ChangeBits.None;

                    if (SystemAPI.HasComponent<LocalTransform>(entity))
                    {
                        var xPositionLists = SystemAPI.GetComponentRW<XPositionLists>(entity);
                        var yPositionLists = SystemAPI.GetComponentRW<YPositionLists>(entity);
                        ref var baseXPositionList = ref *xPositionLists.ValueRO.BaseFrames;
                        ref var baseYPositionList = ref *yPositionLists.ValueRO.BaseFrames;
                        var transform = SystemAPI.GetComponent<LocalTransform>(entity);
                        
                        if (isBaseFrame)
                        {
                            if (baseXPositionList.IsEmpty || transform.Position.x != baseXPositionList[0])
                            {
                                baseXPositionList.Add(transform.Position.x);
                                curChangeBits |= ChangeBits.XPosition;
                            }
                            
                            if (baseYPositionList.IsEmpty || transform.Position.y != baseYPositionList[0])
                            {
                                baseYPositionList.Add(transform.Position.y);
                                curChangeBits |= ChangeBits.YPosition;
                            }
                        }
                        else
                        {
                            ref var xPositionList = ref *xPositionLists.ValueRO.TweenFrames;
                            ref var yPositionList = ref *yPositionLists.ValueRO.TweenFrames;

                            if (baseXPositionList.IsEmpty || transform.Position.x != baseXPositionList.Peek())
                            {
                                xPositionList.Add(transform.Position.x);
                                xPositionLists.ValueRW.TweenFrameSeekIndex++;
                                curChangeBits |= ChangeBits.XPosition;
                            }
                            if (baseYPositionList.IsEmpty || transform.Position.y != baseYPositionList.Peek())
                            {
                                yPositionList.Add(transform.Position.y);
                                yPositionLists.ValueRW.TweenFrameSeekIndex++;
                                curChangeBits |= ChangeBits.YPosition;
                            }
                        }
                    }

                    if (SystemAPI.HasComponent<CharacterVelocity>(entity))
                    {
                        var velocityLists = SystemAPI.GetComponentRW<VelocityListPtr>(entity);
                        ref var baseVelocityList = ref *velocityLists.ValueRO.BaseFrames;
                        var characterVelocity = SystemAPI.GetComponent<CharacterVelocity>(entity);

                        if (isBaseFrame)
                        {
                            if (baseVelocityList.IsEmpty || !characterVelocity.Value.Equals(baseVelocityList[0]))
                            {
                                baseVelocityList.Add(characterVelocity.Value);
                                curChangeBits |= ChangeBits.CharacterVelocity;
                            }
                        }
                        else
                        {
                            ref var velocityTweenList = ref *velocityLists.ValueRO.TweenFrames;
                            if (baseVelocityList.IsEmpty || !characterVelocity.Value.Equals(baseVelocityList.Peek()))
                            {
                                velocityTweenList.Add(characterVelocity.Value);
                                velocityLists.ValueRW.TweenFrameSeekIndex++;
                                curChangeBits |= ChangeBits.CharacterVelocity;
                            }
                        }
                    }
                    
                    cbList.Add(curChangeBits);
                }
                
                rewindData.ValueRW.MaxFrameIndex++;
                rewindData.ValueRW.SeekIndex = rewindData.ValueRO.MaxFrameIndex;
            }
        }
    }

    public static class NativeListExtensions
    {
        public static T Pop<T>(this NativeList<T> list) where T : unmanaged
        {
            var element = list[^1];
            list.RemoveAt(list.Length - 1);
            return element;
        }

        public static T Pop<T>(this ref UnsafeList<T> list) where T : unmanaged
        {
            var element = list[^1];
            list.RemoveAt(list.Length - 1);
            return element;
        }

        public static T Peek<T>(this UnsafeList<T> list) where T : unmanaged
        {
            var element = list[^1];
            return element;
        }
    }
}