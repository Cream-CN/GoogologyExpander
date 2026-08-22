// PrSSExpander.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogologyExpander
{
	/// <summary>
	/// PrSS (Primitive Sequence System) 初等序列系统
	/// 
	/// 定义：初等序列 (a0, a1, ..., am-1, am)
	/// 
	/// 规则：
	/// (1) () = 0
	/// (2) (#, 0) = (#) + 1，式中 # 为任意合法序列
	/// (3) (#1, ai, #2, ak) = (#1, ai, #2, ai, #2, ...)，式中 #1, #2 为任意两段合法序列，
	///     ak > 0，ai = ak - 1 为 ak 前首个小于 ak 的数，省略号代表任意有限次循环的极限
	/// </summary>
	public class PrSSExpander
	{
		/// <summary>
		/// 解析逗号分隔的序列字符串
		/// </summary>
		public List<int> ParseSequence(string input)
		{
			if (string.IsNullOrWhiteSpace(input))
				throw new FormatException("输入不能为空");

			var parts = input.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
			var parsedResult = new List<int>();

			foreach (var part in parts)
			{
				if (int.TryParse(part.Trim(), out int value))
				{
					parsedResult.Add(value);
				}
				else
				{
					throw new FormatException($"无法解析 '{part.Trim()}' 为整数");
				}
			}

			return parsedResult;
		}

		/// <summary>
		/// 格式化序列为字符串
		/// </summary>
		public string FormatSequence(List<int> sequence)
		{
			return string.Join(",", sequence);
		}

		/// <summary>
		/// 验证序列是否合法（所有元素非负）
		/// </summary>
		public bool IsValidSequence(List<int> sequence)
		{
			return sequence.All(x => x >= 0);
		}

		/// <summary>
		/// 检查序列是否为标准PrSS序列
		/// 标准要求：
		/// 1. 所有元素非负
		/// 2. 第一个元素必须是0
		/// 3. 每个元素与前一个元素的差不能超过1
		/// </summary>
		public bool IsStandardSequence(List<int> sequence)
		{
			if (!IsValidSequence(sequence))
				return false;

			if (sequence.Count == 0)
				return true;

			if (sequence[0] != 0)
				return false;

			for (int i = 1; i < sequence.Count; i++)
			{
				int diff = sequence[i] - sequence[i - 1];
				if (diff < 0 || diff > 1)
					return false;
			}

			return true;
		}

		/// <summary>
		/// 展开序列（执行指定次数的展开）
		/// </summary>
		public List<int> ExpandSequence(List<int> sequence, int times)
		{
			if (times < 1)
				throw new ArgumentException("展开次数必须大于0");

			if (!IsStandardSequence(sequence))
				throw new ArgumentException($"序列 {FormatSequence(sequence)} 不是标准的PrSS序列");

			var current = new List<int>(sequence);

			for (int i = 0; i < times; i++)
			{
				if (current.Count == 0)
					break;

				current = ExpandOnce(current);
			}

			return current;
		}

		/// <summary>
		/// 执行一次展开
		/// </summary>
		private List<int> ExpandOnce(List<int> sequence)
		{
			if (sequence.Count == 0)
				return new List<int>();

			int n = sequence.Count;
			int last = sequence[n - 1];

			// 规则(2): (#, 0) = (#) + 1
			// 删除最后一个0
			if (last == 0)
			{
				var zeroResult = new List<int>(sequence);
				zeroResult.RemoveAt(n - 1);
				return zeroResult;
			}

			// 规则(3): (#1, ai, #2, ak) = (#1, ai, #2, ai, #2, ...)
			// ak > 0，ai = ak - 1 为 ak 前首个小于 ak 的数
			int ak = last;
			int ai = ak - 1;

			// 从后向前找第一个值为 ai 的元素
			int badRootIndex = -1;
			for (int i = n - 2; i >= 0; i--)
			{
				if (sequence[i] == ai)
				{
					badRootIndex = i;
					break;
				}
			}

			// 如果找不到，则用第一个元素作为坏根
			if (badRootIndex == -1)
				badRootIndex = 0;

			// 好部 (#1): 从开始到坏根（包含坏根）
			var goodPart = sequence.Take(badRootIndex + 1).ToList();

			// 坏部 (#2): 坏根之后到末项之前的部分
			var badPart = sequence.Skip(badRootIndex + 1).Take(n - badRootIndex - 2).ToList();

			// 展开: (#1, ai, #2, ak) = (#1, ai, #2, ai, #2, ...)
			// 结果 = 好部 + (坏部 + ai) 重复 ak 次
			var expandResult = new List<int>(goodPart);

			for (int i = 0; i < ak; i++)
			{
				expandResult.AddRange(badPart);
				expandResult.Add(ai);
			}

			return expandResult;
		}

		/// <summary>
		/// 计算序列展开到始祖（所有元素归零）所需的步数
		/// </summary>
		public int GetExpansionStepsToZero(List<int> sequence)
		{
			if (!IsStandardSequence(sequence))
				throw new ArgumentException($"序列 {FormatSequence(sequence)} 不是标准的PrSS序列");

			int steps = 0;
			var current = new List<int>(sequence);

			while (current.Count > 0 && current.Any(x => x > 0))
			{
				current = ExpandOnce(current);
				steps++;

				if (steps > 1000000)
					break;
			}

			return steps;
		}

		/// <summary>
		/// 获取序列的始祖（所有元素都变为0）
		/// </summary>
		public List<int> GetAncestor(List<int> sequence)
		{
			if (!IsStandardSequence(sequence))
				throw new ArgumentException($"序列 {FormatSequence(sequence)} 不是标准的PrSS序列");

			var current = new List<int>(sequence);

			while (current.Count > 0 && current.Any(x => x > 0))
			{
				current = ExpandOnce(current);
			}

			return current;
		}

		/// <summary>
		/// 获取展开的详细信息
		/// </summary>
		public ExpansionInfo GetExpansionInfo(List<int> sequence)
		{
			if (!IsStandardSequence(sequence))
				throw new ArgumentException($"序列 {FormatSequence(sequence)} 不是标准的PrSS序列");

			if (sequence.Count == 0)
			{
				return new ExpansionInfo
				{
					Original = sequence,
					IsEmpty = true,
					Description = "空序列 = 0"
				};
			}

			int n = sequence.Count;
			int last = sequence[n - 1];

			// 规则(2)
			if (last == 0)
			{
				return new ExpansionInfo
				{
					Original = sequence,
					IsEmpty = false,
					IsRule2 = true,
					GoodPart = sequence.Take(n - 1).ToList(),
					Ak = 0,
					Description = $"规则(2): (#,0) = (#) + 1",
					ResultSequence = sequence.Take(n - 1).ToList()
				};
			}

			// 规则(3)
			int ak = last;
			int ai = ak - 1;

			int badRootIndex = -1;
			for (int i = n - 2; i >= 0; i--)
			{
				if (sequence[i] == ai)
				{
					badRootIndex = i;
					break;
				}
			}

			if (badRootIndex == -1)
				badRootIndex = 0;

			var goodPart = sequence.Take(badRootIndex + 1).ToList();
			var badPart = sequence.Skip(badRootIndex + 1).Take(n - badRootIndex - 2).ToList();

			var infoResult = new List<int>(goodPart);
			for (int i = 0; i < ak; i++)
			{
				infoResult.AddRange(badPart);
				infoResult.Add(ai);
			}

			return new ExpansionInfo
			{
				Original = sequence,
				IsEmpty = false,
				IsRule2 = false,
				GoodPart = goodPart,
				BadPart = badPart,
				BadRoot = badRootIndex,
				Ak = ak,
				Ai = ai,
				Description = $"规则(3): (#1, ai, #2, ak) = (#1, ai, #2, ai, #2, ...)",
				ResultSequence = infoResult
			};
		}
	}

	/// <summary>
	/// 展开信息类
	/// </summary>
	public class ExpansionInfo
	{
		public List<int> Original { get; set; }
		public bool IsEmpty { get; set; }
		public bool IsRule2 { get; set; }
		public List<int> GoodPart { get; set; }
		public List<int> BadPart { get; set; }
		public int BadRoot { get; set; }
		public int Ak { get; set; }
		public int Ai { get; set; }
		public string Description { get; set; }
		public List<int> ResultSequence { get; set; }

		public override string ToString()
		{
			if (IsEmpty)
				return "空序列";

			string original = FormatSequence(Original);
			string good = FormatSequence(GoodPart);
			string bad = FormatSequence(BadPart);
			string resultSeq = FormatSequence(ResultSequence);

			if (IsRule2)
			{
				return $"原始序列: [{original}]\n" +
					   $"规则: (#,0) = (#) + 1\n" +
					   $"好部: [{good}]\n" +
					   $"结果: [{resultSeq}]\n" +
					   $"说明: {Description}";
			}
			else
			{
				return $"原始序列: [{original}]\n" +
					   $"规则: (#1, ai, #2, ak) = (#1, ai, #2, ai, #2, ...)\n" +
					   $"好部 (#1): [{good}]\n" +
					   $"坏部 (#2): [{bad}]\n" +
					   $"坏根索引: {BadRoot}\n" +
					   $"ai (坏根值): {Ai}\n" +
					   $"ak (末项值): {Ak}\n" +
					   $"结果: [{resultSeq}]\n" +
					   $"说明: {Description}";
			}
		}

		private string FormatSequence(List<int> sequence)
		{
			if (sequence == null || sequence.Count == 0)
				return "空";
			return string.Join(",", sequence);
		}
	}
}