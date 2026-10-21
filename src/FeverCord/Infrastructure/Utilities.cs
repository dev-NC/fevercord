using System.Reflection;

namespace FeverCordSvc.Infrastructure
{
    public static class Utilities
    {
        public static int CalculateCharacterLength(object instance)
        {
            var length = 0;
            var properties = instance.GetType().GetProperties(
                BindingFlags.Public |
                BindingFlags.Instance);
            foreach(var property in properties)
            {
                var value = property.GetValue(instance);
                if (value == null)
                {
                    continue;
                }

                if (property.PropertyType.IsClass &&
                    property.PropertyType != typeof(string))
                {
                    length += CalculateCharacterLength(value);
                }
                else
                {
                    length += value.ToString()?.Length ?? 0;
                }
            }

            return length;
        }
    }
}
