using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace ImmersiveWoodSawing
{
    public static class ParticleProvider
    {
        public static Vec3d FaceBlockPos(IBlockAccessor blockAccess, BlockPos pos, Block block, BlockFacing facing, float secondsUsed, float sawingSpeed, float logSliceSize, bool oppositeSide)
        {
            Cuboidf particleBreakBox = block.GetParticleBreakBox(blockAccess, pos, facing);
            float progress = (secondsUsed - 0.6f) / sawingSpeed;
            return new Vec3d((double)((float)pos.X + particleBreakBox.X1 + logSliceSize * Constants.BlockProportion), (double)((float)(pos.Y + 1) - progress), (double)(pos.Z + ((!oppositeSide) ? 1 : 0)));
        }

        public static Vec3f GetVelocity(BlockFacing facing, Random rand)
        {
            Vec3i normali = facing.Normali;
            return new Vec3f((float)((normali.X == 0) ? (rand.NextDouble() - 0.5) : ((0.25 + rand.NextDouble()) * (double)normali.X)), (float)((normali.Y == 0) ? (rand.NextDouble() - 0.25) : ((0.75 + rand.NextDouble()) * (double)normali.Y)), (float)((normali.Z == 0) ? (rand.NextDouble() - 0.5) : ((0.25 + rand.NextDouble()) * (double)normali.Z))) * (1f + (float)rand.NextDouble() / 2f);
        }

        public static SimpleParticleProperties GetParticleProperties(IBlockAccessor blockAccessor, BlockSelection blockSel, Block block, EntityAgent byEntity, float secondsUsed, float sawingSpeed, float logSliceSize, bool oppositeSide)
        {
            Random rand = new Random();
            BlockBreakingParticleProps bprops = new BlockBreakingParticleProps();
            Vec3d v3d = ParticleProvider.FaceBlockPos(blockAccessor, blockSel.Position, block, blockSel.Face, secondsUsed, sawingSpeed, logSliceSize, oppositeSide);
            Vec3f v3f = ParticleProvider.GetVelocity(oppositeSide ? BlockFacing.NORTH : BlockFacing.SOUTH, rand);
            int color = block.GetRandomColor(byEntity.Api as ICoreClientAPI, blockSel.Position, BlockFacing.UP, -1);
            float size = 0.5f + (float)rand.NextDouble() * 0.4f;
            float num = (float)rand.NextDouble() / 4f;
            return new SimpleParticleProperties(1.5f, 3f, color, v3d, v3d, v3f, v3f, bprops.LifeLength, 1f, size, size, EnumParticleModel.Cube)
            {
                SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.8f)
            };
        }
    }
}
