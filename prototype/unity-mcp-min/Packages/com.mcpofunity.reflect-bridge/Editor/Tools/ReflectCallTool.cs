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
    /// Minimal reflection-driven editor entrypoint: resolve target instance, bind JSON args to CLR types,
    /// then perform get/set/invoke. Business operations are expressed only via parameters (targetType, member, args),
    /// not via app-level switches like "setPosition". Mode dispatch uses a static handler map (get/set/invoke only).
    /// </summary>
    public static class ReflectCallTool
    {
        private static readonly string[] AllowedNamespacePrefixes =
        {
            "UnityEngine",
            "UnityEditor"
        };

        private static readonly HashSet<string> DeniedMembers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "DeleteAsset",
            "MoveAssetToTrash",
            "Exit",
            "Quit",
            "DestroyImmediate"
        };

        private const BindingFlags CommonFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

        private delegate object ReflectModeHandler(
            Type targetType,
            string memberName,
            JArray args,
            bool dryRun,
            JObject objectSelector);

        private static readonly Dictionary<string, ReflectModeHandler> ReflectModeHandlers =
            new Dictionary<string, ReflectModeHandler>(StringComparer.OrdinalIgnoreCase)
            {
                ["get"] = (t, m, _, d, s) => ExecuteGet(t, m, d, s),
                ["set"] = ExecuteSet,
                ["invoke"] = ExecuteInvoke
            };

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
                if (!ReflectModeHandlers.TryGetValue(mode, out var handler))
                {
                    var supported = string.Join(", ", ReflectModeHandlers.Keys.OrderBy(k => k));
                    return Error($"Unsupported mode '{mode}'. Supported: {supported}.");
                }

                return handler(targetType, memberName, args, dryRun, objectSelector);
            }
            catch (Exception ex)
            {
                return Error($"Reflection call failed: {ex.Message}", new { exception = ex.GetType().Name });
            }
        }

        private static object ExecuteGet(Type targetType, string memberName, bool dryRun, JObject objectSelector)
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
                    LogDryRun($"get {BuildSignature(property)}");
                    return Success("Dry-run get resolved.", new
                    {
                        dryRun = true,
                        executed = false,
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
                    LogDryRun($"get {BuildSignature(field)}");
                    return Success("Dry-run get resolved.", new
                    {
                        dryRun = true,
                        executed = false,
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

        private static object ExecuteSet(Type targetType, string memberName, JArray args, bool dryRun, JObject objectSelector)
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
                    LogDryRun($"set {BuildSignature(property)}");
                    return Success("Dry-run set resolved.", new
                    {
                        dryRun = true,
                        executed = false,
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
                    LogDryRun($"set {BuildSignature(field)}");
                    return Success("Dry-run set resolved.", new
                    {
                        dryRun = true,
                        executed = false,
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

        private static object ExecuteInvoke(Type targetType, string memberName, JArray args, bool dryRun, JObject objectSelector)
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
                    LogDryRun($"invoke {BuildSignature(method)}");
                    return Success("Dry-run invoke resolved.", new
                    {
                        dryRun = true,
                        executed = false,
                        mode = "invoke",
                        member = BuildSignature(method),
                        convertedArgs = convertedArgs.Select(NormalizeValue).ToArray()
                    });
                }

                var result = method.Invoke(targetObject, convertedArgs);
                return Success("Invoke succeeded.", new
                {
                    dryRun = false,
                    executed = true,
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
                    var attached = go.GetComponent(targetType);
                    if (attached != null)
                    {
                        return attached;
                    }

                    return Error($"Component '{targetType.FullName}' not found on GameObject '{go.name}'.");
                }

                if (source is Component src && targetType.IsAssignableFrom(src.GetType()))
                {
                    return src;
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

                if (targetType == typeof(Type))
                {
                    var typeName = token.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(typeName))
                    {
                        error = "Type argument requires a non-empty string (full name or simple name).";
                        return false;
                    }

                    var resolvedType = ResolveConcreteComponentType(typeName, out var typeError);
                    if (resolvedType == null)
                    {
                        error = typeError;
                        return false;
                    }

                    converted = resolvedType;
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

        private static bool IsConcreteComponent(Type type)
        {
            return typeof(Component).IsAssignableFrom(type)
                   && type is { IsAbstract: false, IsInterface: false, ContainsGenericParameters: false };
        }

        /// <summary>
        /// Resolves a Unity Component type from user/script assemblies for AddComponent(Type) and similar.
        /// </summary>
        private static Type ResolveConcreteComponentType(string typeName, out string error)
        {
            error = string.Empty;

            var resolved = ResolveType(typeName, out var ambiguity);
            if (ambiguity.Length > 0)
            {
                error = $"Ambiguous type '{typeName}'. Use a fully qualified type name.";
                return null;
            }

            if (resolved != null && IsConcreteComponent(resolved))
            {
                return resolved;
            }

            var matches = new List<Type>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = asm.GetTypes();
                }
                catch
                {
                    continue;
                }

                foreach (var candidate in types)
                {
                    if (!IsConcreteComponent(candidate))
                    {
                        continue;
                    }

                    if (string.Equals(candidate.Name, typeName, StringComparison.Ordinal) ||
                        string.Equals(candidate.FullName, typeName, StringComparison.Ordinal))
                    {
                        matches.Add(candidate);
                    }
                }
            }

            var distinct = matches.Distinct().ToArray();
            if (distinct.Length > 1)
            {
                error =
                    $"Ambiguous component type '{typeName}'. Candidates: {string.Join(", ", distinct.Select(t => t.FullName).OrderBy(n => n))}";
                return null;
            }

            if (distinct.Length == 0)
            {
                error = $"No concrete Component type matched '{typeName}'.";
                return null;
            }

            return distinct[0];
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
            if (memberInfo is PropertyInfo propertyInfo)
            {
                return $"{propertyInfo.PropertyType.Name} {propertyInfo.DeclaringType?.Name}.{propertyInfo.Name}";
            }

            if (memberInfo is FieldInfo fieldInfo)
            {
                return $"{fieldInfo.FieldType.Name} {fieldInfo.DeclaringType?.Name}.{fieldInfo.Name}";
            }

            if (memberInfo is MethodInfo methodInfo)
            {
                var parameters = string.Join(
                    ", ",
                    methodInfo.GetParameters().Select(parameter => $"{parameter.ParameterType.Name} {parameter.Name}"));

                return $"{methodInfo.ReturnType.Name} {methodInfo.DeclaringType?.Name}.{methodInfo.Name}({parameters})";
            }

            return memberInfo.Name;
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

        private static void LogDryRun(string summary)
        {
            Debug.Log(
                $"[MCP Reflect Bridge] Dry-run: {summary} — no scene or editor changes. " +
                "Send dryRun:false (or omit dryRun) to execute.");
        }

        private class ErrorPayload
        {
            public bool ok { get; set; }
            public string message { get; set; }
            public object details { get; set; }
        }
    }
}
