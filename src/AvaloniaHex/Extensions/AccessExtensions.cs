using System.Reflection;

namespace AvaloniaHex.Extensions
{
    /// <inheritdoc />
    public static class AccessExtensions
    {
        /// <inheritdoc />
        public static MethodInfo? GetMethod(this object o, string methodName, params object[] args)
        {
            return o.GetType().GetMethod(
                name: methodName,
                bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance);
        }

        /// <inheritdoc />
        public static MethodInfo? GetMethod(this object o, string methodName, Type[] types, params object[] args)
        {
            return o.GetType().GetMethod(
                name: methodName,
                bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance,
                binder: null,
                types: types,
                modifiers: null
                );
        }

        /// <inheritdoc />
        public static Delegate? GetDelegate<T>(this object o, string methodName, params object[] args)
        {
            var mi = GetMethod(o, methodName, args);

            if (mi != null)
                return Delegate.CreateDelegate(typeof(T), mi);

            return null;
        }

        /// <inheritdoc />
        public static object? Call(this object o, string methodName, params object[] args)
        {
            var mi = GetMethod(o, methodName, args);

            if (mi != null)
                return mi.Invoke(o, args);

            return null;
        }

        /// <inheritdoc />
        public static object? Call(this object o, string methodName, Type[] types, params object[] args)
        {
            var mi = GetMethod(o, methodName, types, args);

            if (mi != null)
                return mi.Invoke(o, args);

            return null;
        }

        /// <inheritdoc />
        public static T? GetPropertyValue<T>(this object o, string propertyName)
        {
            var field = o.GetType().GetField(propertyName, BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(o) is T value  ? value : default;
        }

        /// <inheritdoc />
        public static void SetPropertyValue<T>(this object o, string propertyName, T? value)
        {
            var field = o.GetType().GetField(propertyName, BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(o, value);
        }        
    }
}
