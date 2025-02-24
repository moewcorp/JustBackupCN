using Dalamud.Interface.Components;
using ECommons.Funding;
using ECommons.GameHelpers;
using ECommons.Logging;
using System.IO;

namespace JustBackup;

internal class ConfigWindow : Window
{
    private JustBackup p;
    private string newIgnoredFile = string.Empty;

    public ConfigWindow(JustBackup p) : base("JustBackup configuration")
    {
        this.p = p;
    }

    void Settings()
    {
        ImGuiEx.LineCentered("restore", () =>
        {
            ImGuiEx.WithTextColor(ImGuiColors.DalamudOrange, delegate
            {
                if (ImGui.Button("如何还原你的设置"))
                {
                    ShellStart("https://github.com/NightmareXIV/JustBackup/blob/master/README.md#restoring-a-backup");
                }
            });
        });
        ImGuiEx.Text(@"自定义备份路径 (默认: %localappdata%\JustBackup):");
        ImGui.SetNextItemWidth(400f);
        ImGui.InputText("##PathToBkp", ref p.config.BackupPath, 100);
        ImGuiEx.Text(@"自定义临时文件路径 (默认: %temp%):");
        ImGui.SetNextItemWidth(400f);
        ImGui.InputText("##PathToTmp", ref p.config.TempPath, 100);
        ImGui.Checkbox("自动移除旧的备份文件", ref p.config.DeleteBackups);
        if (p.config.DeleteBackups)
        {
            ImGui.SetNextItemWidth(50f);
            ImGui.DragInt("删除超过指定天数的备份", ref p.config.DaysToKeep, 0.1f, 3, 730);
            if (p.config.DaysToKeep < 3) p.config.DaysToKeep = 3;
            if (p.config.DaysToKeep > 730) p.config.DaysToKeep = 730;
            ImGui.Checkbox("如果可以移动到回收站", ref p.config.DeleteToRecycleBin);
            ImGui.SetNextItemWidth(50f);
            ImGui.DragInt("保留指定份数的备份(忽略时间)", ref p.config.BackupsToKeep, 0.1f, 10, 100000);
            if (p.config.BackupsToKeep < 0) p.config.BackupsToKeep = 0;
            ImGui.Separator();
        }
        ImGui.Checkbox("备份插件配置文件", ref p.config.BackupPluginConfigs);
        ImGui.Checkbox("备份所有FFXIV数据文件夹中的文件", ref p.config.BackupAll);
        ImGuiEx.Text("  (否则只会保存配置文件，截图、日志等将被跳过)");
        ImGui.Checkbox($"不备份录像文件", ref p.config.ExcludeReplays);
        ImGui.Checkbox("使用内部7ZIP方案", ref p.config.UseDefaultZip);
        if (p.config.UseDefaultZip) ImGuiEx.Text(ImGuiColors.DalamudRed, "7-Zip 压缩的文件占用空间最多可减少 15 倍！");
        ImGui.Checkbox("不限制 7-Zip 可使用的资源数量。", ref p.config.NoThreadLimit);
        ImGui.SetNextItemWidth(100f);
        ImGui.SliderInt($"备份之间的最小间隔 (分钟)", ref p.config.MinIntervalBetweenBackups, 0, 60);
        ImGuiComponents.HelpMarker("如果上次备份创建时间少于此分钟数，则不会创建新的备份。请注意，只有成功完成的备份才会更新间隔。");
    }

    void Tools()
    {
        if (ImGui.Button("打开备份文件夹"))
        {
            ShellStart(p.GetBackupPath());
        }
        ImGuiEx.WithTextColor(ImGuiColors.DalamudOrange, delegate
        {
            if (ImGui.Button("Read how to restore a backup"))
            {
                ShellStart("https://github.com/NightmareXIV/JustBackup/blob/master/README.md#restoring-a-backup");
            }
        });
        if (ImGui.Button("打开FFXIV数据文件夹"))
        {
            ShellStart(p.GetFFXIVConfigFolder());
        }
        if (ImGui.Button("打开插件配置文件夹"))
        {
            ShellStart(JustBackup.GetPluginsConfigDir().FullName);
        }
        if (Svc.ClientState.LocalPlayer != null)
        {
            if (ImGui.Button("打开当前角色配置文件夹"))
            {
                ShellStart(Path.Combine(p.GetFFXIVConfigFolder(), $"FFXIV_CHR{Svc.ClientState.LocalContentId:X16}"));
            }
            if (ImGui.Button("添加识别信息"))
            {
                Safe(() =>
                {
                    var fname = Path.Combine(p.GetFFXIVConfigFolder(), $"FFXIV_CHR{Svc.ClientState.LocalContentId:X16}",
                        $"_{Player.NameWithWorld}.dat");
                    File.Create(fname).Dispose();
                    Notify.Success("已为当前角色添加识别信息");
                }, (e) =>
                {
                    Notify.Error("添加当前角色的识别信息时出错:\n" + e);
                });
            }
            ImGuiEx.Tooltip("将一个空文件添加到角色的配置目录中\n" +
				"文件包含角色的名称和主世界");
        }
    }


    void Ignored()
    {
        var id = 0;
        foreach (var file in p.config.Ignore.ToArray())
        {
            if (ImGui.SmallButton($"x##{id++}"))
            {
                p.config.Ignore.Remove(file);
            }
            ImGui.SameLine();
            ImGui.Text(file);
        }

        if (ImGui.SmallButton("+"))
        {
            if (!p.config.Ignore.Contains(newIgnoredFile, StringComparer.InvariantCultureIgnoreCase))
            {
                p.config.Ignore.Add(newIgnoredFile);
                newIgnoredFile = string.Empty;
            }
        }
        ImGui.SameLine();

        ImGui.InputText("略过路径", ref newIgnoredFile, 512);
    }

    void Expert()
    {
        ImGuiEx.Text($"覆盖游戏配置文件夹路径:");
        ImGuiEx.SetNextItemFullWidth();
        ImGui.InputText($"##pathGame", ref p.config.OverrideGamePath, 2000);
        ImGui.SetNextItemWidth(150f);
        ImGui.InputInt("最大线程", ref p.config.MaxThreads.ValidateRange(1, 99), 1, 99);
        ImGui.SetNextItemWidth(100f);
        ImGui.SliderInt($"限制复制速度，单位为毫秒", ref p.config.CopyThrottle.ValidateRange(0, 50), 0, 5);
        ImGuiComponents.HelpMarker("这个值越高，备份创建所需的时间越长，但你的 SSD/HDD 负载越低。如果在备份过程中遇到延迟，可以增大这个值.");
    }

    public override void Draw()
    {
        PatreonBanner.DrawRight();
        ImGuiEx.EzTabBar("default", PatreonBanner.Text,
            ("设置", Settings, null, true),
            ("工具", Tools, null, true),
            ("忽略路径 (beta)", Ignored, null, true),
            ("导出设置", Expert, null, true),
            InternalLog.ImGuiTab()
            );
                   
    }

    public override void OnClose()
    {
        Svc.PluginInterface.SavePluginConfig(p.config);
        Notify.Success("配置已经保存");
        base.OnClose();
    }
}
