using System.Reflection;

public class MonoStaticMethod : IStaticMethod
{
    private readonly MethodInfo methodInfo;

    private readonly object[] param;

    public MonoStaticMethod(MethodInfo methon)
    {
        methodInfo = methon;
        param = new object[methodInfo.GetParameters().Length];
    }

    public void Run()
    {
        methodInfo.Invoke(null, param);
    }

    public void Run(object a)
    {
        param[0] = a;
        methodInfo.Invoke(null, param);
    }

    public void Run(object a, object b)
    {
        param[0] = a;
        param[1] = b;
        methodInfo.Invoke(null, param);
    }

    public void Run(object a, object b, object c)
    {
        param[0] = a;
        param[1] = b;
        param[2] = c;
        methodInfo.Invoke(null, param);
    }
}
