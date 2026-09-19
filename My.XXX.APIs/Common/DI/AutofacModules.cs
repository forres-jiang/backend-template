using Autofac;
using FluentValidation;
using My.XXX.APIs.Common.JWT;
using My.XXX.Infrastructure;
using My.XXX.Service;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Service.Validators;
using My.XXX.Shared;
using System.Linq;

namespace My.XXX.APIs.Common.DI
{
    public class AutofacModules : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // 注入对象生命周期说明 https://autofac.readthedocs.io/en/latest/lifetime/instance-scope.html
            //Other 手动注入
            builder.RegisterType<WebHelper>().As<IWebHelper>().InstancePerLifetimeScope();
            builder.RegisterType<DemoValidator>().As<IValidator<DemoModel>>().InstancePerLifetimeScope();
            builder.RegisterType<MailValidator>().As<IValidator<Mail>>().InstancePerLifetimeScope();
            builder.RegisterType<UserService>().As<IUserService>().InstancePerLifetimeScope();
            builder.RegisterType<HttpCurrentRequest>().As<ICurrentRequest>().InstancePerLifetimeScope();

            builder.RegisterType<JwtTokenIssuer>().As<ITokenIssuer>().InstancePerLifetimeScope();

            //获取需要注入对象的程序集
            var service = typeof(UserService).Assembly;
            var data = typeof(My.XXX.Persistence.DBContext).Assembly;

            //Service & Repository 根据继承的接口实现自动注入(IScopeDependency)
            builder.RegisterAssemblyTypes(data, service, typeof(HttpService).Assembly).Where(m => m.IsAssignableTo(typeof(IScopeDependency)))
                    .AsImplementedInterfaces().InstancePerLifetimeScope();
        }
    }
}
