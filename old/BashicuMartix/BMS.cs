// Form1.cs - 支持 BMS (BM4) 展开器
using System;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;
using System.Text;

namespace GoogologyExpander
{
	public partial class mainForm : Form
	{
		private BmsEngine bmsEngine;
		private TextBox txtInput;
		private TextBox txtOutput;
		private NumericUpDown nudSteps;
		private Button btnExpand;
		private Button btnClear;
		private Label lblInput;
		private Label lblSteps;
		private Label lblOutput;
		private MenuStrip menuStrip;
		private CheckBox chkDetailed;
		private Label lblInfo;

		public mainForm()
		{
			// 初始化引擎
			bmsEngine = new BmsEngine();

			// 设置窗体属性
			this.Text = "BMS (BM4) 展开器";
			this.Size = new Size(700, 650);
			this.MinimumSize = new Size(650, 550);
			this.MaximumSize = new Size(850, 750);
			this.FormBorderStyle = FormBorderStyle.FixedSingle;
			this.StartPosition = FormStartPosition.CenterScreen;
			this.MaximizeBox = false;
			this.MinimizeBox = true;
			this.BackColor = SystemColors.Control;
			this.Font = new Font("宋体", 9f);

			SetupMenu();
			SetupUI();
		}

		private void SetupMenu()
		{
			menuStrip = new MenuStrip();

			ToolStripMenuItem fileMenu = new ToolStripMenuItem("文件(&F)");

			ToolStripMenuItem exitItem = new ToolStripMenuItem("退出(&X)", null, (s, e) => Application.Exit());
			exitItem.ShortcutKeys = Keys.Alt | Keys.F4;
			fileMenu.DropDownItems.Add(exitItem);

			ToolStripMenuItem helpMenu = new ToolStripMenuItem("帮助(&H)");

			ToolStripMenuItem helpItem = new ToolStripMenuItem("使用说明(&U)", null, ShowHelp);
			helpItem.ShortcutKeys = Keys.F1;
			helpMenu.DropDownItems.Add(helpItem);

			helpMenu.DropDownItems.Add(new ToolStripSeparator());

			ToolStripMenuItem aboutItem = new ToolStripMenuItem("关于(&A)", null, ShowAbout);
			helpMenu.DropDownItems.Add(aboutItem);

			menuStrip.Items.Add(fileMenu);
			menuStrip.Items.Add(helpMenu);

			this.MainMenuStrip = menuStrip;
			this.Controls.Add(menuStrip);
		}

		private void SetupUI()
		{
			int menuHeight = menuStrip.Height;

			int margin = 12;
			int left = margin;
			int top = margin + menuHeight;
			int gap = 6;
			int ctrlH = 24;
			int clientW = this.ClientSize.Width;
			int availW = clientW - margin * 2;

			// 输入标签
			lblInput = new Label
			{
				Text = "矩阵:",
				Location = new Point(left, top + 2),
				Size = new Size(40, ctrlH),
				TextAlign = ContentAlignment.MiddleLeft
			};
			txtInput = new TextBox
			{
				Location = new Point(left + 40 + gap, top),
				Width = availW - 40 - gap,
				Height = ctrlH,
				Font = new Font("Consolas", 10f),
				ForeColor = SystemColors.GrayText,
				Text = "请输入矩阵，如 (0,0)(1,1)(2,0)..."
			};
			txtInput.GotFocus += TxtInput_GotFocus;
			txtInput.LostFocus += TxtInput_LostFocus;

			top += ctrlH + 6;

			// 步数
			lblSteps = new Label
			{
				Text = "步数:",
				Location = new Point(left, top + 2),
				Size = new Size(40, ctrlH),
				TextAlign = ContentAlignment.MiddleLeft
			};
			nudSteps = new NumericUpDown
			{
				Location = new Point(left + 40 + gap, top),
				Width = 60,
				Height = ctrlH,
				Minimum = 1,
				Maximum = 100,
				Value = 5
			};
			top += ctrlH + 6;

			// 详细输出选项
			chkDetailed = new CheckBox
			{
				Text = "显示详细展开过程",
				Location = new Point(left, top + 2),
				AutoSize = true
			};
			top += 26 + 4;

			// 按钮
			int btnGap = 8;
			int btnWidth = (availW - btnGap) / 2;
			btnExpand = new Button
			{
				Text = "展开 (&E)",
				Location = new Point(left, top),
				Width = btnWidth,
				Height = 30,
			};
			btnExpand.Click += BtnExpand_Click;

			btnClear = new Button
			{
				Text = "清空 (&C)",
				Location = new Point(left + btnWidth + btnGap, top),
				Width = btnWidth,
				Height = 30,
			};
			btnClear.Click += (s, e) => { txtOutput.Clear(); txtOutput.Text = ""; };
			top += 30 + 8;

			// 信息标签
			lblInfo = new Label
			{
				Text = "提示: 按 Enter 快速展开",
				Location = new Point(left, top + 4),
				AutoSize = true,
				ForeColor = SystemColors.GrayText
			};
			top += 24 + 4;

			// 输出标签
			lblOutput = new Label
			{
				Text = "结果:",
				Location = new Point(left, top + 2),
				AutoSize = true
			};
			top += 20 + 4;

			// 输出文本框
			int remainHeight = this.ClientSize.Height - top - margin;
			txtOutput = new TextBox
			{
				Location = new Point(left, top),
				Width = availW,
				Height = remainHeight,
				Multiline = true,
				ScrollBars = ScrollBars.Vertical,
				ReadOnly = true,
				Font = new Font("Consolas", 10f),
				BackColor = SystemColors.Window,
				WordWrap = true
			};

			this.Controls.AddRange(new Control[] {
				lblInput, txtInput,
				lblSteps, nudSteps,
				chkDetailed,
				btnExpand, btnClear,
				lblInfo,
				lblOutput, txtOutput
			});

			this.AcceptButton = btnExpand;
			this.ActiveControl = txtInput;
		}

		private void TxtInput_GotFocus(object sender, EventArgs e)
		{
			if (txtInput.Text == "请输入矩阵，如 (0,0)(1,1)(2,0)...")
			{
				txtInput.Text = "";
				txtInput.ForeColor = SystemColors.WindowText;
			}
		}

		private void TxtInput_LostFocus(object sender, EventArgs e)
		{
			if (string.IsNullOrWhiteSpace(txtInput.Text))
			{
				txtInput.Text = "请输入矩阵，如 (0,0)(1,1)(2,0)...";
				txtInput.ForeColor = SystemColors.GrayText;
			}
		}

		private void BtnExpand_Click(object sender, EventArgs e)
		{
			string input = txtInput.Text.Trim();

			if (string.IsNullOrEmpty(input) || input == "请输入矩阵，如 (0,0)(1,1)(2,0)...")
			{
				txtOutput.Text = "请输入矩阵";
				return;
			}

			try
			{
				int steps = (int)nudSteps.Value;
				bool detailed = chkDetailed.Checked;

				txtOutput.Clear();

				// 解析矩阵
				int[][] matrix = BmsParser.Parse(input);

				if (matrix == null || matrix.Length == 0)
				{
					txtOutput.Text = "请输入有效的矩阵\n格式示例: (0,0)(1,1)(2,0)";
					return;
				}

				if (detailed)
				{
					var result = bmsEngine.ExpandWithDetails(matrix, steps);

					var sb = new StringBuilder();
					sb.AppendLine($"BMS (BM4) 展开过程 (步数: {steps}):");
					sb.AppendLine("=".PadRight(60, '='));
					sb.AppendLine();

					foreach (var detail in result.Details)
					{
						sb.AppendLine(detail);
					}

					sb.AppendLine();
					sb.AppendLine("=".PadRight(60, '='));
					sb.AppendLine($"最终结果: {BmsParser.Format(result.Final)}");

					txtOutput.Text = sb.ToString();
				}
				else
				{
					int[][] result = bmsEngine.Expand(matrix, steps);

					var sb = new StringBuilder();
					sb.AppendLine($"展开结果 (步数: {steps}):");
					sb.AppendLine(BmsParser.Format(result));

					if (!bmsEngine.IsEmpty(matrix))
					{
						sb.AppendLine();
						sb.AppendLine("矩阵信息:");
						sb.AppendLine($"  行数: {bmsEngine.GetRowCount(matrix)}");
						sb.AppendLine($"  列数: {bmsEngine.GetColCount(matrix)}");
						sb.AppendLine($"  标准形式: {(bmsEngine.IsStandard(matrix) ? "是" : "否")}");
						sb.AppendLine($"  当前版本: {bmsEngine.GetVersion()}");
					}

					txtOutput.Text = sb.ToString();
				}
			}
			catch (Exception ex)
			{
				txtOutput.Text = "错误: " + ex.Message + "\n\n" + ex.StackTrace;
			}
		}

		private void ShowHelp(object sender, EventArgs e)
		{
			string helpText =
				"BMS (BM4) 使用说明\n\n" +
				"【输入格式】\n" +
				"使用括号表示列向量，每列用括号括起来，列之间直接连接\n" +
				"示例:\n" +
				"  (0,0)(1,1)(2,0) - 3列2行矩阵\n" +
				"  (0,0,0)(1,1,1)(2,2,0)(3,3,0) - 4列3行矩阵\n" +
				"  (0)(1)(2) - 3列1行矩阵 (PrSS)\n\n" +
				"【展开规则 (BM4)】\n" +
				"• 父项查找：第一行找左边第一个小于当前值\n" +
				"• 其他行找左边第一个小于当前值且上方是上方元素的祖先\n" +
				"• 坏根：最后一列从下往上第一个非零元素的父项所在列\n" +
				"• 阶差向量：末列减坏根列，最后一项始终为零\n" +
				"• 坏部复制时，非祖先项保持不变\n\n" +
				"【操作说明】\n" +
				"• 步数: 展开的步数 (1-100)\n" +
				"• 详细展开: 显示每一步的详细计算过程\n" +
				"• 快捷键: F1 查看帮助，Alt+F4 退出\n" +
				"• 点击「展开」或按 Enter 执行展开";

			MessageBox.Show(helpText, "使用说明", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ShowAbout(object sender, EventArgs e)
		{
			string aboutText =
				"BMS (BM4) 展开器\n" +
				"版本 1.0\n\n" +
				"支持的系統:\n" +
				"  • BMS (BM4) - Bashicu Matrix System 版本4 (2018)\n\n" +
				"实现特点:\n" +
				"  • 严格的父项查找 (左向遍历)\n" +
				"  • 完整的祖先链 (while 循环)\n" +
				"  • 精确的坏根查找\n" +
				"  • 正确的阶差向量计算\n" +
				"  • 非祖先项保持不变\n\n" +
				"开发者: Cream-CN (Github同名)\n\n" +
				"Copyright(C) Cream-CN 及所有贡献者";

			MessageBox.Show(aboutText, "关于", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}