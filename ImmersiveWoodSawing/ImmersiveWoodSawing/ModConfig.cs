using Vintagestory.API.Common;

namespace ImmersiveWoodSawing
{
    public class ModConfig
    {
        ImmersiveWoodSawingConfig config;

        public void ReadOrGenerateConfig(ICoreAPI api)
        {
            try
            {
                config = api.LoadModConfig<ImmersiveWoodSawingConfig>("ImmersiveWoodSawingConfig.json");
                if (config == null)
                {
                    api.StoreModConfig(new ImmersiveWoodSawingConfig(), "ImmersiveWoodSawingConfig.json");
                    config = api.LoadModConfig<ImmersiveWoodSawingConfig>("ImmersiveWoodSawingConfig.json");
                }
                else
                {
                    api.StoreModConfig(new ImmersiveWoodSawingConfig(config), "ImmersiveWoodSawingConfig.json");
                }
            }
            catch
            {
                api.StoreModConfig(new ImmersiveWoodSawingConfig(), "ImmersiveWoodSawingConfig.json");
                config = api.LoadModConfig<ImmersiveWoodSawingConfig>("ImmersiveWoodSawingConfig.json");
            }

            /*try
            {
                config = api.LoadModConfig<ImmersiveWoodchoppingConfig>("ImmersiveWoodchoppingConfig.json");
            }
            catch
            {
                config = new ImmersiveWoodchoppingConfig();
            }
            finally
            {
                api.StoreModConfig(config, "ImmersiveWoodchoppingConfig.json");
            }*/


            api.World.Config.SetBool(Constants.ModId + ":AutoLogPlacement", config.AutoLogPlacement);
            api.World.Config.SetInt(Constants.ModId + ":PlanksPerUse", config.PlanksPerUse);
            api.World.Config.SetBool(Constants.ModId + ":DisableGridRecipe", config.DisableGridRecipe);
            api.World.Config.SetFloat(Constants.ModId + ":SawSpeedMultilier", config.SawSpeedMultiplier);
        }
    }
}
