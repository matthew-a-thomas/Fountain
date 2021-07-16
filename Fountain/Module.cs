namespace Fountain
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Security.Cryptography;
    using Autofac;

    class Module : Autofac.Module
    {
        [SuppressMessage("ReSharper", "RedundantTypeArgumentsOfMethod")]
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<App>();
            builder.RegisterType<CommandLineParser>();
            builder.RegisterType<FountainFileDecoder>();
            builder.RegisterType<FountainFileEncoder>();
            builder.RegisterType<FountainFileMerger>();
            builder.RegisterType<FountainFileShrinker>();
            builder.RegisterType<FountainFileInfoProvider>();
            builder.Register<CoefficientsFactory>(_ =>
            {
                return new CoefficientsFactory(
                    () => RandomNumberGenerator.GetInt32(int.MaxValue) % 2 == 0,
                    () => RandomNumberGenerator.GetInt32(int.MaxValue)
                );
            });
            builder.RegisterType<OverviewReader>();
        }
    }
}