using System.Diagnostics;
using System.Net.NetworkInformation;

namespace PingTools
{
    internal sealed class ToolsForm : Form
    {
        private readonly Form1 ownerForm;

        private ListView adaptersListView = null!;
        private Button pingGatewayButton = null!;
        private Button refreshAdaptersButton = null!;
        private Button openAdaptersButton = null!;
        private Button openSettingsButton = null!;
        private List<AdapterRow> adapterRows = [];

        public ToolsForm(Form1 ownerForm)
        {
            this.ownerForm = ownerForm;
            InitializeToolsInterface();
            Load += (_, _) => RefreshAdapterList();
        }

        private void InitializeToolsInterface()
        {
            AutoScaleMode = AutoScaleMode.Dpi;
            BackColor = Color.FromArgb(241, 245, 249);
            ClientSize = new Size(1040, 780);
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 134);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Text = "PingTools - 工具箱";

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18),
                BackColor = BackColor,
                ColumnCount = 1,
                RowCount = 3,
            };
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110F));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 170F));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            root.Controls.Add(CreateHeader(), 0, 0);
            root.Controls.Add(CreateQuickActions(), 0, 1);
            root.Controls.Add(CreateMainArea(), 0, 2);

            Controls.Add(root);
        }

        private Control CreateHeader()
        {
            var panel = CreateCard(Color.FromArgb(15, 23, 42), new Padding(22, 18, 22, 18));
            panel.Margin = new Padding(0, 0, 0, 16);

            var title = new Label
            {
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 21F, FontStyle.Bold),
                ForeColor = Color.White,
                Text = "网络工具箱",
                Location = new Point(20, 12),
            };
            var subtitle = new Label
            {
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 10F),
                ForeColor = Color.FromArgb(148, 163, 184),
                Text = "快速目标、网卡网关与系统入口",
                Location = new Point(23, 58),
            };

            panel.Controls.Add(title);
            panel.Controls.Add(subtitle);

            return panel;
        }

        private Control CreateQuickActions()
        {
            var wrapper = CreateCard(Color.White, new Padding(18));
            wrapper.Margin = new Padding(0, 0, 0, 16);

            var content = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
            };
            content.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var title = new Label
            {
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 12.5F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Text = "快捷功能",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0),
            };

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 2,
                Margin = new Padding(0, 12, 0, 0),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

            layout.Controls.Add(CreateQuickButton("Ping 本地", Color.FromArgb(14, 165, 233), () => TriggerAndClose("127.0.0.1"), false), 0, 0);
            layout.Controls.Add(CreateQuickButton("Ping 网关", Color.FromArgb(37, 99, 235), TriggerDefaultGateway, true), 1, 0);
            layout.Controls.Add(CreateQuickButton("Ping 百度", Color.FromArgb(15, 118, 110), () => TriggerAndClose("www.baidu.com"), false), 0, 1);
            layout.Controls.Add(CreateQuickButton("Ping QQ", Color.FromArgb(194, 65, 12), () => TriggerAndClose("www.qq.com"), true), 1, 1);

            content.Controls.Add(title, 0, 0);
            content.Controls.Add(layout, 0, 1);
            wrapper.Controls.Add(content);
            return wrapper;
        }

        private Control CreateQuickButton(string text, Color accent, Action onClick, bool lastColumn)
        {
            var button = CreatePrimaryButton(text);
            button.BackColor = accent;
            button.Margin = lastColumn ? new Padding(0, 0, 0, 10) : new Padding(0, 0, 12, 10);
            button.Click += (_, _) => onClick();
            return button;
        }

        private Control CreateMainArea()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
            };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 74F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 26F));

            layout.Controls.Add(CreateAdaptersCard(), 0, 0);
            layout.Controls.Add(CreateSystemActionsCard(), 0, 1);
            return layout;
        }

        private Control CreateAdaptersCard()
        {
            var card = CreateCard(Color.White, new Padding(18));
            card.Margin = new Padding(0, 0, 0, 16);

            var title = new Label
            {
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Text = "网卡与网关",
                Dock = DockStyle.Top,
            };

            var body = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(0, 18, 0, 0),
            };
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            body.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));

            adaptersListView = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 250, 252),
                Font = new Font("Microsoft YaHei UI", 9.5F),
                Margin = new Padding(0, 0, 0, 12),
            };
            adaptersListView.Columns.Add("网卡名称", 220);
            adaptersListView.Columns.Add("DNS", 210);
            adaptersListView.Columns.Add("默认网关", 150);
            adaptersListView.Columns.Add("IPv4", 130);
            adaptersListView.Columns.Add("状态", 70);
            adaptersListView.Columns.Add("类型", 90);
            adaptersListView.SelectedIndexChanged += (_, _) => UpdateAdapterActionState();

            var actionBar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
            };
            actionBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            actionBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            pingGatewayButton = CreatePrimaryButton("Ping 所选网关");
            refreshAdaptersButton = CreateSecondaryButton("刷新网卡");
            pingGatewayButton.Click += (_, _) => UseSelectedGateway();
            refreshAdaptersButton.Click += (_, _) => RefreshAdapterList();
            actionBar.Controls.Add(pingGatewayButton, 0, 0);
            actionBar.Controls.Add(refreshAdaptersButton, 1, 0);

            body.Controls.Add(adaptersListView, 0, 0);
            body.Controls.Add(actionBar, 0, 1);

            card.Controls.Add(body);
            card.Controls.Add(title);
            return card;
        }

        private Control CreateSystemActionsCard()
        {
            var card = CreateCard(Color.White, new Padding(18));

            var title = new Label
            {
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold),
                ForeColor = Color.FromArgb(15, 23, 42),
                Text = "系统入口",
                Dock = DockStyle.Top,
            };

            var actions = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(0, 18, 0, 0),
                Height = 68,
            };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            openAdaptersButton = CreateSecondaryButton("打开网络适配器");
            openSettingsButton = CreateSecondaryButton("打开网络设置");
            openAdaptersButton.Click += (_, _) => OpenSystemPath("ncpa.cpl");
            openSettingsButton.Click += (_, _) => OpenSystemPath("ms-settings:network");

            actions.Controls.Add(openAdaptersButton, 0, 0);
            actions.Controls.Add(openSettingsButton, 1, 0);

            card.Controls.Add(actions);
            card.Controls.Add(title);
            return card;
        }

        private void RefreshAdapterList()
        {
            adapterRows = NetworkInterface.GetAllNetworkInterfaces()
                .Select(CreateAdapterRow)
                .Where(row => row is not null)
                .Cast<AdapterRow>()
                .OrderByDescending(row => row.IsUp)
                .ThenBy(row => row.IsVirtual)
                .ThenBy(row => row.Name)
                .ToList();

            adaptersListView.BeginUpdate();
            adaptersListView.Items.Clear();

            foreach (var row in adapterRows)
            {
                var item = new ListViewItem(
                [
                    row.Name,
                    row.DnsServers,
                    row.GatewayAddress ?? "--",
                    row.Ipv4Address ?? "--",
                    row.StatusLabel,
                    row.TypeLabel,
                ])
                {
                    Tag = row,
                };

                if (!row.IsUp || string.IsNullOrWhiteSpace(row.GatewayAddress))
                {
                    item.ForeColor = Color.FromArgb(100, 116, 139);
                }

                adaptersListView.Items.Add(item);
            }

            adaptersListView.EndUpdate();

            UpdateAdapterActionState();
        }

        private void UpdateAdapterActionState()
        {
            var selected = GetSelectedAdapterRow();
            pingGatewayButton.Enabled = selected is { IsUp: true, GatewayAddress.Length: > 0 };
        }

        private AdapterRow? GetSelectedAdapterRow() =>
            adaptersListView.SelectedItems.Count > 0 ? adaptersListView.SelectedItems[0].Tag as AdapterRow : null;

        private string? GetDefaultGatewayAddress() =>
            adapterRows.FirstOrDefault(row => row.IsUp && !string.IsNullOrWhiteSpace(row.GatewayAddress) && !row.IsVirtual)?.GatewayAddress
            ?? adapterRows.FirstOrDefault(row => row.IsUp && !string.IsNullOrWhiteSpace(row.GatewayAddress))?.GatewayAddress;

        private void UseSelectedGateway()
        {
            var selected = GetSelectedAdapterRow();
            if (selected is null || !selected.IsUp || string.IsNullOrWhiteSpace(selected.GatewayAddress))
            {
                return;
            }

            TriggerAndClose(selected.GatewayAddress);
        }

        private void TriggerAndClose(string target)
        {
            if (ownerForm.RequestToolProbe(target))
            {
                Close();
            }
        }

        private void TriggerDefaultGateway()
        {
            var gateway = GetDefaultGatewayAddress();
            if (string.IsNullOrWhiteSpace(gateway))
            {
                MessageBox.Show(this, "当前没有可用的默认网关。", "PingTools", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            TriggerAndClose(gateway);
        }

        private void OpenSystemPath(string path)
        {
            try
            {
                using var process = Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"无法打开系统页面：{ex.Message}", "PingTools", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static AdapterRow? CreateAdapterRow(NetworkInterface nic)
        {
            try
            {
                var properties = nic.GetIPProperties();
                var ipv4 = properties.UnicastAddresses
                    .FirstOrDefault(address => address.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    ?.Address.ToString();

                var gateway = properties.GatewayAddresses
                    .FirstOrDefault(address => address.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork && !address.Address.Equals(System.Net.IPAddress.Any))
                    ?.Address.ToString();

                var dnsServers = properties.DnsAddresses
                    .Where(address => address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    .Select(address => address.ToString())
                    .Take(2)
                    .ToList();

                return new AdapterRow(
                    nic.Name,
                    GetTypeLabel(nic),
                    nic.OperationalStatus == OperationalStatus.Up ? "已启用" : "未启用",
                    ipv4,
                    gateway,
                    dnsServers.Count > 0 ? string.Join(", ", dnsServers) : "--",
                    nic.OperationalStatus == OperationalStatus.Up,
                    IsVirtual(nic));
            }
            catch
            {
                return null;
            }
        }

        private static string GetTypeLabel(NetworkInterface nic) =>
            nic.NetworkInterfaceType switch
            {
                NetworkInterfaceType.Wireless80211 => "无线",
                NetworkInterfaceType.Ethernet or NetworkInterfaceType.GigabitEthernet or NetworkInterfaceType.FastEthernetFx or NetworkInterfaceType.FastEthernetT => "有线",
                NetworkInterfaceType.Loopback => "回环",
                NetworkInterfaceType.Ppp => "拨号/VPN",
                _ => IsVirtual(nic) ? "虚拟" : nic.NetworkInterfaceType.ToString(),
            };

        private static bool IsVirtual(NetworkInterface nic)
        {
            var name = $"{nic.Name} {nic.Description}".ToLowerInvariant();
            return name.Contains("virtual") || name.Contains("vmware") || name.Contains("hyper-v") || name.Contains("vethernet") || name.Contains("wsl") || name.Contains("docker") || name.Contains("vpn");
        }

        private static Panel CreateCard(Color backColor, Padding padding) =>
            new() { Dock = DockStyle.Fill, BackColor = backColor, Padding = padding };

        private static Button CreatePrimaryButton(string text)
        {
            var button = new Button
            {
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(14, 165, 233),
                ForeColor = Color.White,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                Text = text,
                Margin = new Padding(0, 0, 8, 10),
            };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        private static Button CreateSecondaryButton(string text)
        {
            var button = new Button
            {
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(51, 65, 85),
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                Text = text,
                Margin = new Padding(0, 0, 8, 10),
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            return button;
        }

        private sealed record AdapterRow(
            string Name,
            string TypeLabel,
            string StatusLabel,
            string? Ipv4Address,
            string? GatewayAddress,
            string DnsServers,
            bool IsUp,
            bool IsVirtual);
    }
}
