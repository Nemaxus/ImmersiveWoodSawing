using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent.Mechanics;

namespace ImmersiveWoodSawing
{
    public class SawMarkRenderer : IRenderer
    {
        private ICoreClientAPI api;
        private BlockPos pos;

        MeshRef meshref;

        public Matrixf ModelMat = new();

        public float YOffset;
        public float XOffset;

        internal bool ShouldRender;
        internal bool ShouldMove;

        public SawMarkRenderer(ICoreClientAPI capi, BlockPos pos, MeshData meshData)
        {
            api = capi;
            this.pos = pos;
            meshref = capi.Render.UploadMesh(meshData);
        }

        public double RenderOrder
        {
            get { return 0.5; }
        }

        public int RenderRange => 24;

        public void Dispose()
        {
            api.Event.UnregisterRenderer(this, EnumRenderStage.Opaque);

            meshref.Dispose();
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (meshref == null || !ShouldRender) return;

            IRenderAPI rpi = api.Render;
            Vec3d camPos = api.World.Player.Entity.CameraPos;

            rpi.GlDisableCullFace();
            rpi.GlToggleBlend(true);

            IStandardShaderProgram prog = rpi.PreparedStandardShader(pos.X, pos.Y, pos.Z);
            prog.Tex2D = api.BlockTextureAtlas.AtlasTextures[0].TextureId;


            prog.ModelMatrix = ModelMat
                .Identity()
                .Translate(pos.X - camPos.X, pos.Y - camPos.Y, pos.Z - camPos.Z)
                .Scale(1f, YOffset, 1f)
                .Values
            ;

            prog.ViewMatrix = rpi.CameraMatrixOriginf;
            prog.ProjectionMatrix = rpi.CurrentProjectionMatrix;
            rpi.RenderMesh(meshref);
            prog.Stop();

            if (!ShouldMove)
            {
                YOffset = 1f;
            }
            /*else
            {
                YOffset = 0;
            }*/

            
        }

        public void UpdateRendererMesh(MeshData meshData)
        {
           api.Render.UpdateMesh(meshref,meshData);
        }
    }
}
