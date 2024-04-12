namespace ImmersiveWoodSawing
{
    public class ImmersiveWoodSawingConfig
    {
        public bool AutoLogPlacement = false;
        public int PlanksPerUse = 1;
        public bool DisableGridRecipe = true;
        
        public ImmersiveWoodSawingConfig()
        {

        }

        public ImmersiveWoodSawingConfig(ImmersiveWoodSawingConfig previousConfig)
        {
            AutoLogPlacement = previousConfig.AutoLogPlacement;
            PlanksPerUse = previousConfig.PlanksPerUse;
            DisableGridRecipe = previousConfig.DisableGridRecipe;
        }
    }
}