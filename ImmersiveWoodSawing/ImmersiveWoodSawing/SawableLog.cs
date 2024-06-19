using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;

namespace ImmersiveWoodSawing
{
    class SawableLog : Block
    {
        public override Cuboidf[] GetCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            var be = blockAccessor.GetBlockEntity<BESawableLog>(pos);
            if (be != null)
            {
                return be.Boxes;
            }
            return base.GetCollisionBoxes(blockAccessor, pos);
        }
        public override Cuboidf[] GetSelectionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            var be = blockAccessor.GetBlockEntity<BESawableLog>(pos);
            if (be != null)
            {
                return be.Boxes;
            }
            return base.GetCollisionBoxes(blockAccessor, pos);
        }

        public override Cuboidf[] GetParticleCollisionBoxes(IBlockAccessor blockAccessor, BlockPos pos)
        {
            var be = blockAccessor.GetBlockEntity<BESawableLog>(pos);
            if (be != null)
            {
                return be.Boxes;
            }
            return base.GetParticleCollisionBoxes(blockAccessor, pos);
        }

        public override string GetPlacedBlockName(IWorldAccessor world, BlockPos pos)
        {
            var be = world.BlockAccessor.GetBlockEntity<BESawableLog>(pos);
            if (be != null)
            {
                return be.BlockStack.Block.GetPlacedBlockName(world,pos);
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

                if (forPlayer.Entity.Controls.ShiftKey)
                {
                    dsc.AppendLine("This is not a log");
                    dsc.AppendLine("Copied from: " + be.BlockStack.Block.Code.ToString());
                    dsc.AppendLine("Plank Type: " + be.PlankType);
                    dsc.AppendLine("Planks Left: " + be.PlanksLeft);
                }
                dsc.AppendLine(be.BlockStack.Block.GetPlacedBlockInfo(world, pos, forPlayer));
                return dsc.ToString();
            }
            return base.GetPlacedBlockInfo(world, pos, forPlayer);
        }

    }
}