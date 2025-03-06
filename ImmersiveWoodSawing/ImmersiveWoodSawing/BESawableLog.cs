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


        internal MeshData blockMesh;

        public Cuboidf[] ColBox { get; private set; }
        public Cuboidf[] SelBox { get; private set; }

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
                    var box = blockStack.Block.CollisionBoxes[0];
                    logSliceSize = (Math.Abs(box.X1 - box.X2)) *
                        16f / planksTotal;
                }
                UpdateBlockMesh();
            }
        }

        public SawMarkRenderer renderer;


        public BESawableLog() : base()
        {
            ColBox = new Cuboidf[] { new Cuboidf(0, 0, 0, 1, 1, 1) };
            SelBox = new Cuboidf[] { new Cuboidf(0, 0, 0, 1, 1, 1) };
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

            //dsc.AppendLine(blockStack.Block.GetPlacedBlockInfo(Api.World, forPlayer.CurrentBlockSelection.Position, forPlayer));
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

            if (Api != null)
            {
                int planksToCreate = GameMath.Clamp(Api.World.Config.GetInt(Constants.ModId + ":PlanksPerUse", 1), 1, planksTotal);
                int planksToTake = (planksLeft - planksToCreate >= 0) ? planksToCreate : planksLeft;
                planksToTakeOut = planksToTake;
            }

            if (Api is ICoreClientAPI capi && blockStack != null)
            {


                Shape referenceShape = capi.Assets.Get<Shape>(new AssetLocation(blockStack.Block.Shape.Base.WithPathPrefixOnce("shapes/").WithPathAppendixOnce(".json").ToString()));
                Shape completeBlockShape = ResolveShapeElementsSizes(GetShape(capi, "complete"), meshOffset, planksToTakeOut * logSliceSize, referenceShape);
                Shape blockShape = CopyElementsFromShape(completeBlockShape, GetShape(capi, "sawablelog"));
                Shape rendererShape = CopyElementsFromShape(completeBlockShape, GetShape(capi, "sawmark"));
                renderer.UpdateRendererMesh(GenRendererMesh(rendererShape));

                //capi.Tesselator.TesselateShape(blockStack.Collectible, blockShape, out blockMesh);
                ITexPositionSource source = capi.Tesselator.GetTextureSource(blockStack.Block);
                capi.Tesselator.TesselateShape("sawablelog", blockShape, out blockMesh, source);

            }

            MarkDirty(true);

            if (Block is SawableLog)
            {
                ColBox[0] = blockStack.Block.CollisionBoxes[0].Clone();
                SelBox[0] = blockStack.Block.SelectionBoxes[0].Clone();
                ColBox[0].X1 += meshOffset * Constants.BlockProportion;
                SelBox[0].X1 += meshOffset * Constants.BlockProportion;
            }
        }

        public Shape ResolveShapeElementsSizes(Shape shape, float meshOffset, float SawablePartWithSawmarkXLength, Shape referenceShape)
        {
            //Step 1) Get part proportions for our block
            ShapeElement resizablePart = shape.GetElementByName("ResizablePart");
            ShapeElement sawmarkPart = shape.GetElementByName("Sawmark");
            ShapeElement sawablePart = shape.GetElementByName("SawablePart");
            ShapeElement refShapeElement = referenceShape.Elements[0];


            //Need to take in account the size of sawmark X length when changing its position(and setting the size of this part as well)
            foreach (var elem in shape.Elements)
            {
                for (int i = 1; i < elem.From.Length; i++)
                {
                    elem.From[i] = refShapeElement.From[i];
                    elem.To[i] = refShapeElement.To[i];
                }
            }
            resizablePart.To[0] = refShapeElement.To[0];

            resizablePart.From[0] = resizablePart.To[0] - Math.Abs(refShapeElement.From[0] - refShapeElement.To[0]) + SawablePartWithSawmarkXLength + meshOffset;
            float ResizablePartXLength = (float)Math.Abs(resizablePart.To[0] - resizablePart.From[0]);
            float sawmarkXLength = (float)Math.Abs(sawmarkPart.To[0] - sawmarkPart.From[0]);
            sawablePart.From[0] = meshOffset + refShapeElement.From[0];
            sawablePart.To[0] = SawablePartWithSawmarkXLength - sawmarkXLength + sawablePart.From[0];
            sawmarkPart.To[0] = resizablePart.From[0];
            sawmarkPart.From[0] = sawmarkPart.To[0] - sawmarkXLength;

            //Step 2) Make adjustments for textures types, their rotarion, position, etc.
            bool isLogSection = blockStack.Block.Code.Path.Contains("logsection");
            bool isLog = blockStack.Block.Code.Path.Contains("log-");
            for (int i = 0; i < BlockFacing.ALLFACES.Length; i++)
            {
                ShapeElementFace resizablePartFace = resizablePart.FacesResolved[i];
                ShapeElementFace sawmarkPartFace = sawmarkPart.FacesResolved[i];
                ShapeElementFace sawablePartFace = sawablePart.FacesResolved[i];
                ShapeElementFace refShapeElementFace = refShapeElement.FacesResolved[i];
                resizablePartFace.Texture = refShapeElementFace.Texture;
                sawablePartFace.Texture = resizablePartFace.Texture;
                sawmarkPartFace.Texture = sawablePartFace.Texture;

                float uvToSideSizeRatio = (float)(Math.Abs(refShapeElementFace.Uv[0] - refShapeElementFace.Uv[2]) / Math.Abs(refShapeElement.From[0] - refShapeElement.To[0])); 
                if (i == BlockFacing.indexEAST || i == BlockFacing.indexWEST)
                {
                    bool hasInsideTextureVariant = blockStack.Block.Textures.ContainsKey("inside-" + sawablePartFace.Texture);
                    string insideTexture = "inside" + ((!isLogSection && hasInsideTextureVariant) ? ("-" + sawablePartFace.Texture) : "");
                    if (isLogSection || hasInsideTextureVariant)
                    {
                        if (i == BlockFacing.indexEAST)
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
                    if (i == BlockFacing.indexUP && isLog)
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
                    int textureInitIndexForSawmarkPart;
                    int textureEndIndexForSawmarkPart;
                    int textureInitIndexForSawablePart;
                    int textureEndIndexForSawablePart;

                    if (resizablePartFace.Uv[0] == 0f|| resizablePartFace.Uv[0] == 16f)
                    {
                        textureInitIndexForResizablePart = 0;
                        textureEndIndexForResizablePart = 2;
                    }
                    else
                    {
                        textureInitIndexForResizablePart = 2;
                        textureEndIndexForResizablePart = 0;
                    }
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
                    resizablePartFace.Uv[textureEndIndexForResizablePart] = resizablePartFace.Uv[textureInitIndexForResizablePart] - ResizablePartXLength * (float)faceDirection * uvToSideSizeRatio;//(isLogSection ? 0.5f : 1f);
                    sawablePartFace.Uv[textureInitIndexForSawablePart] = resizablePartFace.Uv[textureInitIndexForResizablePart] - (ResizablePartXLength + SawablePartWithSawmarkXLength) * (float)faceDirection * uvToSideSizeRatio;
                    sawablePartFace.Uv[textureEndIndexForSawablePart] = resizablePartFace.Uv[textureInitIndexForResizablePart] - (ResizablePartXLength + sawmarkXLength) * (float)faceDirection * uvToSideSizeRatio;
                    sawmarkPartFace.Uv[textureInitIndexForSawmarkPart] = resizablePartFace.Uv[textureEndIndexForResizablePart];
                    sawmarkPartFace.Uv[textureEndIndexForSawmarkPart] = sawablePartFace.Uv[textureEndIndexForSawablePart];
                    resizablePartFace.Uv[1] = (sawablePartFace.Uv[1] = (sawmarkPartFace.Uv[1] = refShapeElementFace.Uv[1]));
                    resizablePartFace.Uv[3] = (sawablePartFace.Uv[3] = (sawmarkPartFace.Uv[3] = refShapeElementFace.Uv[3]));
                }
            }
            //Step 3) Resize shape, move uv in according to sawable part.


            return shape;
        }

        public Shape GetShape(ICoreClientAPI capi, string type)
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