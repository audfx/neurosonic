using System;

using theori.Resources;
using theori.Scripting;

namespace NeuroSonic.GamePlay
{
    public class ScriptableBackground : Disposable
    {
        private ClientResourceLocator m_locator;

        public float HorizonHeight { get; set; }
        public float CombinedTilt { get; set; }
        public float EffectRotation { get; set; }
        public float SpinTimer { get; set; }
        public float SwingTimer { get; set; }

        private LuaScript m_script;

        public ScriptableBackground(ClientResourceLocator locator)
        {
            m_locator = locator;
        }

        protected override void DisposeManaged()
        {
            m_script?.Dispose();
            m_script = null;
        }

        public bool AsyncLoad()
        {
            m_script = new LuaScript();

            m_script.LoadFile(Plugin.DefaultResourceLocator.OpenFileStream("scripts/game/bg-stars.lua"));
            m_script.InitResourceLoading(m_locator);

            if (!m_script.LuaAsyncLoad())
                return false;

            return true;
        }

        public bool AsyncFinalize()
        {
            if (!m_script.LuaAsyncFinalize())
                return false;

            m_script.InitSpriteRenderer(m_locator);

            return true;
        }

        public void Init()
        {
            m_script.CallIfExists("Init");
        }

        public void Update(float delta, float total)
        {
            m_script["HorizonHeight"] = HorizonHeight;
            m_script["CombinedTilt"] = CombinedTilt;
            m_script["EffectRotation"] = EffectRotation;
            m_script["SpinTimer"] = SpinTimer;
            m_script["SwingTimer"] = SwingTimer;

            m_script.Update(delta, total);
        }

        public void Render()
        {
            m_script.Draw();
        }
    }
}
