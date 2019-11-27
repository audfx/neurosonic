using theori;

using NeuroSonic.ChartSelect;
using NeuroSonic.Properties;
using NeuroSonic.Platform;

namespace NeuroSonic.Startup
{
#if false
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
            Push(layer);
        }

        private void EnterBindingConfig()
        {
            Layer layer;
            layer = new ControllerConfigurationLayer();
            Push(layer);
        }

        private void EnterConfiguration()
        {
            Push(new UserConfigLayer());
        }

        private void EnterChartManagement()
        {
            Push(new ChartManagerLayer());
        }

        protected override void OnExit()
        {
            //ClientAs<NscClient>().CloseCurtain(0.25f, () => Host.Exit());
            Pop();
        }
    }
#endif
}
