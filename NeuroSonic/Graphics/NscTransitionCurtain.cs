using System;
using System.Numerics;
using theori;
using theori.Graphics;

namespace NeuroSonic.Graphics
{
    public sealed class NscTransitionCurtain : TransitionCurtain
    {
        protected override void Render(float animTimer, float idleTimer)
        {
            renderer.BeginFrame();
            {
                float width = Window.Width, height = Window.Height, size = MathL.Min(width, height) * 0.3f;
                float originx = width / 2 + MathL.Sin(idleTimer * 0.5f) * size * 0.05f, originy = height / 2 + MathL.Cos(idleTimer * 0.7f) * size * 0.05f;

                float bgRotation = animTimer * 45;
                float bgDist = (width / 2) * (1 - animTimer);
                float bgWidth = width;
                float bgHeight = height * 4;

                renderer.Rotate(bgRotation);
                renderer.Translate(originx, originy);

                int l0hue = Plugin.Config.GetInt(NscConfigKey.Laser0Color);
                int l1hue = Plugin.Config.GetInt(NscConfigKey.Laser1Color);

                var l0color = Color.HSVtoRGB(new Vector3(l0hue / 360.0f, 0.75f, 1));
                var l1color = Color.HSVtoRGB(new Vector3(l1hue / 360.0f, 0.75f, 1));

                renderer.SetColor((int)(l1color.X * 255), (int)(l1color.Y * 255), (int)(l1color.Z * 255));
                renderer.FillRect(bgDist, -bgHeight / 2, bgWidth, bgHeight);
                renderer.SetColor((int)(l0color.X * 255), (int)(l0color.Y * 255), (int)(l0color.Z * 255));
                renderer.FillRect(-bgDist - bgWidth, -bgHeight / 2, bgWidth, bgHeight);

                renderer.ResetTransform();
                renderer.Rotate(360 * (1 - animTimer));
                renderer.Scale(1 + 9 * (1 - animTimer) + MathL.Abs(MathL.Sin(idleTimer)) * 0.1f);
                renderer.Translate(originx, originy);

                renderer.SetImageColor(255, 255, 255, 255 * animTimer);
                renderer.Image(Host.StaticResources.GetTexture("textures/theori-logo-large"), -size * 0.5f, -size * 0.5f, size, size);
            }
            renderer.EndFrame();
        }
    }
}
