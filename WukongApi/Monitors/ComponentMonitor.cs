using ILRuntime.Runtime;
using System.Collections.Generic;
using System.ComponentModel;
using UnrealEngine.Engine;
using UnrealEngine.Runtime;

namespace WukongApi.Monitors
{
    internal class ComponentMonitor
    {
        private readonly Dictionary<string, object> _properties = [];
        private readonly string _componentName;
        private readonly object _component;

        internal ComponentMonitor(object component, string componentName)
        {
            _componentName = componentName;
            _component = component;
            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(component))
            {
                if (descriptor == null)
                    continue;
                object value = descriptor.GetValue(component);
                if (value == null)
                    continue;

                string name = descriptor.Name;
                _properties[name] = value;
            }
        }

        internal void Update()
        {
            if (_component == null)
                return;

            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(_component))
            {
                if (descriptor == null || descriptor.PropertyType == null)
                    continue;
                object value = descriptor.GetValue(_component);
                if (value == null)
                    continue;
                string name = descriptor.Name;
                if (!_properties.TryGetValue(name, out var currentValue) || currentValue == null)
                    continue;

                if ((value.GetType() == typeof(FVector) && !((FVector)value).Equals((FVector)_properties[name], 50)) ||
                    (value.GetType() == typeof(FRotator) && !((FRotator)value).Equals((FRotator)_properties[name], 10)) ||
                    (value.GetType() == typeof(float) && !((float)value).Equals((float)_properties[name], 1)) ||
                    (!descriptor.PropertyType.IsAssignableFrom(typeof(FVector)) && !descriptor.PropertyType.IsAssignableFrom(typeof(FRotator)) && !descriptor.PropertyType.IsAssignableFrom(typeof(float)) &&!value.Equals(_properties[name])))
                {
                    Logging.LogDebug("[{Component}] Property {Name} changed from {OldValue} to {NewValue}", _componentName, name, _properties[name].ToString(), value.ToString());
                    _properties[name] = value;
                }
            }
        }
    }
}
