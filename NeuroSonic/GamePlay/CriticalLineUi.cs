using System.Numerics;

using theori.Gui;
using theori.Graphics;
using theori.Resources;

namespace NeuroSonic.GamePlay
{
    public sealed class CriticalLineUi : Panel, IAsyncLoadable
    {
        private bool m_isDirty = true;

        private readonly ClientResourceManager m_resources;

        private Panel? m_container;
        private Sprite? m_image, m_capLeft, m_capRight;

        public CriticalLineUi(ClientResourceManager resources)
        {
            m_resources = resources;
        }

        public bool AsyncLoad()
        {
            m_resources.QueueTextureLoad("textures/game/scorebar");
            m_resources.QueueTextureLoad("textures/game/critical_cap");

            return true;
        }

        public bool AsyncFinalize()
        {
            var critTexture = m_resources.GetTexture("textures/game/scorebar");
            var capTexture = m_resources.GetTexture("textures/game/critical_cap");

            RelativeSizeAxes = Axes.X;
            Size = new Vector2(0.75f, 0);

            Children = new GuiElement[]
            {
                m_container = new Panel()
                {
                    RelativeSizeAxes = Axes.X,
                    Size = new Vector2(1, 0),

                    Children = new GuiElement[]
                    {
                        m_image = new Sprite(critTexture),
                        m_capLeft = new Sprite(capTexture)
                        {
                            Position = new Vector2(20, 0),
                        },
                        m_capRight = new Sprite(capTexture)
                        {
                            Scale = new Vector2(-1, 1),
                        },
                    }
                },
            };

            float critImageWidth = m_image.Size.X;

            m_container.Size = new Vector2(critImageWidth, 0);
            m_container.Origin = m_container.Size / 2;

            m_image.Origin = new Vector2(0, m_image.Size.Y / 2);

            m_capLeft.Origin = new Vector2(m_capLeft.Size.X, m_capLeft.Size.Y / 2);

            m_capRight.Position = new Vector2(critImageWidth - 20, 0);
            m_capRight.Origin = new Vector2(m_capRight.Size.X, m_capRight.Size.Y / 2);

            return true;
        }

        public override void Update()
        {
            if (m_isDirty)
            {
                UpdateOrientation();
                m_isDirty = false;
            }
        }

        private void UpdateOrientation()
        {
            float desiredCritWidth = Window.Width * 0.75f;
            m_container!.Scale = new Vector2(desiredCritWidth / m_image!.Size.X);
        }
    }
}
