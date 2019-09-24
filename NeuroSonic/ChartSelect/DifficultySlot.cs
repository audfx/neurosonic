using System.Numerics;

using theori.Graphics.OpenGL;
using theori.Gui;

namespace NeuroSonic.ChartSelect
{
    public class DifficultySlot : Panel
    {
        private Sprite m_sprite;

        public DifficultySlot()
        {
            Children = new GuiElement[]
            {
                new Sprite(null)
                {
                    RelativeSizeAxes = Axes.Both,
                    Size = Vector2.One,
                },
            };
        }
    }
}
