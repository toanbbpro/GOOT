using System;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using Microsoft.VSDiagnostics;

// Benchmark to exercise Form1.MainTimer_Tick logic paths
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[CPUUsageDiagnoser]
public class Form1Benchmark
{
    private object _formInstance;
    private MethodInfo _tickMethod;
    [GlobalSetup]
    public void Setup()
    {
        // Create the Form1 instance via reflection to avoid designer dependencies at compile time
        var asm = Assembly.Load("GOOT");
        var type = asm.GetType("GOOT.Form1");
        _formInstance = Activator.CreateInstance(type);
        // Access private MainTimer_Tick method
        _tickMethod = type.GetMethod("MainTimer_Tick", BindingFlags.NonPublic | BindingFlags.Instance);
        // Prepare target time for countdown mode (mode 1)
        var modeField = type.GetField("_mode", BindingFlags.NonPublic | BindingFlags.Instance);
        var targetField = type.GetField("_targetTime", BindingFlags.NonPublic | BindingFlags.Instance);
        modeField.SetValue(_formInstance, 1);
        targetField.SetValue(_formInstance, DateTime.Now.AddSeconds(10));
    }

    [Benchmark]
    public void InvokeMainTimerTick()
    {
        _tickMethod.Invoke(_formInstance, new object[] { null, EventArgs.Empty });
    }
}