namespace Fountain
{
    using System;
    using Autofac;

    class Program
    {
        static void Main(string[] args)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule<Module>();
            builder.RegisterInstance(new CommandLineArgs(args));
            using var container = builder.Build();
            var app = container.Resolve<App>();
            try
            {
                app.Run();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.Message);
                Environment.ExitCode = 1;
            }
        }
    }
}