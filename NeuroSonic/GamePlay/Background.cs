using System;
using System.Numerics;
using MoonSharp.Interpreter;
using theori;
using theori.Graphics;
using theori.Graphics.OpenGL;
using theori.Resources;
using theori.Scripting;

using static MoonSharp.Interpreter.DynValue;

namespace NeuroSonic.GamePlay
{
    public class ScriptableBackground : Disposable
    {
        private readonly ClientResourceLocator m_locator;
        private readonly ClientResourceManager m_resources;

        public float HorizonHeight { get; set; }
        public float CombinedTilt { get; set; }
        public float EffectRotation { get; set; }
        public float SpinTimer { get; set; }
        public float SwingTimer { get; set; }

        private readonly RenderBatch2D m_renderer2D;
        private RenderBatcher2D? m_batch = null;

        private readonly ScriptProgram m_script;

        public readonly Table tblTheori;
        public readonly Table tblTheoriGraphics;

        private ClientResourceManager StaticResources => theori.Host.StaticResources;

        public ScriptableBackground(ClientResourceLocator locator)
        {
            m_locator = locator;
            m_resources = new ClientResourceManager(m_locator);

            m_renderer2D = new RenderBatch2D(m_resources);

            m_script = new ScriptProgram(m_locator);

            m_script["theori"] = tblTheori = m_script.NewTable();
            tblTheori["graphics"] = tblTheoriGraphics = m_script.NewTable();
            tblTheoriGraphics["queueStaticTextureLoad"] = (Func<string, Texture>)(textureName => StaticResources.QueueTextureLoad($"textures/{ textureName }"));
            tblTheoriGraphics["getStaticTexture"] = (Func<string, Texture>)(textureName => StaticResources.GetTexture($"textures/{ textureName }"));
            tblTheoriGraphics["queueTextureLoad"] = (Func<string, Texture>)(textureName => m_resources.QueueTextureLoad($"textures/{ textureName }"));
            tblTheoriGraphics["getTexture"] = (Func<string, Texture>)(textureName => m_resources.GetTexture($"textures/{ textureName }"));
            tblTheoriGraphics["createFont"] = (Func<string, VectorFont>)(fontName => new VectorFont(m_locator.OpenFileStreamWithExtension($"fonts/{ fontName }", new[] { ".ttf", ".otf" }, out string _)));
            //tblTheoriGraphics["getFont"] = (Func<string, VectorFont>)(fontName => m_resources.GetTexture($"fonts/{ fontName }"));
            tblTheoriGraphics["getViewportSize"] = (Func<DynValue>)(() => NewTuple(NewNumber(Window.Width), NewNumber(Window.Height)));
            tblTheoriGraphics["createPathCommands"] = (Func<Path2DCommands>)(() => new Path2DCommands());

            tblTheoriGraphics["flush"] = (Action)(() => m_batch?.Flush());
            tblTheoriGraphics["saveTransform"] = (Action)(() => m_batch?.SaveTransform());
            tblTheoriGraphics["restoreTransform"] = (Action)(() => m_batch?.RestoreTransform());
            tblTheoriGraphics["resetTransform"] = (Action)(() => m_batch?.ResetTransform());
            tblTheoriGraphics["translate"] = (Action<float, float>)((x, y) => m_batch?.Translate(x, y));
            tblTheoriGraphics["rotate"] = (Action<float>)(d => m_batch?.Rotate(d));
            tblTheoriGraphics["scale"] = (Action<float, float>)((x, y) => m_batch?.Scale(x, y));
            tblTheoriGraphics["shear"] = (Action<float, float>)((x, y) => m_batch?.Shear(x, y));
            tblTheoriGraphics["setFillToColor"] = (Action<float, float, float, float>)((r, g, b, a) => m_batch?.SetFillColor(r / 255.0f, g / 255.0f, b / 255.0f, a / 255.0f));
            tblTheoriGraphics["setFillToTexture"] = (Action<Texture, float, float, float, float>)((texture, r, g, b, a) => m_batch?.SetFillTexture(texture, new Vector4(r, g, b, a) / 255.0f));
            tblTheoriGraphics["fillRect"] = (Action<float, float, float, float>)((x, y, w, h) => m_batch?.FillRectangle(x, y, w, h));
            tblTheoriGraphics["fillRoundedRect"] = (Action<float, float, float, float, float>)((x, y, w, h, r) => m_batch?.FillRoundedRectangle(x, y, w, h, r));
            tblTheoriGraphics["fillRoundedRectVarying"] = (Action<float, float, float, float, float, float, float, float>)((x, y, w, h, rtl, rtr, rbr, rbl) => m_batch?.FillRoundedRectangleVarying(x, y, w, h, rtl, rtr, rbr, rbl));
            tblTheoriGraphics["setFont"] = (Action<VectorFont?>)(font => m_batch?.SetFont(font));
            tblTheoriGraphics["setFontSize"] = (Action<int>)(size => m_batch?.SetFontSize(size));
            tblTheoriGraphics["setTextAlign"] = (Action<Anchor>)(align => m_batch?.SetTextAlign(align));
            tblTheoriGraphics["fillString"] = (Action<string, float, float>)((text, x, y) => m_batch?.FillString(text, x, y));
            tblTheoriGraphics["fillPathAt"] = (Action<Path2DCommands, float, float, float, float>)((path, x, y, sx, sy) => m_batch?.FillPathAt(path, x, y, sx, sy));
            tblTheoriGraphics["saveScissor"] = (Action)(() => m_batch?.SaveScissor());
            tblTheoriGraphics["restoreScissor"] = (Action)(() => m_batch?.RestoreScissor());
            tblTheoriGraphics["resetScissor"] = (Action)(() => m_batch?.ResetScissor());
            tblTheoriGraphics["scissor"] = (Action<float, float, float, float>)((x, y, w, h) => m_batch?.Scissor(x, y, w, h));
        }

        protected override void DisposeManaged()
        {
            m_script.Dispose();
        }

        public bool AsyncLoad()
        {
            string[] bgs = ClientSkinService.CurrentlySelectedSkin.GetResourcesInDirectory("scripts/game/backgrounds");
            if (bgs.Length == 0)
                return true;

            string bg = bgs[MathL.RandomInt(0, bgs.Length)];

            var fileStream = ClientSkinService.CurrentlySelectedSkin.OpenFileStream($"scripts/game/backgrounds/{bg}");
            if (fileStream is null)
                return true;

            m_script.LoadFile(fileStream);
            if (!m_script.LuaAsyncLoad())
                return false;

            if (!m_resources.LoadAll())
                return false;

            return true;
        }

        public bool AsyncFinalize()
        {
            if (!m_script.LuaAsyncFinalize())
                return false;

            if (!m_resources.FinalizeLoad())
                return false;

            return true;
        }

        public void Init()
        {
            m_script.CallIfExists("initialize");
        }

        public void Update(float delta, float total)
        {
            m_script["horizonHeight"] = HorizonHeight;
            m_script["combinedTilt"] = CombinedTilt;
            m_script["effectRotation"] = EffectRotation;
            m_script["spinTimer"] = SpinTimer;
            m_script["swingTimer"] = SwingTimer;

            m_script.Update(delta, total);
        }

        public void Render()
        {
            using var batch = m_renderer2D.Use();
            m_batch = batch;

            m_script.Render();
        }
    }
}
