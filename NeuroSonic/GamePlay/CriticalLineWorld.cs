using System.Numerics;

using theori.Gui;
using theori.Graphics;
using theori.Resources;
using theori;
using theori.Graphics.OpenGL;

namespace NeuroSonic.GamePlay
{
    public sealed class CriticalLineWorld : Panel, IAsyncLoadable
    {
        private const float buttonAnimTimerDuration = 0.25f;

        private bool m_isDirty = true;

        private readonly ClientResourceManager m_resources;

        private Sprite? m_cursorLeft, m_cursorRight;
        private float m_leftPos, m_rightPos;

        public float LeftCursorPosition { get => m_leftPos; set { m_leftPos = value; m_isDirty = true; } }
        public float RightCursorPosition { get => m_rightPos; set { m_rightPos = value; m_isDirty = true; } }

        public float LeftCursorAlpha { get; set; }
        public float RightCursorAlpha { get; set; }

        public float WorldUnitSize { get; set; }

        private readonly Sprite?[] m_buttonAnims = new Sprite?[6];
        private readonly float[] m_buttonAnimTimers = new float[6];
        
        public CriticalLineWorld(ClientResourceManager resources)
        {
            m_resources = resources;
        }

        public bool AsyncLoad()
        {
            m_resources.QueueTextureLoad("textures/game/cursor");

            return true;
        }

        public bool AsyncFinalize()
        {
            var lVolColor = Color.HSVtoRGB(new Vector3(NscConfig.Laser0Color / 360.0f, 1, 1));
            var rVolColor = Color.HSVtoRGB(new Vector3(NscConfig.Laser1Color / 360.0f, 1, 1));

            RelativeSizeAxes = Axes.X;
            Size = new Vector2(0.75f, 0);

            GuiElement[] children = new GuiElement[8];

            for (int i = 0; i < 6; i++)
            {
                var anim = new Sprite(Texture.Empty)
                {
                    Color = new Vector4(0.5f, 0.8f, 0.25f, 0.6f),
                    Size = new Vector2(100),
                    Scale = Vector2.Zero,
                };
                anim.Origin = anim.Size / 2;

                m_buttonAnims[i] = anim;
                children[i] = anim;
            }

            var cursorTexture = m_resources.GetTexture("textures/game/cursor");
            children[6] = m_cursorLeft = new Sprite(cursorTexture) { Color = new Vector4(lVolColor, 1) };
            children[7] = m_cursorRight = new Sprite(cursorTexture) { Color = new Vector4(rVolColor, 1) };

            Children = children;

            m_cursorLeft.Origin = m_cursorLeft.Size / 2;
            m_cursorRight.Origin = m_cursorRight.Size / 2;

            return true;
        }

        public void TriggerButtonAnimation(int lane, Vector3 color)
        {
            m_buttonAnimTimers[lane] = buttonAnimTimerDuration;
            m_buttonAnims[lane]!.Color = new Vector4(color, 0.6f);
        }

        public override void Update()
        {
            if (m_isDirty)
            {
                UpdateOrientation();
                m_isDirty = false;
            }

            for (int i = 0; i < 6; i++)
            {
                ref float timer = ref m_buttonAnimTimers[i];
                if (timer <= 0)
                {
                    timer = 0;
                    m_buttonAnims[i]!.Scale = Vector2.Zero;
                }
                else
                {
                    timer -= Time.Delta;
                    m_buttonAnims[i]!.Scale = Vector2.One * ((1 - timer / buttonAnimTimerDuration) * 0.5f + 0.5f);
                    m_buttonAnims[i]!.Rotation = -30 + 90 * (1 - timer / buttonAnimTimerDuration);
                }
            }
        }

        private void UpdateOrientation()
        {
            static Vector4 ColorWithAlpha(Vector4 color, float alpha)
            {
                color.W = alpha;
                return color;
            }

            for (int i = 0; i < 6; i++)
            {
                var anim = m_buttonAnims[i]!;
                if (i < 4)
                    anim.Position = new Vector2(((-3 + 2 * i) / 12.0f) * WorldUnitSize, 0);
                else anim.Position = new Vector2(((-1 + 2 * (i - 4)) / 6.0f) * WorldUnitSize, 0);
            }

            m_cursorLeft!.Position = new Vector2(LeftCursorPosition * WorldUnitSize * 0.5f, 0);
            m_cursorLeft.Color = ColorWithAlpha(m_cursorLeft.Color, LeftCursorAlpha);

            m_cursorRight!.Position = new Vector2(RightCursorPosition * WorldUnitSize * 0.5f, 0);
            m_cursorRight.Color = ColorWithAlpha(m_cursorRight.Color, RightCursorAlpha);
        }
    }
}
