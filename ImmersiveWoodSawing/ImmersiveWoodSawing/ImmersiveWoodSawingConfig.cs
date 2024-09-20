using ProtoBuf;
using System.ComponentModel;

namespace ImmersiveWoodSawing
{
    [ProtoContract]
    public class ImmersiveWoodSawingConfig
    {
        [ProtoMember(1)] public int PlanksPerUse = 1;
        [ProtoMember(2), DefaultValue(true)] public bool DisableGridRecipe { get; set; } = true;
        [ProtoMember(3)] public float SawSpeedMultiplier = 1.0f;
        [ProtoMember(4), DefaultValue(false)] public bool AutoLogPlacement { get; set; } = false;

        public ImmersiveWoodSawingConfig()
        {

        }

        public ImmersiveWoodSawingConfig(ImmersiveWoodSawingConfig previousConfig)
        {
            AutoLogPlacement = previousConfig.AutoLogPlacement;
            PlanksPerUse = previousConfig.PlanksPerUse;
            DisableGridRecipe = previousConfig.DisableGridRecipe;
            SawSpeedMultiplier = previousConfig.SawSpeedMultiplier;
        }
    }
}