using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogologyExpander
{
	public static class LPrSSExpander
	{
		/// <summary>
		/// 按照LPrSS定义展开序列一次
		/// </summary>
		/// <param name="Sequence">输入的原始序列</param>
		/// <returns>展开后的新序列</returns>
		/// <exception cref="ArgumentException">当序列为空时抛出</exception>
		public static int[] ExpandLPrSS(int[] Sequence)
		{
			// 规则(1): 空序列不能展开
			if (Sequence == null || Sequence.Length == 0)
			{
				throw new ArgumentException("序列不能为空", nameof(Sequence));
			}

			// 获取序列长度
			int m = Sequence.Length;

			// 获取最后一个元素作为末项
			int lastElement = Sequence[m - 1];

			// 规则(2): 如果末项为1，则直接删除末项
			if (lastElement == 1)
			{
				// (#, 1) -> (#) + 1，这里+1表示已经完成一次展开
				int[] resultSequence = new int[m - 1];
				Array.Copy(Sequence, 0, resultSequence, 0, m - 1);
				return resultSequence;
			}

			// 规则(3): 末项不为1的情况（即lastElement > 1）
			// 找到最后一个元素左边，且小于最后一个元素的第一个元素（即坏根）
			int badRootIndex = -1;
			for (int i = m - 2; i >= 0; i--)
			{
				if (Sequence[i] < lastElement)
				{
					badRootIndex = i;
					break;
				}
			}

			if (badRootIndex == -1)
			{
				throw new InvalidOperationException("无法找到坏根，序列可能不符合LPrSS规范");
			}

			// 根据LPrSS定义：
			// 好部：坏根左边的元素，不包含坏根
			// 坏部：坏根右边和最后一个元素之间的元素，包含坏根，但不包含最后一个元素
			// 阶差：末项与坏根之间的差值

			// 构建好部：从开头到坏根之前（不包含坏根）
			int[] goodPart = new int[badRootIndex];
			if (badRootIndex > 0)
			{
				Array.Copy(Sequence, 0, goodPart, 0, badRootIndex);
			}

			// 构建坏部：从坏根到末项之前（包含坏根，不包含末项）
			int badPartLength = m - badRootIndex - 1; // -1 因为不包含末项
			int[] badPart = new int[badPartLength];
			if (badPartLength > 0)
			{
				Array.Copy(Sequence, badRootIndex, badPart, 0, badPartLength);
			}

			// 计算阶差：末项 - 坏根
			int badRootValue = Sequence[badRootIndex];
			int difference = lastElement - badRootValue;

			// 展开规则：好部 + 坏部 + (坏部 + 阶差减一) + (坏部 + 2*(阶差减一)) + ...
			// 这里我们只展开一次，所以复制一次坏部并加上阶差减一

			// 构建新的坏部（复制一次并加上阶差减一）
			int[] incrementedBadPart = new int[badPartLength];
			if (badPartLength > 0)
			{
				int increment = difference - 1; // 阶差减一
				for (int i = 0; i < badPartLength; i++)
				{
					incrementedBadPart[i] = badPart[i] + increment;
				}
			}

			// 构建新序列：好部 + 坏部（原始） + 新坏部（增加后的）
			int[] expandedSequence = new int[goodPart.Length + badPart.Length + incrementedBadPart.Length];

			// 复制好部
			if (goodPart.Length > 0)
			{
				Array.Copy(goodPart, 0, expandedSequence, 0, goodPart.Length);
			}

			// 复制坏部（原始）
			if (badPart.Length > 0)
			{
				Array.Copy(badPart, 0, expandedSequence, goodPart.Length, badPart.Length);
			}

			// 复制新坏部（增加后的）
			if (incrementedBadPart.Length > 0)
			{
				Array.Copy(incrementedBadPart, 0, expandedSequence, goodPart.Length + badPart.Length, incrementedBadPart.Length);
			}

			return expandedSequence;
		}

		/// <summary>
		/// 辅助方法：打印序列
		/// </summary>
		public static string SequenceToString(int[] sequence)
		{
			return "(" + string.Join(", ", sequence) + ")";
		}
	}
}