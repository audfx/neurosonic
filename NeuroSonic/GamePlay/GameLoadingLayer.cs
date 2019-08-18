using theori;
using theori.Audio;
using theori.Charting;
using theori.IO;
using theori.Resources;
using theori.Scripting;

namespace NeuroSonic.GamePlay
{
    public class GameLoadingLayer : NscLayer
    {
        const float MIN_TIME_ALIVE = 2.0f;

        enum State
        {
            Entering,
            Loading,
            Waiting,
            Exiting,
        }

        private bool m_blockParent = false;
        public override bool BlocksParentLayer => m_blockParent;

        private float m_timeStart;
        private State m_state = State.Entering;

        private ClientResourceLocator m_locator;
        private ClientResourceManager m_resources;

        private AsyncLoader m_loader;

        /// <summary>
        /// The game layer being loaded
        /// </summary>
        private GameLayer m_game;

        private LuaScript m_script;

        public GameLoadingLayer(ClientResourceLocator locator, ChartInfo chartInfo, AutoPlay autoPlay)
        {
            m_state = State.Entering;

            m_locator = locator;
            m_resources = new ClientResourceManager(locator);

            m_game = new GameLayer(m_locator, chartInfo, autoPlay);
        }

        public GameLoadingLayer(ClientResourceLocator locator, Chart chart, AudioTrack audio, AutoPlay autoPlay)
        {
            m_state = State.Entering;

            m_locator = locator;
            m_resources = new ClientResourceManager(locator);

            m_game = new GameLayer(m_locator, chart, audio, autoPlay);
        }

        public override void Init()
        {
            base.Init();

            m_script = new LuaScript();
            m_script.LoadFile(m_locator.OpenFileStream("scripts/game/loader.lua"));
            m_script.InitResourceLoading(m_locator);
            m_script.InitSpriteRenderer(m_locator);

            m_loader = new AsyncLoader();
            m_loader.Add(m_game);

            m_script.CallIfExists("Init");
        }

        public override void Destroy()
        {
            base.Destroy();

            m_script.Dispose();
            m_script = null;

            m_resources.Dispose();
            m_resources = null;
        }

        #region Always Block Inputs (for now)

        public override bool KeyPressed(KeyInfo info) => true;
        public override bool KeyReleased(KeyInfo info) => true;

        public override bool ButtonPressed(ButtonInfo info) => true;
        public override bool ButtonReleased(ButtonInfo info) => true;

        protected internal override bool ControllerButtonPressed(ControllerInput input) => true;
        protected internal override bool ControllerButtonReleased(ControllerInput input) => true;

        #endregion

        public override void Update(float delta, float total)
        {
            base.Update(delta, total);

            switch (m_state)
            {
                case State.Entering:
                {
                    // CheckIntro returns False when the intro is finished
                    var result = m_script.CallIfExists("CheckIntro");
                    if (result == null || !result.CastToBool())
                    {
                        m_timeStart = Time.Total;

                        m_state = State.Loading;
                        m_blockParent = true;

                        m_loader.LoadAll();
                    }
                } break;

                case State.Loading:
                {
                    m_loader.Update();
                    if (m_loader.Failed)
                    {
                        m_game = null;

                        m_script.Call("TriggerOutro");
                        m_state = State.Exiting;
                    }
                    else if (m_loader.IsCompleted)
                    {
                        if (m_loader.IsFinalizeSuccessful)
                        {
                            m_blockParent = false; // don't suspend the game as soon as it's added
                            Host.AddLayerBelow(this, m_game);
                        }

                        if (Time.Total - m_timeStart < MIN_TIME_ALIVE)
                            m_state = State.Waiting;
                        else
                        {
                            m_script.Call("TriggerOutro");
                            m_state = State.Exiting;
                        }
                    }
                } break;

                case State.Waiting:
                {
                    if (Time.Total - m_timeStart >= MIN_TIME_ALIVE)
                    {
                        m_script.Call("TriggerOutro");
                        m_state = State.Exiting;
                    }
                } break;

                case State.Exiting:
                {
                    // CheckOutro returns False when the intro is finished
                    var result = m_script.CallIfExists("CheckOutro");
                    if (result == null || !result.CastToBool())
                    {
                        Host.RemoveLayer(this);
                        //m_game?.Begin();

                        return;
                    }
                } break;
            }

            m_script.Update(delta, total);
        }

        public override void Render()
        {
            m_script.Draw();
        }
    }
}
