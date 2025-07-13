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
                WorkspaceChanged?.Invoke();
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
            }
        }

        public void SwitchWorkspace(Workspace ws)
        {
            if (Workspaces.Contains(ws) && ws != CurrentWorkspace)
            {
                CurrentWorkspace = ws;
                ConfigService.Config.LastWorkspacePath = ws.Path;
                WorkspaceChanged?.Invoke();
            }
        }

        public async Task<Core.Lip> BuildLip(Workspace workspace, IUserInteraction userInteraction, ILogger logger)
        {
            return Core.Lip.Create(
                ConfigService.RuntimeConfig,
                new Context.Context
                {
                    CommandRunner = new CommandRunner(),
                    Downloader = new Context.Downloader(userInteraction),
                    FileSystem = new FileSystem(),
                    Git = await StandaloneGit.Create(),
                    Logger = logger,
                    UserInteraction = userInteraction,
                    WorkingDir = workspace.Path
                }
            );
        }

        public async Task<Core.Lip?> BuildCurrentLip(IUserInteraction userInteraction, ILogger logger)
        {
            return CurrentWorkspace != null ? await BuildLip(CurrentWorkspace, userInteraction, logger) : null;
        }
    }
}