using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
namespace CaptureService.Instruments
{
    public class Containerable
    {
        IContainer container;
        const string imageName = "transcriber";

        protected async Task<int> InitializeContainer(string pathToDockerFileDirectory)
        {
            Console.WriteLine("Building the docker image, this will probably take a while.\n\n");
            await BuildImage(pathToDockerFileDirectory);
            Console.WriteLine("Done!");
            Console.WriteLine("\n\nLaunching a container, this shouldn't take too long.\n\n");
            int hostPort = await BuildContainer();
            Console.WriteLine("Done!");
            return hostPort;
        }
        async Task BuildImage(string pathToDockerFileDirectory)
        {
            var futureImage = new ImageFromDockerfileBuilder()
                .WithCleanUp(false)
                .WithImageBuildPolicy(PullPolicy.Missing)
                .WithDockerfileDirectory(pathToDockerFileDirectory)
                .WithDockerfile("Dockerfile")
                .WithName(imageName)
                .Build();

            await futureImage.CreateAsync();
        }
        async Task<int> BuildContainer()
        {
            var _container = new ContainerBuilder()
                .WithPortBinding(8000, true)
                .WithImage(imageName)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(8000))
                .Build();

            // Start the container.
            await _container.StartAsync();

            container = _container;
            return container.GetMappedPublicPort();
        }
    }
}
