using Autofac;
using FluentValidation;
using My.XXX.Infra;
using My.XXX.Service;
using My.XXX.Service.DTOs;
using My.XXX.Service.Interfaces;
using My.XXX.Service.Validators;
using System;
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

            //获取需要注入对象的程序集
            var service = typeof(UserService).Assembly;
            var data = typeof(My.XXX.Data.DBContext).Assembly;

            //Service & Repository 根据继承的接口实现自动注入(IScopeDependency)
            builder.RegisterAssemblyTypes(data, service).Where(m => m.IsAssignableTo(typeof(IScopeDependency)))
                    .AsImplementedInterfaces().InstancePerLifetimeScope();
        }
    }
}