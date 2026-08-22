// Form1.cs - 400x400 布局
using System;
using System.Windows.Forms;
using System.Drawing;

namespace GoogologyExpander
{
	public partial class mainForm : Form
	{
		private PrssEngine engine;
		private TextBox txtInput;
		private TextBox txtOutput;
		private NumericUpDown nudSteps;
		private Button btnExpand;
		private Button btnClear;
		private Label lblInput;
		private Label lblSteps;
		private Label lblOutput;

		public mainForm()
		{
			engine = new PrssEngine();
			InitializeComponent();
			SetupUI();
		}

		private void SetupUI()
		{
			// 窗体固定 400x400
			this.Text = "PrSS 展开器";
			this.Size = new System.Drawing.Size(400, 400);
			this.MinimumSize = new System.Drawing.Size(400, 400);
			this.MaximumSize = new System.Drawing.Size(400, 400);
			this.FormBorderStyle = FormBorderStyle.FixedSingle;
			this.StartPosition = FormStartPosition.CenterScreen;
			this.MaximizeBox = false;
			this.MinimizeBox = true;
			this.BackColor = SystemColors.Control;
			this.Font = new Font("宋体", 9f, FontStyle.Regular, GraphicsUnit.Point, 134);

			// ===== 布局演算 =====
			// ClientSize ≈ 384 x 362
			// 左右边距 12px
			int margin = 12;
			int left = margin;
			int top = margin;
			int gap = 6;
			int ctrlH = 24;
			int clientW = this.ClientSize.Width;     // ≈384
			int availW = clientW - margin * 2;       // ≈360

			// ========== 第1行: 序列标签 + 输入框 ==========
			lblInput = new Label
			{
				Text = "序列:",
				Location = new Point(left, top + 2),
				Size = new Size(36, ctrlH),
				Font = new Font("宋体", 9f),
				TextAlign = ContentAlignment.MiddleLeft
			};

			txtInput = new TextBox
			{
				Location = new Point(left + 36 + gap, top),
				Width = availW - 36 - gap,  // 360 - 36 - 6 = 318
				Height = ctrlH,
				Font = new Font("Consolas", 10f),
				BorderStyle = BorderStyle.Fixed3D,
				Text = "1, 2, 3, 0"
			};
			top += ctrlH + 6;

			// ========== 第2行: 步数标签 + 步数输入 ==========
			lblSteps = new Label
			{
				Text = "步数:",
				Location = new Point(left, top + 2),
				Size = new Size(36, ctrlH),
				Font = new Font("宋体", 9f),
				TextAlign = ContentAlignment.MiddleLeft
			};

			nudSteps = new NumericUpDown
			{
				Location = new Point(left + 36 + gap, top),
				Width = 60,
				Height = ctrlH,
				Minimum = 1,
				Maximum = 50,
				Value = 5,
				Font = new Font("宋体", 9f),
				BorderStyle = BorderStyle.Fixed3D
			};
			top += ctrlH + 6;

			// ========== 第3行: 展开按钮 + 清空按钮 ==========
			int btnGap = 8;
			int btnWidth = (availW - btnGap) / 2;  // (360 - 8) / 2 = 176

			btnExpand = new Button
			{
				Text = "展开 (&E)",
				Location = new Point(left, top),
				Width = btnWidth,
				Height = 30,
				Font = new Font("宋体", 9f),
				FlatStyle = FlatStyle.Standard,
				UseVisualStyleBackColor = true
			};
			btnExpand.Click += BtnExpand_Click;

			btnClear = new Button
			{
				Text = "清空 (&C)",
				Location = new Point(left + btnWidth + btnGap, top),
				Width = btnWidth,
				Height = 30,
				Font = new Font("宋体", 9f),
				FlatStyle = FlatStyle.Standard,
				UseVisualStyleBackColor = true
			};
			btnClear.Click += (s, e) => txtOutput.Clear();
			top += 30 + 8;

			// ========== 第4行: 结果标签 ==========
			lblOutput = new Label
			{
				Text = "结果:",
				Location = new Point(left, top + 2),
				AutoSize = true,
				Font = new Font("宋体", 9f)
			};
			top += 20 + 4;

			// ========== 第5行: 输出框 ==========
			int remainHeight = this.ClientSize.Height - top - margin;  // ≈362 - 140 - 12 = 210
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
				BorderStyle = BorderStyle.Fixed3D,
				WordWrap = true
			};

			// ========== 添加到窗体 ==========
			this.Controls.AddRange(new Control[] {
				lblInput, txtInput,
				lblSteps, nudSteps,
				btnExpand, btnClear,
				lblOutput, txtOutput
			});

			this.AcceptButton = btnExpand;
			this.ActiveControl = txtInput;
		}

		private void BtnExpand_Click(object sender, EventArgs e)
		{
			string input = txtInput.Text.Trim();

			if (string.IsNullOrEmpty(input))
			{
				txtOutput.Text = "请输入序列";
				return;
			}

			try
			{
				int steps = (int)nudSteps.Value;
				var sequence = PrssParser.Parse(input);
				var result = engine.Expand(sequence, steps);
				txtOutput.Text = result;
			}
			catch (Exception ex)
			{
				txtOutput.Text = "错误: " + ex.Message;
			}
		}
	}
}