// Form1.cs - 修复菜单栏位置，新增 LPrSS 模式
using System;
using System.Windows.Forms;
using System.Drawing;
using System.Linq;

namespace GoogologyExpander
{
	public partial class mainForm : Form
	{
		private PrssEngine prssEngine;
		private LPrssEngine lprssEngine;
		private BmsEngine bmsEngine;
		private TextBox txtInput;
		private TextBox txtOutput;
		private NumericUpDown nudSteps;
		private ComboBox cmbMode;
		private Button btnExpand;
		private Button btnClear;
		private Label lblInput;
		private Label lblSteps;
		private Label lblMode;
		private Label lblOutput;
		private MenuStrip menuStrip;

		public mainForm()
		{
			prssEngine = new PrssEngine();
			lprssEngine = new LPrssEngine();
			bmsEngine = new BmsEngine();
			InitializeComponent();
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
			this.Text = "PrSS / LPrSS / BMS 展开器";
			this.Size = new Size(400, 400);
			this.MinimumSize = new Size(400, 400);
			this.MaximumSize = new Size(400, 400);
			this.FormBorderStyle = FormBorderStyle.FixedSingle;
			this.StartPosition = FormStartPosition.CenterScreen;
			this.MaximizeBox = false;
			this.MinimizeBox = true;
			this.BackColor = SystemColors.Control;
			this.Font = new Font("宋体", 9f);

			int menuHeight = menuStrip.Height;

			int margin = 12;
			int left = margin;
			int top = margin + menuHeight;
			int gap = 6;
			int ctrlH = 24;
			int clientW = this.ClientSize.Width;
			int availW = clientW - margin * 2;

			lblInput = new Label
			{
				Text = "序列:",
				Location = new Point(left, top + 2),
				Size = new Size(36, ctrlH),
				TextAlign = ContentAlignment.MiddleLeft
			};
			txtInput = new TextBox
			{
				Location = new Point(left + 36 + gap, top),
				Width = availW - 36 - gap,
				Height = ctrlH,
				Font = new Font("Consolas", 10f),
				Text = ""
			};
			txtInput.Text = "";
			txtInput.ForeColor = SystemColors.GrayText;
			txtInput.Text = "请输入序列...";
			txtInput.GotFocus += TxtInput_GotFocus;
			txtInput.LostFocus += TxtInput_LostFocus;

			top += ctrlH + 6;

			lblSteps = new Label
			{
				Text = "步数:",
				Location = new Point(left, top + 2),
				Size = new Size(36, ctrlH),
				TextAlign = ContentAlignment.MiddleLeft
			};
			nudSteps = new NumericUpDown
			{
				Location = new Point(left + 36 + gap, top),
				Width = 60,
				Height = ctrlH,
				Minimum = 1,
				Maximum = 50,
				Value = 5
			};
			top += ctrlH + 6;

			lblMode = new Label
			{
				Text = "模式:",
				Location = new Point(left, top + 2),
				Size = new Size(36, ctrlH),
				TextAlign = ContentAlignment.MiddleLeft
			};
			cmbMode = new ComboBox
			{
				Location = new Point(left + 36 + gap, top),
				Width = 100,
				Height = ctrlH,
				DropDownStyle = ComboBoxStyle.DropDownList,
				Font = new Font("宋体", 9f)
			};
			cmbMode.Items.AddRange(new object[] { "PrSS", "LPrSS", "BMS" });
			cmbMode.SelectedIndex = 0;
			top += ctrlH + 6;

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

			lblOutput = new Label
			{
				Text = "结果:",
				Location = new Point(left, top + 2),
				AutoSize = true
			};
			top += 20 + 4;

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
				lblMode, cmbMode,
				btnExpand, btnClear,
				lblOutput, txtOutput
			});

			this.AcceptButton = btnExpand;
			this.ActiveControl = txtInput;
		}

		private void TxtInput_GotFocus(object sender, EventArgs e)
		{
			if (txtInput.Text == "请输入序列...")
			{
				txtInput.Text = "";
				txtInput.ForeColor = SystemColors.WindowText;
			}
		}

		private void TxtInput_LostFocus(object sender, EventArgs e)
		{
			if (string.IsNullOrWhiteSpace(txtInput.Text))
			{
				txtInput.Text = "请输入序列...";
				txtInput.ForeColor = SystemColors.GrayText;
			}
		}

		private void BtnExpand_Click(object sender, EventArgs e)
		{
			string input = txtInput.Text.Trim();

			if (string.IsNullOrEmpty(input) || input == "请输入序列...")
			{
				txtOutput.Text = "请输入序列";
				return;
			}

			try
			{
				int steps = (int)nudSteps.Value;
				string mode = cmbMode.SelectedItem.ToString();

				if (mode == "PrSS")
				{
					var seq = PrssParser.Parse(input);
					var result = prssEngine.Expand(seq, steps);
					txtOutput.Text = result;
				}
				else if (mode == "LPrSS")
				{
					var seq = LPrssParser.Parse(input);
					var result = lprssEngine.Expand(seq, steps);
					txtOutput.Text = result;
				}
				else if (mode == "BMS")
				{
					var matrix = BmsParser.Parse(input);
					var result = bmsEngine.Expand(matrix, steps);
					txtOutput.Text = result;
				}
			}
			catch (Exception ex)
			{
				txtOutput.Text = "错误: " + ex.Message;
			}
		}

		private void ShowHelp(object sender, EventArgs e)
		{
			string helpText =
				"=== PrSS / LPrSS / BMS 展开器 使用说明 ===\n\n" +
				"【PrSS 模式】\n" +
				"输入格式：用逗号分隔的数字序列\n" +
				"示例：1, 2, 3, 0\n" +
				"说明：原始数列系统 (Primitive Sequence System)\n\n" +
				"【LPrSS 模式】\n" +
				"输入格式：用逗号分隔的数字序列\n" +
				"示例：1, 2, 3, 4\n" +
				"说明：极限原始数列系统 (Limit Primitive Sequence System)\n" +
				"展开规则：\n" +
				"  (1) ( ) = 0\n" +
				"  (2) (#, 1) = (#) + 1\n" +
				"  (3) 否则坏部复制，每复制一次各项加上阶差减一\n\n" +
				"【BMS 模式】\n" +
				"输入格式：用括号表示的列向量\n" +
				"示例：(0,0)(1,1)(2,0)\n" +
				"说明：Bashicu 矩阵系统 (Bashicu Matrix System)\n\n" +
				"【通用操作】\n" +
				"• 步数：展开的步数 (1-50)\n" +
				"• 快捷键：F1 查看帮助，Alt+F4 退出\n" +
				"• 点击「展开」或按 Enter 执行展开";

			MessageBox.Show(helpText, "使用说明", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void ShowAbout(object sender, EventArgs e)
		{
			string aboutText =
				"展开器\n" +
				"版本0.2\n\n" +
				"支持: PrSS, LPrSS, BMS\n" +
				"LPrSS 基于定义 14.1 和 14.2 实现\n" +
				"BMS 基于 Bashicu Matrix System 定义 13.4 实现\n" +
				"开发者：Cream-CN (Github同名)\n" +
				"\n\n" +
				"Copyright(C) Cream-CN 及所有贡献者";

			MessageBox.Show(aboutText, "关于", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
	}
}