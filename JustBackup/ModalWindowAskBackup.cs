using Dalamud.Interface.Utility;
using ECommons;
using ECommons.Logging;
using System.Diagnostics;
using System.IO;

namespace JustBackup;

internal class ModalWindowAskBackup : Window
{
    public ModalWindowAskBackup() : base("JustBackup - another instance is already running", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.AlwaysAutoResize, true)
    {
        this.IsOpen = false;
        this.RespectCloseHotkey = false;
        this.ShowCloseButton = false;
    }

    public override void Draw()
    {
        ImGuiEx.Text($"另一个备份正在进行中。\n在上一个备份尚未完成时尝试创建新备份，可能会导致两个备份都无法正确完成。");
        if (ImGui.Button($"强制停止旧备份并创建新备份"))
        {
            try
            {
                var FileName = Path.Combine(Svc.PluginInterface.AssemblyLocation.DirectoryName, "7zr.exe");
                foreach (var x in Process.GetProcesses().Where(x => x.ProcessName.Contains("7zr")))
                {
                    if (x.MainModule.FileName == FileName)
                    {
                        PluginLog.Information($"Terminating process {x.ProcessName} ({x.Id})");
                        x.Kill();
                    }
                }
                (ECommonsMain.Instance as JustBackup).DoBackupInternal();
                this.IsOpen = false;
            }
            catch(Exception ex )
            {
                ex.Log();
                Notify.Error("Could not terminate old backup");
            }
        }
        if(ImGui.Button($"不要启动新备份，等待旧备份完成"))
        {
            this.IsOpen = false;
        }
        if(ImGui.Button($"（不推荐）仍然运行新备份"))
        {
            (ECommonsMain.Instance as JustBackup).DoBackupInternal();
            this.IsOpen = false;
        }
        this.Position = (ImGuiHelpers.MainViewport.Size / 2) - ImGui.GetWindowSize() / 2;
    }
}
