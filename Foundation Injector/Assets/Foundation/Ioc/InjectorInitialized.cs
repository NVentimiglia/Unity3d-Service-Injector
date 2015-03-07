// -------------------------------------
//  Domain		: Avariceonline.com
//  Author		: Nicholas Ventimiglia
//  Product		: Unity3d Foundation
//  Published		: 2015
//  -------------------------------------
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Foundation.Ioc
{
    /// <summary>
    /// Decorates ScriptableObject or CLR Services with a default ctor.
    /// //
    /// Loads the service into memory on application initialization. 
    /// For ScriptableObject, assign the Resource File Name. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class InjectorInitialized : Attribute
    {
        /// <summary>
        /// aborts automatic loading into memory
        /// </summary>
        public bool AbortLoad { get; private set; }

        /// <summary>
        /// Location / File name of the ScriptableObject resource
        /// </summary>
        public string ResourceName { get; private set; }


        public InjectorInitialized()
        {

        }

        /// <summary>
        /// With resource name. IE:
        /// MyService or /Services/MyService
        /// </summary>
        /// <param name="resourceName"></param>
        public InjectorInitialized(string resourceName)
        {
            ResourceName = resourceName;
        }

        /// <summary>
        /// With resource name. IE:
        /// MyService or /Services/MyService
        /// </summary>
        /// <param name="resourceName"></param>
        /// <param name="abortLoad"></param>
        public InjectorInitialized(string resourceName, bool abortLoad)
        {
            ResourceName = resourceName;
            AbortLoad = abortLoad;
        }

        /// <summary>
        /// Has the LoadScriptableObjects been called ?
        /// </summary>
        public static bool IsLoaded { get; private set; }

        /// <summary>
        /// Editor creation is running
        /// </summary>
        public static bool IsLoading { get; set; }

        /// <summary>
        /// All Object Types that are considered services
        /// </summary>
        public static Type[] GetServiceTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes().Where(o => HasAttribute<InjectorInitialized>(o))).ToArray();
        }

        /// <summary>
        /// Loads all services into memory
        /// </summary>
        public static void LoadServices()
        {
            if (IsLoaded)
            {
                return;
            }

            if (IsLoading)
            {
                return;
            }

            IsLoaded = true;
            var types = GetServiceTypes();

            foreach (var type in types)
            {
                //Already Loaded
                if (Injector.GetFirst(type) != null)
                    continue;

                //check for a static accessor
                if (CheckForStaticAccessor(type))
                    return;

                if (typeof(ScriptableObject).IsAssignableFrom(type))
                {
                    var deco = GetAttribute<InjectorInitialized>(type);

                    if (deco.AbortLoad)
                        continue;

                    // note Object has the responsibility of exporting itself to the injector
                    var resource = Resources.Load(deco.ResourceName);

                    if (resource == null)
                    {
                        Debug.LogWarning("Resource " + deco.ResourceName + " is not found");
                        Debug.LogWarning("Run Tools/Foundation/Instantiate Resources");
                    }
                    else
                    {
                        Injector.AddExport(resource);
                    }
                }
                else if (typeof(UnityEngine.Object).IsAssignableFrom(type))
                {
                    Debug.LogError(string.Format("Service {0} should not inherit from UnityEngine.Object", type));
                }
                else
                {
                    try
                    {
                        Debug.LogWarning(string.Format("Service {0} should have a Singleton Instance Property", type));
                        var resource = Activator.CreateInstance(type);
                        Injector.AddExport(resource);


                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(ex);
                        Debug.LogError("Failed to create instance of " + type);
                        Debug.LogWarning(string.Format("Service {0} should have a Singleton Instance Property", type));
                  
                    }
                }
            }
        }

        /// <summary>
        /// Checks for a static instance member
        /// </summary>
        /// <returns></returns>
        static bool CheckForStaticAccessor(Type type)
        {
            // check for singleton variable
            var type1 = type;

            // check props
            var props = type.GetProperties(BindingFlags.Static | BindingFlags.Public).Where(o => o.PropertyType == type1).ToArray();
            if (props.Any())
            {
                // call it
                var resource = props.First().GetValue(null, null);

                if (resource == null)
                    return false;
                Injector.AddExport(resource);
                return true;
            }
            else
            {
                // check fields
                var fields = type.GetFields(BindingFlags.Static | BindingFlags.Public).Where(o => o.FieldType == type1).ToArray();
                if (fields.Any())
                {
                    // call it
                    var resource = fields.First().GetValue(null);

                    if (resource == null)
                        return false;

                    Injector.AddExport(resource);
                    return true;
                }
            }

            return false;
        }

        #region misc
        /// <summary>
        /// return Attribute.IsDefined(m, typeof(T));
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="m"></param>
        /// <returns></returns>
        static bool HasAttribute<T>(MemberInfo m) where T : Attribute
        {
            return IsDefined(m, typeof(T));
        }

        /// <summary>
        ///  return m.GetCustomAttributes(typeof(T), true).FirstOrDefault() as T;
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="m"></param>
        /// <returns></returns>
        static T GetAttribute<T>(MemberInfo m) where T : Attribute
        {
            return m.GetCustomAttributes(typeof(T), true).FirstOrDefault() as T;
        }
        #endregion
    }
}