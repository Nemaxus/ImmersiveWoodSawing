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
        private int planksLeft = 0;
        private int planksTotal = 0;
        private int planksToTakeOut;
        private AssetLocation plankType = new("game:plank-aged");


        private MeshData blockMesh;

        public Cuboidf[] Boxes { get; private set; }

        public float LogSliceSize
        {
            get => logSliceSize;
        }

        public int PlanksLeft
        {
            get => planksLeft;
            set
            {
                planksLeft = value;

                UpdateBlockMesh();
            }
        }

        public int PlanksTotal
        {
            get => planksTotal;
        }

        public int PlanksToTakeOut
        {
            get => planksToTakeOut;
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
                    planksTotal = planksLeft = behavior.dropAmount;
                    logSliceSize = 16f / planksTotal;
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

            int planksToCreate = GameMath.Clamp(Api.World.Config.GetInt(Constants.ModId + ":PlanksPerUse", 1), 1, planksTotal);
            planksToTakeOut = (planksLeft - planksToCreate >= 0) ? planksToCreate : planksLeft;

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
                dsc.AppendLine("Planks Left: " + planksLeft);
            }
            dsc.AppendLine(blockStack.Block.GetPlacedBlockInfo(Api.World, forPlayer.CurrentBlockSelection.Position, forPlayer));
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);
            tree.SetString("copiedBlock", blockStack.Collectible.Code.ToString());
            tree.SetString("plankType", plankType.ToString());
            tree.SetInt("planksAmount", planksLeft);
            tree.SetInt("planksTotalAmount", planksTotal);
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
            planksLeft = tree.GetInt("planksAmount");
            planksTotal = tree.GetInt("planksTotalAmount");
            logSliceSize = tree.GetFloat("logSliceSize");
            UpdateBlockMesh();
        }


        private void UpdateBlockMesh()
        {

            float meshOffset = logSliceSize * (planksTotal - planksLeft);
            float offset = meshOffset * Constants.BlockProportion;

            int planksToCreate = GameMath.Clamp(Api.World.Config.GetInt(Constants.ModId + ":PlanksPerUse", 1), 1, planksTotal);
            int planksToTake = (planksLeft - planksToCreate >= 0) ? planksToCreate : planksLeft;
            planksToTakeOut = planksToTake;

            if (Api is ICoreClientAPI capi && blockStack != null)
            {


                Shape referenceShape = capi.Assets.Get<Shape>(new AssetLocation(blockStack.Block.Shape.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json").ToString()));
                Shape completeBlockShape = ResolveShapeElementsSizes(GetShape(capi, "complete"), meshOffset, (float)planksToTake * logSliceSize, referenceShape);
                Shape blockShape = CopyElementsFromShape(completeBlockShape, GetShape(capi, "sawablelog"));
                Shape rendererShape = CopyElementsFromShape(completeBlockShape, GetShape(capi, "sawmark"));
                renderer.UpdateRendererMesh(GenRendererMesh(rendererShape));

                //capi.Tesselator.TesselateShape(blockStack.Collectible, blockShape, out blockMesh);
                ITexPositionSource source = capi.Tesselator.GetTextureSource(blockStack.Block);
                capi.Tesselator.TesselateShape("sawablelog", blockShape, out blockMesh, source);

            }
            /*   
                Shape referenceShape = capi.Assets.Get<Shape>(new AssetLocation(this.blockStack.Block.Shape.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json").ToString()));
                Shape completeBlockShape = this.ResolveShapeElementsSizes(this.GetShape(capi, "complete"), meshOffset, (float)this.planksToTakeOut * this.logSliceSize, referenceShape);
                Shape blockShape = this.CopyElementsFromShape(completeBlockShape, this.GetShape(capi, "sawablelog"));
                Shape rendererShape = this.CopyElementsFromShape(completeBlockShape, this.GetShape(capi, "sawmark"));
                this.renderer.UpdateRendererMesh(this.GenRendererMesh(rendererShape));
                ITexPositionSource source = capi.Tesselator.GetTextureSource(this.blockStack.Block, 0, false);
                capi.Tesselator.TesselateShape("sawablelog", blockShape, out this.blockMesh, source, null, 0, 0, 0, null, null);
            }*/
            MarkDirty(true);
            if (Block is SawableLog)
            {
                Boxes[0] = Block.CollisionBoxes[0].Clone();
                Boxes[0].X1 = offset;
            }
        }

        private Shape ResolveShapeElementsSizes(Shape shape, float meshOffset, float SawablePartWithSawmarkXLength, Shape referenceShape)
        {
            //Step 1) Get part proportions for our block
            ShapeElement resizablePart = shape.GetElementByName("ResizablePart");
            ShapeElement sawmarkPart = shape.GetElementByName("Sawmark");
            ShapeElement sawablePart = shape.GetElementByName("SawablePart");
            ShapeElement refShapeElement = referenceShape.Elements[0];


            //Need to take in account the size of sawmark X length when changing its position(and setting the size of this part as well)

            resizablePart.From[0] = (double)(SawablePartWithSawmarkXLength + meshOffset);
            float ResizablePartXLength = (float)(resizablePart.To[0] - resizablePart.From[0]);
            float sawmarkXLength = (float)(sawmarkPart.To[0] - sawmarkPart.From[0]);
            sawablePart.From[0] = (double)meshOffset;
            sawablePart.To[0] = (double)(SawablePartWithSawmarkXLength - sawmarkXLength + meshOffset);
            sawmarkPart.To[0] = resizablePart.From[0];
            sawmarkPart.From[0] = sawablePart.To[0];

            //Step 2) Make adjustments for textures types, their rotarion, position, etc.
            bool isLogSection = this.blockStack.Block.Code.Path.Contains("logsection");
            for (int i = 0; i < BlockFacing.ALLFACES.Length; i++)
            {
                ShapeElementFace resizablePartFace = resizablePart.FacesResolved[i];
                ShapeElementFace sawmarkPartFace = sawmarkPart.FacesResolved[i];
                ShapeElementFace sawablePartFace = sawablePart.FacesResolved[i];
                ShapeElementFace refShapeElementFace = refShapeElement.FacesResolved[i];
                resizablePartFace.Texture = refShapeElementFace.Texture;
                sawablePartFace.Texture = resizablePartFace.Texture;
                sawmarkPartFace.Texture = sawablePartFace.Texture;
                if (i == 1 || i == 3)
                {
                    bool hasInsideTextureVariant = this.blockStack.Block.Textures.ContainsKey("inside-" + sawablePartFace.Texture);
                    string insideTexture = "inside" + ((!isLogSection && hasInsideTextureVariant) ? ("-" + sawablePartFace.Texture) : "");
                    if (isLogSection || hasInsideTextureVariant)
                    {
                        if (i == 1)
                        {
                            sawablePartFace.Texture = insideTexture;
                        }
                        else
                        {
                            resizablePartFace.Texture = insideTexture;
                            if (meshOffset != 0f)
                            {
                                sawablePartFace.Texture = insideTexture;
                            }
                        }
                    }
                    resizablePartFace.Uv = refShapeElementFace.Uv;
                    sawablePartFace.Uv = resizablePartFace.Uv;
                    sawmarkPartFace.Uv = resizablePartFace.Uv;
                }
                else
                {
                    if (i == 4 && !isLogSection)
                    {
                        resizablePartFace.Rotation = 180f;
                        sawablePartFace.Rotation = 180f;
                        sawmarkPartFace.Rotation = 180f;
                        resizablePartFace.Uv = resizablePart.FacesResolved[BlockFacing.ALLFACES[i].Opposite.Index].Uv;
                        sawablePartFace.Uv = sawablePart.FacesResolved[BlockFacing.ALLFACES[i].Opposite.Index].Uv;
                        sawmarkPartFace.Uv = sawmarkPart.FacesResolved[BlockFacing.ALLFACES[i].Opposite.Index].Uv;
                    }
                    int textureInitIndexForResizablePart;
                    int textureEndIndexForResizablePart;
                    if (resizablePartFace.Uv[0] == 0f || resizablePartFace.Uv[0] == 16f)
                    {
                        textureInitIndexForResizablePart = 0;
                        textureEndIndexForResizablePart = 2;
                    }
                    else
                    {
                        textureInitIndexForResizablePart = 2;
                        textureEndIndexForResizablePart = 0;
                    }
                    int textureInitIndexForSawmarkPart;
                    int textureEndIndexForSawmarkPart;
                    if (sawmarkPartFace.Uv[0] == resizablePartFace.Uv[textureEndIndexForResizablePart])
                    {
                        textureInitIndexForSawmarkPart = 0;
                        textureEndIndexForSawmarkPart = 2;
                    }
                    else
                    {
                        textureInitIndexForSawmarkPart = 2;
                        textureEndIndexForSawmarkPart = 0;
                    }
                    int textureInitIndexForSawablePart;
                    int textureEndIndexForSawablePart;
                    if (sawablePartFace.Uv[0] == 0f || sawablePartFace.Uv[0] == 16f)
                    {
                        textureInitIndexForSawablePart = 0;
                        textureEndIndexForSawablePart = 2;
                    }
                    else
                    {
                        textureInitIndexForSawablePart = 2;
                        textureEndIndexForSawablePart = 0;
                    }
                    int faceDirection = BlockFacing.ALLNORMALI[i].X + BlockFacing.ALLNORMALI[i].Y + BlockFacing.ALLNORMALI[i].Z;
                    resizablePartFace.Uv[textureInitIndexForResizablePart] = refShapeElementFace.Uv[textureInitIndexForResizablePart];
                    resizablePartFace.Uv[textureEndIndexForResizablePart] = resizablePartFace.Uv[textureInitIndexForResizablePart] - ResizablePartXLength * (float)faceDirection * (isLogSection ? 0.5f : 1f);
                    sawablePartFace.Uv[textureInitIndexForSawablePart] = resizablePartFace.Uv[textureInitIndexForResizablePart] - (ResizablePartXLength + SawablePartWithSawmarkXLength) * (float)faceDirection * (isLogSection ? 0.5f : 1f);
                    sawablePartFace.Uv[textureEndIndexForSawablePart] = resizablePartFace.Uv[textureInitIndexForResizablePart] - (ResizablePartXLength + sawmarkXLength) * (float)faceDirection * (isLogSection ? 0.5f : 1f);
                    sawmarkPartFace.Uv[textureInitIndexForSawmarkPart] = resizablePartFace.Uv[textureEndIndexForResizablePart];
                    sawmarkPartFace.Uv[textureEndIndexForSawmarkPart] = sawablePartFace.Uv[textureEndIndexForSawablePart];
                    resizablePartFace.Uv[1] = (sawablePartFace.Uv[1] = (sawmarkPartFace.Uv[1] = refShapeElementFace.Uv[1]));
                    resizablePartFace.Uv[3] = (sawablePartFace.Uv[3] = (sawmarkPartFace.Uv[3] = refShapeElementFace.Uv[3]));
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

        public Shape CopyElementsFromShape(Shape completeShape, Shape partialShape)
        {
            for (int i = 0; i < partialShape.Elements.Length; i++)
            {
                partialShape.Elements[i] = completeShape.GetElementByName(partialShape.Elements[i].Name, StringComparison.InvariantCultureIgnoreCase);
            }
            return partialShape;
        }

        private MeshData GenRendererMesh(Shape shape)
        {
            ItemStack itemStack = this.blockStack;
            Block block = (itemStack != null) ? itemStack.Block : null;
            if (block == null)
            {
                block = this.GetDefaultBlockStack().Block;
            }
            if (block.BlockId == 0)
            {
                return null;
            }
            MeshData mesh;
            ((ICoreClientAPI)this.Api).Tesselator.TesselateShape(block, shape, out mesh, null, null, null);
            return mesh;
        }
    }
}