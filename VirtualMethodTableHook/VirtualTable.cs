using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using System.Collections.Generic;
using System;
using System.Linq;

namespace VirtualMethodTableHook;

public class VirtualTable
{
    private readonly Dictionary<Type, Dictionary<MethodInfo, VirtualMethod>> originMap = new();
    private readonly Dictionary<MethodInfo, MethodInfo> _map = new();

    private unsafe struct VirtualMethod
    {
        public nint* Address { get; set; }
        public nint Value { get; set; }
    }

    public unsafe MethodInfo this[MethodInfo methodInfo]
    {
        get
        {
            if (_map.TryGetValue(methodInfo, out var value))
            {
                return value;
            }
            else
            {
                return methodInfo;
            }
        }
        set
        {
            var type = methodInfo.DeclaringType ?? throw new Exception("The type of the method cannot be found");
            var valueType = value.DeclaringType ?? throw new Exception("The type of the method cannot be found");

            if (originMap.TryGetValue(type, out var map) is false)
            {
                var origin = GetVirtualMethodMap(type);
                map = new(origin.Count);
                foreach (var item in origin)
                {
                    map[item.Key] = new VirtualMethod() { Address = (nint*)item.Value, Value = *(nint*)item.Value };
                }

                originMap[type] = map;
            }
            if (originMap.TryGetValue(valueType, out var valueMap) is false)
            {
                var origin = GetVirtualMethodMap(valueType);
                valueMap = new(origin.Count);
                foreach (var item in origin)
                {
                    valueMap[item.Key] = new VirtualMethod() { Address = (nint*)item.Value, Value = *(nint*)item.Value };
                }

                originMap[valueType] = valueMap;
            }

            *map[methodInfo].Address = valueMap[value].Value;

            _map[methodInfo] = value;
        }
    }

    public void Reset()
    {
        foreach (var item in _map.ToArray())
        {
            this[item.Key] = item.Key;
        }
        _map.Clear();
    }

#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("Functionality can be broken when pruning application code.")]
#endif
    public static unsafe nint** GetVirtualTables(Type type)
    {
        return *(nint***)(type.TypeHandle.Value + (6 * IntPtr.Size + 16));
    }

#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("Functionality can be broken when pruning application code.")]
#endif
    public static unsafe int GetVirtualsCount(Type type)
    {
        return *(ushort*)(type.TypeHandle.Value + 12);
    }

#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("Functionality can be broken when pruning application code.")]
#endif
    public static MethodInfo[] GetVirtualMethos(Type type) => type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        .Where(v => v.Attributes.HasFlag(MethodAttributes.Virtual))
        .ToArray();

#if NET5_0_OR_GREATER
    [RequiresUnreferencedCode("Functionality can be broken when pruning application code.")]
#endif
    public static unsafe Dictionary<MethodInfo, nint> GetVirtualMethodMap(Type type)
    {
        var mt = type.TypeHandle.Value;
        int vcount = *(ushort*)(mt + 12);

        var vmap = *(nint***)(mt + (6 * IntPtr.Size + 16));
        var vmethods = GetVirtualMethos(type);

        if (vmethods.Length != vcount) throw new Exception();

        Dictionary<MethodInfo, nint> map = new();

        foreach (var item in vmethods)
        {
            var m_handle = (nint)item.GetType().GetField("m_handle", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(item)!;
            var offset = *(ushort*)(m_handle + 4);
            map.Add(item, (nint)(void*)&vmap[offset]);
        }

        return map;
    }

}