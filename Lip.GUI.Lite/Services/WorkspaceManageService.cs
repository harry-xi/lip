using Flurl;
using Lip.Context;
using Lip.Core;
using Lip.GUI.Lite.Models;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;

namespace Lip.GUI.Lite.Services
{

    public class WorkspaceManageService
    {
        public event Action? WorkspaceChanged;
        public Workspace? CurrentWorkspace;
        public List<Workspace> Workspaces = [];
        private readonly ConfigService ConfigService;

        public WorkspaceManageService(ConfigService configService)
        {
            ConfigService = configService;
            var config = ConfigService.Config;
            CurrentWorkspace = config.Workspaces.Where(workspace => workspace.Path == config.LastWorkspacePath)
                .FirstOrDefault() ?? config.Workspaces.FirstOrDefault();
            Workspaces = config.Workspaces;
        }

        public void AddWorkspace(Workspace ws)
        {
            if (!Workspaces.Contains(ws))
            {
                Workspaces.Add(ws);
                ConfigService.Config.Workspaces = Workspaces;
                _ = ConfigService.Save();
            }
        }

        public void RemoveWorkspace(Workspace ws)
        {
            if (Workspaces.Contains(ws))
            {
                bool wasCurrent = ws == CurrentWorkspace;
                Workspaces.Remove(ws);
                if (wasCurrent)
                {
                    CurrentWorkspace = Workspaces.FirstOrDefault();
                }
                ConfigService.Config.Workspaces = Workspaces;
                WorkspaceChanged?.Invoke();
                _ = ConfigService.Save();
            }
        }

        public void SwitchWorkspace(Workspace ws)
        {
            if (Workspaces.Contains(ws) && ws != CurrentWorkspace)
            {
                CurrentWorkspace = ws;
                ConfigService.Config.LastWorkspacePath = ws.Path;
                WorkspaceChanged?.Invoke();
                _ = ConfigService.Save();
            }
        }

        private async Task<Core.Lip> BuildLip(Workspace workspace, IUserInteraction userInteraction, ILogger logger)
        {
            return Core.Lip.Create(
                ConfigService.RuntimeConfig,
                await BuildContext(workspace, userInteraction, logger)
            );
        }

        private static async Task<Context.Context> BuildContext(Workspace workspace, IUserInteraction userInteraction, ILogger logger)
        {
            return new Context.Context
            {
                CommandRunner = new CommandRunner(),
                Downloader = new Context.Downloader(userInteraction),
                FileSystem = new FileSystem(),
                Git = await StandaloneGit.Create(),
                Logger = logger,
                UserInteraction = userInteraction,
                WorkingDir = workspace.Path
            };
        }

        public async Task<Core.Lip?> GetCurrentLip(IUserInteraction userInteraction, ILogger logger)
        {
            return CurrentWorkspace != null ? await BuildLip(CurrentWorkspace, userInteraction, logger) : null;
        }

        public async Task<ToolClasses?> GetCurrentToolClass(IUserInteraction userInteraction, ILogger logger)
        {
            return CurrentWorkspace != null ? CreateToolClass(ConfigService.RuntimeConfig, await BuildContext(CurrentWorkspace, userInteraction, logger)) : null;
        }

        private static ToolClasses CreateToolClass(RuntimeConfig runtimeConfig, IContext context)
        {
            List<Url> gitHubProxies = runtimeConfig.GitHubProxies.ConvertAll(url => new Url(url));
            List<Url> goModuleProxies = runtimeConfig.GoModuleProxies.ConvertAll(url => new Url(url));

            PathManager pathManager = new(context.FileSystem, runtimeConfig.Cache, context.WorkingDir);
            CacheManager cacheManager = new(context, pathManager, gitHubProxies, goModuleProxies);
            PackageManager packageManager = new(context, cacheManager, pathManager, gitHubProxies, goModuleProxies);
            DependencySolver dependencySolver = new(context, packageManager);

            return new ToolClasses(cacheManager, dependencySolver, packageManager, pathManager);
        }

        public record ToolClasses(
            CacheManager CacheManager,
            DependencySolver DependencySolver,
            PackageManager PackageManager,
            PathManager PathManager
        );
    }
}