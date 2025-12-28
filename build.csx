#!/usr/bin/env dotnet-script

#r "nuget: System.CommandLine, 2.0.1"
#r "nuget: Bitwarden.Secrets.Sdk, 1.0.0"

#load "./CronJob.Template.Infra/Bitwarden.cs"
#load "./CronJob.Template.Common/Constants.cs"

using System.CommandLine;
using System.Diagnostics;

static readonly string CWD = System.IO.Directory.GetCurrentDirectory();
static readonly string MAIN_PROJECT_DIR = System.IO.Path.Combine(CWD, Constants.PROJECT_NAME);
static readonly string INFRA_PROJECT_DIR = System.IO.Path.Combine(CWD, Constants.INFRA_PROJECT_NAME);
static readonly string BUILD_OUTPUT_DIR = System.IO.Path.Combine(CWD, ".build_output");
static readonly string TOOL_DIR = System.IO.Path.Combine(BUILD_OUTPUT_DIR, "tools");

static Bw bitwarden = null;

Option<bool> dotnetBuildOption = new("--dotnet-build")
{
    Description = "Build the .NET Project"
};

Option<bool> dotnetRunOption = new("--dotnet-run")
{
    Description = "Run the .NET Project"
};

Option<bool> dotnetTestOption = new("--dotnet-test")
{
    Description = "Run .NET Tests"
};

Option<bool> dockerBuildOption = new("--docker-build")
{
    Description = "Build the Docker Image"
};

Option<bool> dockerRunOption = new("--docker-run")
{
    Description = "Run the Docker Image"
};

Option<string> dockerTagOption = new("--docker-tag")
{
    Description = "Tag for the Docker Image",
    DefaultValueFactory = parseResult => "local"
};

Option<bool> dockerPushOption = new("--docker-push")
{
    Description = "Push the Docker Image to the registry"
};

Option<bool> pulumiPreviewOption = new("--pulumi-preview")
{
    Description = "Run Pulumi Preview"
};

Option<bool> pulumiDeployOption = new("--pulumi-deploy")
{
    Description = "Run Pulumi Deploy"
};

Option<bool> localOption = new("--local")
{
    Description = "Run all local build steps (dotnet build, docker build with 'local' tag)"
};

Option<bool> ciOption = new("--ci")
{
    Description = "Run all CI build steps (dotnet build, docker build with build number tag, docker push, and pulumi deploy). This requires the --docker-tag flag"
};

Option<bool> generateEnvOption = new("--generate-env")
{
    Description = "Generate environment files"
};

RootCommand root = new($"Build and Deploy script for {Constants.PROJECT_NAME}");
root.Options.Add(dotnetBuildOption);
root.Options.Add(dotnetRunOption);
root.Options.Add(dotnetTestOption);
root.Options.Add(dockerBuildOption);
root.Options.Add(dockerRunOption);
root.Options.Add(dockerTagOption);
root.Options.Add(dockerPushOption);
root.Options.Add(pulumiPreviewOption);
root.Options.Add(pulumiDeployOption);
root.Options.Add(localOption);
root.Options.Add(ciOption);
root.Options.Add(generateEnvOption);

root.SetAction(parseResult =>
{
    bool dotnetBuild = parseResult.GetValue(dotnetBuildOption);
    bool dotnetRun = parseResult.GetValue(dotnetRunOption);
    bool dotnetTest = parseResult.GetValue(dotnetTestOption);
    bool dockerBuild = parseResult.GetValue(dockerBuildOption);
    bool dockerRun = parseResult.GetValue(dockerRunOption);
    string dockerTag = parseResult.GetValue(dockerTagOption);
    bool dockerPush = parseResult.GetValue(dockerPushOption);
    bool pulumiPreview = parseResult.GetValue(pulumiPreviewOption);
    bool pulumiDeploy = parseResult.GetValue(pulumiDeployOption);
    bool local = parseResult.GetValue(localOption);
    bool ci = parseResult.GetValue(ciOption);
    bool generateEnv = parseResult.GetValue(generateEnvOption);

    if (dotnetBuild || local || ci)
        DotnetBuild();
    if (dotnetRun)
        DotnetRun();
    if (dotnetTest || local || ci)
        DotnetTest();
    if (dockerBuild || dockerRun || local || ci)
        DockerBuild(dockerTag);
    if (dockerRun)
        DockerRun(dockerTag);
    if (dockerPush || ci)
        DockerPush(dockerTag);
    if (pulumiPreview || local)
        PulumiPreview(dockerTag);
    if (pulumiDeploy || ci)
        PulumiDeploy(dockerTag);
    if (generateEnv)
        GenerateEnvFile();

    return 0;
});

ParseResult parseResult = root.Parse(Args.ToArray());
return parseResult.Invoke();

# region Build and Deploy Steps

static void DotnetBuild() => ExecStage(".NET Build", () =>
{
    RunCommand("dotnet", $"build", enableColors: true);
});

static void DotnetRun() => ExecStage(".NET Run", () =>
{
    string envFile = System.IO.Path.Combine(CWD, Constants.PROJECT_NAME ,".env");
    if (!System.IO.File.Exists(envFile))
    {
        Console.WriteLine($"Env file '{envFile}' not found. Generating a new one.");
        GenerateEnvFile();
    }

    RunCommand("dotnet", $"run --project \"{MAIN_PROJECT_DIR}\"", enableColors: true);
});

static void DotnetTest() => ExecStage(".NET Test", () =>
{
    RunCommand("dotnet", $"test", enableColors: true);
});

static void DockerBuild(string dockerTag) => ExecStage("Docker Build", () =>
{
    string imageName = dockerTag == "local"
        ? ComputeLocalContainerImageName(dockerTag)
        : ComputeFullContainerImageName(dockerTag);

    RunCommand("docker", $"build -t {imageName} -f \"{MAIN_PROJECT_DIR}/Dockerfile\" \"{CWD}\"");
});

static void DockerRun(string dockerTag) => ExecStage("Docker Run", () =>
{
    string imageName = dockerTag == "local"
        ? ComputeLocalContainerImageName(dockerTag)
        : ComputeFullContainerImageName(dockerTag);

    string envFile = System.IO.Path.Combine(CWD, Constants.PROJECT_NAME ,".env");
    if (!System.IO.File.Exists(envFile))
    {
        Console.WriteLine($"Env file '{envFile}' not found. Generating a new one.");
        //GenerateEnvFile();
    }

    RunCommand("docker", $"run --rm -it --env-file {envFile} {imageName}");
});

static void DockerPush(string dockerTag) => ExecStage("Docker Push", () =>
{
    if (dockerTag == "local")
        throw new ArgumentException("Cannot push a docker image with the 'local' tag to the registry");

    InitBitwarden();

    string imageName = ComputeFullContainerImageName(dockerTag);
    string dockerPassword = bitwarden.GetSecret(Constants.GITEA_PAT);
    RunCommand("docker", $"login --username \"{Constants.DOCKER_USERNAME}\" --password-stdin {Constants.DOCKER_REGISTRY}", dockerPassword);
    RunCommand("docker", $"push {imageName}");
});

static void PulumiPreview(string dockerTag) => ExecStage("Pulumi Preview", () =>
{
    Dictionary<string, string> env = InitPulumi();

    RunCommand("pulumi", $"login --non-interactive", environmentVariables: env);
    RunCommand("pulumi", $"stack select dev", workingDirectory: INFRA_PROJECT_DIR, environmentVariables: env);
    RunCommand("pulumi", $"--non-interactive config -s dev set {Constants.BUILD_NUMBER_CONFIG_KEY} \"{dockerTag}\"", workingDirectory: INFRA_PROJECT_DIR, environmentVariables: env);
    RunCommand("pulumi", $"preview", workingDirectory: INFRA_PROJECT_DIR, environmentVariables: env);
});

static void PulumiDeploy(string dockerTag) => ExecStage("Pulumi Deploy", () =>
{
    Dictionary<string, string> env = InitPulumi();

    string pulumiPath = $"{Environment.GetEnvironmentVariable("HOME")}/.pulumi/bin/pulumi";
    RunCommand(pulumiPath, $"login --non-interactive", environmentVariables: env);
    RunCommand(pulumiPath, $"stack select dev", workingDirectory: INFRA_PROJECT_DIR, environmentVariables: env);
    RunCommand(pulumiPath, $"--non-interactive config -s dev set {Constants.BUILD_NUMBER_CONFIG_KEY} \"{dockerTag}\"", workingDirectory: INFRA_PROJECT_DIR, environmentVariables: env);
    RunCommand(pulumiPath, $"up --yes", workingDirectory: INFRA_PROJECT_DIR, environmentVariables: env);
});

static void GenerateEnvFile() => ExecStage("Generate Env File", () =>
{
    InitBitwarden();

    string slackWebhookKey = $"dev-{Constants.CONTAINER_NAME}-slack-webhook-url";

    //Either pull from pulumi stack ref or bitwarden and populate env file
    Dictionary<string, string> envVars = new()
    {
        [Constants.SLACK_LOGGER_WEBHOOK_URL] = GetPulumiStackRefOutput(Constants.DISCORD_STACK_REF_NAME, Constants.DEV_LOGGER_WEBHOOK_URL_KEY),
        //[Constants.MY_BITWARDEN_SECRET_KEY] = bitwarden.GetSecret(Constants.MY_BITWARDEN_SECRET_KEY),
    };

    string envFilePath = System.IO.Path.Combine(CWD, Constants.PROJECT_NAME ,".env");

    if (System.IO.File.Exists(envFilePath))
        System.IO.File.Delete(envFilePath);

    using System.IO.StreamWriter file = new(envFilePath);
    foreach (var kvp in envVars)
        file.WriteLine($"{kvp.Key}={kvp.Value}");
});

# endregion

# region Utility Functions

static string ComputeFullContainerImageName(string tag) =>
    $"code.msyke.dev/private/{Constants.CONTAINER_NAME}:{tag}";

static string ComputeLocalContainerImageName(string tag) =>
    $"{Constants.CONTAINER_NAME}:{tag}";

static void ExecStage(string stageName, Action stageAction)
{
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine($"\n=========== Starting stage: {stageName} ===========\n");
    Console.ResetColor();

    stageAction();

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine($"\n=========== Finished stage: {stageName} ===========\n");
    Console.ResetColor();
}

static Dictionary<string, string> InitPulumi()
{
    InitBitwarden();

    if (!System.IO.File.Exists($"{Environment.GetEnvironmentVariable("HOME")}/.pulumi/bin/pulumi"))
    {
        RunCommand("bash", $"-c \"curl -fsSL https://get.pulumi.com/ | sh\"");
    }

    Dictionary<string, string> envVars = new()
    {
        { Constants.PULUMI_ACCESS_TOKEN, bitwarden.GetSecret(Constants.PULUMI_ACCESS_TOKEN) }
    };

    return envVars;
}

static void InitBitwarden()
{
    if (bitwarden is null)
        bitwarden = new Bw();
}

static void InitBuildOutputDirs()
{
    if (!System.IO.Directory.Exists(BUILD_OUTPUT_DIR))
        System.IO.Directory.CreateDirectory(BUILD_OUTPUT_DIR);
}

static void InitToolsDir()
{
    InitBuildOutputDirs();

    if (!System.IO.Directory.Exists(TOOL_DIR))
        System.IO.Directory.CreateDirectory(TOOL_DIR);
}

static void RunCommand(string command,
                       string args,
                       string input = null,
                       string workingDirectory = null,
                       bool enableColors = false,
                       Dictionary<string, string> environmentVariables = null)
{
    using Process process = new();
    process.StartInfo.FileName = command;
    process.StartInfo.Arguments = args;
    process.StartInfo.RedirectStandardOutput = true;
    process.StartInfo.RedirectStandardError = true;
    process.StartInfo.UseShellExecute = false;
    process.StartInfo.CreateNoWindow = true;

    if (enableColors && command.Contains("dotnet"))
        process.StartInfo.EnvironmentVariables["DOTNET_SYSTEM_CONSOLE_ALLOW_ANSI_COLOR_REDIRECTION"] = "true";

    if (workingDirectory is not null)
        process.StartInfo.WorkingDirectory = workingDirectory;

    if (input is not null)
        process.StartInfo.RedirectStandardInput = true;

    if (environmentVariables is not null)
        foreach (var kvp in environmentVariables)
            process.StartInfo.EnvironmentVariables[kvp.Key] = kvp.Value;

    process.OutputDataReceived += (sender, e) =>
    {
        if (!string.IsNullOrEmpty(e.Data))
            Console.WriteLine(e.Data);
    };

    process.ErrorDataReceived += (sender, e) =>
    {
        if (!string.IsNullOrEmpty(e.Data))
            Console.Error.WriteLine(e.Data);
    };

    process.Start();

    if (input is not null)
    {
        process.StandardInput.WriteLine(input);
        process.StandardInput.Close();
    }

    process.BeginOutputReadLine();
    process.BeginErrorReadLine();

    process.WaitForExit();

    if (process.ExitCode != 0)
        throw new Exception($"Command '{command} {args}' failed with exit code {process.ExitCode}");
}

static string GetPulumiStackRefOutput(string stackName, string outputKey)
{
    using Process process = new();
    process.StartInfo.FileName = "pulumi";
    process.StartInfo.Arguments = $"stack output --stack {stackName} {outputKey} --json --show-secrets";
    process.StartInfo.RedirectStandardOutput = true;
    process.StartInfo.UseShellExecute = false;
    process.StartInfo.CreateNoWindow = true;

    process.Start();

    string output = process.StandardOutput.ReadToEnd();
    process.WaitForExit();

    if (process.ExitCode != 0)
        throw new Exception($"Failed to get Pulumi stack output '{outputKey}' from stack '{stackName}'");

    return output.Trim().Trim('"');
}

# endregion
