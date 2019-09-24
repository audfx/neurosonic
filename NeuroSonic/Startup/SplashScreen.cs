using theori;
using theori.Graphics;
using theori.IO;

using NeuroSonic.Platform;

namespace NeuroSonic.Startup
{
    sealed class SplashScreen : Layer
    {
        const float WAIT_TIME = 0.25f;
        const float TRANSITION_TIME = 0.5f;
        const float HOLD_TIME = 2.0f;
        const float TOTAL_TIME = WAIT_TIME + TRANSITION_TIME + HOLD_TIME;

        private BasicSpriteRenderer? m_renderer;

        private float m_timer = 0.0f, m_alpha = 0.0f;

        public SplashScreen()
        {
        }

        public override void Initialize()
        {
            m_renderer = new BasicSpriteRenderer();
        }

        public override bool KeyPressed(KeyInfo info)
        {
            if (info.KeyCode == KeyCode.ESCAPE)
                Host.Exit();
            else m_timer = TOTAL_TIME;

            return true;
        }

        public override void Update(float delta, float total)
        {
            m_timer += delta;
            if (m_timer > TOTAL_TIME)
            {
                ClientAs<NscClient>().CloseCurtain(() => Push(new NeuroSonicStandaloneStartup()));
                SetInvalidForResume();
            }
            else
            {
                float timer = m_timer;
                if (timer >= WAIT_TIME)
                {
                    timer -= WAIT_TIME;
                    if (timer >= TRANSITION_TIME)
                    {
                        timer -= TRANSITION_TIME;
                        if (timer >= HOLD_TIME)
                        {
                            // should have been triggered above
                        }
                        else m_alpha = 1;
                    }
                    else m_alpha = timer / TRANSITION_TIME;
                }
                else m_alpha = 0;
            }
        }

        public override void Render()
        {
            var r = m_renderer!;
            var text = theori.Host.StaticResources.GetTexture("textures/audfx-text-large");

            float width = Window.Width * 0.7f;
            float height = width * text.Height / text.Width;

            r.BeginFrame();
            r.SetImageColor(255, 255, 255, 255 * m_alpha);
            r.Image(text, (Window.Width - width) / 2, (Window.Height - height) / 2, width, height);
            r.EndFrame();
        }
    }
}
