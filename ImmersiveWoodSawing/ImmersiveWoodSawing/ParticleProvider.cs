using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace ImmersiveWoodSawing
{
    public static class ParticleProvider
    {
        public static Vec3d FaceBlockPos(IBlockAccessor blockAccess, BlockPos pos, Block block, BlockFacing facing, float secondsUsed, float sawingSpeed, float logSliceSize)
        {
            Cuboidf particleBreakBox = block.GetParticleBreakBox(blockAccess, pos, facing);
            float progress = (secondsUsed - 0.6f) / sawingSpeed;
            //Vec3d vec3d = new Vec3d((float)pos.X + 0.5f + (float)normali.X / 1.9f + ((!flag || facing.Axis != EnumAxis.X) ? 0f : ((normali.X > 0) ? (particleBreakBox.X2 - 1f) : particleBreakBox.X1)), (float)pos.Y + 0.5f + (float)normali.Y / 1.9f + ((!flag || facing.Axis != EnumAxis.Y) ? 0f : ((normali.Y > 0) ? (particleBreakBox.Y2 - 1f) : particleBreakBox.Y1)), (float)pos.Z + 0.5f + (float)normali.Z / 1.9f + ((!flag || facing.Axis != EnumAxis.Z) ? 0f : ((normali.Z > 0) ? (particleBreakBox.Z2 - 1f) : particleBreakBox.Z1)));
            Vec3d vec3d = new Vec3d(pos.X + particleBreakBox.X1 + logSliceSize * Constants.BlockProportion, pos.Y + 1 - progress, pos.Z);
            return vec3d;
        }

        public static Vec3f GetVelocity(BlockFacing facing, Random rand)
        {
            Vec3i normali = facing.Normali;
            return new Vec3f((float)((normali.X == 0) ? (rand.NextDouble() - 0.5) : ((0.25 + rand.NextDouble()) * (double)normali.X)), (float)((normali.Y == 0) ? (rand.NextDouble() - 0.25) : ((0.75 + rand.NextDouble()) * (double)normali.Y)), (float)((normali.Z == 0) ? (rand.NextDouble() - 0.5) : ((0.25 + rand.NextDouble()) * (double)normali.Z))) * (1f + (float)rand.NextDouble() / 2f);
        }

        public static SimpleParticleProperties GetParticleProperties(IBlockAccessor blockAccessor, BlockSelection blockSel, Block block, EntityAgent byEntity, float secondsUsed, float sawingSpeed, float logSliceSize)
        {
            //bprops.RandomBlockPos(blockAccessor,blockSel.Position, block,blockSel.Face);
            //Vec3f v3f = bprops.GetVelocity(v3d);
            Random rand = new Random();
            BlockBreakingParticleProps bprops = new BlockBreakingParticleProps();
            Vec3d v3d = FaceBlockPos(blockAccessor, blockSel.Position, block, blockSel.Face, secondsUsed, sawingSpeed, logSliceSize);
            Vec3f v3f = GetVelocity(blockSel.Face, rand);
            int color = block.GetRandomColor(byEntity.Api as ICoreClientAPI, blockSel.Position, blockSel.Face);
            float size = 0.5f + (float)rand.NextDouble() * 0.8f;
            float life = 2f + (float)rand.NextDouble() / 4f;

            SimpleParticleProperties spprops = new SimpleParticleProperties(0, 0.3f, color, v3d, v3d, v3f,
                                v3f, bprops.LifeLength, 1f, size, size, EnumParticleModel.Cube)
            {
                SizeEvolve = new EvolvingNatFloat(EnumTransformFunction.LINEAR, -0.5f)
            };

            return spprops;
        }
    }
}
