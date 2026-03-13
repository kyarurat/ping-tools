using System.Drawing.Drawing2D;

namespace PingTools
{
    public partial class Form1 : Form
    {
        private readonly int[] previewLatency = [18, 20, 19, 24, 23, 27, 21, 25, 29, 26, 22, 24];
        private Label headerStatusBadge = null!;
        private Label avgValueLabel = null!;
        private Label maxValueLabel = null!;
        private Label lossValueLabel = null!;
        private Label healthValueLabel = null!;
        private TextBox targetTextBox = null!;
        private NumericUpDown countInput = null!;
        private NumericUpDown timeoutInput = null!;
        private NumericUpDown intervalInput = null!;
        private Button startButton = null!;
        private Button stopButton = null!;
        private Button clearButton = null!;
        private ListView resultsListView = null!;
        private ListBox eventsListBox = null!;
        private Panel timelineCanvas = null!;

        public Form1()
        {
            InitializeComponent();
            BuildInterface();
            SeedPreviewData();
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

            var title = new Label { AutoSize = true, Font = new Font("Microsoft YaHei UI", 22F, FontStyle.Bold), ForeColor = Color.White, Text = "PingTools 控制台", Location = new Point(24, 15) };
            var subtitle = new Label { AutoSize = true, Font = new Font("Microsoft YaHei UI", 10.5F), ForeColor = Color.FromArgb(148, 163, 184), Text = "Visual ICMP dashboard for latency, packet loss and reachability", Location = new Point(27, 61) };
            headerStatusBadge = new Label
            {
                Size = new Size(195, 36),
                Location = new Point(1030, 26),
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.FromArgb(125, 211, 252),
                Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Bold),
                Text = "READY  ·  WAITING",
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

            var buttonLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0, 6, 0, 0) };
            buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333F));
            buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333F));
            buttonLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3333F));
            buttonLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            targetTextBox = new TextBox
            {
                Anchor = AnchorStyles.Left | AnchorStyles.Right,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Microsoft YaHei UI", 10F),
                PlaceholderText = "输入域名或 IP，例如 1.1.1.1 / baidu.com / localhost",
                Text = "1.1.1.1",
                Margin = new Padding(3, 0, 16, 0),
            };
            countInput = CreateNumeric(20, 1, 999, 1);
            timeoutInput = CreateNumeric(1000, 100, 10000, 100);
            intervalInput = CreateNumeric(1000, 100, 5000, 100);
            startButton = CreateActionButton("开始探测", Color.FromArgb(14, 165, 233), Color.White);
            stopButton = CreateActionButton("停止", Color.FromArgb(255, 244, 229), Color.FromArgb(194, 65, 12));
            clearButton = CreateOutlineButton("清空数据");

            startButton.Click += startButton_Click;
            stopButton.Click += stopButton_Click;
            clearButton.Click += clearButton_Click;

            leftLayout.Controls.Add(CreateFieldLabel("探测目标", true), 0, 0);
            leftLayout.Controls.Add(targetTextBox, 1, 0);

            configLayout.Controls.Add(CreateConfigField("探测数", countInput), 0, 0);
            configLayout.Controls.Add(CreateConfigField("超时值", timeoutInput), 1, 0);
            configLayout.Controls.Add(CreateConfigField("间隔值", intervalInput), 2, 0);

            startButton.Dock = DockStyle.Fill;
            stopButton.Dock = DockStyle.Fill;
            clearButton.Dock = DockStyle.Fill;
            buttonLayout.Controls.Add(startButton, 0, 0);
            buttonLayout.Controls.Add(stopButton, 1, 0);
            buttonLayout.Controls.Add(clearButton, 2, 0);

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

            avgValueLabel = CreateMetricCard(layout, 0, Color.FromArgb(14, 165, 233), "平均延迟", "23 ms", Color.FromArgb(224, 242, 254));
            maxValueLabel = CreateMetricCard(layout, 1, Color.FromArgb(37, 99, 235), "峰值延迟", "31 ms", Color.FromArgb(219, 234, 254));
            lossValueLabel = CreateMetricCard(layout, 2, Color.FromArgb(251, 146, 60), "丢包率估计", "0 %", Color.FromArgb(255, 237, 213));
            healthValueLabel = CreateMetricCard(layout, 3, Color.FromArgb(15, 118, 110), "链路健康度", "优良", Color.FromArgb(204, 251, 241));
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
            card.Controls.Add(new Label { AutoSize = true, Font = new Font("Microsoft YaHei UI", 9.5F), ForeColor = Color.FromArgb(100, 116, 139), Text = "右侧波形区用于承载后续实时延迟折线图", Location = new Point(21, 42) });

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
            card.Controls.Add(new Label { AutoSize = true, Font = new Font("Microsoft YaHei UI", 9.5F), ForeColor = Color.FromArgb(100, 116, 139), Text = "记录开始、停止、清空与状态切换等事件", Location = new Point(21, 42) });

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

        private static Button CreateActionButton(string text, Color backColor, Color foreColor) =>
            new()
            {
                FlatStyle = FlatStyle.Flat,
                BackColor = backColor,
                ForeColor = foreColor,
                Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold),
                Text = text,
                Margin = new Padding(0, 0, 12, 0),
            };

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

        private Label CreateMetricCard(TableLayoutPanel layout, int column, Color backColor, string caption, string value, Color captionColor)
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
                Margin = new Padding(0, 0, 0, 0),
            };

            content.Controls.Add(valueLabel, 0, 0);
            content.Controls.Add(captionLabel, 0, 1);
            panel.Controls.Add(content);
            layout.Controls.Add(panel, column, 0);
            return valueLabel;
        }

        private void SeedPreviewData()
        {
            resultsListView.Items.Clear();
            AddProbeResult("1", "成功", "1.1.1.1", "18 ms", "55", "15:48:01");
            AddProbeResult("2", "成功", "1.1.1.1", "20 ms", "55", "15:48:02");
            AddProbeResult("3", "成功", "1.1.1.1", "24 ms", "55", "15:48:03");
            AddProbeResult("4", "抖动", "1.1.1.1", "31 ms", "55", "15:48:04");
            AddProbeResult("5", "成功", "1.1.1.1", "23 ms", "55", "15:48:05");

            eventsListBox.Items.Clear();
            eventsListBox.Items.Add("系统已就绪，等待开始探测");
            eventsListBox.Items.Add("预览数据已载入，用于展示视觉布局");
            eventsListBox.Items.Add("目标默认设置为 1.1.1.1");
        }

        private void AddProbeResult(string seq, string status, string address, string latency, string ttl, string time)
        {
            resultsListView.Items.Add(new ListViewItem([seq, status, address, latency, ttl, time]));
        }

        private void startButton_Click(object? sender, EventArgs e)
        {
            headerStatusBadge.Text = "LIVE  ·  RUNNING";
            avgValueLabel.Text = "21 ms";
            maxValueLabel.Text = "29 ms";
            lossValueLabel.Text = "0 %";
            healthValueLabel.Text = "稳定";
            eventsListBox.Items.Insert(0, $"开始探测 {targetTextBox.Text}");
            timelineCanvas.Invalidate();
        }

        private void stopButton_Click(object? sender, EventArgs e)
        {
            headerStatusBadge.Text = "PAUSED  ·  STOPPED";
            healthValueLabel.Text = "已暂停";
            eventsListBox.Items.Insert(0, "探测已停止");
        }

        private void clearButton_Click(object? sender, EventArgs e)
        {
            resultsListView.Items.Clear();
            avgValueLabel.Text = "--";
            maxValueLabel.Text = "--";
            lossValueLabel.Text = "--";
            healthValueLabel.Text = "待机";
            headerStatusBadge.Text = "READY  ·  CLEARED";
            eventsListBox.Items.Insert(0, "已清空历史数据");
            timelineCanvas.Invalidate();
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

            for (var i = 1; i < 4; i++)
            {
                var y = area.Height * i / 4F;
                e.Graphics.DrawLine(gridPen, 10, y, area.Width - 10, y);
            }

            if (resultsListView.Items.Count == 0)
            {
                using var emptyBrush = new SolidBrush(Color.FromArgb(100, 116, 139));
                e.Graphics.DrawString("暂无探测数据", new Font("Microsoft YaHei UI", 10F), emptyBrush, new PointF(16, 16));
                return;
            }

            var points = new PointF[previewLatency.Length];
            for (var i = 0; i < previewLatency.Length; i++)
            {
                var x = 14 + i * (area.Width - 28F) / (previewLatency.Length - 1F);
                var normalized = Math.Clamp(previewLatency[i] / 40F, 0F, 1F);
                var y = area.Height - 16 - normalized * (area.Height - 32);
                points[i] = new PointF(x, y);
            }

            using var path = new GraphicsPath();
            path.AddLines(points);
            var fillPoints = points.Concat([new PointF(points[^1].X, area.Height - 12), new PointF(points[0].X, area.Height - 12)]).ToArray();
            using var fillPath = new GraphicsPath();
            fillPath.AddPolygon(fillPoints);
            e.Graphics.FillPath(fillBrush, fillPath);
            e.Graphics.DrawLines(linePen, points);

            foreach (var point in points)
            {
                e.Graphics.FillEllipse(pointBrush, point.X - 3.2F, point.Y - 3.2F, 6.4F, 6.4F);
            }
        }
    }
}
