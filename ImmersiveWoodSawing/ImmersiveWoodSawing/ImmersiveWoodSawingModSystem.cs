using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ImmersiveWoodSawing
{
    public class ImmersiveWoodSawingModSystem : ModSystem
    {
        ModConfig config = new ModConfig();
        public readonly Dictionary<string, CraftingRecipeIngredient> sawingRecipes = new Dictionary<string, CraftingRecipeIngredient>();
        public readonly List<string> logTypes = new();
        public override void StartPre(ICoreAPI api)
        {
            base.StartPre(api);
            config.ReadOrGenerateConfig(api);
        }
        public override void Start(ICoreAPI api)
        {
            api.RegisterCollectibleBehaviorClass("CBehaviorWoodSawing", typeof(WoodSawing));
            api.RegisterBlockBehaviorClass("BehaviorSawable", typeof(BlockBehaviorSawable));
            api.RegisterBlockClass("SawableLog", typeof(SawableLog));
            api.RegisterBlockEntityClass("BESawableLog", typeof(BESawableLog));

        }

        public override double ExecuteOrder()
        {
            return 1;
        }

        public override void AssetsFinalize(ICoreAPI api)
        {
            GeneratePlanksRecipeList(api);
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
            foreach (var grecipe in api.World.GridRecipes)
            {
                if (grecipe.Output.Code.Path.StartsWith("plank-"))
                {
                    AssetLocation icode;
                    string ipath;
                    bool enabled = !api.World.Config.GetBool(Constants.ModId + ":DisableGridRecipe", true);

                    foreach (CraftingRecipeIngredient ingredient in grecipe.resolvedIngredients)
                    {
                        icode = ingredient.Code;
                        ipath = icode.Path;

                        if (ingredient.IsTool) continue;
                        if (ingredient.ResolvedItemstack.Block == null) continue;

                        var variant = ingredient.ResolvedItemstack.Block.Variant;
                        if(!variant.ContainsKey("rotation")) continue;

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
    }
}
