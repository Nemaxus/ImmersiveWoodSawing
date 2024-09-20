using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;


namespace ImmersiveWoodSawing
{
    class WoodSawing : CollectibleBehavior
    {
        private float stressPoints;
        public WoodSawing(CollectibleObject collObj) : base(collObj)
        {
            stressPoints = 0;
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling, ref EnumHandling handling)
        {
            if (blockSel == null) return;
            if (byEntity == null) return;
            handHandling = EnumHandHandling.PreventDefault;
            IWorldAccessor world = byEntity.World;
            IBlockAccessor blockAccessor = world.BlockAccessor;
            Block block = blockAccessor.GetBlock(blockSel.Position);
            ICoreClientAPI coreClientAPI = byEntity.Api as ICoreClientAPI;

            if (!byEntity.Api.World.Claims.TryAccess((byEntity as EntityPlayer).Player, blockSel.Position, EnumBlockAccessFlags.BuildOrBreak))
            {
                if (coreClientAPI == null)
                {
                    return;
                }
                coreClientAPI.TriggerIngameError(this, "notsawable-claimedland", Lang.Get(Constants.ModId + ":ingameerror-notsawable-claimedland"));
                return;
            }
            else
            {
                if (!byEntity.Api.ModLoader.GetModSystem<ModSystemBlockReinforcement>(true).IsReinforced(blockSel.Position))
                {
                    if (IsSawable(block) && !(block is SawableLog))
                    {
                        handling = EnumHandling.PreventDefault;
                        SawableLog slog = world.GetBlock(new AssetLocation(Constants.ModId + ":sawablelog")) as SawableLog;
                        if (slog != null)
                        {
                            blockAccessor.SetBlock(slog.BlockId, blockSel.Position);
                            blockAccessor.GetBlockEntity<BESawableLog>(blockSel.Position).BlockStack = new ItemStack(block, 1);
                            blockAccessor.MarkBlockDirty(blockSel.Position);
                            block = blockAccessor.GetBlock(blockSel.Position);
                        }
                    }
                    if (block is SawableLog)
                    {
                        handling = EnumHandling.PreventDefault;
                        byEntity.Attributes.SetBool(Constants.ModId + ":sawnblock", false);
                        byEntity.Attributes.SetBool(Constants.ModId + ":registeredcallback", false);
                        byEntity.StartAnimation("immersivewoodsawing");
                    }
                    return;
                }
                else
                {
                    if (coreClientAPI == null) return;
                    coreClientAPI.TriggerIngameError(this, "notsawable-reinforced", Lang.Get(Constants.ModId + ":ingameerror-notsawable-reinforced"));
                }

                return;


                /*
                //DELETE AFTER TESTING ======
                BESawableLog beslog = (blockAccessor.GetBlockEntity(blockSel.Position)) as BESawableLog;
                IPlayer byPlayer = (byEntity as EntityPlayer).Player;

                int planksToCreate = GameMath.Clamp(world.Config.GetInt(Constants.ModId + ":PlanksPerUse", 1), 1, beslog.PlanksTotalAmount);
                int planksToTakeOut = (beslog.PlanksAmount - planksToCreate >= 0) ? planksToCreate : beslog.PlanksAmount;

                beslog.PlanksAmount -= planksToTakeOut;
                if (beslog.PlanksAmount <= 0)
                {

                    blockAccessor.BreakBlock(blockSel.Position, (byEntity as EntityPlayer).Player, 0);
                    if (world.Config.TryGetBool(Constants.ModId + ":AutoLogPlacement") == true)
                    {
                        PlaceNextLog(byEntity, byPlayer, blockAccessor, blockSel);
                    }
                    blockAccessor.MarkBlockDirty(blockSel.Position);
                }
                //DELETE AFTER TESTING ======*/
            }

        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            IPlayer byPlayer = (byEntity as EntityPlayer).Player;
            IWorldAccessor world = byEntity.World;
            IBlockAccessor blockAccessor = world.BlockAccessor;
            Item sawtool = byEntity.ActiveHandItemSlot.Itemstack.Item;

            if (blockSel != null)
            {
                Block block = blockAccessor.GetBlock(blockSel.Position);
                BESawableLog beslog = (blockAccessor.GetBlockEntity(blockSel.Position)) as BESawableLog;
                if (block is SawableLog)
                {
                    int planksToTakeOut = beslog.PlanksToTakeOut;

                    float sawSpeedMultiplier = world.Config.GetFloat(Constants.ModId + ":" + "SawSpeedMultilier");

                    float preparationStage = 0.4f;
                    float sawingSpeed = 15f / sawtool.ToolTier / sawSpeedMultiplier;
                    float progress = (secondsUsed - preparationStage) / sawingSpeed;

                    if (world.Side == EnumAppSide.Client && secondsUsed > preparationStage)
                    {
                        beslog.IsProcessing(true);
                        beslog.renderer.YOffset = 1f - progress;
                        AnimationMetaData animData;
                        string anim = byEntity.AnimManager.ActiveAnimationsByAnimCode.TryGetValue("immersivewoodsawing-fp", out animData) ? "immersivewoodsawing-fp" : "immersivewoodsawing";
                        bool triggeredSawSound = byEntity.Attributes.GetBool(Constants.ModId + ":registeredcallback", false);
                        bool flag = byEntity.AnimManager.Animator.GetAnimationState(anim).CurrentFrame > getSourceFrame(byEntity, anim, "soundAtFrame");
                        if (flag && !triggeredSawSound)
                        {
                            world.SpawnParticles(ParticleProvider.GetParticleProperties(blockAccessor, blockSel, block, byEntity, secondsUsed, sawingSpeed, beslog.LogSliceSize * (float)planksToTakeOut, true), null);
                            playSawSound(byEntity);
                            byEntity.Attributes.SetBool(Constants.ModId + ":registeredcallback", true);
                        }
                        if (!flag && triggeredSawSound)
                        {
                            world.SpawnParticles(ParticleProvider.GetParticleProperties(blockAccessor, blockSel, block, byEntity, secondsUsed, sawingSpeed, beslog.LogSliceSize * (float)planksToTakeOut, false), null);
                            byEntity.Attributes.SetBool(Constants.ModId + ":registeredcallback", false);
                        }
                    }
                    if (world.Side == EnumAppSide.Server)
                    {
                        if (progress > 0.99f && !byEntity.Attributes.GetBool(Constants.ModId + ":sawnblock", false))
                        {
                            Item plank = byEntity.World.GetItem(beslog.PlankType);
                            if (beslog.PlanksLeft > 0)
                            {

                                beslog.PlanksLeft -= planksToTakeOut;
                                if (beslog.PlanksLeft - planksToTakeOut <= 0)
                                {
                                    planksToTakeOut += beslog.PlanksLeft;
                                    beslog.PlanksLeft = 0;
                                }
                                stressPoints += (float)planksToTakeOut / beslog.PlanksTotal;

                                for (int i = 0; i < planksToTakeOut; i++)
                                {
                                    world.SpawnItemEntity(new ItemStack(plank, 1), blockSel.Position.ToVec3d(), null);
                                }
                            }
                            if (beslog.PlanksLeft <= 0)
                            {
                                blockAccessor.BreakBlock(blockSel.Position, (byEntity as EntityPlayer).Player, 0f);
                                if (world.Config.TryGetBool(Constants.ModId + ":AutoLogPlacement") == true)
                                {
                                    PlaceNextLog(byEntity, byPlayer, blockAccessor, blockSel);
                                }
                                blockAccessor.MarkBlockDirty(blockSel.Position);
                            }
                            if (stressPoints >= 0.99f)
                            {
                                stressPoints = (stressPoints - 0.99f < 0) ? 0 : stressPoints - 0.99f;
                                sawtool.DamageItem(world, byEntity, byEntity.RightHandItemSlot, 1);
                            }
                            byEntity.Attributes.SetBool(Constants.ModId + ":sawnblock", true);
                        }
                    }

                    handling = EnumHandling.PreventSubsequent;
                    return progress <= 1f;
                }
            }
            return false;
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, ref EnumHandling handling)
        {
            byEntity.StopAnimation("immersivewoodsawing");
            if (blockSel != null)
            {
                if (byEntity.World.BlockAccessor.GetBlockEntity(blockSel?.Position) is BESawableLog sawableLog)
                {
                    sawableLog.IsProcessing(false);
                }
            }
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel, ref handling);
        }

        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason, ref EnumHandling handled)
        {
            byEntity.StopAnimation("immersivewoodsawing");
            if (blockSel != null)
            {
                if (byEntity.World.BlockAccessor.GetBlockEntity(blockSel?.Position) is BESawableLog sawableLog)
                {
                    sawableLog.IsProcessing(false);
                }
            }
            return base.OnHeldInteractCancel(secondsUsed, slot, byEntity, blockSel, entitySel, cancelReason, ref handled);
        }




        private void PlaceNextLog(EntityAgent byEntity, IPlayer byPlayer, IBlockAccessor blockAccessor, BlockSelection blockSel)
        {
            Block foungLog = null;
            ItemSlot logInSlot = null;
            byEntity.WalkInventory((currentSlot) =>
            {
                if (currentSlot is ItemSlotCreative) return true;
                if (!(currentSlot.Inventory is InventoryBasePlayer)) return true;
                Block blockInCurrentSlot = currentSlot.Itemstack?.Block;
                if (blockInCurrentSlot != null && IsSawable(blockInCurrentSlot))
                {
                    foungLog = blockInCurrentSlot;
                    logInSlot = currentSlot;
                    return false;
                }
                return true;
            });
            if (foungLog != null)
            {
                blockAccessor.SetBlock(foungLog.Id, blockSel.Position);
                byEntity.World.PlaySoundAt(foungLog.Sounds.Place, byPlayer);
                if (byPlayer.WorldData.CurrentGameMode != EnumGameMode.Creative)
                {
                    logInSlot.TakeOut(1);
                    logInSlot.MarkDirty();
                }
            }
        }

        private bool IsSawable(Block block)
        {
            return block.HasBehavior<BlockBehaviorSawable>();
        }

        private void AnimationStep(float secondsUsed, EntityAgent byEntity, int toolTier)
        {
            //Adjusting animation speed
            float t = secondsUsed * 2f;
            //ItemFirestarter
            float num = GameMath.Clamp(t, 0f, 1f);


            var tf = new ModelTransform();
            tf.EnsureDefaultValues();

            tf.Translation.Set(-num * 0.6f, 0f, -num);
            tf.Rotation.X = -Math.Max(0f, num * 90f);
            tf.Rotation.Y = Math.Max(0f, num * 20f);
            tf.Rotation.Z = Math.Max(0f, num * 5f);


            if (t > 1.2f)
            {
                //Idk if there is a much more simple formula for saw movement
                float waveLength = 0.5f;
                float frequency = 2.9f;
                float res = (float)Math.Pow(GameMath.Tan(GameMath.Sin(t * frequency)), 2) * waveLength;
                float res2 = (float)Math.Pow(GameMath.Tan(GameMath.Sin((t - 0.3f) * frequency)), 2) * waveLength;
                tf.Translation.Add(0f, 0f, -res);
                if (res <= 0.3f && res2 < res && byEntity.WatchedAttributes.GetBool(Constants.ModId + ":sawforward"))
                {
                    //(byEntity.World as IClientWorldAccessor)?.AddCameraShake(0.20f);
                    byEntity.World.PlaySoundAt(new AssetLocation(Constants.ModId, "sounds/saw_combined"), byEntity as EntityPlayer);
                    byEntity.WatchedAttributes.SetBool(Constants.ModId + ":sawforward", false);
                }
                if (res >= 1.1f && !byEntity.WatchedAttributes.GetBool(Constants.ModId + ":sawforward"))
                {
                    byEntity.WatchedAttributes.SetBool(Constants.ModId + ":sawforward", true);
                }

            }
            if (t > 1.4f)
            {
                //Subtracting time to prevent sudden movement and make it go down smoother 
                tf.Translation.Add(0f, -(t - 1.5f) * 0.04f * toolTier, 0f);
            }


            byEntity.Controls.UsingHeldItemTransformAfter = tf;

        }


        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot, ref EnumHandling handling)
        {
            return new WorldInteraction[]
            {
                new WorldInteraction
                {
                    ActionLangCode = "immersivewoodsawing:heldhelp-saw",
                    MouseButton = EnumMouseButton.Right
                }
            };
        }

        public static float getSourceFrame(EntityAgent byEntity, string animCode, string framecode)
        {
            AnimationMetaData animdata;
            if (byEntity.Properties.Client.AnimationsByMetaCode.TryGetValue(animCode, out animdata))
            {
                JsonObject attributes = animdata.Attributes;
                if (attributes != null && attributes[framecode].Exists)
                {
                    return animdata.Attributes[framecode].AsFloat(-1f);
                }
            }
            return -1f;
        }

        protected void playSawSound(EntityAgent byEntity)
        {
            IPlayer byPlayer = (byEntity as EntityPlayer).Player;
            if (byPlayer == null)
            {
                return;
            }
            byPlayer.Entity.World.PlaySoundAt(new AssetLocation(Constants.ModId, "sounds/saw_combined"), byPlayer.Entity, byPlayer, 0.9f + (float)byEntity.World.Rand.NextDouble() * 0.2f, 16f, 0.35f);

        }
    }
}