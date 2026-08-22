// Form1.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace GoogologyExpander
{
	public partial class Form1 : Form
	{
		private TextBox txtInput;
		private ComboBox cmbSystem;
		private NumericUpDown nudTimes;
		private Button btnExpand;
		private Button btnAncestor;
		private Button btnSteps;
		private Button btnInfo;
		private Label lblInput;
		private Label lblSystem;
		private Label lblTimes;
		private Label lblInfo;

		private PrSSExpander expander;

		public Form1()
		{
			InitializeComponent();
			SetupUI();
			expander = new PrSSExpander();
		}

		private void SetupUI()
		{
			// 窗体设置
			this.Text = "PrSS 初等序列展开器";
			this.Size = new Size(320, 420);
			this.FormBorderStyle = FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.StartPosition = FormStartPosition.CenterScreen;

			// 输入标签
			lblInput = new Label
			{
				Text = "输入序列 (如: 0,1,2):",
				Location = new Point(12, 12),
				AutoSize = true,
				Font = new Font("Segoe UI", 9, FontStyle.Bold)
			};
			this.Controls.Add(lblInput);

			// 输入框
			txtInput = new TextBox
			{
				Location = new Point(12, 32),
				Width = 280,
				Text = "0,1,2",
				Font = new Font("Consolas", 11)
			};
			this.Controls.Add(txtInput);

			// 系统选择标签
			lblSystem = new Label
			{
				Text = "选择系统:",
				Location = new Point(12, 62),
				AutoSize = true,
				Font = new Font("Segoe UI", 9, FontStyle.Bold)
			};
			this.Controls.Add(lblSystem);

			// 系统选择下拉框
			cmbSystem = new ComboBox
			{
				Location = new Point(12, 82),
				Width = 280,
				DropDownStyle = ComboBoxStyle.DropDownList,
				Font = new Font("Segoe UI", 10)
			};
			cmbSystem.Items.AddRange(new object[] { "PrSS ✓", "LPrSS [WIP]" });
			cmbSystem.SelectedIndex = 0;

			cmbSystem.DrawMode = DrawMode.OwnerDrawFixed;
			cmbSystem.DrawItem += CmbSystem_DrawItem;
			this.Controls.Add(cmbSystem);

			// 展开次数标签
			lblTimes = new Label
			{
				Text = "展开次数:",
				Location = new Point(12, 112),
				AutoSize = true,
				Font = new Font("Segoe UI", 9, FontStyle.Bold)
			};
			this.Controls.Add(lblTimes);

			// 展开次数输入
			nudTimes = new NumericUpDown
			{
				Location = new Point(12, 132),
				Width = 280,
				Minimum = 1,
				Maximum = 100,
				Value = 1,
				Font = new Font("Segoe UI", 10)
			};
			this.Controls.Add(nudTimes);

			// 展开按钮
			btnExpand = new Button
			{
				Text = "▶ 展开",
				Location = new Point(12, 168),
				Width = 135,
				Height = 40,
				Font = new Font("Segoe UI", 10, FontStyle.Bold),
				BackColor = Color.LightGreen
			};
			btnExpand.Click += BtnExpand_Click;
			this.Controls.Add(btnExpand);

			// 始祖按钮
			btnAncestor = new Button
			{
				Text = "★ 始祖",
				Location = new Point(157, 168),
				Width = 135,
				Height = 40,
				Font = new Font("Segoe UI", 10, FontStyle.Bold),
				BackColor = Color.LightBlue
			};
			btnAncestor.Click += BtnAncestor_Click;
			this.Controls.Add(btnAncestor);

			// 步数按钮
			btnSteps = new Button
			{
				Text = "步数统计",
				Location = new Point(12, 218),
				Width = 135,
				Height = 40,
				Font = new Font("Segoe UI", 10, FontStyle.Bold),
				BackColor = Color.LightYellow
			};
			btnSteps.Click += BtnSteps_Click;
			this.Controls.Add(btnSteps);

			// 信息按钮
			btnInfo = new Button
			{
				Text = "展开详情",
				Location = new Point(157, 218),
				Width = 135,
				Height = 40,
				Font = new Font("Segoe UI", 10, FontStyle.Bold),
				BackColor = Color.Lavender
			};
			btnInfo.Click += BtnInfo_Click;
			this.Controls.Add(btnInfo);

			// 信息标签
			lblInfo = new Label
			{
				Text = "💡 PrSS: 首个元素为0，相邻增量≤1",
				Location = new Point(12, 270),
				AutoSize = true,
				ForeColor = Color.Gray,
				Font = new Font("Segoe UI", 8)
			};
			this.Controls.Add(lblInfo);

			// 示例标签
			Label lblExample = new Label
			{
				Text = "示例: 0,1,2 → 0,1,1,1 (展开1次)",
				Location = new Point(12, 290),
				AutoSize = true,
				ForeColor = Color.DarkBlue,
				Font = new Font("Consolas", 8)
			};
			this.Controls.Add(lblExample);
		}

		private void CmbSystem_DrawItem(object sender, DrawItemEventArgs e)
		{
			var comboBox = sender as ComboBox;
			if (e.Index < 0) return;

			e.DrawBackground();

			string text = comboBox.Items[e.Index].ToString();

			if (text.Contains("[WIP]"))
			{
				using (var brush = new SolidBrush(Color.Gray))
				{
					e.Graphics.DrawString(text, e.Font, brush, e.Bounds);
				}
			}
			else
			{
				using (var brush = new SolidBrush(e.ForeColor))
				{
					e.Graphics.DrawString(text, e.Font, brush, e.Bounds);
				}
			}

			e.DrawFocusRectangle();
		}

		private List<int> GetSequence()
		{
			try
			{
				return expander.ParseSequence(txtInput.Text);
			}
			catch (FormatException ex)
			{
				MessageBox.Show(ex.Message, "输入错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return null;
			}
		}

		private bool ValidateSequence(List<int> sequence)
		{
			if (sequence == null)
				return false;

			if (!expander.IsValidSequence(sequence))
			{
				MessageBox.Show($"序列 {expander.FormatSequence(sequence)} 包含负数", "验证失败",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			if (!expander.IsStandardSequence(sequence))
			{
				MessageBox.Show($"序列 {expander.FormatSequence(sequence)} 不是标准的PrSS序列\n\n" +
					"PrSS标准要求:\n" +
					"1. 第一个元素必须为0\n" +
					"2. 每个元素与前一个元素的差不能超过1",
					"验证失败",
					MessageBoxButtons.OK, MessageBoxIcon.Error);
				return false;
			}

			return true;
		}

		private bool CheckSystem()
		{
			string systemStr = cmbSystem.SelectedItem.ToString();
			if (systemStr.Contains("[WIP]"))
			{
				MessageBox.Show("LPrSS 目前尚未实现 (Work In Progress)", "功能未完成",
					MessageBoxButtons.OK, MessageBoxIcon.Information);
				return false;
			}
			return true;
		}

		private void BtnExpand_Click(object sender, EventArgs e)
		{
			if (!CheckSystem()) return;

			var sequence = GetSequence();
			if (sequence == null) return;

			if (!ValidateSequence(sequence)) return;

			try
			{
				int times = (int)nudTimes.Value;
				var expandedResult = expander.ExpandSequence(sequence, times);
				ShowResultDialog(expandedResult, sequence, times);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"展开失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void BtnAncestor_Click(object sender, EventArgs e)
		{
			if (!CheckSystem()) return;

			var sequence = GetSequence();
			if (sequence == null) return;

			if (!ValidateSequence(sequence)) return;

			try
			{
				var ancestor = expander.GetAncestor(sequence);
				int steps = expander.GetExpansionStepsToZero(sequence);

				MessageBox.Show(
					$"原始序列: {expander.FormatSequence(sequence)}\n" +
					$"始祖序列: {expander.FormatSequence(ancestor)}\n" +
					$"展开步数: {steps}",
					"始祖信息",
					MessageBoxButtons.OK,
					MessageBoxIcon.Information
				);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"获取始祖失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void BtnSteps_Click(object sender, EventArgs e)
		{
			if (!CheckSystem()) return;

			var sequence = GetSequence();
			if (sequence == null) return;

			if (!ValidateSequence(sequence)) return;

			try
			{
				int steps = expander.GetExpansionStepsToZero(sequence);

				MessageBox.Show(
					$"序列: {expander.FormatSequence(sequence)}\n" +
					$"展开到始祖的步数: {steps}",
					"步数统计",
					MessageBoxButtons.OK,
					MessageBoxIcon.Information
				);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"计算步数失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void BtnInfo_Click(object sender, EventArgs e)
		{
			if (!CheckSystem()) return;

			var sequence = GetSequence();
			if (sequence == null) return;

			if (!ValidateSequence(sequence)) return;

			try
			{
				var info = expander.GetExpansionInfo(sequence);
				MessageBox.Show(info.ToString(), "展开详细信息",
					MessageBoxButtons.OK, MessageBoxIcon.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"获取详情失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void ShowResultDialog(List<int> finalResult, List<int> original, int times)
		{
			string originalStr = expander.FormatSequence(original);
			string resultStr = expander.FormatSequence(finalResult);

			var form = new Form
			{
				Text = "展开结果",
				Size = new Size(480, 360),
				FormBorderStyle = FormBorderStyle.FixedDialog,
				MaximizeBox = false,
				MinimizeBox = false,
				StartPosition = FormStartPosition.CenterParent
			};

			var lblOriginal = new Label
			{
				Text = $"原始: {originalStr}",
				Location = new Point(12, 12),
				AutoSize = true,
				Font = new Font("Consolas", 10, FontStyle.Bold)
			};
			form.Controls.Add(lblOriginal);

			var lblTimes = new Label
			{
				Text = $"展开次数: {times}",
				Location = new Point(12, 36),
				AutoSize = true,
				Font = new Font("Segoe UI", 9)
			};
			form.Controls.Add(lblTimes);

			var lblResult = new Label
			{
				Text = "结果:",
				Location = new Point(12, 64),
				AutoSize = true,
				Font = new Font("Segoe UI", 9, FontStyle.Bold)
			};
			form.Controls.Add(lblResult);

			var txtResult = new TextBox
			{
				Location = new Point(12, 84),
				Width = 440,
				Height = 100,
				Text = resultStr,
				ReadOnly = true,
				Font = new Font("Consolas", 12),
				Multiline = true,
				ScrollBars = ScrollBars.Vertical
			};
			form.Controls.Add(txtResult);

			// 计算步数信息
			try
			{
				int steps = expander.GetExpansionStepsToZero(finalResult);
				var lblSteps = new Label
				{
					Text = $"继续展开到始祖还需 {steps} 步",
					Location = new Point(12, 196),
					AutoSize = true,
					Font = new Font("Segoe UI", 9),
					ForeColor = Color.DarkGreen
				};
				form.Controls.Add(lblSteps);
			}
			catch { }

			// 复制按钮
			var btnCopy = new Button
			{
				Text = "复制",
				Location = new Point(12, 230),
				Width = 80,
				Height = 30,
				Font = new Font("Segoe UI", 9)
			};
			btnCopy.Click += (s, ev) =>
			{
				Clipboard.SetText(resultStr);
				MessageBox.Show("已复制到剪贴板!", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
			};
			form.Controls.Add(btnCopy);

			// 关闭按钮
			var btnClose = new Button
			{
				Text = "关闭",
				Location = new Point(372, 230),
				Width = 80,
				Height = 30,
				Font = new Font("Segoe UI", 9),
				DialogResult = DialogResult.OK
			};
			form.Controls.Add(btnClose);

			form.ShowDialog();
		}
	}
}