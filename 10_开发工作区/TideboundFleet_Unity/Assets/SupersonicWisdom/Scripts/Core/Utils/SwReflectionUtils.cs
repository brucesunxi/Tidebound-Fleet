using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Object = UnityEngine.Object;

namespace SupersonicWisdomSDK
{
    public class SwReflectionUtils
    {
        #region --- Constants ---
        
        private const string TEST_EXCLUDE_CLASSES = "Test";
        private const string MAIN_ASSEMBLY_NAME = "SupersonicWisdom";
		private const string PRIVATE_MEMBER_PREFIX = "_";

        #endregion
        
        
        #region --- Public Methods ---
        
        public static IEnumerable<Type> GetAllTypes<T>()
        {
            var baseType = typeof(T);
            
            var filteredAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(assembly => assembly.GetName().Name.StartsWith(MAIN_ASSEMBLY_NAME));
            
            var allSubtypes = new List<Type>();
            
            foreach (var assembly in filteredAssemblies)
            {
                allSubtypes.AddRange(assembly.GetTypes()
                    .Where(type => type.IsClass && !type.IsAbstract && baseType.IsAssignableFrom(type) &&
                                   type != baseType && !type.Name.Contains(TEST_EXCLUDE_CLASSES)));
            }
            
            return allSubtypes.Distinct();
        }

        public static List<Type> GetAllTypesByStage<T>(string stage = "")
        {
            var allTypes = new List<Type>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies().Where(assembly => assembly.GetName().Name.Contains(stage)))
            {
                allTypes.AddRange(assembly.GetTypes().Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(T)) && !myType.Name.Contains(TEST_EXCLUDE_CLASSES)));
            }

            return allTypes;
        }

        public static bool SetProperty(string fieldName, string fieldValue, Object instance)
        {
            if (instance == null) return false;

            var didUpdate = false;

            try
            {
                instance.GetType().GetProperty(fieldName).SetValue(instance, fieldValue);
                didUpdate = true;
            }
            catch (Exception e)
            {
                SwInfra.Logger.LogException(e, EWisdomLogType.Utils, "Failed Reflection");
            }

            return didUpdate;
        }

        public static T GetProperty<T>(string fieldName, T defaultValue, Object instance) where T : class
        {
            var value = defaultValue;

            if (instance == null) return value;

            try
            {
                value = instance.GetType().GetProperty(fieldName).GetValue(instance) as T;
            }
            catch (Exception e)
            {
                SwInfra.Logger.LogException(e, EWisdomLogType.Utils, "Failed Reflection");
            }

            return value ?? defaultValue;
        }
        
        public static Dictionary<string, object> ToDictionary(object obj)
        {
            var dictionary = new Dictionary<string, object>();

            var type = obj.GetType();
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (var field in fields)
            {
                var fieldName = field.Name.StartsWith(PRIVATE_MEMBER_PREFIX) ? field.Name[1..] : field.Name;
                var value = field.GetValue(obj);
                dictionary[fieldName] = value;
            }

            return dictionary;
        }

        public static bool IsSubclass(object obj,Type parent)
        {
            return obj.GetType().IsSubclassOf(parent);
        }
        
        public static object InvokeStaticMethod(string typeName, string assemblyName, string methodName, params object[] parameters)
        {
            object result = null;
            
            try
            {
                var type = Type.GetType($"{typeName}, {assemblyName}");
                
                if (type != null)
                {
                    var method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static);
                    
                    if (method != null)
                    {
                        result = method.Invoke(null, parameters);
                    }
                    else
                    {
                        SwInfra.Logger.LogError(EWisdomLogType.Utils, $"Method '{methodName}' not found in type '{typeName}'.");
                    }
                }
                else
                {
                    SwInfra.Logger.LogError(EWisdomLogType.Utils, $"Type '{typeName}' not found in assembly '{assemblyName}'.");
                }
            }
            catch (Exception ex)
            {
                SwInfra.Logger.LogException(ex, EWisdomLogType.Utils, $"Error invoking method '{methodName}' on type '{typeName}'.");
            }
            
            return result;
        }

        #endregion
    }
}