public class Constants
{
    // General Constants
    public const string PROJECT_NAME = "CronJob.Template";
    public const string INFRA_PROJECT_NAME = $"{PROJECT_NAME}.Infra";
    public const string CONTAINER_NAME = "dotnetcronjobtemplate";
    public const string DOCKER_USERNAME = "mSyke";
    public const string DOCKER_REGISTRY = $"code.msyke.dev/private/{CONTAINER_NAME}";
    public const string IMAGE_PULL_SECRET_NAME = "gitea-image-pull-secret";

    // Environment Variables
    public const string DOTNET_ENVIRONMENT = nameof(DOTNET_ENVIRONMENT);
    public const string PULUMI_ACCESS_TOKEN = nameof(PULUMI_ACCESS_TOKEN);
    public const string SLACK_LOGGER_WEBHOOK_URL = nameof(SLACK_LOGGER_WEBHOOK_URL);
    // Your other environment variables here...
    
    // Secret Keys
    public const string GITEA_PAT = nameof(GITEA_PAT);
    public const string DEV_KUBECONFIG = nameof(DEV_KUBECONFIG);

    // Pulumi Constants
    public const string PULUMI_ORG = "moutansos";
    public const string DEV_LOGGER_WEBHOOK_URL_KEY = "<<DEV_LOGGER_WEBHOOK_URL_KEY>>"; //TODO: Maybe populate this with input from user in setup.sh?
    public const string PROD_LOGGER_WEBHOOK_URL_KEY = "<<PROD_LOGGER_WEBHOOK_URL_KEY>>"; //TODO: Maybe populate this with input from user in setup.sh?
    public const string DISCORD_STACK_REF_NAME = $"{PULUMI_ORG}/Bbt.Config.Discord/prod";
    public const string BUILD_NUMBER_CONFIG_KEY = "buildNumber";
}
