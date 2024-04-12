using System;
using System.Diagnostics;
using System.Text;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.NoObf;

namespace ImmersiveWoodSawing
{
    public class BESawableLog : BlockEntity
    {
        private float logSliceSize;
        private ItemStack blockStack;
        private int planksAmount = 0;
        private int planksTotalAmount = 0;
        private AssetLocation plankType = new("game:plank-aged");


        private MeshData blockMesh;

        public Cuboidf[] Boxes { get; private set; }

        public float LogSliceSize
        {
            get => logSliceSize;
        }

        public int PlanksAmount
        {
            get => planksAmount;
            set
            {
                planksAmount = value;

                UpdateBlockMesh();
            }
        }

        public int PlanksTotalAmount
        {
            get => planksTotalAmount;
        }
        public AssetLocation PlankType
        {
            get => plankType;
        }
        public ItemStack BlockStack
        {
            get => blockStack;
            set
            {
                blockStack = value;
                if (blockStack.Block.HasBehavior<BlockBehaviorSawable>())
                {
                    BlockBehaviorSawable behavior = blockStack.Block.GetBehavior(typeof(BlockBehaviorSawable), true) as BlockBehaviorSawable;
                    plankType = behavior.drop;
                    planksTotalAmount = planksAmount = behavior.dropAmount;
                    logSliceSize = 16f / planksTotalAmount;
                }
                UpdateBlockMesh();
            }
        }

        public SawMarkRenderer renderer;

        public BESawableLog() : base()
        {
            Boxes = new Cuboidf[] { new Cuboidf(0, 0, 0, 1, 1, 1) };
        }
        public override void Initialize(ICoreAPI api)
        {
            base.Initialize(api);

            if (api.Side == EnumAppSide.Client)
            {
                renderer = new SawMarkRenderer(api as ICoreClientAPI, Pos, GenDefaultRendererMesh())
                {
                    ShouldRender = true
                };

                (api as ICoreClientAPI).Event.RegisterRenderer(renderer, EnumRenderStage.Opaque, "sawmark");
            }

            blockStack ??= GetDefaultBlockStack();
            if (api is ICoreClientAPI)
            {
                UpdateBlockMesh();
            }
        }

        private ItemStack GetDefaultBlockStack()
        {
            return new ItemStack(Api.World.GetBlock(new AssetLocation("game:debarkedlog-aged-ns")));
        }

        public override bool OnTesselation(ITerrainMeshPool mesher, ITesselatorAPI tessThreadTesselator)
        {
            mesher.AddMeshData(blockMesh);
            return true;
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            base.GetBlockInfo(forPlayer, dsc);
            if (forPlayer.Entity.Controls.ShiftKey)
            {
                dsc.AppendLine("This is not a log");
                dsc.AppendLine("Copied from: " + blockStack.Block.Code.ToString());
                dsc.AppendLine("Plank Type: " + plankType);
                dsc.AppendLine("Planks Left: " + planksAmount);
            }
            dsc.AppendLine(blockStack.Block.GetPlacedBlockInfo(Api.World, forPlayer.CurrentBlockSelection.Position, forPlayer));
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("copiedBlock", blockStack.Collectible.Code.ToString());
            tree.SetString("plankType", plankType.ToString());
            tree.SetInt("planksAmount", planksAmount);
            tree.SetInt("planksTotalAmount", planksTotalAmount);
            tree.SetFloat("logSliceSize", logSliceSize);
        }

        public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
        {
            base.FromTreeAttributes(tree, worldAccessForResolve);
            string blockName = tree.GetString("copiedBlock");
            if (blockName != null)
            {
                Block block = worldAccessForResolve.GetBlock(new AssetLocation(blockName));
                if (block != null)
                {
                    blockStack = new ItemStack(block);
                }
            }
            plankType = new AssetLocation(tree.GetString("plankType"));
            planksAmount = tree.GetInt("planksAmount");
            planksTotalAmount = tree.GetInt("planksTotalAmount");
            logSliceSize = tree.GetFloat("logSliceSize");
            UpdateBlockMesh();
        }


        private void UpdateBlockMesh()
        {

            float meshOffset = logSliceSize * (planksTotalAmount - planksAmount);
            float offset = meshOffset * Constants.BlockProportion;
            if (Api is ICoreClientAPI capi && blockStack != null)
            {

                int planksToCreate = GameMath.Clamp(Api.World.Config.GetInt(Constants.ModId + ":PlanksPerUse", 1), 1, planksTotalAmount);
                int planksToTakeOut = (planksAmount - planksToCreate >= 0) ? planksToCreate : planksAmount;

                float offsetX = offset + planksToTakeOut * logSliceSize * Constants.BlockProportion;

                Shape referenceShape = capi.Assets.Get<Shape>(new AssetLocation(blockStack.Block.Shape.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json").ToString()));
                Shape blockShape = ResolveShapeElementsSizes(GetShape(capi, "sawablelog"), meshOffset, planksToTakeOut * logSliceSize, GetShape(capi, "sawmark").Elements[0], referenceShape);

                renderer.XOffset = offsetX;
                
                //capi.Tesselator.TesselateShape(blockStack.Collectible, blockShape, out blockMesh);
                ITexPositionSource source = capi.Tesselator.GetTextureSource(blockStack.Block);
                capi.Tesselator.TesselateShape("sawablelog", blockShape, out blockMesh, source);

            }
            MarkDirty(true);
            if (Block is SawableLog)
            {
                Boxes[0] = Block.CollisionBoxes[0].Clone();
                Boxes[0].X1 = offset;
            }
        }

        private Shape ResolveShapeElementsSizes(Shape shape, float meshOffset, float SawablePartWithSawmarkXLength, ShapeElement sawmark, Shape referenceShape)
        {
            //Step 1) Get part proportions for our block
            ShapeElement sawablePart = shape.GetElementByName("SawablePart");
            ShapeElement resizablePart = shape.GetElementByName("ResizablePart");
            ShapeElement refShapeElement = referenceShape.Elements[0];


            //Need to take in account the size of sawmark X length when changing its position(and setting the size of this part as well)
            float sawmarkXLength = (float)(sawmark.To[0] - sawmark.From[0]);

            resizablePart.From[0] = SawablePartWithSawmarkXLength + meshOffset;

            sawablePart.From[0] = meshOffset;
            sawablePart.To[0] = SawablePartWithSawmarkXLength - sawmarkXLength + meshOffset;

            float ResizablePartXLength = (float)(resizablePart.To[0] - resizablePart.From[0]);

            //Step 2) Make adjustments for textures types, their rotarion, position, etc.
            bool isLogSection = blockStack.Block.Code.Path.Contains("logsection");

            for (int i = 0; i < BlockFacing.ALLFACES.Length; i++)
            {
                ShapeElementFace resizablePartFace = resizablePart.FacesResolved[i];
                ShapeElementFace sawablePartFace = sawablePart.FacesResolved[i];
                ShapeElementFace refShapeElementFace = refShapeElement.FacesResolved[i];

                resizablePartFace.Texture = refShapeElementFace.Texture;
                sawablePartFace.Texture = resizablePartFace.Texture;

                if (i == BlockFacing.indexEAST || i == BlockFacing.indexWEST)
                {
                    if (meshOffset != 0 && i == BlockFacing.indexWEST)
                    {
                        if (isLogSection)
                        {
                            sawablePartFace.Texture = "inside";
                        }
                        else if (blockStack.Block.Textures.ContainsKey("inside-" + sawablePartFace.Texture))
                        {
                            sawablePartFace.Texture = "inside-" + sawablePartFace.Texture;
                        }
                    }
                    resizablePartFace.Uv = refShapeElementFace.Uv;
                    sawablePartFace.Uv = resizablePartFace.Uv;
                }
                else
                {

                    if (i == BlockFacing.indexUP && !isLogSection)
                    {
                        resizablePartFace.Rotation = 180;
                        sawablePartFace.Rotation = 180;
                        resizablePartFace.Uv = resizablePart.FacesResolved[BlockFacing.ALLFACES[i].Opposite.Index].Uv;
                        sawablePartFace.Uv = sawablePart.FacesResolved[BlockFacing.ALLFACES[i].Opposite.Index].Uv;
                    }
                    int textUInitIndex;
                    int textUEndIndex;

                    if (resizablePartFace.Uv[0] == 0 || resizablePartFace.Uv[0] == 16)
                    {
                        textUInitIndex = 0;
                        textUEndIndex = 2;

                    }
                    else
                    {
                        textUInitIndex = 2;
                        textUEndIndex = 0;
                    }

                    int textUInitIndex1;
                    int textUEndIndex1;

                    if (sawablePartFace.Uv[0] == 0 || sawablePartFace.Uv[0] == 16)
                    {
                        textUInitIndex1 = 0;
                        textUEndIndex1 = 2;
                    }
                    else
                    {
                        textUInitIndex1 = 2;
                        textUEndIndex1 = 0;
                    }



                    int faceDirection = (BlockFacing.ALLNORMALI[i].X + BlockFacing.ALLNORMALI[i].Y + BlockFacing.ALLNORMALI[i].Z);


                    resizablePartFace.Uv[textUInitIndex] = refShapeElementFace.Uv[textUInitIndex];
                    resizablePartFace.Uv[textUEndIndex] = resizablePartFace.Uv[textUInitIndex] - ResizablePartXLength * faceDirection * (isLogSection ? 0.5f : 1);
                    sawablePartFace.Uv[textUInitIndex1] = resizablePartFace.Uv[textUInitIndex] - (ResizablePartXLength + SawablePartWithSawmarkXLength) * faceDirection * (isLogSection ? 0.5f : 1);
                    sawablePartFace.Uv[textUEndIndex1] = resizablePartFace.Uv[textUInitIndex] - (ResizablePartXLength + sawmarkXLength) * faceDirection * (isLogSection ? 0.5f : 1);


                    resizablePartFace.Uv[1] = refShapeElementFace.Uv[1];
                    resizablePartFace.Uv[3] = refShapeElementFace.Uv[3];
                    sawablePartFace.Uv[1] = refShapeElementFace.Uv[1];
                    sawablePartFace.Uv[3] = refShapeElementFace.Uv[3];
                }
            }
            //Step 3) Resize shape, move uv in according to sawable part.


            return shape;
        }

        private Shape GetShape(ICoreClientAPI capi, string type)
        {
            AssetLocation shapeCode = new AssetLocation(Constants.ModId, "shapes/block/" + type + ".json");
            Shape shape = capi.Assets.Get<Shape>(shapeCode).Clone();
            return shape;
        }

        private MeshData GenDefaultRendererMesh()
        {
            Block block = GetDefaultBlockStack().Block;
            if (block.BlockId == 0) return null;

            MeshData mesh;
            ITesselatorAPI mesher = ((ICoreClientAPI)Api).Tesselator;

            mesher.TesselateShape(block, GetShape((ICoreClientAPI)Api, "sawmark"), out mesh);

            return mesh;
        }

        public override void OnBlockRemoved()
        {
            base.OnBlockRemoved();

            renderer?.Dispose();
            renderer = null;
        }

        public void IsProcessing(bool processing)
        {
            if (renderer != null)
            {
                renderer.ShouldMove = processing;
            }
        }
    }
}