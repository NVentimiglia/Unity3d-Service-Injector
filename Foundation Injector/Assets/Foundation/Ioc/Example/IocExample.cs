using System;
using System.Collections.Generic;
using System.Linq;
using Foundation.Ioc;
using UnityEngine;
using UnityEngine.UI;

public class IocExample : MonoBehaviour
{

    public interface IExampleExport
    {
        int Id { get; set; }
    }

    public class ExampleExport : IExampleExport
    {
        public int Id { get; set; }

        protected static int Counter = 0;

        public static ExampleExport Create()
        {
            return new ExampleExport
            {
                Id = Counter++
            };
        }
    }

    [InjectorInitialized]
    public class ExampleExport2
    {
        public int Id { get; set; }

        public readonly static ExampleExport2 Instance = new ExampleExport2();
    }

    [Import]
    public ExampleExport Importer1;
    [Import]
    public IEnumerable<ExampleExport> Importer2;
    [Import]
    public ExampleExport[] Importer3;

    public Text Logger;

    public string Log
    {
        set { Logger.text = value + Environment.NewLine + Logger.text; }
    }

    void Awake()
    {
        Application.logMessageReceived += Application_logMessageReceived;
    }

    void Application_logMessageReceived(string condition, string stackTrace, LogType type)
    {
        Log = condition;
    }

    void Start()
    {

        Logger.text = string.Empty;
        Debug.Log("Starting IOC Test");

        Injector.ConfirmInit();
        Injector.Subscribe(this);

        var inst5 = Injector.GetFirst<ExampleExport2>();
        Assert(() => inst5 != null, "InjectorInitialized");

        Injector.AddExport(ExampleExport.Create());

        var inst = Injector.GetFirst<ExampleExport>();
        Assert(() => inst != null, "GetFirst Class");

        var inst2 = Injector.GetFirst<IExampleExport>();
        Assert(() => inst2 != null, "GetFirst Interface");
        Assert(() => Importer1 != null, "Import Single");
        Assert(() => Importer2 != null, "Import IEnumerable");
        Assert(() => Importer3 != null, "Import Array");

        Injector.RemoveExport(inst2);
        var inst3 = Injector.GetFirst<IExampleExport>();
        Assert(() => inst3 == null, "Removed");
        Assert(() => Importer1 == null, "Import Single");
        Assert(() => Importer2 == null || !Importer2.Any(), "Import Multiple");

        Injector.AddExport(ExampleExport.Create());
        Injector.AddExport(ExampleExport.Create());
        Injector.AddExport(ExampleExport.Create());
        Injector.AddExport(ExampleExport.Create());
        Injector.AddExport(ExampleExport.Create());

        var inst4 = Injector.GetAll<IExampleExport>();
        Assert(() => inst4.Count() == 5, "Get All");

        Assert(() => Importer1 != null, "Import Single");
        Assert(() => Importer2.Count() == 5, "Import IEnumerable");
        Assert(() => Importer3.Length == 5, "Import Array");

        Debug.Log("All Done");
    }

    void Assert(Func<bool> func, string title)
    {
        if (func())
        {
            Debug.Log(title + " PASSED");
        }
        else
        {
            Debug.LogError(title + " FAILED");
            throw new Exception(title + " ASSERT FAILED");
        }
    }
}
