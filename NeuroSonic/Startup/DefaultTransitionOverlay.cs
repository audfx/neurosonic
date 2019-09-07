using System;
using System.Numerics;
using theori.Graphics;

namespace NeuroSonic.Startup
{
    internal sealed class DefaultTransitionOverlay : NscOverlay
    {
        const float MIN_TIME_ALIVE = 0.75f;

        private const float animDuration = 0.2f;

        public static DefaultTransitionOverlay Instance = new DefaultTransitionOverlay();

        private readonly BasicSpriteRenderer m_renderer;

        private int m_state = 0;
        private float m_timeStart;

        private Action? m_onTransitioned;

        private float m_animTimer = animDuration;
        private float m_animValue = 1.0f;

        private bool IsOpen => m_animValue == 1;

        private DefaultTransitionOverlay()
        {
            m_renderer = new BasicSpriteRenderer();
        }

        public bool TransitionClose(Action onClosed)
        {
            if (m_state != 0) return false;

            m_state = 1;
            m_animTimer = animDuration;

            m_onTransitioned = onClosed;

            return true;
        }

        public void TransitionOpen(Action? onOpened = null)
        {
            if (IsOpen) return;

            m_state = 2;
            m_animTimer = animDuration;

            m_onTransitioned = onOpened;
        }

        public override void Update(float delta, float total)
        {
            base.Update(delta, total);
            
            if (m_state == 0)
            {
            }

            else if (m_state == 1)
            {
                m_animTimer -= delta;
                m_animValue = MathL.Clamp01(m_animTimer / animDuration);

                if (m_animTimer <= 0)
                {
                    m_state = 3;

                    m_animTimer = 0;
                    m_animValue = 0;

                    m_timeStart = total;
                }
            }

            else if (m_state == 2)
            {
                if (total - m_timeStart < MIN_TIME_ALIVE) return;

                m_onTransitioned?.Invoke();
                m_onTransitioned = null;

                m_animTimer -= delta;
                m_animValue = 1 - MathL.Clamp01(m_animTimer / animDuration);

                if (m_animTimer <= 0)
                {
                    m_state = 0;

                    m_animTimer = 0;
                    m_animValue = 1;
                }
            }

            else if (m_state == 3)
            {
                m_state = 0;

                m_onTransitioned?.Invoke();
                m_onTransitioned = null;
            }
        }

        public override void Render()
        {
            base.Render();

            if (m_animValue == 1) return;

            m_renderer.BeginFrame();
            {
                int width = Window.Width, height = Window.Height;
                int originx = width / 2, originy = height / 2;

                float bgRotation = (1 - m_animValue) * 45;
                float bgDist = (width / 2) * m_animValue;
                float bgWidth = width;
                float bgHeight = height * 4;

                float l0hue = Plugin.Config.GetInt(NscConfigKey.Laser0Color) / 360.0f;
                float l1hue = Plugin.Config.GetInt(NscConfigKey.Laser1Color) / 360.0f;

                var l0color = Color.HSVtoRGB(new Vector3(l0hue, 1, 1)) * 255;
                var l1color = Color.HSVtoRGB(new Vector3(l1hue, 1, 1)) * 255;

                m_renderer.Rotate(bgRotation);
                m_renderer.Translate(originx, originy);

                m_renderer.SetColor((int)l0color.X, (int)l0color.Y, (int)l0color.Z, 255);
                m_renderer.FillRect(bgDist, -bgHeight / 2, bgWidth, bgHeight);
                m_renderer.SetColor((int)l1color.X, (int)l1color.Y, (int)l1color.Z, 255);
                m_renderer.FillRect(-bgDist - bgWidth, -bgHeight / 2, bgWidth, bgHeight);
            }
            m_renderer.EndFrame();
        }
    }
}
