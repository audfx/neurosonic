using System.Numerics;

using theori;
using theori.Graphics.OpenGL;
using theori.Gui;
using theori.Resources;

namespace NeuroSonic.GamePlay
{
    public class ComboDisplay : Panel, IAsyncLoadable
    {
        private readonly ClientResourceManager m_resources;

        private int m_combo;
        private float m_lastDisplayTime;

        private readonly Texture[] m_digits = new Texture[10];

        private Vector4 m_color = Vector4.One;

        public float DigitSize = 100.0f;

        public int Combo
        {
            get => m_combo;
            set
            {
                if (value < m_combo)
                    m_color = Vector4.One;

                if (value != 0)
                    m_lastDisplayTime = Time.Total;
                m_combo = value;
            }
        }

        public ComboDisplay(ClientResourceManager resources)
        {
            m_resources = resources;

            m_color = new Vector4(1, 1, 0.5f, 1);
        }

        public bool AsyncLoad()
        {
            for (int i = 0; i < 10; i++)
                m_digits[i] = m_resources.QueueTextureLoad($"textures/combo/{ i }");

            return true;
        }

        public bool AsyncFinalize()
        {
            for (int i = 0; i < 10; i++)
                m_digits[i] = m_resources.GetTexture($"textures/combo/{ i }");

            return true;
        }

        public override void Render(GuiRenderQueue rq)
        {
            if (m_combo > 0 && Time.Total - m_lastDisplayTime <= 0.75)
            {
                int[] digits = new int[4];
                for (int i = 3, combo = m_combo; i >= 0; i--)
                {
                    digits[i] = combo % 10;
                    combo /= 10;
                }

                float alpha = 0.2f;
                for (int i = 0; i < 4; i++)
                {
                    if (digits[i] != 0) alpha = 1.0f;
                    rq.DrawRect(CompleteTransform, new Rect((i - 2) * DigitSize, -DigitSize / 2, DigitSize, DigitSize), m_digits[digits[i]], m_color * new Vector4(1, 1, 1, alpha));
                }
            }
        }
    }
}
