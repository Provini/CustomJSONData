﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using JetBrains.Annotations;
using Newtonsoft.Json;
using UnityEngine;

namespace CustomJSONData.CustomBeatmap
{
    [JsonConverter(typeof(CustomDataConverter))]
    public class CustomData : ConcurrentDictionary<string, object?>
    {
        public CustomData()
        {
        }

        public CustomData(IEnumerable<KeyValuePair<string, object?>> collection)
            : base(collection)
        {
        }

        [PublicAPI]
        public static CustomData FromJSON(string jsonString)
        {
            using JsonTextReader reader = new(new StringReader(jsonString));
            return FromJSON(reader);
        }

        public static CustomData FromJSON(JsonReader reader, Func<string, bool>? specialCase = null)
        {
            CustomData dictioanry = new();
            JsonExtensions.ObjectReadObject(reader, dictioanry, specialCase);
            return dictioanry;
        }

        // TODO: remove all dictionary copying and make it immutable
        public CustomData Copy()
        {
            return new CustomData(this);
        }

        [PublicAPI]
        public T GetRequired<T>(string key)
        {
            return TryGet(key, out T? result) ? result : throw new JsonNotDefinedException(key);
        }

        public T? Get<T>(string key)
        {
            return TryGet(key, out T? result) ? result : default;
        }

        [PublicAPI]
        public Vector3? GetVector3(string key)
        {
            List<float>? data = Get<List<object>>(key)?.Select(Convert.ToSingle).ToList();
            Vector3? final = null;
            if (data != null)
            {
                final = new Vector3(data[0], data[1], data[2]);
            }

            return final;
        }

        [PublicAPI]
        public Quaternion? GetQuaternion(string key)
        {
            Vector3? final = GetVector3(key);
            if (final.HasValue)
            {
                return Quaternion.Euler(final.Value);
            }

            return null;
        }

        [PublicAPI]
        public Color? GetColor(string key)
        {
            List<float>? color = Get<List<object>>(key)?.Select(Convert.ToSingle).ToList();
            if (color == null)
            {
                return null;
            }

            return new Color(color[0], color[1], color[2], color.Count > 3 ? color[3] : 1);
        }

        [PublicAPI]
        public T GetStringToEnumRequired<T>(string key)
            where T : Enum
        {
            return TryGetStringToEnum(key, out T? result) ? result : throw new JsonNotDefinedException(key);
        }

        [PublicAPI]
        public T? GetStringToEnum<T>(string key)
        {
            return TryGetStringToEnum(key, out T? result) ? result : default;
        }

        public override string ToString()
        {
            return FormatDictionary(this);
        }

        private static string FormatDictionary(CustomData dictionary, int indent = 0)
        {
            string prefix = new(' ', indent * 2);
            StringBuilder builder = new();
            builder.AppendLine("{");
            builder.AppendLine(string.Join(
                ",\n",
                dictionary.Select(n => $"{prefix}  \"{n.Key}\": {FormatObject(n.Value, indent + 1)}")));
            builder.AppendLine();
            builder.Append(prefix + "}");
            return builder.ToString();
        }

        private static string FormatList(IEnumerable<object?> list, int indent = 0)
        {
            return "[" + string.Join(", ", list.Select(n => FormatObject(n, indent))) + "]";
        }

        private static string FormatObject(object? obj, int indent = 0)
        {
            return obj switch
            {
                List<object?> recursiveList => FormatList(recursiveList, indent),
                CustomData dictionary => FormatDictionary(dictionary, indent),
                _ => obj?.ToString() ?? "NULL"
            };
        }

        private bool TryGetStringToEnum<T>(string key, [NotNullWhen(true)] out T? result)
        {
            if (!TryGetValue(key, out object? value) || value == null)
            {
                result = default;
                return false;
            }

            Type? underlyingType = Nullable.GetUnderlyingType(typeof(T));
            if (underlyingType != null)
            {
                result = (T)Enum.Parse(underlyingType, (string)value);
            }
            else
            {
                result = (T)Enum.Parse(typeof(T), (string)value);
            }

            return true;
        }

        private bool TryGet<T>(string key, [NotNullWhen(true)] out T? result)
        {
            if (!TryGetValue(key, out object? value) || value == null)
            {
                result = default;
                return false;
            }

            Type resultType = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
            if (Type.GetTypeCode(value.GetType()) switch
                {
                    TypeCode.Byte
                        or TypeCode.SByte
                        or TypeCode.UInt16
                        or TypeCode.UInt32
                        or TypeCode.UInt64
                        or TypeCode.Int16
                        or TypeCode.Int32
                        or TypeCode.Int64
                        or TypeCode.Decimal
                        or TypeCode.Double
                        or TypeCode.Single => true,
                    _ => false
                })
            {
                result = (T)Convert.ChangeType(value, resultType);
            }
            else
            {
                result = (T)value;
            }

            return true;
        }
    }
}
