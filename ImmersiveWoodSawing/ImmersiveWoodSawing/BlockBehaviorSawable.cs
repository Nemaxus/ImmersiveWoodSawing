using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;

namespace ImmersiveWoodSawing
{
    class BlockBehaviorSawable : BlockBehavior
    {
        public static Dictionary<string, List<AssetLocation>> VariantsByType = new Dictionary<string, List<AssetLocation>>();

        bool hideInteractionHelpInSurvival;
        public AssetLocation drop;
        public int dropAmount;



        private static List<ItemStack> sawItems = new List<ItemStack>();

        public BlockBehaviorSawable(Block block) : base(block)
        {

        }

        public override WorldInteraction[] GetPlacedBlockInteractionHelp(IWorldAccessor world, BlockSelection selection, IPlayer forPlayer, ref EnumHandling handling)
        {
            if (hideInteractionHelpInSurvival && forPlayer?.WorldData.CurrentGameMode == EnumGameMode.Survival) return base.GetPlacedBlockInteractionHelp(world, selection, forPlayer, ref handling);
            handling = EnumHandling.PassThrough;
            if (sawItems.Count == 0)   // This is a potentially rather slow wildcard search of all items (especially if mods add many items) therefore we want to run this only once per game
            {
                Item[] saws = world.SearchItems(new AssetLocation("saw-*"));
                foreach (Item item in saws) sawItems.Add(new ItemStack(item));
            }

            bool notProtected = true;

            if (world.Claims != null && world is IClientWorldAccessor clientWorld && clientWorld.Player?.WorldData.CurrentGameMode == EnumGameMode.Survival)
            {
                EnumWorldAccessResponse resp = world.Claims.TestAccess(clientWorld.Player, selection.Position, EnumBlockAccessFlags.BuildOrBreak);
                if (resp != EnumWorldAccessResponse.Granted) notProtected = false;
            }

            if (sawItems.Count > 0 && notProtected)
            {
                return new WorldInteraction[] { new WorldInteraction()
                {
                    ActionLangCode = "immersivewoodsawing:blockinteract-saw",
                    Itemstacks = sawItems.ToArray(),
                    MouseButton = EnumMouseButton.Right
                } };
            }
            else return new WorldInteraction[0];
        }

        public override void Initialize(JsonObject properties)
        {
            base.Initialize(properties);
            hideInteractionHelpInSurvival = properties["hideInteractionHelpInSurvival"].AsBool(false);
            if (block is not SawableLog)
            {
                drop = new AssetLocation(properties["drop"].ToString());
                dropAmount = properties["dropAmount"].AsInt(4);
            }

        }



        public override void OnUnloaded(ICoreAPI api)
        {
            sawItems.Clear();
        }

    }

}