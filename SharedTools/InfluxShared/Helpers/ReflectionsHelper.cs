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
    }
}
