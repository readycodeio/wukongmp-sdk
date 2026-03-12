using System.Collections.Generic;

namespace WukongMp.Api.Monitors
{
    internal class ComponentMonitorManager
    {
        public static ComponentMonitorManager Instance { get; } = new ComponentMonitorManager();

        private ComponentMonitorManager() { }

        private readonly List<ComponentMonitor> _componentMonitors = [];

        public void AddComponentMonitor(object component, string componentName)
        {
            _componentMonitors.Add(new ComponentMonitor(component, componentName));
        }

        public void Update()
        {
            foreach (var componentMonitor in _componentMonitors)
            {
                componentMonitor.Update();
            }
        }
    }
}
