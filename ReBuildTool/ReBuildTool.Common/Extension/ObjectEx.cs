using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Common 
{ 
    public static class ObjectEx
    {
        /// <summary>
        /// 用于避免null
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <param name="def"></param>
        /// <returns></returns>
        public static T IfNull<T>(this object obj, T def)
        {
            if (obj == null)
            {
                return def;
            }
            return (T)obj;
        }
        
        public static object? GetDefaultValue(this Type type)
        {
            if (type.IsValueType)
                return RuntimeHelpers.GetUninitializedObject(type);

            return null;
        }

    }
}
