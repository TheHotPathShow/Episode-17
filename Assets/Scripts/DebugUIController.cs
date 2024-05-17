using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace TMG.BraiDOTS
{
    public class DebugUIController : MonoBehaviour
    {
        public static DebugUIController Instance;
        
        [SerializeField] private TextMeshProUGUI _debugIndexText;
        [SerializeField] private Image _playStatusIcon;
        [SerializeField] private Sprite[] _playStatusSprites;
        
        private void Awake()
        {
            Instance = this;
        }
        
        public void SetDebugIndexText(uint maxIndex, uint seekIndex, int byteCounter)
        {
            _debugIndexText.text = $"Max Index: {maxIndex}\nSeek Index: {seekIndex}\nBytes: {byteCounter}";
        }
        
        public void SetPlayStatusIcon(int iconIndex)
        {
            _playStatusIcon.sprite = _playStatusSprites[iconIndex];
        }
    }

    public struct DebugUIControllerRef : IComponentData
    {
        public UnityObjectRef<DebugUIController> Value;
    }

    public partial struct InitializeUISystem : ISystem
    {
        public void OnUpdate(ref SystemState state)
        {
            if (DebugUIController.Instance == null) return;
            var debugEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(debugEntity, new DebugUIControllerRef
            {
                Value = DebugUIController.Instance
            });
            state.Enabled = false;
        }
    }
    
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup), OrderLast = true)]
    public partial struct DebugUISystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<RewindData>();
        }

        public void OnUpdate(ref SystemState state)
        {
            var rewindData = SystemAPI.GetSingleton<RewindData>();
            foreach (var debugUIRef in SystemAPI.Query<DebugUIControllerRef>())
            {

                var iconIndex = -1;
                if (rewindData.RewindInput == false)
                {
                    iconIndex = 1;
                }
                else if ((sbyte)rewindData.PlaybackSpeed < 0)
                {
                    iconIndex = 0;
                }
                else if ((sbyte)rewindData.PlaybackSpeed == 0)
                {
                    iconIndex = 2;
                }
                else if ((sbyte)rewindData.PlaybackSpeed > 0)
                {
                    iconIndex = 3;
                }

                debugUIRef.Value.Value.SetPlayStatusIcon(iconIndex);
                var byteCounter = 0;
                
                unsafe
                {

                    foreach (var (xLists, yLists, vLists, cList) in SystemAPI.Query<XPositionLists, YPositionLists, VelocityListPtr, ChangeBitList>())
                    {
                        var xBaseFrames = *xLists.BaseFrames;
                        var xTweenFrames = *xLists.TweenFrames;
                        byteCounter += 4 * (xBaseFrames.Length + xTweenFrames.Length);

                        var yBaseFrames = *yLists.BaseFrames;
                        var yTweenFrames = *yLists.TweenFrames;
                        byteCounter += 4 * (yBaseFrames.Length + yTweenFrames.Length);

                        var vBaseFrames = *vLists.BaseFrames;
                        var vTweenFrames = *vLists.TweenFrames;
                        byteCounter += 8 * (vBaseFrames.Length + vTweenFrames.Length);

                        var cBaseFrames = *cList.Value;
                        byteCounter += cBaseFrames.Length;
                    }
                }

                debugUIRef.Value.Value.SetDebugIndexText(rewindData.MaxFrameIndex, rewindData.SeekIndex, byteCounter);
            }
        }
    }
}