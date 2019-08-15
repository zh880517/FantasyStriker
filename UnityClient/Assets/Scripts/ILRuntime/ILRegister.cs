using ILRuntime.Runtime.Enviorment;
using ILRuntime.Runtime.Intepreter;
using System;
using System.Collections.Generic;
using System.Reflection;

public static class ILRegister
{
	public static void InitILRuntime(ILRuntime.Runtime.Enviorment.AppDomain appdomain)
	{
		// 注册重定向函数

		// 注册委托
		appdomain.DelegateManager.RegisterMethodDelegate<List<object>>();
		appdomain.DelegateManager.RegisterMethodDelegate<byte[], int, int>();
		appdomain.DelegateManager.RegisterMethodDelegate<ILTypeInstance>();
        appdomain.DelegateManager.RegisterFunctionDelegate<Google.Protobuf.Adapt_IMessage.Adaptor>();
        appdomain.DelegateManager.RegisterMethodDelegate<Google.Protobuf.Adapt_IMessage.Adaptor>();

        //注册绑定的导出类
        //CLRBindings.Initialize(appdomain);

        // 注册适配器
        Assembly assembly = typeof(ILRegister).Assembly;
		foreach (Type type in assembly.GetTypes())
		{
			object[] attrs = type.GetCustomAttributes(typeof(ILAdapterAttribute), false);
			if (attrs.Length == 0)
			{
				continue;
			}
			object obj = Activator.CreateInstance(type);
			CrossBindingAdaptor adaptor = obj as CrossBindingAdaptor;
			if (adaptor == null)
			{
				continue;
			}
			appdomain.RegisterCrossBindingAdaptor(adaptor);
		}

		LitJson.JsonMapper.RegisterILRuntimeCLRRedirection(appdomain);
	}
}