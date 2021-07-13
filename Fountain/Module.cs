namespace Fountain
{
    using System;
    using System.Diagnostics.CodeAnalysis;
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
            builder.Register<CoefficientsFactory>(_ =>
            {
                var random = new Random();
                return new CoefficientsFactory(
                    () => random.Next() % 2 == 0,
                    () => random.Next()
                );
            });
        }
    }
}