using System;
using Pulumi;
using Pulumi.Kubernetes.Batch.V1;
using Pulumi.Kubernetes.Types.Inputs.Batch.V1;
using Pulumi.Kubernetes.Types.Inputs.Core.V1;
using Pulumi.Kubernetes.Types.Inputs.Meta.V1;

return await Deployment.RunAsync(() =>
{
    Config config = new Config();
    Bw bw = new();

    Pulumi.Kubernetes.Provider k8sProvider = new("k8s-provider", new Pulumi.Kubernetes.ProviderArgs
    {
        KubeConfig = bw.GetSecret(Constants.DEV_KUBECONFIG)
    });

    StackReference discordConfigRef = new(Constants.DISCORD_STACK_REF_NAME);

    string stackName = Deployment.Instance.StackName;

    EnvValues env = stackName switch
    {
        "prod" => new EnvValues(
            DotnetEnvironment: "Production",
            LoggerWebhookUrl: discordConfigRef.GetOutput(Constants.PROD_LOGGER_WEBHOOK_URL_KEY)
                .Apply(url => url?.ToString() ?? throw new ArgumentNullException("Discord Webhook URL", "Discord Webhook URL not found in the referenced stack")),
            Namespace: "prod"
        ),
        "dev" => new EnvValues(
            DotnetEnvironment: "Development",
            LoggerWebhookUrl: discordConfigRef.GetOutput(Constants.DEV_LOGGER_WEBHOOK_URL_KEY)
                .Apply(url => url?.ToString() ?? throw new ArgumentNullException("Discord Webhook URL", "Discord Webhook URL not found in the referenced stack")),
            Namespace: "default"
        ),
        _ => throw new ArgumentException($"Unknown stack name: {stackName}. Expected 'prod' or 'dev'.")
    };

    string? buildNumber = config.Get(Constants.BUILD_NUMBER_CONFIG_KEY) ?? throw new ArgumentNullException(Constants.BUILD_NUMBER_CONFIG_KEY, "You must provid 'buildNumber' as a configuration parameter");
    CronJob job = new CronJob($"cron-job-{Constants.CONTAINER_NAME}", new CronJobArgs
    {
        Metadata = new ObjectMetaArgs
        {
            Name = Constants.CONTAINER_NAME,
            Namespace = env.Namespace,
        },
        Spec = new CronJobSpecArgs
        {
            Schedule = "35 10 * * *", //Runs at 2:35 AM PST (4:35 UTC) every day
            JobTemplate = new JobTemplateSpecArgs
            {
                Spec = new JobSpecArgs
                {
                    Template = new PodTemplateSpecArgs
                    {
                        Spec = new PodSpecArgs
                        {
                            Containers =
                            {
                                new ContainerArgs
                                {
                                    Name = Constants.CONTAINER_NAME,
                                    Image = $"{Constants.DOCKER_REGISTRY}:{buildNumber}",
                                    Env =
                                    {
                                        new EnvVarArgs
                                        {
                                            Name = Constants.DOTNET_ENVIRONMENT,
                                            Value = env.DotnetEnvironment
                                        },
                                        new EnvVarArgs
                                        {
                                            Name = Constants.SLACK_LOGGER_WEBHOOK_URL,
                                            Value = env.LoggerWebhookUrl
                                        }
                                    }
                                }
                            },
                            RestartPolicy = "OnFailure",
                            ImagePullSecrets = 
                            {
                                new LocalObjectReferenceArgs
                                {
                                    Name = Constants.IMAGE_PULL_SECRET_NAME
                                }
                            }
                        }
                    }
                }
            }

        }
    },
    new CustomResourceOptions
    {
        Provider = k8sProvider,
        Protect = true
    });
});

public record EnvValues(
    string DotnetEnvironment,
    Pulumi.Input<string> LoggerWebhookUrl,
    string Namespace
);
