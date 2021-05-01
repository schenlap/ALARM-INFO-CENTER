using AlarmInfoCenter.Base;
using Ninject.Modules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AlarmInfoCenter
{
    public class DefaultNinjectBindings : NinjectModule
    {
        public override void Load()
        {
            this.Bind<ILogger>().To<EventLogger>().InSingletonScope();

            this.Bind<IStream>().To<DefaultStream>().InTransientScope();
            this.Bind<IPing>().To<DefaultPing>().InSingletonScope();
            this.Bind<ITcpClient>().To<DefaultTcpClient>().InTransientScope();
            this.Bind<ITcpListener>().To<DefaultTcpListener>().InTransientScope();

            this.Bind<IAlarmState>().To<DefaultAlarmState>().InSingletonScope();
            this.Bind<IWasListener>().To<DefaultWasListener>().InSingletonScope();
            this.Bind<IClientListener>().To<DefaultClientListener>().InTransientScope();
            this.Bind<IClientSession>().To<DefaultClientSession>().InTransientScope();
            this.Bind<IListeningManager>().To<DefaultListeningManager>().InTransientScope();
            this.Bind<ICoreObjectFactory>().To<DefaultCoreObjectFactory>().InSingletonScope();
        }
    }
}
