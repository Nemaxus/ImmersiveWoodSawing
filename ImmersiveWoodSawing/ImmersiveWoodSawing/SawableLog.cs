using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace ImmersiveWoodSawing
{
    class SawableLog : Block
    {
        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            var be = blockAccessor.GetBlockEntity<BESawableLog>(pos);
            if (be != null)
            {
                return be.ColBox;
            }
            return base.GetCollisionBoxes(blockAccessor, pos);
        }
        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            var be = blockAccessor.GetBlockEntity<BESawableLog>(pos);
            if (be != null)
            {
                return be.SelBox;
            }
            return base.GetCollisionBoxes(blockAccessor, pos);
        }

        public override Cuboidf[] GetParticleCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            var be = blockAccessor.GetBlockEntity<BESawableLog>(pos);
            if (be != null)
            {
                return be.ColBox;
            }
            return base.GetParticleCollisionBoxes(blockAccessor, pos);
        }

        public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
        {
            var be = world.BlockAccessor.GetBlockEntity<BESawableLog>(pos);
            if (be != null)
            {
                return be.BlockStack.Block.GetPlacedBlockName(world, pos);
            }
            return base.GetPlacedBlockName(world, pos);
        }

        public override int GetRandomColor(ICoreClientAPI capi, BlockPos pos, BlockFacing facing, int rndIndex = -1)
        {
            var be = api.World.BlockAccessor.GetBlockEntity<BESawableLog>(pos);
            if (be != null)
            {
                int i = be.BlockStack.Block.GetRandomColor(api as ICoreClientAPI, pos, facing, rndIndex);
                return i;
            }
            int b = base.GetRandomColor(capi, pos, facing, rndIndex);
            return b;
        }

        public override string GetPlacedBlockInfo(IWorldAccessor world, BlockPos pos, IPlayer forPlayer)
        {
            var be = world.BlockAccessor.GetBlockEntity<BESawableLog>(pos);
            if (be != null)
            {
                StringBuilder dsc = new StringBuilder();

#if DEBUG
                if (forPlayer.Entity.Controls.ShiftKey)
                {
                    dsc.AppendLine("This is not a log");
                    dsc.AppendLine("Copied from: " + be.BlockStack.Block.Code.ToString());
                    dsc.AppendLine("Plank Type: " + be.PlankType);
                    dsc.AppendLine("Planks Left: " + be.PlanksLeft);
                    dsc.AppendLine(GenerateInfoString(be));
                }
#endif
                dsc.AppendLine(be.BlockStack.Block.GetPlacedBlockInfo(world, pos, forPlayer));
                return dsc.ToString();
            }
            return base.GetPlacedBlockInfo(world, pos, forPlayer);
        }

        public string GenerateInfoString(BESawableLog be)
        {
            StringBuilder sb = new StringBuilder();

            float meshOffset = be.LogSliceSize * (be.PlanksTotal - be.PlanksLeft);
            Shape s = be.ResolveShapeElementsSizes(be.GetShape(api as ICoreClientAPI, "complete"),
                meshOffset, be.PlanksToTakeOut * be.LogSliceSize,
                (api as ICoreClientAPI).Assets.Get<Shape>(new AssetLocation(
                    be.BlockStack.Block.Shape.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json").ToString())));

            for (int i = s.Elements.Length; i > 0; i--)
            {
                var el = s.Elements[i-1];
                int b = 4;
                sb.Append($"{el.Name} Uv {BlockFacing.ALLFACES[b].ToString()}: ");
                    el.FacesResolved[b].Uv.Foreach((x) =>
                {
                    sb.Append(x + "| ");
                });
                sb.AppendLine();
                //sb.AppendLine($"{el.Name} part from: " + el.From[0].ToString("0.0"));
                //sb.AppendLine($"{el.Name} part to: " + el.To[0].ToString("0.0"));
                //sb.AppendLine($"{el.Name} part X size: " +Math.Abs(el.From[0] - el.To[0]).ToString("0.0"));
                //sb.Append("\n");
            }
            return sb.ToString();
        }

        public override ItemStack[] GetDrops(IWorldAccessor world, BlockPos pos, IPlayer byPlayer, float dropQuantityMultiplier = 1)
        {
            var bESLog = world.BlockAccessor.GetBlockEntity<BESawableLog>(pos);
            if (bESLog.PlanksTotal == bESLog.PlanksLeft)
            { return bESLog.BlockStack.Block.GetDrops(world, pos ,byPlayer, dropQuantityMultiplier); }
            else return base.GetDrops(world, pos, byPlayer, dropQuantityMultiplier);
        }
    }
}