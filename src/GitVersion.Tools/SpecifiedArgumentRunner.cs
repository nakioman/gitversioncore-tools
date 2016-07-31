using System;
using System.IO;
using GitTools;
using GitVersion.Helpers;
using Newtonsoft.Json.Linq;

namespace GitVersion.Tools
{
    class SpecifiedArgumentRunner
    {
        public static void Run(Arguments arguments, IFileSystem fileSystem)
        {
            Logger.WriteInfo("Running on .NET Core");

            var noFetch = arguments.NoFetch;
            var authentication = arguments.Authentication;
            var targetPath = arguments.TargetPath;
            var targetUrl = arguments.TargetUrl;
            var dynamicRepositoryLocation = arguments.DynamicRepositoryLocation;
            var targetBranch = arguments.TargetBranch;
            var commitId = arguments.CommitId;
            var overrideConfig = arguments.HasOverrideConfig ? arguments.OverrideConfig : null;

            var executeCore = new ExecuteCore(fileSystem);
            var variables = executeCore.ExecuteGitVersion(targetUrl, dynamicRepositoryLocation, authentication, targetBranch, noFetch, targetPath, commitId, overrideConfig);

            if (arguments.Output == OutputType.BuildServer)
            {
                foreach (var buildServer in BuildServerList.GetApplicableBuildServers())
                {
                    buildServer.WriteIntegration(Console.WriteLine, variables);
                }
            }

            if (arguments.Output == OutputType.Json)
            {
                switch (arguments.ShowVariable)
                {
                    case null:
                        Console.WriteLine(JsonOutputFormatter.ToJson(variables));
                        break;

                    default:
                        string part;
                        if (!variables.TryGetValue(arguments.ShowVariable, out part))
                        {
                            throw new WarningException(string.Format("'{0}' variable does not exist", arguments.ShowVariable));
                        }
                        Console.WriteLine(part);
                        break;
                }
            }

            File.WriteAllText("gitversion.json", JsonOutputFormatter.ToJson(variables));

            if (arguments.UpdateAssemblyInfo)
            {
                var filename = "project.json";
                var projectJson = JObject.Parse(File.ReadAllText(filename));
                projectJson["version"] = variables.SemVer;
                File.WriteAllText(filename, projectJson.ToString());
            }
        }
    }
}