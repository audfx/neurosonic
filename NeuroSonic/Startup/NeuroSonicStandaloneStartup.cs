using theori;

using NeuroSonic.ChartSelect;
using NeuroSonic.Properties;

namespace NeuroSonic.Startup
{
    public class NeuroSonicStandaloneStartup : BaseMenuLayer
    {
        protected override string Title => Strings.SecretMenu_MainTitle;

        protected override void GenerateMenuItems()
        {
            AddMenuItem(new MenuItem(NextOffset, Strings.SecretMenu_InputMethodTitle, EnterInputMethod));
            AddMenuItem(new MenuItem(NextOffset, Strings.SecretMenu_InputBindingConfigTitle, EnterBindingConfig));
            AddMenuItem(new MenuItem(NextOffset, "Configuration", EnterConfiguration));
            AddMenuItem(new MenuItem(NextOffset, Strings.SecretMenu_ChartManagementTitle, EnterChartManagement));
        }

        private void EnterInputMethod()
        {
            var layer = new InputMethodConfigLayer();
            Host.PushLayer(layer);
        }

        private void EnterBindingConfig()
        {
            Layer layer;
            layer = new ControllerConfigurationLayer();
            Host.PushLayer(layer);
        }

        private void EnterConfiguration()
        {
            Host.PushLayer(new UserConfigLayer());
        }

        private void EnterChartManagement()
        {
            Host.PushLayer(new ChartManagerLayer());
        }
    }
}
