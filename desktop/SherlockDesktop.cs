// Build with desktop\build.cmd
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace SherlockDesktop
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (args.Length > 0 && String.Equals(args[0], "--self-test", StringComparison.OrdinalIgnoreCase))
            {
                Environment.ExitCode = RunSelfTest();
                return;
            }

            if (args.Length > 0 && String.Equals(args[0], "--screenshot", StringComparison.OrdinalIgnoreCase))
            {
                Environment.ExitCode = RunScreenshot();
                return;
            }

            if (args.Length > 0 && String.Equals(args[0], "--integration-test", StringComparison.OrdinalIgnoreCase))
            {
                Environment.ExitCode = RunIntegrationTest();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string projectRoot = Deployment.FindProjectRoot();
            if (projectRoot == null)
            {
                MessageBox.Show(
                    "无法找到 Sherlock 运行时。请确保此程序与 sherlock 目录位于同一部署目录中。",
                    "Sherlock 桌面版",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            Application.Run(new MainForm(projectRoot));
        }

        private static int RunScreenshot()
        {
            try
            {
                string root = Deployment.FindProjectRoot();
                if (root == null)
                {
                    return 2;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                using (MainForm form = new MainForm(root))
                {
                    form.StartPosition = FormStartPosition.Manual;
                    form.Location = new Point(-20000, -20000);
                    form.ShowInTaskbar = false;
                    form.Show();
                    Application.DoEvents();

                    using (Bitmap image = new Bitmap(form.Width, form.Height))
                    {
                        form.DrawToBitmap(image, new Rectangle(0, 0, form.Width, form.Height));
                        image.Save(Path.Combine(root, "work", "desktop-ui.png"), ImageFormat.Png);
                    }
                    form.Close();
                }
                return 0;
            }
            catch
            {
                return 3;
            }
        }

        private static int RunIntegrationTest()
        {
            try
            {
                string root = Deployment.FindProjectRoot();
                if (root == null)
                {
                    return 2;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                using (MainForm form = new MainForm(root))
                {
                    form.StartPosition = FormStartPosition.Manual;
                    form.Location = new Point(-20000, -20000);
                    form.ShowInTaskbar = false;
                    form.Show();
                    Application.DoEvents();

                    bool passed = form.RunAutomatedSearch();
                    string details =
                        "Passed: " + passed + Environment.NewLine +
                        "Status: " + form.LastSearchStatus + Environment.NewLine +
                        "OutputDirectory: " + form.LastOutputDirectory + Environment.NewLine;

                    if (!String.IsNullOrEmpty(form.LastOutputDirectory) && Directory.Exists(form.LastOutputDirectory))
                    {
                        string[] files = Directory.GetFiles(form.LastOutputDirectory);
                        foreach (string file in files)
                        {
                            details += "File: " + Path.GetFileName(file) + Environment.NewLine;
                        }
                    }

                    File.WriteAllText(Path.Combine(root, "work", "desktop-integration-selftest.txt"), details, Encoding.UTF8);
                    form.Close();
                    return passed ? 0 : 1;
                }
            }
            catch (Exception error)
            {
                try
                {
                    string root = Deployment.FindProjectRoot();
                    if (root != null)
                    {
                        File.WriteAllText(
                            Path.Combine(root, "work", "desktop-integration-selftest.txt"),
                            error.ToString(),
                            Encoding.UTF8);
                    }
                }
                catch
                {
                }
                return 3;
            }
        }

        private static int RunSelfTest()
        {
            try
            {
                string root = Deployment.FindProjectRoot();
                if (root == null)
                {
                    return 2;
                }

                string exe = Deployment.SherlockExecutable(root);
                ProcessStartInfo startInfo = new ProcessStartInfo(exe, "--version");
                startInfo.UseShellExecute = false;
                startInfo.CreateNoWindow = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.StandardOutputEncoding = Encoding.UTF8;
                startInfo.StandardErrorEncoding = Encoding.UTF8;
                startInfo.EnvironmentVariables["PYTHONUTF8"] = "1";
                startInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";

                using (Process process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    string workDir = Path.Combine(root, "work");
                    Directory.CreateDirectory(workDir);
                    File.WriteAllText(
                        Path.Combine(workDir, "desktop-selftest.txt"),
                        "ExitCode: " + process.ExitCode + Environment.NewLine +
                        "Output: " + output + Environment.NewLine +
                        "Error: " + error,
                        Encoding.UTF8);

                    return process.ExitCode == 0 && output.IndexOf("Sherlock v", StringComparison.OrdinalIgnoreCase) >= 0
                        ? 0
                        : 1;
                }
            }
            catch (Exception error)
            {
                try
                {
                    string root = Deployment.FindProjectRoot();
                    if (root != null)
                    {
                        string workDir = Path.Combine(root, "work");
                        Directory.CreateDirectory(workDir);
                        File.WriteAllText(Path.Combine(workDir, "desktop-selftest.txt"), error.ToString(), Encoding.UTF8);
                    }
                }
                catch
                {
                }

                return 3;
            }
        }
    }

    internal static class Deployment
    {
        public static string SherlockExecutable(string root)
        {
            string executable = FindInstalledExecutable(root);
            return executable ?? Path.Combine(root, "sherlock", "runtime", "python", "Scripts", "sherlock.exe");
        }

        public static string FindInstalledExecutable(string root)
        {
            string[] candidates = new string[]
            {
                Path.Combine(root, "sherlock", "runtime", "python", "Scripts", "sherlock.exe"),
                Path.Combine(root, "sherlock", ".venv", "Scripts", "sherlock.exe")
            };

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }

        public static string FindProjectRoot()
        {
            string[] starts = new string[]
            {
                AppDomain.CurrentDomain.BaseDirectory,
                Environment.CurrentDirectory
            };

            foreach (string start in starts)
            {
                try
                {
                    DirectoryInfo current = new DirectoryInfo(start);
                    for (int depth = 0; current != null && depth < 6; depth++)
                    {
                        if (FindInstalledExecutable(current.FullName) != null)
                        {
                            return current.FullName;
                        }
                        current = current.Parent;
                    }
                }
                catch
                {
                }
            }

            return null;
        }
    }

    internal sealed class MainForm : Form
    {
        private static readonly Color Accent = Color.FromArgb(239, 146, 43);
        private static readonly Color DarkBlue = Color.FromArgb(25, 50, 76);
        private static readonly Regex AnsiPattern = new Regex(
            @"\x1B(?:[@-Z\\-_]|\[[0-?]*[ -/]*[@-~]|\][^\x07]*(?:\x07|\x1B\\))|[\x00-\x08\x0B\x0C\x0E-\x1F]",
            RegexOptions.Compiled);

        private readonly string projectRoot;
        private readonly string resultsRoot;
        private readonly TextBox usernameBox;
        private readonly ComboBox scopeBox;
        private readonly TextBox customSitesBox;
        private readonly TextBox proxyBox;
        private readonly NumericUpDown timeoutBox;
        private readonly CheckBox printAllBox;
        private readonly CheckBox txtBox;
        private readonly CheckBox csvBox;
        private readonly CheckBox xlsxBox;
        private Button searchButton;
        private Button stopButton;
        private Button openButton;
        private Button clearButton;
        private readonly RichTextBox logBox;
        private readonly Label statusLabel;
        private readonly Label resultPathLabel;
        private Process runningProcess;
        private bool stopRequested;
        private string lastOutputDirectory;

        internal string LastSearchStatus
        {
            get { return statusLabel == null ? "未启动" : statusLabel.Text; }
        }

        internal string LastOutputDirectory
        {
            get { return lastOutputDirectory; }
        }

        public MainForm(string projectRoot)
        {
            this.projectRoot = projectRoot;
            this.resultsRoot = Path.Combine(projectRoot, "outputs", "Sherlock检索结果");
            Directory.CreateDirectory(resultsRoot);

            Text = "Sherlock 桌面版";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(880, 640);
            Size = new Size(1020, 760);
            BackColor = Color.FromArgb(245, 247, 250);
            Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Regular, GraphicsUnit.Point);

            TableLayoutPanel rootLayout = new TableLayoutPanel();
            rootLayout.Dock = DockStyle.Fill;
            rootLayout.ColumnCount = 1;
            rootLayout.RowCount = 3;
            rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 88F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42F));
            rootLayout.Padding = new Padding(0);
            rootLayout.Margin = new Padding(0);
            Controls.Add(rootLayout);

            Panel header = BuildHeader();
            header.Dock = DockStyle.Fill;
            rootLayout.Controls.Add(header, 0, 0);

            Panel status = BuildStatusBar();
            status.Dock = DockStyle.Fill;
            rootLayout.Controls.Add(status, 0, 2);

            Panel content = new Panel();
            content.Dock = DockStyle.Fill;
            content.Padding = new Padding(20, 16, 20, 16);
            rootLayout.Controls.Add(content, 0, 1);

            TableLayoutPanel contentLayout = new TableLayoutPanel();
            contentLayout.Dock = DockStyle.Fill;
            contentLayout.ColumnCount = 1;
            contentLayout.RowCount = 3;
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 204F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
            contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            content.Controls.Add(contentLayout);

            GroupBox formGroup = new GroupBox();
            formGroup.Text = "检索设置";
            formGroup.Font = new Font(Font, FontStyle.Bold);
            formGroup.Dock = DockStyle.Fill;
            formGroup.Margin = new Padding(0, 0, 0, 10);
            formGroup.Padding = new Padding(16);
            contentLayout.Controls.Add(formGroup, 0, 0);

            Label usernameLabel = MakeLabel("用户名", 20, 36);
            formGroup.Controls.Add(usernameLabel);
            usernameBox = new TextBox();
            usernameBox.Left = 116;
            usernameBox.Top = 31;
            usernameBox.Width = 780;
            usernameBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            usernameBox.Font = new Font("Microsoft YaHei UI", 10F);
            formGroup.Controls.Add(usernameBox);

            Label scopeLabel = MakeLabel("检查范围", 20, 78);
            formGroup.Controls.Add(scopeLabel);
            scopeBox = new ComboBox();
            scopeBox.DropDownStyle = ComboBoxStyle.DropDownList;
            scopeBox.Left = 116;
            scopeBox.Top = 73;
            scopeBox.Width = 270;
            scopeBox.Items.Add("全部支持的站点（完整）");
            scopeBox.Items.Add("GitHub（快速验证）");
            scopeBox.Items.Add("常用站点");
            scopeBox.Items.Add("自定义站点");
            scopeBox.SelectedIndex = 0;
            scopeBox.SelectedIndexChanged += ScopeChanged;
            formGroup.Controls.Add(scopeBox);

            Label timeoutLabel = MakeLabel("超时（秒）", 408, 78);
            formGroup.Controls.Add(timeoutLabel);
            timeoutBox = new NumericUpDown();
            timeoutBox.Left = 496;
            timeoutBox.Top = 73;
            timeoutBox.Width = 70;
            timeoutBox.Minimum = 3;
            timeoutBox.Maximum = 180;
            timeoutBox.Value = 20;
            formGroup.Controls.Add(timeoutBox);

            Label proxyLabel = MakeLabel("代理（可选）", 590, 78);
            formGroup.Controls.Add(proxyLabel);
            proxyBox = new TextBox();
            proxyBox.Left = 686;
            proxyBox.Top = 73;
            proxyBox.Width = 210;
            proxyBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            proxyBox.Text = "";
            formGroup.Controls.Add(proxyBox);

            Label customLabel = MakeLabel("自定义站点", 20, 120);
            formGroup.Controls.Add(customLabel);
            customSitesBox = new TextBox();
            customSitesBox.Left = 116;
            customSitesBox.Top = 115;
            customSitesBox.Width = 780;
            customSitesBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            customSitesBox.Text = "GitHub, Reddit, Instagram";
            customSitesBox.Enabled = false;
            formGroup.Controls.Add(customSitesBox);

            printAllBox = MakeCheckBox("显示/导出未找到项", 116, 157, true);
            txtBox = MakeCheckBox("TXT", 292, 157, true);
            csvBox = MakeCheckBox("CSV", 374, 157, true);
            xlsxBox = MakeCheckBox("Excel", 456, 157, true);
            formGroup.Controls.Add(printAllBox);
            formGroup.Controls.Add(txtBox);
            formGroup.Controls.Add(csvBox);
            formGroup.Controls.Add(xlsxBox);

            Label logTitle = new Label();
            logTitle.Text = "实时检索结果";
            logTitle.Font = new Font(Font.FontFamily, 10F, FontStyle.Bold);
            logTitle.ForeColor = DarkBlue;
            logTitle.Dock = DockStyle.Fill;
            logTitle.Padding = new Padding(2, 9, 0, 0);
            contentLayout.Controls.Add(logTitle, 0, 1);

            Panel logPanel = new Panel();
            logPanel.Dock = DockStyle.Fill;
            logPanel.Padding = new Padding(1);
            logPanel.BackColor = Color.FromArgb(210, 217, 225);
            logPanel.Margin = new Padding(0);
            contentLayout.Controls.Add(logPanel, 0, 2);

            logBox = new RichTextBox();
            logBox.Dock = DockStyle.Fill;
            logBox.ReadOnly = true;
            logBox.BackColor = Color.FromArgb(20, 29, 38);
            logBox.ForeColor = Color.FromArgb(218, 228, 236);
            logBox.Font = new Font("Consolas", 9.5F);
            logBox.BorderStyle = BorderStyle.None;
            logBox.DetectUrls = false;
            logBox.WordWrap = false;
            logPanel.Controls.Add(logBox);

            resultPathLabel = new Label();
            resultPathLabel.Text = "结果目录：" + resultsRoot;
            resultPathLabel.AutoEllipsis = true;
            resultPathLabel.Dock = DockStyle.Fill;
            resultPathLabel.TextAlign = ContentAlignment.MiddleLeft;
            resultPathLabel.ForeColor = Color.FromArgb(80, 92, 105);
            status.Controls.Add(resultPathLabel);

            statusLabel = new Label();
            statusLabel.Text = "就绪";
            statusLabel.Width = 250;
            statusLabel.Dock = DockStyle.Right;
            statusLabel.TextAlign = ContentAlignment.MiddleRight;
            statusLabel.ForeColor = DarkBlue;
            statusLabel.Font = new Font(Font, FontStyle.Bold);
            status.Controls.Add(statusLabel);

            FormClosing += MainFormClosing;
            Activated += delegate
            {
                if (usernameBox.Text.Length == 0)
                {
                    usernameBox.Focus();
                }
            };
        }

        private Panel BuildHeader()
        {
            Panel header = new Panel();
            header.Dock = DockStyle.Top;
            header.Height = 88;
            header.BackColor = DarkBlue;

            Label title = new Label();
            title.Text = "Sherlock 本地桌面版";
            title.ForeColor = Color.White;
            title.Font = new Font("Microsoft YaHei UI", 17F, FontStyle.Bold);
            title.AutoSize = true;
            title.Left = 22;
            title.Top = 15;
            header.Controls.Add(title);

            Label subtitle = new Label();
            subtitle.Text = "输入公开用户名，自动检查站点并生成检索报告";
            subtitle.ForeColor = Color.FromArgb(196, 211, 225);
            subtitle.AutoSize = true;
            subtitle.Left = 25;
            subtitle.Top = 53;
            header.Controls.Add(subtitle);

            FlowLayoutPanel actions = new FlowLayoutPanel();
            actions.Dock = DockStyle.Right;
            actions.Width = 430;
            actions.Padding = new Padding(0, 20, 12, 0);
            actions.WrapContents = false;
            actions.FlowDirection = FlowDirection.LeftToRight;
            actions.BackColor = DarkBlue;
            header.Controls.Add(actions);

            searchButton = MakeActionButton("开始检索", 0, 0, 112, true);
            searchButton.Margin = new Padding(0, 0, 8, 0);
            searchButton.Click += StartSearch;
            actions.Controls.Add(searchButton);

            stopButton = MakeActionButton("停止", 0, 0, 75, false);
            stopButton.Margin = new Padding(0, 0, 8, 0);
            stopButton.Enabled = false;
            stopButton.Click += StopSearch;
            actions.Controls.Add(stopButton);

            openButton = MakeActionButton("结果目录", 0, 0, 92, false);
            openButton.Margin = new Padding(0, 0, 8, 0);
            openButton.Click += delegate { OpenResultsFolder(resultsRoot); };
            actions.Controls.Add(openButton);

            clearButton = MakeActionButton("清空", 0, 0, 58, false);
            clearButton.Margin = new Padding(0);
            clearButton.Click += delegate { logBox.Clear(); };
            actions.Controls.Add(clearButton);

            return header;
        }

        private Panel BuildStatusBar()
        {
            Panel status = new Panel();
            status.Dock = DockStyle.Bottom;
            status.Height = 42;
            status.Padding = new Padding(20, 0, 20, 0);
            status.BackColor = Color.White;
            return status;
        }

        private static Label MakeLabel(string text, int left, int top)
        {
            Label label = new Label();
            label.Text = text;
            label.Left = left;
            label.Top = top;
            label.AutoSize = true;
            return label;
        }

        private static CheckBox MakeCheckBox(string text, int left, int top, bool isChecked)
        {
            CheckBox box = new CheckBox();
            box.Text = text;
            box.Left = left;
            box.Top = top;
            box.AutoSize = true;
            box.Checked = isChecked;
            return box;
        }

        private static Button MakeActionButton(string text, int left, int top, int width, bool primary)
        {
            Button button = new Button();
            button.Text = text;
            button.Left = left;
            button.Top = top;
            button.Width = width;
            button.Height = 40;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = primary ? 0 : 1;
            button.BackColor = primary ? Accent : Color.FromArgb(43, 74, 103);
            button.ForeColor = Color.White;
            button.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold);
            button.Cursor = Cursors.Hand;
            return button;
        }

        private void ScopeChanged(object sender, EventArgs e)
        {
            customSitesBox.Enabled = scopeBox.SelectedIndex == 3;
        }

        internal bool RunAutomatedSearch()
        {
            usernameBox.Text = "octocat";
            scopeBox.SelectedIndex = 1;
            timeoutBox.Value = 15;
            printAllBox.Checked = true;
            txtBox.Checked = true;
            csvBox.Checked = true;
            xlsxBox.Checked = true;
            StartSearch(this, EventArgs.Empty);

            DateTime deadline = DateTime.UtcNow.AddMinutes(2);
            while (runningProcess != null && DateTime.UtcNow < deadline)
            {
                Application.DoEvents();
                Thread.Sleep(50);
            }
            Application.DoEvents();

            if (!String.IsNullOrEmpty(lastOutputDirectory) && Directory.Exists(lastOutputDirectory))
            {
                string name = usernameBox.Text.Trim();
                return statusLabel.Text == "检索完成" &&
                    File.Exists(Path.Combine(lastOutputDirectory, name + ".txt")) &&
                    File.Exists(Path.Combine(lastOutputDirectory, name + ".csv")) &&
                    File.Exists(Path.Combine(lastOutputDirectory, name + ".xlsx"));
            }

            return false;
        }

        private void StartSearch(object sender, EventArgs e)
        {
            string username = usernameBox.Text.Trim();
            if (username.Length == 0)
            {
                MessageBox.Show("请输入要检索的用户名。", "缺少用户名", MessageBoxButtons.OK, MessageBoxIcon.Information);
                usernameBox.Focus();
                return;
            }

            if (username.IndexOf("://", StringComparison.Ordinal) >= 0 || username.IndexOf("@", StringComparison.Ordinal) == 0)
            {
                MessageBox.Show(
                    "请填写用户名本身，不要填写网址或带 @ 的账号引用。\n例如填写：octocat",
                    "用户名格式",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                usernameBox.Focus();
                return;
            }

            if (!txtBox.Checked && !csvBox.Checked && !xlsxBox.Checked)
            {
                DialogResult answer = MessageBox.Show(
                    "当前没有选择任何报告格式，检索结果只会显示在窗口中。是否继续？",
                    "未选择报告格式",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (answer != DialogResult.Yes)
                {
                    return;
                }
            }

            string safeUser = MakeSafeFolderName(username);
            string timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string outputDirectory = Path.Combine(resultsRoot, safeUser, timestamp);
            Directory.CreateDirectory(outputDirectory);
            lastOutputDirectory = outputDirectory;

            string arguments = BuildArguments(username);
            ProcessStartInfo startInfo = new ProcessStartInfo(Deployment.SherlockExecutable(projectRoot), arguments);
            startInfo.WorkingDirectory = outputDirectory;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.StandardOutputEncoding = Encoding.UTF8;
            startInfo.StandardErrorEncoding = Encoding.UTF8;
            startInfo.EnvironmentVariables["PYTHONUTF8"] = "1";
            startInfo.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
            startInfo.EnvironmentVariables["PYTHONUNBUFFERED"] = "1";

            Process process = new Process();
            process.StartInfo = startInfo;
            process.EnableRaisingEvents = true;
            process.OutputDataReceived += delegate(object outputSender, DataReceivedEventArgs output)
            {
                if (!String.IsNullOrEmpty(output.Data))
                {
                    AppendOutput(output.Data, Color.FromArgb(218, 228, 236));
                }
            };
            process.ErrorDataReceived += delegate(object errorSender, DataReceivedEventArgs error)
            {
                if (!String.IsNullOrEmpty(error.Data))
                {
                    AppendOutput("[错误] " + error.Data, Color.FromArgb(255, 162, 138));
                }
            };
            process.Exited += delegate
            {
                try
                {
                    process.WaitForExit();
                    BeginInvoke(new Action<Process, string>(FinishSearch), process, outputDirectory);
                }
                catch
                {
                }
            };

            stopRequested = false;
            runningProcess = process;
            searchButton.Enabled = false;
            stopButton.Enabled = true;
            openButton.Enabled = false;
            statusLabel.Text = "正在检索…";
            resultPathLabel.Text = "本次结果目录：" + outputDirectory;
            logBox.Clear();
            AppendOutput("Sherlock 桌面版", Accent);
            AppendOutput("目标用户名：" + username, Color.White);
            AppendOutput("结果目录：" + outputDirectory, Color.FromArgb(160, 205, 240));
            AppendOutput(new String('-', 90), Color.FromArgb(90, 108, 124));

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
            }
            catch (Exception error)
            {
                runningProcess = null;
                searchButton.Enabled = true;
                stopButton.Enabled = false;
                openButton.Enabled = true;
                statusLabel.Text = "启动失败";
                AppendOutput("[启动失败] " + error.Message, Color.FromArgb(255, 120, 120));
                MessageBox.Show(error.Message, "无法启动 Sherlock", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private string BuildArguments(string username)
        {
            StringBuilder args = new StringBuilder();
            AppendArgument(args, username);
            args.Append(" --local --no-color --timeout ");
            args.Append(((int)timeoutBox.Value).ToString());

            if (printAllBox.Checked)
            {
                args.Append(" --print-all");
            }
            if (txtBox.Checked)
            {
                args.Append(" --txt");
            }
            if (csvBox.Checked)
            {
                args.Append(" --csv");
            }
            if (xlsxBox.Checked)
            {
                args.Append(" --xlsx");
            }

            if (scopeBox.SelectedIndex == 1)
            {
                args.Append(" --site ");
                AppendArgument(args, "GitHub");
            }
            else if (scopeBox.SelectedIndex == 2)
            {
                string[] commonSites = new string[] { "GitHub", "Reddit", "Instagram", "TikTok" };
                foreach (string site in commonSites)
                {
                    args.Append(" --site ");
                    AppendArgument(args, site);
                }
            }
            else if (scopeBox.SelectedIndex == 3)
            {
                string[] sites = customSitesBox.Text.Split(new char[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string item in sites)
                {
                    string site = item.Trim();
                    if (site.Length > 0)
                    {
                        args.Append(" --site ");
                        AppendArgument(args, site);
                    }
                }
            }

            string proxy = proxyBox.Text.Trim();
            if (proxy.Length > 0)
            {
                args.Append(" --proxy ");
                AppendArgument(args, proxy);
            }

            return args.ToString();
        }

        private static void AppendArgument(StringBuilder builder, string value)
        {
            bool quote = value.Length == 0 || value.IndexOfAny(new char[] { ' ', '\t', '"' }) >= 0;
            if (!quote)
            {
                builder.Append(value);
                return;
            }

            builder.Append('"');
            int backslashes = 0;
            foreach (char character in value)
            {
                if (character == '\\')
                {
                    backslashes++;
                }
                else if (character == '"')
                {
                    builder.Append('\\', backslashes * 2 + 1);
                    builder.Append('"');
                    backslashes = 0;
                }
                else
                {
                    builder.Append('\\', backslashes);
                    builder.Append(character);
                    backslashes = 0;
                }
            }
            builder.Append('\\', backslashes * 2);
            builder.Append('"');
        }

        private void FinishSearch(Process process, string outputDirectory)
        {
            if (runningProcess == process)
            {
                runningProcess = null;
            }

            searchButton.Enabled = true;
            stopButton.Enabled = false;
            openButton.Enabled = true;

            AppendOutput(new String('-', 90), Color.FromArgb(90, 108, 124));
            if (stopRequested)
            {
                statusLabel.Text = "已停止";
                AppendOutput("检索已停止。已生成文件保存在：" + outputDirectory, Color.FromArgb(245, 190, 100));
            }
            else if (process.ExitCode == 0)
            {
                statusLabel.Text = "检索完成";
                AppendOutput("检索完成。报告位于：" + outputDirectory, Color.FromArgb(127, 221, 157));
            }
            else
            {
                statusLabel.Text = "检索结束，存在错误";
                AppendOutput("Sherlock 退出代码：" + process.ExitCode, Color.FromArgb(255, 140, 140));
            }

            process.Dispose();
        }

        private void StopSearch(object sender, EventArgs e)
        {
            Process process = runningProcess;
            if (process == null)
            {
                return;
            }

            stopRequested = true;
            statusLabel.Text = "正在停止…";
            stopButton.Enabled = false;

            try
            {
                ProcessStartInfo killInfo = new ProcessStartInfo(
                    "taskkill.exe",
                    "/PID " + process.Id.ToString() + " /T /F");
                killInfo.UseShellExecute = false;
                killInfo.CreateNoWindow = true;
                Process.Start(killInfo);
            }
            catch (Exception error)
            {
                AppendOutput("[停止失败] " + error.Message, Color.FromArgb(255, 140, 140));
            }
        }

        private void AppendOutput(string line, Color color)
        {
            if (IsDisposed)
            {
                return;
            }

            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, Color>(AppendOutput), line, color);
                return;
            }

            string clean = AnsiPattern.Replace(line, "").Replace("\r", "");
            if (clean.IndexOf("A problem occurred while checking for an update", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return;
            }

            logBox.SelectionStart = logBox.TextLength;
            logBox.SelectionLength = 0;
            logBox.SelectionColor = color;
            logBox.AppendText(clean + Environment.NewLine);
            logBox.SelectionColor = logBox.ForeColor;
            logBox.ScrollToCaret();
        }

        private void OpenResultsFolder(string folder)
        {
            try
            {
                Directory.CreateDirectory(folder);
                ProcessStartInfo info = new ProcessStartInfo("explorer.exe", "\"" + folder + "\"");
                info.UseShellExecute = true;
                Process.Start(info);
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, "无法打开结果目录", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void MainFormClosing(object sender, FormClosingEventArgs e)
        {
            Process process = runningProcess;
            if (process != null)
            {
                DialogResult answer = MessageBox.Show(
                    "检索仍在进行。是否停止检索并关闭窗口？",
                    "Sherlock 正在运行",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);
                if (answer != DialogResult.Yes)
                {
                    e.Cancel = true;
                    return;
                }

                try
                {
                    ProcessStartInfo killInfo = new ProcessStartInfo(
                        "taskkill.exe",
                        "/PID " + process.Id.ToString() + " /T /F");
                    killInfo.UseShellExecute = false;
                    killInfo.CreateNoWindow = true;
                    Process.Start(killInfo);
                }
                catch
                {
                }
            }
        }

        private static string MakeSafeFolderName(string value)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            StringBuilder safe = new StringBuilder(value.Length);
            foreach (char character in value)
            {
                safe.Append(Array.IndexOf(invalid, character) >= 0 ? '_' : character);
            }

            string result = safe.ToString().Trim().TrimEnd('.');
            return result.Length == 0 ? "username" : result;
        }
    }
}
