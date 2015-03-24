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
#if UNITY_WSA
using System.Collections.Generic;
using Windows.ApplicationModel;
using Windows.Storage;
using System.Threading.Tasks;
#endif

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

#if UNITY_WSA
        /// <summary>
        /// All Object Types that are considered services
        /// </summary>
        public static async Task<TypeInfo[]> GetServiceTypes()
        {
            var ass = await GetLoadedAssemblies();
            return ass.SelectMany(a => a.DefinedTypes.Where(o => o.GetCustomAttribute<InjectorInitialized>() != null)).ToArray();
        }


        protected static async Task<Assembly[]> GetLoadedAssemblies()
        {
            // Find assemblies.
            StorageFolder folder = Package.Current.InstalledLocation;

            var loadedAssemblies = new List<Assembly>();

            var folderFilesAsync = await folder.GetFilesAsync().AsTask();

            foreach (var file in folderFilesAsync)
            {
                if (file.FileType == ".dll" || file.FileType == ".exe")
                {
                    try
                    {
                        var filename = file.Name.Substring(0, file.Name.Length - file.FileType.Length);
                        AssemblyName name = new AssemblyName { Name = filename };
                        Assembly asm = Assembly.Load(name);
                        loadedAssemblies.Add(asm);
                    }
                    catch (BadImageFormatException)
                    {
                        // Thrown reflecting on C++ executable files for which the C++ compiler stripped the relocation addresses (such as Unity dlls): http://msdn.microsoft.com/en-us/library/x4cw969y(v=vs.110).aspx
                    }
                }
            }

            return loadedAssemblies.OrderBy(o => o.FullName).ToArray();
        }

        /// <summary>
        /// Loads all services into memory
        /// </summary>
        public static async void LoadServices()
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


            var types = await GetServiceTypes();

            foreach (var type in types)
            {
                //Already Loaded
                if (Injector.GetFirst(type.AsType()) != null)
                    continue;

                //check for a static accessor
                if (CheckForStaticAccessor(type.AsType()))
                    return;

                if (type.IsSubclassOf(typeof(ScriptableObject)))
                {
                    var deco = ReflectionExtensions.GetAttribute<InjectorInitialized>(type);

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
                else if (type.IsSubclassOf(typeof(GameObject)))
                {
                    Debug.LogError(string.Format("Service {0} should not inherit from UnityEngine.Object", type));
                }
                else
                {
                    try
                    {
                        Debug.LogWarning(string.Format("Service {0} should have a Singleton Instance Property", type));
                        var resource = Activator.CreateInstance(type.AsType());
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
#else
        /// <summary>
        /// All Object Types that are considered services
        /// </summary>
        public static Type[] GetServiceTypes()
        {
            return AppDomain.CurrentDomain.GetAssemblies().SelectMany(a => a.GetTypes().Where(o => ReflectionExtensions.HasAttribute<InjectorInitialized>(o))).ToArray();
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
                    var deco = ReflectionExtensions.GetAttribute<InjectorInitialized>(type);

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
#endif
        /// <summary>
        /// Checks for a static instance member
        /// </summary>
        /// <returns></returns>
        static bool CheckForStaticAccessor(Type type)
        {
            // check for singleton variable
            var type1 = type;

#if UNITY_WSA
            // check props
            var props = type.GetTypeInfo().DeclaredProperties.Where(o => o.PropertyType == type1).ToArray();
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
                var fields = type.GetTypeInfo().DeclaredFields.Where(o => o.FieldType == type1).ToArray();
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
#else
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
#endif

            return false;
        }
    }
}