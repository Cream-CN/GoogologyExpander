using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace GoogologyExpander
{
	/// <summary>
	/// GoogologyExpander 主窗体：为所有记法提供统一的展开入口。
	/// 一维记法遵循 PrSS 接口规范（int[] → int[]，一次展开一步）；
	/// 二维记法遵循 BMS 接口规范（矩阵 + 步数 n，原地展开）。
	/// </summary>
	public class Form1 : Form
	{
		// ==================== 记法注册 ====================

		/// <summary>所有一维记法：名称 → 展开函数。</summary>
		private static readonly (string Name, Func<int[], int[]> Expand)[] OneDimSystems =
		{
			("PrSS",  PrSS.ExpandPrSS),
			("HPrSS", HPrSS.ExpandHPrSS),
			("LPrSS", LPrSS.ExpandLPrSS),
			("0-Y",   ZeroY.Expand0Y),
			("Y",     Y.ExpandY),
			("WY",    WY.ExpandWY),
			("EY",    EY.ExpandEY),
		};

		/// <summary>所有二维记法。</summary>
		private static readonly string[] TwoDimSystems = { "BMS", "UPMS" };

		/// <summary>每个记法的示例输入，方便用户一键体验。</summary>
		private static readonly Dictionary<string, string> Examples = new Dictionary<string, string>
		{
			["PrSS"]  = "1, 2, 3",
			["HPrSS"] = "1, 2, 3",
			["LPrSS"] = "1, 3",
			["0-Y"]   = "1, 2, 3",
			["Y"]     = "1, 2, 3",
			["WY"]    = "1, 2, 3",
			["EY"]    = "1, 2, 3",
			["BMS"]   = "1, 1, 1\r\n2, 2, 2",
			["UPMS"]  = "0, 1, 2\r\n0, 0, 0",
		};

		/// <summary>元素分隔符（半角/全角逗号、空格、制表符）。</summary>
		private static readonly char[] TokenSeparators = { ',', '，', ' ', '\t' };

		// ==================== 控件 ====================

		private ComboBox cboSystem = null!;
		private Label lblN = null!;
		private NumericUpDown numSteps = null!;
		private TextBox txtInput = null!;
		private TextBox txtOutput = null!;
		private Button btnExpand = null!;
		private Button btnIterate = null!;
		private Button btnCopy = null!;
		private Button btnClear = null!;
		private ToolStripStatusLabel lblStatus = null!;

		/// <summary>当前选中的记法名称。</summary>
		private string CurrentSystem => cboSystem.SelectedItem as string ?? "PrSS";

		private bool IsTwoDim => Array.IndexOf(TwoDimSystems, CurrentSystem) >= 0;

		public Form1()
		{
			InitializeUi();

			// 首次打开即填入当前记法的示例并全选，用户按 Enter 即可体验
			txtInput.Text = Examples[CurrentSystem];
			txtInput.SelectAll();
		}

		// ==================== 界面构建 ====================

		private void InitializeUi()
		{
			Text = "Googology Expander";
			Font = new Font("Microsoft YaHei UI", 9F);
			StartPosition = FormStartPosition.CenterScreen;
			Size = new Size(780, 580);
			MinimumSize = new Size(660, 480);
			KeyPreview = true;

			var tooltip = new ToolTip();

			// —— 顶部选项栏：记法选择 + 展开步数 ——
			var topPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Top,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Padding = new Padding(12, 10, 12, 4),
			};

			topPanel.Controls.Add(CreateCaption("记法："));

			cboSystem = new ComboBox
			{
				DropDownStyle = ComboBoxStyle.DropDownList,
				Width = 110,
				Margin = new Padding(0, 2, 20, 0),
			};
			cboSystem.Items.AddRange(OneDimSystems.Select(s => (object)s.Name)
				.Concat(TwoDimSystems.Cast<object>()).ToArray());
			cboSystem.SelectedIndex = 0;
			cboSystem.SelectedIndexChanged += OnSystemChanged;
			tooltip.SetToolTip(cboSystem, "一维记法每次展开一步；二维记法可指定展开步数。");
			topPanel.Controls.Add(cboSystem);

			lblN = CreateCaption("展开步数 n：");
			lblN.Enabled = false;
			topPanel.Controls.Add(lblN);

			numSteps = new NumericUpDown
			{
				Minimum = 1,
				Maximum = 1000,
				Value = 1,
				Width = 72,
				Margin = new Padding(0, 2, 0, 0),
				Enabled = false,
			};
			tooltip.SetToolTip(numSteps, "重复展开的次数，仅对二维记法（BMS/UPMS）有效。");
			topPanel.Controls.Add(numSteps);

			// —— 主区域：上输入、下输出 ——
			var root = new TableLayoutPanel
			{
				Dock = DockStyle.Fill,
				Padding = new Padding(12, 4, 12, 4),
				ColumnCount = 1,
			};
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));
			root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
			root.RowStyles.Add(new RowStyle(SizeType.Percent, 50F));

			var lblInput = CreateCaption("输入（一维：整数序列；二维：每行一行的矩阵）");
			lblInput.Margin = new Padding(0, 2, 0, 2);
			root.Controls.Add(lblInput, 0, 0);

			txtInput = new TextBox
			{
				Multiline = true,
				AcceptsReturn = true,
				ScrollBars = ScrollBars.Vertical,
				Dock = DockStyle.Fill,
				Font = new Font("Consolas", 10F),
			};
			tooltip.SetToolTip(txtInput, "整数用逗号或空格分隔；矩阵每行一行（也可用分号分隔行）。\r\n按 Ctrl+Enter 快速展开。");
			root.Controls.Add(txtInput, 0, 1);

			var lblOutput = CreateCaption("展开结果");
			lblOutput.Margin = new Padding(0, 8, 0, 2);
			root.Controls.Add(lblOutput, 0, 2);

			txtOutput = new TextBox
			{
				Multiline = true,
				ReadOnly = true,
				WordWrap = false,
				ScrollBars = ScrollBars.Both,
				Dock = DockStyle.Fill,
				BackColor = SystemColors.Window,
				Font = new Font("Consolas", 10F),
			};
			root.Controls.Add(txtOutput, 0, 3);

			// —— 底部按钮栏 ——
			var bottomPanel = new FlowLayoutPanel
			{
				Dock = DockStyle.Bottom,
				AutoSize = true,
				AutoSizeMode = AutoSizeMode.GrowAndShrink,
				Padding = new Padding(12, 6, 12, 6),
			};

			btnExpand = CreateButton("展开 (Ctrl+Enter)", DoExpand, true);
			btnIterate = CreateButton("用结果继续展开", DoIterate, false);
			btnCopy = CreateButton("复制结果", DoCopy, false);
			btnClear = CreateButton("清空", DoClear, false);
			bottomPanel.Controls.AddRange(new Control[] { btnExpand, btnIterate, btnCopy, btnClear });

			// —— 状态栏 ——
			var statusStrip = new StatusStrip();
			lblStatus = new ToolStripStatusLabel("就绪。选择记法，输入序列（已预填示例），按 Enter 展开。");
			statusStrip.Items.Add(lblStatus);

			// Dock 布局加入顺序：先 Fill，再 Bottom，再 Top，最后状态栏
			Controls.Add(root);
			Controls.Add(bottomPanel);
			Controls.Add(topPanel);
			Controls.Add(statusStrip);

			AcceptButton = btnExpand;
		}

		private static Label CreateCaption(string text) => new Label
		{
			Text = text,
			AutoSize = true,
			Margin = new Padding(0, 6, 4, 0),
		};

		private static Button CreateButton(string text, EventHandler onClick, bool isPrimary)
		{
			var button = new Button
			{
				Text = text,
				AutoSize = true,
				Margin = new Padding(0, 0, 10, 0),
			};
			if (isPrimary)
				button.Font = new Font(button.Font, FontStyle.Bold);
			button.Click += onClick;
			return button;
		}

		// ==================== 交互逻辑 ====================

		/// <summary>切换记法：同步步数控件可用性，并自动换上新记法的示例。</summary>
		private void OnSystemChanged(object? sender, EventArgs e)
		{
			lblN.Enabled = numSteps.Enabled = IsTwoDim;

			// 输入为空、或仍是任一记法的示例时，自动替换为新记法的示例
			string current = txtInput.Text.Trim();
			if (current.Length == 0 || Examples.ContainsValue(current))
				txtInput.Text = Examples[CurrentSystem];

			lblStatus.Text = IsTwoDim
				? $"已选择 {CurrentSystem}：输入矩阵（每行一行），可指定展开步数 n。"
				: $"已选择 {CurrentSystem}：输入整数序列，每次展开一步。";
		}

		/// <summary>Ctrl+Enter 快捷展开（多行输入框中 Enter 用于换行）。</summary>
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
		{
			if (keyData == (Keys.Control | Keys.Enter))
			{
				DoExpand(null, EventArgs.Empty);
				return true;
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}

		private void DoExpand(object? sender, EventArgs e)
		{
			string system = CurrentSystem;

			if (string.IsNullOrWhiteSpace(txtInput.Text))
			{
				NotifyError("请先输入内容。");
				txtInput.Focus();
				return;
			}

			var stopwatch = Stopwatch.StartNew();
			try
			{
				string result = IsTwoDim ? ExpandTwoDim(system) : ExpandOneDim(system);
				stopwatch.Stop();

				txtOutput.Text = result;
				lblStatus.Text = $"{system} 展开成功，耗时 {stopwatch.ElapsedMilliseconds} ms。可点“用结果继续展开”连续迭代。";
			}
			catch (Exception ex) when (ex is FormatException || ex is OverflowException
									   || ex is ArgumentException || ex is InvalidOperationException)
			{
				lblStatus.Text = $"{system} 展开失败。";
				NotifyError($"{system} 展开失败：\r\n{ex.Message}");
			}
		}

		/// <summary>把结果回填输入框再展开一次，方便连续迭代。</summary>
		private void DoIterate(object? sender, EventArgs e)
		{
			if (txtOutput.TextLength == 0)
			{
				NotifyError("还没有展开结果，无法继续展开。");
				return;
			}

			txtInput.Text = txtOutput.Text;
			DoExpand(null, EventArgs.Empty);
		}

		private void DoCopy(object? sender, EventArgs e)
		{
			if (txtOutput.TextLength == 0)
			{
				lblStatus.Text = "暂无结果可复制。";
				return;
			}

			try
			{
				Clipboard.SetText(txtOutput.Text);
				lblStatus.Text = "结果已复制到剪贴板。";
			}
			catch
			{
				lblStatus.Text = "复制失败：剪贴板被占用，请稍后重试。";
			}
		}

		private void DoClear(object? sender, EventArgs e)
		{
			txtInput.Text = Examples[CurrentSystem];
			txtOutput.Clear();
			lblStatus.Text = "已清空，并重新填入示例，按 Enter 即可展开。";
			txtInput.Focus();
			txtInput.SelectAll();
		}

		private void NotifyError(string message)
		{
			MessageBox.Show(this, message, "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}

		// ==================== 展开分发 ====================

		/// <summary>一维展开：PrSS 样式接口，int[] → int[]。</summary>
		private string ExpandOneDim(string system)
		{
			if (!TryParseSequence(txtInput.Text, out int[] sequence, out string error))
				throw new FormatException(error);

			Func<int[], int[]> expand = OneDimSystems.First(s => s.Name == system).Expand;
			return string.Join(", ", expand(sequence));
		}

		/// <summary>二维展开：BMS 样式接口，矩阵 + 步数，原地展开。</summary>
		private string ExpandTwoDim(string system)
		{
			if (!TryParseMatrix(txtInput.Text, system == "UPMS", out List<List<double>> rows, out string error))
				throw new FormatException(error);

			int n = (int)numSteps.Value;

			if (system == "BMS")
			{
				BMS.ExpandBMS(rows, n);
				return FormatRows(rows);
			}

			// UPMS 内部按列存储：把输入的行转置为列，展开后再转回行显示
			List<List<int>> columns = TransposeToColumns(rows);
			UPMS.ExpandUPMS(columns, n);

			if (columns.Count == 0)
				throw new InvalidOperationException("UPMS 判定输入矩阵非法（要求非负整数等），已返回空矩阵，请检查输入。");

			return FormatRows(TransposeToRows(columns));
		}

		// ==================== 输入校验 ====================

		/// <summary>解析一维整数序列；失败时给出带出错位置的提示。</summary>
		private static bool TryParseSequence(string text, out int[] sequence, out string error)
		{
			sequence = Array.Empty<int>();
			string[] tokens = text.Split(TokenSeparators, StringSplitOptions.RemoveEmptyEntries);
			if (tokens.Length == 0)
			{
				error = "输入为空，请至少输入一个整数。";
				return false;
			}

			var values = new List<int>(tokens.Length);
			for (int i = 0; i < tokens.Length; i++)
			{
				if (!int.TryParse(tokens[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
				{
					error = $"第 {i + 1} 项 “{tokens[i]}” 不是合法的整数，请检查后重新输入。";
					return false;
				}
				values.Add(value);
			}

			sequence = values.ToArray();
			error = string.Empty;
			return true;
		}

		/// <summary>解析矩阵：每行一行（或用分号分隔行），元素用逗号/空格分隔。</summary>
		private static bool TryParseMatrix(string text, bool integerOnly, out List<List<double>> rows, out string error)
		{
			rows = new List<List<double>>();

			string[] lines = text.Replace("\r", string.Empty)
								 .Split(new[] { '\n', ';', '；' }, StringSplitOptions.RemoveEmptyEntries);
			if (lines.Length == 0)
			{
				error = "输入为空，请至少输入一行矩阵数据。";
				return false;
			}

			for (int r = 0; r < lines.Length; r++)
			{
				string[] tokens = lines[r].Split(TokenSeparators, StringSplitOptions.RemoveEmptyEntries);
				if (tokens.Length == 0)
				{
					error = $"第 {r + 1} 行没有任何元素，请检查输入。";
					return false;
				}

				var row = new List<double>(tokens.Length);
				for (int c = 0; c < tokens.Length; c++)
				{
					if (integerOnly)
					{
						if (!int.TryParse(tokens[c], NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
						{
							error = $"第 {r + 1} 行第 {c + 1} 列 “{tokens[c]}” 不是合法的整数（UPMS 仅支持整数矩阵）。";
							return false;
						}
						row.Add(intValue);
					}
					else if (!double.TryParse(tokens[c], NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
					{
						error = $"第 {r + 1} 行第 {c + 1} 列 “{tokens[c]}” 不是合法的数字。";
						return false;
					}
					else
					{
						row.Add(doubleValue);
					}
				}
				rows.Add(row);
			}

			error = string.Empty;
			return true;
		}

		// ==================== 结果格式化与矩阵转置 ====================

		private static string FormatRows(List<List<double>> rows)
		{
			if (rows.Count == 0)
				return "(空矩阵)";
			return string.Join(Environment.NewLine,
				rows.Select(r => string.Join(", ", r.Select(FormatNumber))));
		}

		private static string FormatNumber(double value)
		{
			if (Math.Abs(value) < 1e15 && value == Math.Floor(value))
				return ((long)value).ToString(CultureInfo.InvariantCulture);
			return value.ToString(CultureInfo.InvariantCulture);
		}

		/// <summary>行列表 → 列列表（UPMS 输入），短行缺位补 0。</summary>
		private static List<List<int>> TransposeToColumns(List<List<double>> rows)
		{
			int columnCount = rows.Max(r => r.Count);
			var columns = new List<List<int>>(columnCount);
			for (int c = 0; c < columnCount; c++)
			{
				var column = new List<int>(rows.Count);
				foreach (List<double> row in rows)
					column.Add(c < row.Count ? (int)row[c] : 0);
				columns.Add(column);
			}
			return columns;
		}

		/// <summary>列列表 → 行列表（UPMS 输出显示），短列缺位补 0。</summary>
		private static List<List<double>> TransposeToRows(List<List<int>> columns)
		{
			var rows = new List<List<double>>();
			if (columns.Count == 0)
				return rows;

			int rowCount = columns.Max(c => c.Count);
			for (int r = 0; r < rowCount; r++)
			{
				var row = new List<double>(columns.Count);
				foreach (List<int> column in columns)
					row.Add(r < column.Count ? column[r] : 0);
				rows.Add(row);
			}
			return rows;
		}
	}
}
