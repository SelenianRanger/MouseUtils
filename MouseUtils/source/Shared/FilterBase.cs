using OpenTabletDriver;
using OpenTabletDriver.Plugin;
using OpenTabletDriver.Plugin.Attributes;
using OpenTabletDriver.Plugin.DependencyInjection;
using OpenTabletDriver.Plugin.Output;
using OpenTabletDriver.Plugin.Tablet;

namespace MouseUtils;

public abstract class FilterBase : IPositionedPipelineElement<IDeviceReport>, IDisposable
{
    [Resolved] 
    public IDriver DriverRef { get; set; }
    
    private TabletReference _tabletRef;

    [TabletReference]
    public TabletReference TabletRef
    {
        get => _tabletRef;
        set
        {
            _tabletRef = value;
            _properties = Properties.GetOrAddProperties(value, out _);
            SubscribeToPropertyChanges();
        }
    }

    public IOutputMode? OutputMode;

    protected Properties _properties;

    public void Consume(IDeviceReport value)
    {
        if (!IsValidReport(value))
        {
            InvokeEmit(value);
            return;
        }

        OnValidReport(value);
    }

    public event Action<IDeviceReport>? Emit;
    public virtual PipelinePosition Position => PipelinePosition.PreTransform;
    
    [OnDependencyLoad]
    public virtual void OnDependencyLoad()
    {
        OutputMode = null;
        
        InitializeProperties();
    }

    protected abstract void OnValidReport(IDeviceReport report);

    protected abstract void SubscribeToPropertyChanges();

    protected abstract void InitializeProperties();

    protected abstract void OnOutputModeFound();

    protected virtual bool IsValidReport(IDeviceReport report)
    {
        // no reference to output mode
        if (OutputMode == null && !TryGetOutputMode())
            return false;

        return true;
    }

    protected void InvokeEmit(IDeviceReport report)
    {
        Emit?.Invoke(report);
    }
    
    private bool TryGetOutputMode()
    {
        if (DriverRef is not Driver drv)
            return false;
        
        OutputMode = drv.InputDevices
            .Where(device => device?.OutputMode?.Elements?.Contains(this) ?? false)
            .Select(device => device?.OutputMode)
            .FirstOrDefault();
        
        if (OutputMode == null)
        {
            Log.WriteNotify("Mouse Utils", "Output mode not found.", LogLevel.Error);
            throw new Exception("Output mode not found");
        }

        OnOutputModeFound();

        return true;
    }

    protected virtual void OnDispose()
    {
        _properties.UnsubscribeFromAll(this);
    }
    
    public void Dispose()
    {
        OnDispose();
        GC.SuppressFinalize(this);
    }
}