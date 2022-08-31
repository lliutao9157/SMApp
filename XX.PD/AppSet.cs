using Autofac;
using Autofac.Extras.DynamicProxy;
using Castle.DynamicProxy;
using SMApp;
using System;
using System.Reflection;
using System.Threading;


namespace XX.PD
{
    class AppSet
    {
        private readonly static Lazy<AppSet> AppSetInstance = new Lazy<AppSet>(() => new AppSet());
        internal static AppSet Instance
        {
            get
            {
                return AppSetInstance.Value;
            }
        }
        private IContainer _container;//申明一个字段这个字段用来对接容器
        private IContainer Container //将对接的内容传输入这个属性！
        {
            get
            {
                if (_container == null)
                {
                    _container = _Builder.Build();
                }
                return _container;
            }
        }
        private ContainerBuilder _Builder;//申明容器
        AppSet()
        {
            try
            {
                //RegisterType(System.AppDomain.CurrentDomain.BaseDirectory + "HYS.Instance.dll");
                RegisterType();
            }
            catch (Exception e)
            {
                var msg = e.Message;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        internal void RegisterType(string dllpath)
        {
            if (_Builder == null)
            {
                _Builder = new ContainerBuilder();//实例化
                _Builder.RegisterAssemblyTypes(Assembly.LoadFile(dllpath))
                    .AsImplementedInterfaces().EnableInterfaceInterceptors();
                _Builder.RegisterType<HYXInterceptor>();
            }

        }
        internal void RegisterType()
        {
            if (_Builder == null)
            {
                _Builder = new ContainerBuilder();//实例化
                _Builder.RegisterAssemblyTypes(Assembly.GetAssembly(this.GetType()))
                    .AsImplementedInterfaces().EnableInterfaceInterceptors();
                _Builder.RegisterType<HYXInterceptor>();
            }

        }
        /// <summary>
        /// 获取接口实现
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        internal T GetFromFac<T>()
        {
            T t = Container.Resolve<T>();
            return t;
        }
    }
    class HYXInterceptor : IInterceptor
    {
        public void Intercept(IInvocation invocation)
        {
            try
            {
                var logcustomer = invocation.Method.GetCustomAttribute(typeof(LogAttribute));
                if (logcustomer != null)
                {
                    LogAttribute loga = (LogAttribute)logcustomer;
                    
                    var threadid = Thread.CurrentThread.ManagedThreadId.ToString();
                }
                invocation.Proceed();
            }
            catch
            {
                throw;
            }
            finally
            {
            
            }
        }
    }
}
