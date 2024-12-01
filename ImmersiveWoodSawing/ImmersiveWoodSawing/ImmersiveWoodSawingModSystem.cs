using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;

namespace ImmersiveWoodSawing
{
    public class ImmersiveWoodSawingModSystem : ModSystem
    {
        ModConfig config = new ModConfig();
        public readonly Dictionary<string, CraftingRecipeIngredient> sawingRecipes = new Dictionary<string, CraftingRecipeIngredient>();
        public readonly List<string> logTypes = new();

        private IServerNetworkChannel schannel;
        private IClientNetworkChannel cchannel;
        private ICoreAPI _api = null;
        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            _api = api;
            config.ReadOrGenerateConfig(api);
            if(api is ICoreServerAPI sApi)
            {
                schannel = sApi.Network.RegisterChannel(Constants.ModId + "-syncconfig")
                    .RegisterMessageType<ImmersiveWoodSawingConfig>();
            }
            if (api is ICoreClientAPI cApi)
            {
                cchannel = cApi.Network.RegisterChannel(Constants.ModId + "-syncconfig")
                    .RegisterMessageType<ImmersiveWoodSawingConfig>()
                    .SetMessageHandler<ImmersiveWoodSawingConfig>(OnSyncConfigReceived);
            }
        }

        private void OnSyncConfigReceived(ImmersiveWoodSawingConfig modConfig)
        {
            config.config = modConfig;
            config.SetWorldConfig(_api as ICoreClientAPI);
        }
        public override void Start(ICoreAPI api)
        {
            api.RegisterCollectibleBehaviorClass("CBehaviorWoodSawing", typeof(WoodSawing));
            api.RegisterBlockBehaviorClass("BehaviorSawable", typeof(BlockBehaviorSawable));
            api.RegisterBlockClass("SawableLog", typeof(SawableLog));
            api.RegisterBlockEntityClass("BESawableLog", typeof(BESawableLog));

        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            api.Event.PlayerJoin += byPlayer =>
            {
                schannel.SendPacket(config.Clone().config, byPlayer);
            };
        }

        public override double ExecuteOrder()
        {
            return 1;
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            GeneratePlanksRecipeList(api);
            AssignHandbookAttributes(api);
            AssingDrops(api);

            if (api.Side == EnumAppSide.Server)
            {
                foreach (var item in api.World.Items)
                {
                    if (item.Code == null) continue;
                    if (item.Code.Path.StartsWith("saw-"))
                    {
                        var modBehaviorIndex = item.CollectibleBehaviors.IndexOf(name => name.GetType().FullName == "AncientTools.CollectibleBehaviors.CollectibleBehaviorMobileStorageDestruction");
                        if (modBehaviorIndex != -1)
                        {
                            var temp = item.CollectibleBehaviors[modBehaviorIndex];
                            item.CollectibleBehaviors[modBehaviorIndex] = new WoodSawing(item);
                            item.CollectibleBehaviors = item.CollectibleBehaviors.Append(temp);
                        }
                        else
                        {
                            item.CollectibleBehaviors = item.CollectibleBehaviors.Append(new WoodSawing(item));
                        }
                    }
                }
            }
            //Always check on which game side you add your behavior!
        }

        public void GeneratePlanksRecipeList(ICoreAPI api)
        {
            foreach (GridRecipe grecipe in api.World.GridRecipes)
            {
                if (grecipe.Output.Code.Path.StartsWith("plank-"))
                {
                    
                    bool enabled = !api.World.Config.GetBool(Constants.ModId + ":DisableGridRecipe", true);

                    foreach (CraftingRecipeIngredient ingredient in grecipe.resolvedIngredients)
                    {

                        if (ingredient.IsTool) continue;
                        if (ingredient.Type != EnumItemClass.Block) continue;
                        
                        if (ingredient.IsWildCard && ingredient.AllowedVariants != null)
                        {
                            foreach(var alvariant in ingredient.AllowedVariants)
                            RegisterRecipe(ingredient, grecipe, api.World.GetBlock(new AssetLocation(ingredient.Code.ToString().Replace("*",alvariant))), enabled);
                        }
                        else
                        {
                            RegisterRecipe(ingredient, grecipe, ingredient.ResolvedItemstack?.Block, enabled);
                        }
                        //var variant = ingredient.ResolvedItemstack.Block.Variant;
                        
                        /*
                        else if (ipath.StartsWith("logsection-placed-"))
                        {
                            //if (!ipath.Contains('*'))
                            {
                                string genVariant = new AssetLocation(icode.Domain, ipath.Replace("-ne-ud", "-*-*")).ToString();
                                if (!sawingRecipes.ContainsKey(genVariant))
                                {
                                    sawingRecipes.Add(genVariant, grecipe.Output);
                                }
                                else
                                {
                                    sawingRecipes[ingredient.Code.ToString().Replace("-ne-ud", "-*-*")] = grecipe.Output;
                                }
                            }
                            grecipe.Enabled = enabled;
                            grecipe.ShowInCreatedBy = enabled;
                        }*/
                    }
                }
            }
        }

        public void AssingDrops(ICoreAPI api)
        {
            foreach (var block in api.World.Blocks)
            {
                if (block.Code == null) continue;
                if (block is SawableLog)
                {
                    BlockBehaviorSawable behaviour = new BlockBehaviorSawable(block);
                    behaviour.Initialize(new JsonObject(new JObject
                    {
                            { "hideInteractionHelpInSurvival", new JValue(false) }
                    }));
                    block.BlockBehaviors = block.BlockBehaviors.Append(behaviour);
                    continue;
                }
                if (logTypes.Contains(block.Code.FirstCodePart()))
                {
                    CraftingRecipeIngredient planksResult = null;

                    foreach (var key in sawingRecipes.Keys)
                    {
                        if (WildcardUtil.Match(new AssetLocation(key), block.Code))
                        {
                            planksResult = sawingRecipes[key];
                        }
                    }
                    //Debug.WriteLine(block.Code);
                    if (planksResult != null)
                    {
                        BlockBehaviorSawable behaviour = new BlockBehaviorSawable(block);
                        JObject jobj = new JObject
                        {
                            { "hideInteractionHelpInSurvival", new JValue(false) },
                            { "drop", new JValue(planksResult.Code.ToString()) },
                            { "dropAmount", new JValue(planksResult.Quantity) },
                        };
                        behaviour.Initialize(new JsonObject(jobj));

                        block.BlockBehaviors = block.BlockBehaviors.Append(behaviour);
                        //Debug.WriteLine("Found " + block.Code);
                        /*if (api.World.Config.TryGetBool("logInOffhandChopping") == true && block.StorageFlags < EnumItemStorageFlags.Offhand)
                        {
                            block.StorageFlags = block.StorageFlags + (int)EnumItemStorageFlags.Offhand;
                        }*/
                    }
                }
            }
        }

        public void RegisterRecipe(CraftingRecipeIngredient ingredient, GridRecipe grecipe,Block block, bool enabled)
        {
            if(block == null || block.Variant == null) return;

            var variant = block.Variant;

            if (!variant.ContainsKey("rotation"))return;

            string genIngredient = ingredient.Code.ToString().Replace("-ne", "-*").Replace("-ud", "-*");
            if (!sawingRecipes.ContainsKey(genIngredient))
            {
                sawingRecipes.Add(genIngredient, grecipe.Output);
            }
            else
            {
                sawingRecipes[genIngredient] = grecipe.Output;
            }

            if (!logTypes.Contains(ingredient.Code.FirstCodePart()))
            {
                logTypes.Add(ingredient.Code.FirstCodePart());
            }
            grecipe.Enabled = enabled;
            grecipe.ShowInCreatedBy = enabled;
        }

        //Thank you Tyron for providing me with an example for patching JsonObject and JToken nightmare in Attributes 0_o
        public void AssignHandbookAttributes(ICoreAPI api)
        {
            /*
            List<AssetLocation> firewoodOutputs = new();
            foreach(var firewoodOutput in choppingRecipes.Values)
            {
                if (!firewoodOutputs.Contains(firewoodOutput.Code))
                {
                    firewoodOutputs.Add(firewoodOutput.Code);
                }
            }
            */
            foreach (var plankType in sawingRecipes.Values)
            {
                Item plank = api.World.GetItem(plankType.Code);
                JToken token;
                if (plank.Attributes?["handbook"].Exists != true)
                {
                    if (plank.Attributes == null) plank.Attributes = new JsonObject(JToken.Parse("{ handbook: {} }"));
                    else
                    {
                        token = plank.Attributes.Token;
                        token["handbook"] = JToken.Parse("{ }");
                    }
                }

                token = plank.Attributes["handbook"].Token;
                token["createdBy"] = JToken.FromObject(Constants.ModId + ":handbook-sawing-craftinfo");
            }
        }
    }
}
