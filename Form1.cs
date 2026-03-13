using System.Drawing.Drawing2D;
using System.Net;
using System.Net.NetworkInformation;

namespace PingTools
{
    public partial class Form1 : Form
    {
        private const int MaxTimelineSamples = 20;
        private const int MaxEventItems = 8;

        private readonly List<PingSample> samples = [];
        private readonly List<long?> timelineSamples = [];

        private CancellationTokenSource? probeCancellation;
        private bool isRunning;
        private bool isResolvingTarget;
        private string? pendingToolTarget;

        private Label headerStatusBadge = null!;
        private Label avgValueLabel = null!;
        private Label maxValueLabel = null!;
        private Label lossValueLabel = null!;
        private Label healthValueLabel = null!;
        private Label healthCaptionLabel = null!;
        private TextBox targetTextBox = null!;
        private NumericUpDown countInput = null!;
        private NumericUpDown timeoutInput = null!;
        private NumericUpDown intervalInput = null!;
        private Button toolsButton = null!;
        private Button startButton = null!;
        private Button stopButton = null!;
        private Button clearButton = null!;
        private ListView resultsListView = null!;
        private ListBox eventsListBox = null!;
        private Panel timelineCanvas = null!;
        private Panel healthCardPanel = null!;

        public Form1()
        {
            InitializeComponent();
            BuildInterface();
            ResetDashboard();
            SetRunningState(false);
            FormClosing += Form1_FormClosing;
            Shown += (_, _) => FocusTargetInput();
        }

        private void BuildInterface()
        {
            SuspendLayout();
            Controls.Clear();
            Controls.Add(CreateMainLayout());
            ResumeLayout(false);
        }

        private Control CreateMainLayout()
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18),
                BackColor = Color.FromArgb(241, 245, 249),
                ColumnCount = 1,
                RowCount = 4,
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 136F));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 128F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.Controls.Add(CreateHeader(), 0, 0);
            layout.Controls.Add(CreateConfigurationCard(), 0, 1);
            layout.Controls.Add(CreateMetrics(), 0, 2);
            layout.Controls.Add(CreateBody(), 0, 3);
            return layout;
        }

        private Control CreateHeader()
        {
            var panel = CreateCard(Color.FromArgb(15, 23, 42), new Padding(24, 20, 24, 20));
            panel.Margin = new Padding(0, 0, 0, 16);

            var title = new Label
            {
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 22F, FontStyle.Bold),
                ForeColor = Color.White,
                Text = "PingTools 控制台",
                Location = new Point(24, 15),
            };
            var subtitle = new Label
            {
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 10.5F),
                ForeColor = Color.FromArgb(148, 163, 184),
                Text = "面向实时网络诊断的 ICMP 可视化分析面板，聚焦延迟、丢包与链路可达性评估",
                Location = new Point(27, 61),
            };
            headerStatusBadge = new Label
            {
                Size = new Size(195, 36),
                Location = new Point(1030, 26),
                Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
            };
            panel.Controls.Add(title);
            panel.Controls.Add(subtitle);
            panel.Controls.Add(headerStatusBadge);
            return panel;
        }

        private Control CreateConfigurationCard()
        {
            var card = CreateCard(Color.White, new Padding(20, 18, 20, 18));
            card.Margin = new Padding(0, 0, 0, 16);

            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 52F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 48F));

            var leftLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 6, 18, 6) };
            leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 86F));
            leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var configLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0, 6, 0, 6) };
            configLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333F));
            configLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333F));
            configLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333F));
            configLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var buttonLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, RowCount = 1, Margin = new Padding(0, 6, 0, 0) };
            buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            buttonLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            targetTextBox = new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Microsoft YaHei UI", 10F),
                PlaceholderText = "输入域名或 IP，例如 1.1.1.1 / baidu.com / localhost",
                Text = string.Empty,
                Margin = new Padding(3, 0, 16, 0),
            };
            countInput = CreateNumeric(0, -9999, 9999, 1);
            timeoutInput = CreateNumeric(1000, 100, 10000, 100);
            intervalInput = CreateNumeric(1000, 100, 5000, 100);
            toolsButton = CreateOutlineButton("工具");
            startButton = CreateActionButton("开始探测", Color.FromArgb(14, 165, 233), Color.White);
            stopButton = CreateActionButton("停止", Color.FromArgb(255, 244, 229), Color.FromArgb(194, 65, 12));
            clearButton = CreateOutlineButton("清空数据");

            toolsButton.Click += toolsButton_Click;
            startButton.Click += startButton_Click;
            stopButton.Click += stopButton_Click;
            clearButton.Click += clearButton_Click;

            leftLayout.Controls.Add(CreateFieldLabel("探测目标", true), 0, 0);
            leftLayout.Controls.Add(targetTextBox, 1, 0);

            configLayout.Controls.Add(CreateConfigField("探测数", countInput), 0, 0);
            configLayout.Controls.Add(CreateConfigField("超时值", timeoutInput), 1, 0);
            configLayout.Controls.Add(CreateConfigField("间隔值", intervalInput), 2, 0);

            toolsButton.Dock = DockStyle.Fill;
            startButton.Dock = DockStyle.Fill;
            stopButton.Dock = DockStyle.Fill;
            clearButton.Dock = DockStyle.Fill;
            buttonLayout.Controls.Add(toolsButton, 0, 0);
            buttonLayout.Controls.Add(startButton, 1, 0);
            buttonLayout.Controls.Add(stopButton, 2, 0);
            buttonLayout.Controls.Add(clearButton, 3, 0);

            layout.Controls.Add(leftLayout, 0, 0);
            layout.SetRowSpan(leftLayout, 2);
            layout.Controls.Add(configLayout, 1, 0);
            layout.Controls.Add(buttonLayout, 1, 1);

            card.Controls.Add(layout);
            return card;
        }

        private Control CreateMetrics()
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4, Margin = new Padding(0, 0, 0, 16) };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));

            avgValueLabel = CreateMetricCard(layout, 0, Color.FromArgb(14, 165, 233), "平均延迟", "--", Color.FromArgb(224, 242, 254));
            maxValueLabel = CreateMetricCard(layout, 1, Color.FromArgb(37, 99, 235), "峰值延迟", "--", Color.FromArgb(219, 234, 254));
            lossValueLabel = CreateMetricCard(layout, 2, Color.FromArgb(251, 146, 60), "丢包率", "--", Color.FromArgb(255, 237, 213));
            healthValueLabel = CreateMetricCard(layout, 3, Color.FromArgb(71, 85, 105), "链路状态", "待机", Color.FromArgb(226, 232, 240), isHealthCard: true);
            return layout;
        }

        private Control CreateBody()
        {
            var body = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 63F));
            body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37F));
            body.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            body.Controls.Add(CreateResultsCard(), 0, 0);
            body.Controls.Add(CreateSideColumn(), 1, 0);
            return body;
        }

        private Control CreateResultsCard()
        {
            var card = CreateCard(Color.White, new Padding(18));
            card.Margin = new Padding(0, 0, 12, 0);
            card.Controls.Add(new Label { AutoSize = true, Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Text = "探测结果流", Location = new Point(18, 16) });
            card.Controls.Add(new Label { AutoSize = true, Font = new Font("Microsoft YaHei UI", 9.5F), ForeColor = Color.FromArgb(100, 116, 139), Text = "每次探测结果会按时间顺序展示，适合观察抖动、失败和 TTL 变化", Location = new Point(21, 42) });

            resultsListView = new ListView
            {
                Dock = DockStyle.Bottom,
                Height = 320,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                MultiSelect = false,
                HeaderStyle = ColumnHeaderStyle.Nonclickable,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 250, 252),
                Font = new Font("Microsoft YaHei UI", 9.5F),
            };
            resultsListView.Columns.Add("#", 40);
            resultsListView.Columns.Add("状态", 95);
            resultsListView.Columns.Add("响应地址", 170);
            resultsListView.Columns.Add("延迟", 100);
            resultsListView.Columns.Add("TTL", 70);
            resultsListView.Columns.Add("时间戳", 210);
            card.Controls.Add(resultsListView);
            return card;
        }

        private Control CreateSideColumn()
        {
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2 };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 52F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 48F));
            layout.Controls.Add(CreateTimelineCard(), 0, 0);
            layout.Controls.Add(CreateEventsCard(), 0, 1);
            return layout;
        }

        private Control CreateTimelineCard()
        {
            var card = CreateCard(Color.White, new Padding(18));
            card.Margin = new Padding(0, 0, 0, 12);
            card.Controls.Add(new Label { AutoSize = true, Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Text = "延迟波形", Location = new Point(18, 16) });
            card.Controls.Add(new Label { AutoSize = true, Font = new Font("Microsoft YaHei UI", 9.5F), ForeColor = Color.FromArgb(100, 116, 139), Text = "展示最近探测样本，用于观察网络抖动与瞬时波峰", Location = new Point(21, 42) });

            timelineCanvas = new Panel { Dock = DockStyle.Bottom, Height = 124, BackColor = Color.FromArgb(248, 250, 252) };
            timelineCanvas.Paint += timelineCanvas_Paint;
            card.Controls.Add(timelineCanvas);
            return card;
        }

        private Control CreateEventsCard()
        {
            var card = CreateCard(Color.White, new Padding(18));
            card.Margin = new Padding(0, 12, 0, 0);
            card.Controls.Add(new Label { AutoSize = true, Font = new Font("Microsoft YaHei UI", 13F, FontStyle.Bold), ForeColor = Color.FromArgb(15, 23, 42), Text = "操作事件", Location = new Point(18, 16) });
            card.Controls.Add(new Label { AutoSize = true, Font = new Font("Microsoft YaHei UI", 9.5F), ForeColor = Color.FromArgb(100, 116, 139), Text = "记录开始、停止、清空与探测异常等关键事件", Location = new Point(21, 42) });

            eventsListBox = new ListBox
            {
                Dock = DockStyle.Bottom,
                Height = 96,
                BorderStyle = BorderStyle.None,
                BackColor = Color.FromArgb(248, 250, 252),
                Font = new Font("Microsoft YaHei UI", 9.5F),
                ForeColor = Color.FromArgb(51, 65, 85),
            };
            card.Controls.Add(eventsListBox);
            return card;
        }

        private static Panel CreateCard(Color backColor, Padding padding) =>
            new() { Dock = DockStyle.Fill, BackColor = backColor, Padding = padding };

        private static NumericUpDown CreateNumeric(decimal value, decimal min, decimal max, decimal increment) =>
            new()
            {
                Dock = DockStyle.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Microsoft YaHei UI", 10F),
                Minimum = min,
                Maximum = max,
                Increment = increment,
                TextAlign = HorizontalAlignment.Center,
                Value = value,
            };

        private static Label CreateFieldLabel(string text, bool bold = false) =>
            new()
            {
                Anchor = AnchorStyles.Left,
                AutoSize = true,
                Font = new Font("Microsoft YaHei UI", 10F, bold ? FontStyle.Bold : FontStyle.Regular),
                ForeColor = bold ? Color.FromArgb(15, 23, 42) : Color.FromArgb(51, 65, 85),
                Text = text,
            };

        private static Control CreateConfigField(string labelText, Control inputControl)
        {
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 12, 0),
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 58F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            inputControl.Dock = DockStyle.Fill;
            inputControl.Margin = new Padding(0);
            layout.Controls.Add(CreateFieldLabel(labelText), 0, 0);
            layout.Controls.Add(inputControl, 1, 0);
            return layout;
        }

        private static Button CreateActionButton(string text, Color backColor, Color foreColor)
        {
            var button = new Button
            {
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = foreColor,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                Text = text,
                Margin = new Padding(0, 0, 12, 0),
            };
            button.FlatAppearance.BorderSize = 0;
            return button;
        }

        private static Button CreateOutlineButton(string text)
        {
            var button = new Button
            {
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(51, 65, 85),
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                Text = text,
                Margin = new Padding(0, 0, 12, 0),
            };
            button.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
            return button;
        }

        private Label CreateMetricCard(TableLayoutPanel layout, int column, Color backColor, string caption, string value, Color captionColor, bool isHealthCard = false)
        {
            var panel = CreateCard(backColor, new Padding(18, 16, 18, 18));
            panel.Margin = column == 0 ? new Padding(0) : new Padding(12, 0, 0, 0);

            var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = Color.Transparent };
            content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 58F));
            content.RowStyles.Add(new RowStyle(SizeType.Percent, 42F));

            var valueLabel = new Label
            {
                AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
                Font = new Font("Microsoft YaHei UI", 23F, FontStyle.Bold),
                ForeColor = Color.White,
                Text = value,
                Margin = new Padding(0, 0, 0, 2),
            };
            var captionLabel = new Label
            {
                AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Top,
                Font = new Font("Microsoft YaHei UI", 11F),
                ForeColor = captionColor,
                Text = caption,
                Margin = new Padding(0),
            };

            content.Controls.Add(valueLabel, 0, 0);
            content.Controls.Add(captionLabel, 0, 1);
            panel.Controls.Add(content);
            layout.Controls.Add(panel, column, 0);

            if (isHealthCard)
            {
                healthCardPanel = panel;
                healthCaptionLabel = captionLabel;
                valueLabel.Font = new Font("Microsoft YaHei UI", 20F, FontStyle.Bold);
            }

            return valueLabel;
        }

        private async void startButton_Click(object? sender, EventArgs e)
        {
            if (isRunning || isResolvingTarget)
            {
                return;
            }

            var target = targetTextBox.Text.Trim();
            ResetDashboard();

            if (!await ValidateTargetAsync(target))
            {
                return;
            }

            SetRunningState(true);
            SetStatusBadge("LIVE  ·  RUNNING", Color.FromArgb(12, 74, 110), Color.FromArgb(186, 230, 253));
            AddEvent($"开始探测 {target}");

            probeCancellation = new CancellationTokenSource();
            try
            {
                await RunProbeLoopAsync(target, probeCancellation.Token);
            }
            finally
            {
                if (!IsDisposed)
                {
                    SetRunningState(false);
                    probeCancellation?.Dispose();
                    probeCancellation = null;
                    UpdateSummary();
                    FocusTargetInput(selectAll: false);

                    if (!string.IsNullOrWhiteSpace(pendingToolTarget))
                    {
                        var nextTarget = pendingToolTarget;
                        pendingToolTarget = null;
                        BeginInvoke(() =>
                        {
                            if (!IsDisposed && !string.IsNullOrWhiteSpace(nextTarget))
                            {
                                targetTextBox.Text = nextTarget;
                                startButton.PerformClick();
                            }
                        });
                    }
                }
            }
        }

        private void stopButton_Click(object? sender, EventArgs e)
        {
            if (isResolvingTarget)
            {
                return;
            }

            if (!isRunning)
            {
                AddEvent("当前没有正在进行的探测任务");
                return;
            }

            probeCancellation?.Cancel();
            AddEvent("已请求停止探测");
        }

        private void clearButton_Click(object? sender, EventArgs e)
        {
            if (isResolvingTarget)
            {
                return;
            }

            if (isRunning)
            {
                probeCancellation?.Cancel();
            }

            ResetDashboard();
            AddEvent("已清空历史数据");
            FocusTargetInput();
        }

        private async Task RunProbeLoopAsync(string target, CancellationToken cancellationToken)
        {
            var totalCount = (int)countInput.Value;
            var timeout = (int)timeoutInput.Value;
            var interval = (int)intervalInput.Value;
            var buffer = new byte[32];
            var infiniteMode = totalCount <= 0;
            var attempt = 1;

            using var ping = new Ping();
            while (infiniteMode || attempt <= totalCount)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    SetStatusBadge("PAUSED  ·  STOPPED", Color.FromArgb(120, 53, 15), Color.FromArgb(254, 215, 170));
                    AddEvent("探测已停止");
                    return;
                }

                PingReply? reply = null;
                Exception? error = null;
                try
                {
                    reply = await ping.SendPingAsync(target, timeout, buffer, new PingOptions(64, true));
                }
                catch (PingException ex)
                {
                    error = ex;
                }
                catch (Exception ex)
                {
                    error = ex;
                }

                AppendProbeResult(attempt, target, reply, error);

                attempt++;

                if (infiniteMode || attempt <= totalCount)
                {
                    try
                    {
                        await Task.Delay(interval, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        SetStatusBadge("PAUSED  ·  STOPPED", Color.FromArgb(120, 53, 15), Color.FromArgb(254, 215, 170));
                        AddEvent("探测已停止");
                        return;
                    }
                }
            }

            SetStatusBadge("DONE  ·  COMPLETED", Color.FromArgb(22, 101, 52), Color.FromArgb(220, 252, 231));
            AddEvent($"探测完成，共 {samples.Count} 次");
        }

        private void AppendProbeResult(int sequence, string target, PingReply? reply, Exception? error)
        {
            var timestamp = DateTime.Now;
            PingSample sample;

            if (reply is not null && reply.Status == IPStatus.Success)
            {
                sample = new PingSample(
                    sequence,
                    "成功",
                    reply.Address?.ToString() ?? target,
                    reply.RoundtripTime,
                    reply.Options?.Ttl,
                    timestamp,
                    IPStatus.Success);
            }
            else if (reply is not null)
            {
                sample = new PingSample(
                    sequence,
                    MapFailureStatus(reply.Status),
                    reply.Address?.ToString() ?? target,
                    null,
                    reply.Options?.Ttl,
                    timestamp,
                    reply.Status);
            }
            else
            {
                sample = new PingSample(
                    sequence,
                    "异常",
                    target,
                    null,
                    null,
                    timestamp,
                    null,
                    error?.Message);
            }

            samples.Add(sample);
            timelineSamples.Add(sample.LatencyMs);
            if (timelineSamples.Count > MaxTimelineSamples)
            {
                timelineSamples.RemoveAt(0);
            }

            var item = new ListViewItem(
            [
                sample.Sequence.ToString(),
                sample.StatusText,
                sample.Address,
                sample.LatencyMs is long latency ? $"{latency} ms" : "--",
                sample.Ttl?.ToString() ?? "--",
                sample.Timestamp.ToString("HH:mm:ss"),
            ]);

            if (sample.LatencyMs is null)
            {
                item.ForeColor = Color.FromArgb(185, 28, 28);
            }

            resultsListView.Items.Add(item);
            if (resultsListView.Items.Count > 0)
            {
                resultsListView.EnsureVisible(resultsListView.Items.Count - 1);
            }

            UpdateSummary();

            if (sample.LatencyMs is null)
            {
                AddEvent($"第 {sample.Sequence} 次探测失败: {sample.StatusText}");
            }

            timelineCanvas.Invalidate();
        }

        private void UpdateSummary()
        {
            var successLatencies = samples.Where(x => x.LatencyMs.HasValue).Select(x => x.LatencyMs!.Value).ToList();
            var failureCount = samples.Count - successLatencies.Count;
            var recentJitterRange = GetRecentJitterRange();
            var healthState = ResolveHealthState(samples, successLatencies, failureCount, recentJitterRange);

            avgValueLabel.Text = successLatencies.Count > 0 ? $"{successLatencies.Average():0} ms" : "--";
            maxValueLabel.Text = successLatencies.Count > 0 ? $"{successLatencies.Max()} ms" : "--";
            lossValueLabel.Text = samples.Count > 0 ? $"{(failureCount * 100.0 / samples.Count):0.#} %" : "--";
            healthValueLabel.Text = healthState.Text;
            ApplyHealthTheme(healthState);

            if (isRunning)
            {
                return;
            }

            if (samples.Count == 0)
            {
                SetStatusBadge("READY  ·  WAITING", Color.FromArgb(30, 41, 59), Color.FromArgb(125, 211, 252));
            }
        }

        private static HealthState ResolveHealthState(IReadOnlyList<PingSample> allSamples, IReadOnlyCollection<long> successLatencies, int failureCount, long recentJitterRange)
        {
            var totalCount = allSamples.Count;
            if (totalCount == 0)
            {
                return new HealthState("待机", Color.FromArgb(71, 85, 105), Color.FromArgb(226, 232, 240));
            }

            if (allSamples.Any(sample =>
                sample.StatusText == "异常" ||
                sample.Status is IPStatus.DestinationHostUnreachable or IPStatus.DestinationNetworkUnreachable or IPStatus.BadRoute))
            {
                return new HealthState("不可达", Color.FromArgb(127, 29, 29), Color.FromArgb(254, 226, 226));
            }

            var lossRate = failureCount / (double)totalCount;
            if (lossRate >= 0.05)
            {
                return new HealthState("链路异常", Color.FromArgb(153, 27, 27), Color.FromArgb(254, 226, 226));
            }

            if (lossRate >= 0.02)
            {
                return new HealthState("波动", Color.FromArgb(180, 83, 9), Color.FromArgb(254, 243, 199));
            }

            if (successLatencies.Count == 0)
            {
                return new HealthState("不可达", Color.FromArgb(127, 29, 29), Color.FromArgb(254, 226, 226));
            }

            var avg = successLatencies.Average();
            if (avg <= 70)
            {
                if (recentJitterRange >= 10)
                {
                    return new HealthState("波动", Color.FromArgb(180, 83, 9), Color.FromArgb(254, 243, 199));
                }

                return new HealthState("优良", Color.FromArgb(21, 128, 61), Color.FromArgb(220, 252, 231));
            }

            if (avg <= 120)
            {
                if (recentJitterRange >= 15)
                {
                    return new HealthState("波动", Color.FromArgb(180, 83, 9), Color.FromArgb(254, 243, 199));
                }

                return new HealthState("稳定", Color.FromArgb(14, 116, 144), Color.FromArgb(207, 250, 254));
            }

            return new HealthState("偏高", Color.FromArgb(194, 65, 12), Color.FromArgb(255, 237, 213));
        }

        private long GetRecentJitterRange()
        {
            var recentSuccesses = samples
                .Where(sample => sample.LatencyMs.HasValue)
                .Select(sample => sample.LatencyMs!.Value)
                .TakeLast(MaxTimelineSamples)
                .ToList();

            if (recentSuccesses.Count < 2)
            {
                return 0;
            }

            return recentSuccesses.Max() - recentSuccesses.Min();
        }

        private static string MapFailureStatus(IPStatus status) =>
            status switch
            {
                IPStatus.TimedOut => "超时",
                IPStatus.DestinationHostUnreachable => "主机不可达",
                IPStatus.DestinationNetworkUnreachable => "网络不可达",
                IPStatus.BadRoute => "路由异常",
                _ => status.ToString(),
            };

        private void ResetDashboard()
        {
            samples.Clear();
            timelineSamples.Clear();
            resultsListView?.Items.Clear();
            eventsListBox?.Items.Clear();

            avgValueLabel.Text = "--";
            maxValueLabel.Text = "--";
            lossValueLabel.Text = "--";
            healthValueLabel.Text = "待机";
            ApplyHealthTheme(new HealthState("待机", Color.FromArgb(71, 85, 105), Color.FromArgb(226, 232, 240)));
            SetStatusBadge("READY  ·  WAITING", Color.FromArgb(30, 41, 59), Color.FromArgb(125, 211, 252));

            AddEvent("系统已就绪，等待开始探测");
            timelineCanvas?.Invalidate();
        }

        private void SetRunningState(bool running)
        {
            isRunning = running;
            RefreshControlState();
        }

        private async Task<bool> ValidateTargetAsync(string target)
        {
            if (string.IsNullOrWhiteSpace(target))
            {
                AddEvent("请输入有效的探测目标");
                MessageBox.Show(this, "请输入 IP 地址或域名。", "PingTools", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                FocusTargetInput();
                return false;
            }

            if (IPAddress.TryParse(target, out _))
            {
                return true;
            }

            if (LooksLikeInvalidIp(target))
            {
                AddEvent("IP 地址格式无效");
                MessageBox.Show(this, "IP 地址格式不正确。", "PingTools", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                FocusTargetInput();
                return false;
            }

            if (Uri.CheckHostName(target) == UriHostNameType.Unknown)
            {
                AddEvent("输入内容不是有效的 IP 或域名");
                MessageBox.Show(this, "请输入格式正确的 IP 地址或域名。", "PingTools", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                FocusTargetInput();
                return false;
            }

            SetResolvingState(true);
            try
            {
                var addresses = await Dns.GetHostAddressesAsync(target);
                if (addresses.Length == 0)
                {
                    AddEvent($"域名解析失败: {target}");
                    MessageBox.Show(this, "未能解析到该域名对应的 IP 地址。", "PingTools", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    FocusTargetInput();
                    return false;
                }
            }
            catch (Exception)
            {
                AddEvent($"域名解析失败: {target}");
                MessageBox.Show(this, "域名无法解析，探测任务未启动。", "PingTools", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                FocusTargetInput();
                return false;
            }
            finally
            {
                SetResolvingState(false);
            }

            return true;
        }

        private static bool LooksLikeInvalidIp(string target)
        {
            if (target.Count(ch => ch == '.') == 3 && target.All(ch => char.IsDigit(ch) || ch == '.'))
            {
                return true;
            }

            if (target.Contains(':') && target.All(ch => Uri.IsHexDigit(ch) || ch == ':'))
            {
                return true;
            }

            return false;
        }

        private void SetResolvingState(bool resolving)
        {
            isResolvingTarget = resolving;
            if (resolving)
            {
                healthValueLabel.Text = "域名解析中";
                ApplyHealthTheme(new HealthState("域名解析中", Color.FromArgb(67, 56, 202), Color.FromArgb(224, 231, 255)));
            }
            else if (!isRunning && samples.Count == 0)
            {
                healthValueLabel.Text = "待机";
                ApplyHealthTheme(new HealthState("待机", Color.FromArgb(71, 85, 105), Color.FromArgb(226, 232, 240)));
            }

            RefreshControlState();
        }

        private void RefreshControlState()
        {
            startButton.Enabled = !isRunning && !isResolvingTarget;
            stopButton.Enabled = isRunning && !isResolvingTarget;
            clearButton.Enabled = !isResolvingTarget;
            targetTextBox.Enabled = !isRunning && !isResolvingTarget;
            countInput.Enabled = !isRunning && !isResolvingTarget;
            timeoutInput.Enabled = !isRunning && !isResolvingTarget;
            intervalInput.Enabled = !isRunning && !isResolvingTarget;
        }

        private void SetStatusBadge(string text, Color backColor, Color foreColor)
        {
            headerStatusBadge.Text = text;
            headerStatusBadge.BackColor = backColor;
            headerStatusBadge.ForeColor = foreColor;
        }

        private void FocusTargetInput(bool selectAll = true)
        {
            if (IsDisposed || !targetTextBox.CanFocus)
            {
                BeginInvoke(() =>
                {
                    if (!IsDisposed)
                    {
                        targetTextBox.Focus();
                        if (selectAll && !string.IsNullOrEmpty(targetTextBox.Text))
                        {
                            targetTextBox.SelectAll();
                        }
                    }
                });
                return;
            }

            targetTextBox.Focus();
            if (selectAll && !string.IsNullOrEmpty(targetTextBox.Text))
            {
                targetTextBox.SelectAll();
            }
        }

        private void toolsButton_Click(object? sender, EventArgs e)
        {
            using var toolsForm = new ToolsForm(this);
            toolsForm.ShowDialog(this);
        }

        internal bool RequestToolProbe(string target)
        {
            if (isResolvingTarget)
            {
                MessageBox.Show(this, "当前正在解析域名，请稍后再试。", "PingTools", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }

            if (isRunning)
            {
                var result = MessageBox.Show(
                    this,
                    $"当前任务正在执行，是否停止当前任务并开始探测 {target}？",
                    "PingTools",
                    MessageBoxButtons.OKCancel,
                    MessageBoxIcon.Question);

                if (result != DialogResult.OK)
                {
                    return false;
                }

                pendingToolTarget = target;
                stopButton.PerformClick();
                return true;
            }

            targetTextBox.Text = target;
            FocusTargetInput();
            startButton.PerformClick();
            return true;
        }

        private void AddEvent(string message)
        {
            var text = $"{DateTime.Now:HH:mm:ss}  {message}";
            eventsListBox.Items.Insert(0, text);
            while (eventsListBox.Items.Count > MaxEventItems)
            {
                eventsListBox.Items.RemoveAt(eventsListBox.Items.Count - 1);
            }
        }

        private void ApplyHealthTheme(HealthState state)
        {
            healthCardPanel.BackColor = state.Background;
            healthCaptionLabel.ForeColor = state.CaptionColor;
        }

        private void timelineCanvas_Paint(object? sender, PaintEventArgs e)
        {
            var area = timelineCanvas.ClientRectangle;
            if (area.Width <= 20 || area.Height <= 20)
            {
                return;
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using var gridPen = new Pen(Color.FromArgb(226, 232, 240));
            using var linePen = new Pen(Color.FromArgb(14, 165, 233), 2.4F);
            using var fillBrush = new SolidBrush(Color.FromArgb(60, 14, 165, 233));
            using var pointBrush = new SolidBrush(Color.FromArgb(2, 132, 199));
            using var failureBrush = new SolidBrush(Color.FromArgb(220, 38, 38));

            for (var i = 1; i < 4; i++)
            {
                var y = area.Height * i / 4F;
                e.Graphics.DrawLine(gridPen, 10, y, area.Width - 10, y);
            }

            if (timelineSamples.Count == 0)
            {
                using var emptyBrush = new SolidBrush(Color.FromArgb(100, 116, 139));
                e.Graphics.DrawString("暂无探测数据", new Font("Microsoft YaHei UI", 10F), emptyBrush, new PointF(16, 16));
                return;
            }

            var successfulSamples = timelineSamples.Where(x => x.HasValue).Select(x => x!.Value).ToList();
            var maxLatency = successfulSamples.Count > 0 ? Math.Max(successfulSamples.Max(), 10) : 10;
            var chartLeft = 14F;
            var chartRight = area.Width - 14F;
            var chartBottom = area.Height - 12F;
            var chartTop = 12F;
            var chartHeight = chartBottom - chartTop;
            var step = timelineSamples.Count > 1 ? (chartRight - chartLeft) / (timelineSamples.Count - 1F) : 0F;

            var points = new List<PointF>();
            for (var i = 0; i < timelineSamples.Count; i++)
            {
                var x = chartLeft + i * step;
                if (timelineSamples[i] is long latency)
                {
                    var normalized = Math.Clamp(latency / (float)(maxLatency * 1.15), 0F, 1F);
                    var y = chartBottom - normalized * chartHeight;
                    points.Add(new PointF(x, y));
                }
                else
                {
                    e.Graphics.FillEllipse(failureBrush, x - 3F, chartBottom - 4F, 6F, 6F);
                }
            }

            if (points.Count == 0)
            {
                using var emptyBrush = new SolidBrush(Color.FromArgb(185, 28, 28));
                e.Graphics.DrawString("当前样本均为失败请求", new Font("Microsoft YaHei UI", 10F), emptyBrush, new PointF(16, 16));
                return;
            }

            if (points.Count == 1)
            {
                e.Graphics.FillEllipse(pointBrush, points[0].X - 3.2F, points[0].Y - 3.2F, 6.4F, 6.4F);
                return;
            }

            var fillPoints = points.Concat([new PointF(points[^1].X, chartBottom), new PointF(points[0].X, chartBottom)]).ToArray();
            using var fillPath = new GraphicsPath();
            fillPath.AddPolygon(fillPoints);
            e.Graphics.FillPath(fillBrush, fillPath);
            e.Graphics.DrawLines(linePen, points.ToArray());

            foreach (var point in points)
            {
                e.Graphics.FillEllipse(pointBrush, point.X - 3.2F, point.Y - 3.2F, 6.4F, 6.4F);
            }
        }

        private void Form1_FormClosing(object? sender, FormClosingEventArgs e)
        {
            probeCancellation?.Cancel();
            probeCancellation?.Dispose();
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (isResolvingTarget)
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }

            if (!Focused && !ContainsFocus)
            {
                return base.ProcessCmdKey(ref msg, keyData);
            }

            switch (keyData)
            {
                case Keys.Enter when !isRunning:
                    startButton.PerformClick();
                    return true;
                case Keys.Escape when isRunning:
                    stopButton.PerformClick();
                    return true;
                case Keys.Delete:
                    clearButton.PerformClick();
                    return true;
                default:
                    return base.ProcessCmdKey(ref msg, keyData);
            }
        }

        private sealed record PingSample(
            int Sequence,
            string StatusText,
            string Address,
            long? LatencyMs,
            int? Ttl,
            DateTime Timestamp,
            IPStatus? Status = null,
            string? ErrorMessage = null);

        private sealed record HealthState(string Text, Color Background, Color CaptionColor);
    }
}
