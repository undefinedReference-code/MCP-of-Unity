using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;

namespace UnityMcpMin.Editor.Tools
{
    /// <summary>
    /// Minimal reflection-driven entrypoint for editor-side invocation.
    /// This intentionally limits API surface with allowlist/denylist controls.
    /// </summary>
    public static class ReflectCallTool
    {
        private static readonly string[] AllowedNamespacePrefixes =
        {
            "UnityEngine",
            "UnityEditor"
        };

        private static readonly HashSet<string> DeniedMembers = new(StringComparer.OrdinalIgnoreCase)
        {
            "DeleteAsset",
            "MoveAssetToTrash",
            "Exit",
            "Quit",
            "DestroyImmediate"
        };

        private const BindingFlags CommonFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

        public static object HandleCommand(JObject parameters)
        {
            if (parameters == null)
            {
                return Error("Parameters cannot be null.");
            }

            var mode = parameters.Value<string>("mode")?.Trim().ToLowerInvariant();
            var targetTypeName = parameters.Value<string>("targetType")?.Trim();
            var memberName = parameters.Value<string>("member")?.Trim();
            var dryRun = parameters.Value<bool?>("dryRun") ?? false;
            var objectSelector = parameters["objectSelector"] as JObject;
            var args = parameters["args"] as JArray ?? new JArray();

            if (string.IsNullOrWhiteSpace(mode) || string.IsNullOrWhiteSpace(targetTypeName) || string.IsNullOrWhiteSpace(memberName))
            {
                return Error("Required fields: mode, targetType, member.");
            }

            if (DeniedMembers.Contains(memberName))
            {
                return Error($"Member '{memberName}' is blocked by denylist.");
            }

            var targetType = ResolveType(targetTypeName, out var ambiguity);
            if (targetType == null)
            {
                return Error($"Type '{targetTypeName}' not found.", new { ambiguousCandidates = ambiguity });
            }

            if (!IsNamespaceAllowed(targetType))
            {
                return Error($"Type '{targetType.FullName}' is outside allowlist.");
            }

            try
            {
                return mode switch
                {
                    "get" => HandleGet(targetType, memberName, dryRun, objectSelector),
                    "set" => HandleSet(targetType, memberName, args, dryRun, objectSelector),
                    "invoke" => HandleInvoke(targetType, memberName, args, dryRun, objectSelector),
                    _ => Error($"Unsupported mode '{mode}'. Use get, set, or invoke.")
                };
            }
            catch (Exception ex)
            {
                return Error($"Reflection call failed: {ex.Message}", new { exception = ex.GetType().Name });
            }
        }

        private static object HandleGet(Type targetType, string memberName, bool dryRun, JObject objectSelector)
        {
            var property = targetType.GetProperty(memberName, CommonFlags);
            if (property != null)
            {
                var targetObject = ResolveTargetObject(targetType, property.GetMethod?.IsStatic ?? false, objectSelector);
                if (targetObject is ErrorPayload errorPayload)
                {
                    return errorPayload;
                }

                if (dryRun)
                {
                    return Success("Dry-run get resolved.", new
                    {
                        mode = "get",
                        member = BuildSignature(property),
                        staticCall = property.GetMethod?.IsStatic ?? false
                    });
                }

                var value = property.GetValue(targetObject);
                return Success("Get succeeded.", new
                {
                    mode = "get",
                    member = BuildSignature(property),
                    value = NormalizeValue(value)
                });
            }

            var field = targetType.GetField(memberName, CommonFlags);
            if (field != null)
            {
                var targetObject = ResolveTargetObject(targetType, field.IsStatic, objectSelector);
                if (targetObject is ErrorPayload errorPayload)
                {
                    return errorPayload;
                }

                if (dryRun)
                {
                    return Success("Dry-run get resolved.", new
                    {
                        mode = "get",
                        member = BuildSignature(field),
                        staticCall = field.IsStatic
                    });
                }

                var value = field.GetValue(targetObject);
                return Success("Get succeeded.", new
                {
                    mode = "get",
                    member = BuildSignature(field),
                    value = NormalizeValue(value)
                });
            }

            return Error($"No property or field '{memberName}' found on '{targetType.FullName}'.");
        }

        private static object HandleSet(Type targetType, string memberName, JArray args, bool dryRun, JObject objectSelector)
        {
            if (args.Count != 1)
            {
                return Error("Set mode expects exactly one argument.");
            }

            var property = targetType.GetProperty(memberName, CommonFlags);
            if (property != null && property.CanWrite)
            {
                var targetObject = ResolveTargetObject(targetType, property.SetMethod?.IsStatic ?? false, objectSelector);
                if (targetObject is ErrorPayload errorPayload)
                {
                    return errorPayload;
                }

                if (!TryConvert(args[0], property.PropertyType, out var converted, out var conversionError))
                {
                    return Error($"Argument conversion failed: {conversionError}", new
                    {
                        expectedType = property.PropertyType.FullName
                    });
                }

                if (dryRun)
                {
                    return Success("Dry-run set resolved.", new
                    {
                        mode = "set",
                        member = BuildSignature(property),
                        convertedValue = NormalizeValue(converted)
                    });
                }

                property.SetValue(targetObject, converted);
                return Success("Set succeeded.", new
                {
                    mode = "set",
                    member = BuildSignature(property),
                    value = NormalizeValue(converted)
                });
            }

            var field = targetType.GetField(memberName, CommonFlags);
            if (field != null)
            {
                var targetObject = ResolveTargetObject(targetType, field.IsStatic, objectSelector);
                if (targetObject is ErrorPayload errorPayload)
                {
                    return errorPayload;
                }

                if (!TryConvert(args[0], field.FieldType, out var converted, out var conversionError))
                {
                    return Error($"Argument conversion failed: {conversionError}", new
                    {
                        expectedType = field.FieldType.FullName
                    });
                }

                if (dryRun)
                {
                    return Success("Dry-run set resolved.", new
                    {
                        mode = "set",
                        member = BuildSignature(field),
                        convertedValue = NormalizeValue(converted)
                    });
                }

                field.SetValue(targetObject, converted);
                return Success("Set succeeded.", new
                {
                    mode = "set",
                    member = BuildSignature(field),
                    value = NormalizeValue(converted)
                });
            }

            return Error($"No writable property/field '{memberName}' found on '{targetType.FullName}'.");
        }

        private static object HandleInvoke(Type targetType, string memberName, JArray args, bool dryRun, JObject objectSelector)
        {
            var candidates = targetType
                .GetMethods(CommonFlags)
                .Where(m => string.Equals(m.Name, memberName, StringComparison.Ordinal))
                .ToList();

            if (!candidates.Any())
            {
                return Error($"No method '{memberName}' found on '{targetType.FullName}'.");
            }

            foreach (var method in candidates)
            {
                var parameters = method.GetParameters();
                if (parameters.Length != args.Count)
                {
                    continue;
                }

                var convertedArgs = new object[args.Count];
                var convertFailed = false;
                string convertError = string.Empty;

                for (var i = 0; i < parameters.Length; i++)
                {
                    if (TryConvert(args[i], parameters[i].ParameterType, out var converted, out var argError))
                    {
                        convertedArgs[i] = converted;
                        continue;
                    }

                    convertFailed = true;
                    convertError = argError;
                    break;
                }

                if (convertFailed)
                {
                    continue;
                }

                var targetObject = ResolveTargetObject(targetType, method.IsStatic, objectSelector);
                if (targetObject is ErrorPayload errorPayload)
                {
                    return errorPayload;
                }

                if (dryRun)
                {
                    return Success("Dry-run invoke resolved.", new
                    {
                        mode = "invoke",
                        member = BuildSignature(method),
                        convertedArgs = convertedArgs.Select(NormalizeValue).ToArray()
                    });
                }

                var result = method.Invoke(targetObject, convertedArgs);
                return Success("Invoke succeeded.", new
                {
                    mode = "invoke",
                    member = BuildSignature(method),
                    result = NormalizeValue(result)
                });
            }

            var signatures = candidates.Select(BuildSignature).ToArray();
            return Error("No overload matched provided argument types.", new { candidates = signatures });
        }

        private static object ResolveTargetObject(Type targetType, bool isStatic, JObject objectSelector)
        {
            if (isStatic)
            {
                return null;
            }

            if (objectSelector == null)
            {
                return Error("Instance member requires objectSelector.");
            }

            var instanceId = objectSelector.Value<int?>("instanceId");
            if (instanceId.HasValue)
            {
                var selected = EditorUtility.InstanceIDToObject(instanceId.Value);
                if (selected == null)
                {
                    return Error($"instanceId '{instanceId}' was not found.");
                }

                return CoerceTargetObject(selected, targetType);
            }

            var gameObjectName = objectSelector.Value<string>("gameObjectName");
            if (!string.IsNullOrWhiteSpace(gameObjectName))
            {
                var go = GameObject.Find(gameObjectName);
                if (go == null)
                {
                    return Error($"GameObject '{gameObjectName}' not found.");
                }

                return CoerceTargetObject(go, targetType);
            }

            return Error("objectSelector must include instanceId or gameObjectName.");
        }

        private static object CoerceTargetObject(UnityEngine.Object source, Type targetType)
        {
            if (targetType == typeof(GameObject))
            {
                if (source is GameObject go)
                {
                    return go;
                }

                if (source is Component comp)
                {
                    return comp.gameObject;
                }
            }

            if (typeof(Component).IsAssignableFrom(targetType))
            {
                if (source is GameObject go)
                {
                    var component = go.GetComponent(targetType);
                    return component != null
                        ? component
                        : Error($"Component '{targetType.FullName}' not found on GameObject '{go.name}'.");
                }

                if (source is Component component && targetType.IsAssignableFrom(component.GetType()))
                {
                    return component;
                }
            }

            if (targetType.IsAssignableFrom(source.GetType()))
            {
                return source;
            }

            return Error($"Selected object type '{source.GetType().FullName}' cannot be coerced to '{targetType.FullName}'.");
        }

        private static bool TryConvert(JToken token, Type targetType, out object converted, out string error)
        {
            converted = null;
            error = string.Empty;

            if (token.Type == JTokenType.Null)
            {
                if (!targetType.IsValueType || Nullable.GetUnderlyingType(targetType) != null)
                {
                    converted = null;
                    return true;
                }

                error = "Null cannot be assigned to non-nullable value type.";
                return false;
            }

            var nullableUnderlying = Nullable.GetUnderlyingType(targetType);
            if (nullableUnderlying != null)
            {
                targetType = nullableUnderlying;
            }

            try
            {
                if (targetType == typeof(string))
                {
                    converted = token.ToString();
                    return true;
                }

                if (targetType == typeof(bool))
                {
                    converted = token.Value<bool>();
                    return true;
                }

                if (targetType.IsEnum)
                {
                    var enumText = token.Type == JTokenType.String ? token.ToString() : token.Value<int>().ToString(CultureInfo.InvariantCulture);
                    converted = Enum.Parse(targetType, enumText, true);
                    return true;
                }

                if (targetType == typeof(int))
                {
                    converted = token.Value<int>();
                    return true;
                }

                if (targetType == typeof(float))
                {
                    converted = token.Value<float>();
                    return true;
                }

                if (targetType == typeof(double))
                {
                    converted = token.Value<double>();
                    return true;
                }

                if (targetType == typeof(long))
                {
                    converted = token.Value<long>();
                    return true;
                }

                if (targetType == typeof(Vector2))
                {
                    var obj = token as JObject;
                    if (obj == null)
                    {
                        error = "Vector2 requires object with x/y.";
                        return false;
                    }

                    converted = new Vector2(obj.Value<float>("x"), obj.Value<float>("y"));
                    return true;
                }

                if (targetType == typeof(Vector3))
                {
                    var obj = token as JObject;
                    if (obj == null)
                    {
                        error = "Vector3 requires object with x/y/z.";
                        return false;
                    }

                    converted = new Vector3(obj.Value<float>("x"), obj.Value<float>("y"), obj.Value<float>("z"));
                    return true;
                }

                if (typeof(UnityEngine.Object).IsAssignableFrom(targetType))
                {
                    var obj = token as JObject;
                    if (obj == null)
                    {
                        error = "UnityEngine.Object conversion requires object selector payload.";
                        return false;
                    }

                    var instanceId = obj.Value<int?>("instanceId");
                    if (!instanceId.HasValue)
                    {
                        error = "UnityEngine.Object conversion requires instanceId.";
                        return false;
                    }

                    var unityObj = EditorUtility.InstanceIDToObject(instanceId.Value);
                    if (unityObj == null || !targetType.IsAssignableFrom(unityObj.GetType()))
                    {
                        error = $"Instance '{instanceId}' not assignable to '{targetType.FullName}'.";
                        return false;
                    }

                    converted = unityObj;
                    return true;
                }

                converted = token.ToObject(targetType);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static bool IsNamespaceAllowed(Type type)
        {
            var ns = type.Namespace ?? string.Empty;
            return AllowedNamespacePrefixes.Any(prefix => ns.StartsWith(prefix, StringComparison.Ordinal));
        }

        private static Type ResolveType(string typeName, out string[] ambiguity)
        {
            ambiguity = Array.Empty<string>();

            var direct = Type.GetType(typeName, throwOnError: false);
            if (direct != null)
            {
                return direct;
            }

            var matches = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(asm =>
                {
                    try
                    {
                        return asm.GetTypes();
                    }
                    catch
                    {
                        return Array.Empty<Type>();
                    }
                })
                .Where(type =>
                    string.Equals(type.FullName, typeName, StringComparison.Ordinal) ||
                    string.Equals(type.Name, typeName, StringComparison.Ordinal))
                .Distinct()
                .ToArray();

            if (matches.Length > 1)
            {
                ambiguity = matches.Select(type => type.FullName).OrderBy(name => name).ToArray();
                return null;
            }

            return matches.SingleOrDefault();
        }

        private static string BuildSignature(MemberInfo memberInfo)
        {
            return memberInfo switch
            {
                PropertyInfo p => $"{p.PropertyType.Name} {p.DeclaringType?.Name}.{p.Name}",
                FieldInfo f => $"{f.FieldType.Name} {f.DeclaringType?.Name}.{f.Name}",
                MethodInfo m => $"{m.ReturnType.Name} {m.DeclaringType?.Name}.{m.Name}({string.Join(", ", m.GetParameters().Select(x => x.ParameterType.Name + " " + x.Name))})",
                _ => memberInfo.Name
            };
        }

        private static object NormalizeValue(object value)
        {
            if (value == null)
            {
                return null;
            }

            if (value is UnityEngine.Object unityObject)
            {
                return new
                {
                    name = unityObject.name,
                    instanceId = unityObject.GetInstanceID(),
                    type = unityObject.GetType().FullName
                };
            }

            if (value is Vector2 v2)
            {
                return new { x = v2.x, y = v2.y };
            }

            if (value is Vector3 v3)
            {
                return new { x = v3.x, y = v3.y, z = v3.z };
            }

            return value;
        }

        private static ErrorPayload Error(string message, object details = null)
        {
            return new ErrorPayload
            {
                ok = false,
                message = message,
                details = details
            };
        }

        private static object Success(string message, object data)
        {
            return new
            {
                ok = true,
                message,
                data
            };
        }

        private class ErrorPayload
        {
            public bool ok { get; set; }
            public string message { get; set; }
            public object details { get; set; }
        }
    }
}

