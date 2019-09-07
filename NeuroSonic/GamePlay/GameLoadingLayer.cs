using theori;
using theori.Audio;
using theori.Charting;
using theori.IO;
using theori.Resources;

using NeuroSonic.Startup;

namespace NeuroSonic.GamePlay
{
    public class GameLoadingLayer : NscLayer
    {
        enum State
        {
            Loading,
            Exiting,
        }

        private bool m_blockParent = false;
        public override bool BlocksParentLayer => m_blockParent;

        private readonly ClientResourceLocator m_locator;
        private readonly ClientResourceManager m_resources;

        private readonly AsyncLoader m_loader = new AsyncLoader();

        private State m_state = State.Loading;

        /// <summary>
        /// The game layer being loaded
        /// </summary>
        private GameLayer? m_game;

        public GameLoadingLayer(ClientResourceLocator locator)
        {
            m_locator = locator;
            m_resources = new ClientResourceManager(locator);
        }

        public override void Destroy()
        {
            base.Destroy();

            m_resources.Dispose();
        }

        public void Begin(ChartInfo chartInfo, AutoPlay autoPlay)
        {
            m_game = new GameLayer(m_locator, chartInfo, autoPlay);

            m_loader.Add(m_game);
            m_loader.LoadAll();
        }

        public void Begin(Chart chart, AudioTrack audio, AutoPlay autoPlay)
        {
            m_game = new GameLayer(m_locator, chart, audio, autoPlay);

            m_loader.Add(m_game);
            m_loader.LoadAll();
        }

        #region Always Block Inputs (for now)

        public override bool KeyPressed(KeyInfo info) => true;
        public override bool KeyReleased(KeyInfo info) => true;

        public override bool ButtonPressed(ButtonInfo info) => true;
        public override bool ButtonReleased(ButtonInfo info) => true;

        public override bool ControllerButtonPressed(ControllerInput input) => true;
        public override bool ControllerButtonReleased(ControllerInput input) => true;

        #endregion

        public override void Update(float delta, float total)
        {
            base.Update(delta, total);

            switch (m_state)
            {
                case State.Loading:
                {
                    m_loader.Update();
                    if (m_loader.Failed)
                    {
                        m_game = null;
                        m_state = State.Exiting;
                    }
                    else if (m_loader.IsCompleted)
                    {
                        if (m_loader.IsFinalizeSuccessful)
                        {
                            m_blockParent = false; // don't suspend the game as soon as it's added
                            Host.AddLayerBelow(this, m_game!);
                        }

                        m_state = State.Exiting;
                    }
                } break;

                case State.Exiting:
                {
                    Host.RemoveLayer(this); // TODO(local): this can trigger well before the transition timer runs out if it's fast enough
                    DefaultTransitionOverlay.Instance.TransitionOpen();
                } break;
            }
        }
    }
}
