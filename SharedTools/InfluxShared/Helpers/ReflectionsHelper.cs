using System;
using System.Collections.Generic;
using System.Reflection;

namespace InfluxShared.Helpers
{
    public static class ReflectionsHelper
    {
        static readonly Dictionary<Type, Dictionary<string, MemberInfo>> dtsm = new();

        static void InitType(Type t, Type tDump)
        {
            if (tDump == null)
            {
                dtsm[t] = new Dictionary<string, MemberInfo>();
                tDump = t;
            }
            foreach (var mi in tDump.GetMembers(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public))
                dtsm[t][mi.Name] = mi;
            if (tDump.BaseType != null)
                InitType(t, tDump.BaseType);
        }

        static void CheckType(Type t)
        {
            if (t == null)
                return;
            if (!dtsm.ContainsKey(t))
                lock (dtsm)
                    if (!dtsm.ContainsKey(t))
                        InitType(t, null);
        }

        public static object GetAnyField(this object obj, string fieldName)
        {
            if (obj == null)
                return null;
            var t = obj.GetType();
            CheckType(t);
            return ((FieldInfo)dtsm[t][fieldName]).GetValue(obj);
        }

        public static void SetAnyField(this object obj, string fieldName, object value)
        {
            if (obj == null)
                return;
            var t = obj.GetType();
            CheckType(t);
            ((FieldInfo)dtsm[t][fieldName]).SetValue(obj, value);
        }

        public static object InvokeAny(this object obj, string methodName, params object[] paras)
        {
            if (obj == null)
                return null;
            var t = obj.GetType();
            CheckType(t);
            return ((MethodInfo)dtsm[t][methodName]).Invoke(obj, paras);
        }

        public static object GetAnyProperty(this object obj, string propertyName, params object[] index)
        {
            if (obj == null)
                return null;
            var t = obj.GetType();
            CheckType(t);
            return ((PropertyInfo)dtsm[t][propertyName]).GetValue(obj, index.Length == 0 ? null : index);
        }

        public static void SetAnyProperty(this object obj, string propertyName, object value, params object[] index)
        {
            if (obj == null)
                return;
            var t = obj.GetType();
            CheckType(t);
            ((PropertyInfo)dtsm[t][propertyName]).SetValue(obj, value, index.Length == 0 ? null : index);
        }

        public static void CopyProperties(this object source, object destination)
        {
            // If any this null throw an exception
            if (source == null || destination == null)
                throw new Exception("Source or/and Destination Objects are null");
            // Getting the Types of the objects
            Type typeDest = destination.GetType();
            Type typeSrc = source.GetType();

            // Iterate the Properties of the source instance and  
            // populate them from their desination counterparts  
            PropertyInfo[] srcProps = typeSrc.GetProperties();
            foreach (PropertyInfo srcProp in srcProps)
            {
                if (!srcProp.CanRead)
                    continue;

                PropertyInfo targetProperty = typeDest.GetProperty(srcProp.Name);
                if (targetProperty == null)
                    continue;
                if (!targetProperty.CanWrite)
                    continue;
                if (targetProperty.GetSetMethod(true) != null && targetProperty.GetSetMethod(true).IsPrivate)
                    continue;
                if ((targetProperty.GetSetMethod().Attributes & MethodAttributes.Static) != 0)
                    continue;
                if (!targetProperty.PropertyType.IsAssignableFrom(srcProp.PropertyType))
                    continue;

                // Passed all tests, lets set the value
                targetProperty.SetValue(destination, srcProp.GetValue(source, null), null);
            }
        }
    }
}
