using ProtoBuf;

namespace ImmersiveWoodSawing
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class ImmersiveWoodSawingConfig
    {
        public bool AutoLogPlacement = false;
        public int PlanksPerUse = 1;
        public bool DisableGridRecipe = true;
        public float SawSpeedMultiplier = 1.0f;

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