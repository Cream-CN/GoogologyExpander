using System;
using System.Collections.Generic;
using System.Linq;

namespace GoogologyExpander
{
	public static class PrSSExpander
	{
		/// <summary>
		/// 按照PrSS定义展开序列一次
		/// </summary>
		/// <param name="Sequence">输入的原始序列</param>
		/// <returns>展开后的新序列</returns>
		/// <exception cref="ArgumentException">当序列为空时抛出</exception>
		public static int[] ExpandPrSS(int[] Sequence)
		{
			// 规则(1): 空序列不能展开
			if (Sequence == null || Sequence.Length == 0)
			{
				throw new ArgumentException("序列不能为空", nameof(Sequence));
			}

			// 获取序列长度
			int m = Sequence.Length;

			// 获取最后一个元素作为 ak
			int ak = Sequence[m - 1];

			// 规则(2): 如果末项为0，则直接删除末项
			if (ak == 0)
			{
				// (#, 0) -> (#) + 1，这里+1表示已经完成一次展开
				// 实际上就是删除末项并返回
				int[] resultSequence = new int[m - 1];
				Array.Copy(Sequence, 0, resultSequence, 0, m - 1);
				return resultSequence;
			}

			// 规则(3): ak > 0 的情况
			// 需要找到 ak 前首个小于 ak 的数 ai
			int aiIndex = -1;
			for (int i = m - 2; i >= 0; i--)
			{
				if (Sequence[i] < ak)
				{
					aiIndex = i;
					break;
				}
			}

			// 理论上在PrSS中总能找到这样的数（因为第一个元素为0）
			if (aiIndex == -1)
			{
				throw new InvalidOperationException("无法找到坏根，序列可能不符合PrSS规范");
			}

			// 根据定义：
			// (#1, ai, #2, ak) = (#1, ai, #2, ai, #2, ...)
			// 其中：
			// - #1 是 ai 前面的部分（好部的一部分）
			// - ai 是坏根
			// - #2 是 ai 和 ak 之间的部分（坏部）
			// - ak 是末项

			// 构建好部：从开头到坏根（包含坏根）
			int[] goodPart = new int[aiIndex + 1];
			Array.Copy(Sequence, 0, goodPart, 0, aiIndex + 1);

			// 构建坏部：从坏根之后到末项之前（不包含末项）
			int badPartLength = m - aiIndex - 2; // -2 因为要去掉 ai 和 ak
			int[] badPart = new int[badPartLength];
			if (badPartLength > 0)
			{
				Array.Copy(Sequence, aiIndex + 1, badPart, 0, badPartLength);
			}

			// 根据规则，我们需要将坏部无限复制，但实际展开只复制一次
			// 即：好部 + 坏部 + 坏部（复制一次）
			// 注意：规则中的省略号表示任意有限次循环，这里我们只执行一次展开

			// 构建新序列：好部 + 坏部（原始） + 坏部（复制）
			int[] expandedSequence = new int[goodPart.Length + badPart.Length * 2];

			// 复制好部
			Array.Copy(goodPart, 0, expandedSequence, 0, goodPart.Length);

			// 复制坏部（第一次）
			if (badPart.Length > 0)
			{
				Array.Copy(badPart, 0, expandedSequence, goodPart.Length, badPart.Length);
			}

			// 复制坏部（第二次）
			if (badPart.Length > 0)
			{
				Array.Copy(badPart, 0, expandedSequence, goodPart.Length + badPart.Length, badPart.Length);
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