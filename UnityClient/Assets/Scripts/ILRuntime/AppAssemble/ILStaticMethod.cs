using ILRuntime.CLR.Method;

public class ILStaticMethod : IStaticMethod
{
    private readonly ILRuntime.Runtime.Enviorment.AppDomain appDomain;
    private readonly IMethod method;
    private readonly object[] param;

    public ILStaticMethod(ILRuntime.Runtime.Enviorment.AppDomain appDomain, IMethod method, int paramsCount)
    {
        this.appDomain = appDomain;
        this.method = method;
        param = new object[paramsCount];
    }

    public void Run()
    {
        appDomain.Invoke(this.method, null, this.param);
    }

    public void Run(object a)
    {
        param[0] = a;
        appDomain.Invoke(this.method, null, param);
    }

    public void Run(object a, object b)
    {
        param[0] = a;
        param[1] = b;
        appDomain.Invoke(this.method, null, param);
    }

    public void Run(object a, object b, object c)
    {
        param[0] = a;
        param[1] = b;
        param[2] = c;
        appDomain.Invoke(this.method, null, param);
    }
    
}
