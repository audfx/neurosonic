using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NeuroSonic.IO;
using theori;
using theori.Graphics;
using theori.IO;

namespace NeuroSonic.Startup
{
    public sealed class FileSystemBrowser : NscLayer
    {
        private readonly Action<string[]> m_onSelected;

        private string m_directoryPath;
        private readonly List<string> m_directoryChildren = new List<string>();

        private int m_selected = 0;

        private readonly BasicSpriteRenderer m_renderer;

        public FileSystemBrowser(Action<string[]> onSelected)
        {
            string storedLastPath = Plugin.Config.GetString(NscConfigKey.FileBrowserLastDirectory);

            m_directoryPath = string.IsNullOrWhiteSpace(storedLastPath) || !Directory.Exists(storedLastPath) ? Directory.GetCurrentDirectory() : storedLastPath;
            m_onSelected = onSelected;

            m_renderer = new BasicSpriteRenderer();

            PopulateDirectoryChildren();
        }

        private void PopulateDirectoryChildren()
        {
            Plugin.Config.Set(NscConfigKey.FileBrowserLastDirectory, m_directoryPath);
            Plugin.SaveNscConfig();

            m_directoryChildren.Clear();

            if (m_directoryPath == "")
                m_directoryChildren.AddRange(DriveInfo.GetDrives().Select(info => info.Name));
            else
            {
                m_directoryChildren.AddRange(Directory.EnumerateDirectories(m_directoryPath).Select(dir => $"{ Path.GetFileName(dir) }{ Path.DirectorySeparatorChar }"));
                m_directoryChildren.AddRange(Directory.EnumerateFiles(m_directoryPath).Where(file => file.EndsWith(".ksh")).Select(file => Path.GetFileName(file)));
            }

            m_selected = 0;
        }
        
        private void NavigateToParent()
        {
            if (m_directoryPath == "") return;

            string currentDirName = Path.GetFileName(m_directoryPath);

            if (m_directoryPath == Directory.GetDirectoryRoot(m_directoryPath))
                m_directoryPath = "";
            else m_directoryPath = Directory.GetParent(m_directoryPath).FullName;

            PopulateDirectoryChildren();
            m_selected = m_directoryChildren.IndexOf($"{ currentDirName }{ Path.DirectorySeparatorChar }");
        }

        private void SelectEntry()
        {
            if (m_directoryPath == "")
                m_directoryPath = m_directoryChildren[m_selected];
            else
            {
                string selected = m_directoryChildren[m_selected];
                if (selected.EndsWith(Path.DirectorySeparatorChar))
                    m_directoryPath = Path.Combine(m_directoryPath, selected);
                else
                {
                    m_onSelected?.Invoke(new[] { Path.Combine(m_directoryPath, selected) });
                    Host.PopToParent(this);
                    return;
                }
            }

            PopulateDirectoryChildren();
        }

        private void ScrollBy(int amount)
        {
            m_selected = (m_selected + amount + m_directoryChildren.Count) % m_directoryChildren.Count;
        }

        #region Input (which blocks to lower layers ofc)

        public override bool KeyPressed(KeyInfo info)
        {
            bool speedy = Keyboard.IsDown(KeyCode.LCTRL) || Keyboard.IsDown(KeyCode.RCTRL);

            switch (info.KeyCode)
            {
                case KeyCode.ESCAPE: Host.PopToParent(this); break;

                case KeyCode.RETURN: SelectEntry(); break;

                case KeyCode.LEFT: NavigateToParent(); break;

                case KeyCode.UP: ScrollBy(-(speedy ? 10 : 1)); break;
                case KeyCode.DOWN: ScrollBy((speedy ? 10 : 1)); break;
            }

            return true;
        }

        public override bool KeyReleased(KeyInfo info)
        {
            return true;
        }

        public override bool ControllerButtonPressed(ControllerInput input)
        {
            bool speedy = Input.IsButtonDown(ControllerInput.FX0);

            switch (input)
            {
                case ControllerInput.Back: Host.PopToParent(this); break;

                case ControllerInput.Start: SelectEntry(); break;

                case ControllerInput.BT2: NavigateToParent(); break;

                case ControllerInput.BT0: ScrollBy(-(speedy ? 10 : 1)); break;
                case ControllerInput.BT1: ScrollBy((speedy ? 10 : 1)); break;
            }

            return true;
        }

        public override bool ControllerButtonReleased(ControllerInput input)
        {
            return true;
        }

        public override bool ControllerAxisChanged(ControllerInput input, float delta)
        {
            return true;
        }

        #endregion

        public override void Update(float delta, float total)
        {
        }

        public override void Render()
        {
            int entrySpacing = 16;
            int numVisible = (Window.Height - 60) / entrySpacing;

            m_renderer.BeginFrame();
            {
                m_renderer.Write(m_directoryPath == "" ? "<Drive Selection>" : m_directoryPath, 10, 10);

                if (m_directoryChildren.Count > 0)
                {
                    int count = MathL.Min(numVisible, m_directoryChildren.Count);
                    int startIndex = MathL.Clamp(m_selected - numVisible / 2, 0, m_directoryChildren.Count - numVisible);

                    for (int i = 0; i < count; i++)
                    {
                        bool selected = i + startIndex == m_selected;
                        int x = selected ? 45 : 30;
                        if (selected) m_renderer.SetColor(255, 255, 128);
                        else m_renderer.SetColor(255, 255, 255);
                        m_renderer.Write(m_directoryChildren[i + startIndex], x, 30 + i * entrySpacing);
                    }
                }
            }
            m_renderer.EndFrame();
        }
    }
}
