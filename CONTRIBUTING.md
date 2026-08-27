# 贡献指南（CONTRIBUTING）

本文档是向 GoogologyExpander 提交代码的**强制规范**，详细说明项目要求的代码结构、接口形态与提交流程。接口细节的完整存档见根目录《接口规范档案》。

**提交代码即表示你已阅读并同意本文档全部强制要求；不符合要求的贡献将被退回。**

## 一、环境要求

| 项目 | 要求 |
| --- | --- |
| SDK | .NET 10 SDK（目标框架 `net10.0-windows`） |
| 运行平台 | Windows（项目启用 Windows Forms） |
| 缩进 | **制表符（Tab）**，禁止空格缩进 |
| 编码 | UTF-8 |
| 语言特性 | 项目启用 `Nullable` 与 `ImplicitUsings`，新代码需兼容可空引用检查 |

## 二、项目结构（强制）

```
GoogologyExpander/
├── Program.cs                    # 程序入口（勿改）
├── Form1.cs                      # 主窗体，记法注册点（新记法必须在此登记）
├── GoogologyExpander.csproj      # 工程文件（勿改目标框架）
├── Notation/                     # 全部记法实现（唯一指定位置）
│   ├── prss.cs                   # 一维记法（PrSS 样式基准）
│   ├── hprss.cs
│   ├── lprss.cs
│   ├── 0y.cs
│   ├── y.cs
│   ├── wy.cs
│   ├── ey.cs
│   ├── bms.cs                    # 二维记法（BMS 样式基准）
│   └── upms.cs
└── Helpers/                      # 公共库（多个相似记法共用逻辑时使用）
```

结构规则：

1. **一个记法一个文件**，放在 `Notation/` 目录下，文件名 = 记法名小写 + `.cs`（如 `prss.cs`、`bms.cs`）。
2. 记法实现过大需要拆分多文件时，在 `Notation/` 下建立与记法同名（小写）的子文件夹，但**仍只允许一个 public static 入口类**对外。
3. 多个相似记法共用的逻辑必须提取到 `Helpers/`，禁止在各记法文件间复制粘贴。
4. **记法代码是纯算法库**：`Notation/` 与 `Helpers/` 中的代码不得引用 `System.Windows.Forms` 或任何 UI 类型。
5. `old/` 与 `Sequence/` 目录已被工程文件排除编译，**不要把任何新代码放进去**（放了也不会被编译）。

## 三、代码结构要求（强制）

### 3.1 命名空间与类

- 命名空间统一为 `GoogologyExpander`。
- 每个记法有且仅有一个 `public static class`，类名与记法名一致（如 `PrSS`、`BMS`、`UPMS`、`WY`、`EY`）。
- 记法名无法直接作为 C# 标识符时（如 `0-Y` 以数字开头），使用最接近的合法命名（`ZeroY`），并在 Form1 注册时使用正确显示名。
- 所有对外入口必须是静态方法，禁止需要实例化才能调用的入口类。

### 3.2 入口方法签名（核心强制要求）

项目把所有记法接口统一为两种形态，新增记法**必须**二选一并完全遵守对应约定。

**一维记法 → PrSS 样式：**

```csharp
public static int[] Expand<记法名>(int[] sequence)
```

| 输入情况 | 强制行为 |
| --- | --- |
| `sequence == null` | 抛出 `ArgumentNullException(nameof(sequence))` |
| 空数组 | 返回 `Array.Empty<int>()`，不得抛异常 |
| 不符合展开条件 | 抛 `ArgumentException` / `InvalidOperationException`，消息为中文并说明原因 |
| 正常输入 | 返回**新数组**，禁止修改传入数组 |

**二维记法 → BMS 样式：**

```csharp
public static void Expand<记法名>(List<List<T>> matrix, int n)
```

| 输入情况 | 强制行为 |
| --- | --- |
| `matrix == null` | 抛出 `ArgumentNullException(nameof(matrix))` |
| 空矩阵 | 直接返回，不得抛异常 |
| 正常输入 | **原地修改**传入矩阵，连续展开 `n` 步 |
| 矩阵不符合展开条件 | 由记法自行定义（置空或保持原样），不得崩溃 |

### 3.3 异常规范

| 异常类型 | 使用场景 |
| --- | --- |
| `ArgumentNullException` | 输入引用为 `null`，必须配合 `nameof` |
| `ArgumentException` | 输入格式合法但不满足记法展开前提 |
| `InvalidOperationException` | 展开过程中结构上无法继续（找不到坏根、父项等） |

异常消息一律使用中文，写清具体原因，便于窗体直接展示给用户。

### 3.4 内部实现要求

- 复杂逻辑必须拆分为私有静态辅助方法，方法名自解释（参考 `bms.cs` 的 `BuildParentGraph`、`ComputeDeltaVector`）。
- 公开入口方法必须写 `///` XML 文档注释，说明展开规则、输入输出约定。
- **禁止引入可变全局状态**。`wy.cs` 中的 `Config` 静态类属历史遗留，新记法不得效仿；所需参数一律通过方法参数传入。
- 移植自外部项目的代码，必须在文件首行注明来源（参考 `wy.cs` 的 `//修改自Naruyoko/StudyAndExpandSequence`）。
- 偏离标准接口的特殊行为（如 `Y`/`WY` 对不可展开序列回退返回原序列），必须在 XML 注释中写明原因。

## 四、新记法提交流程

### 第 1 步：实现记法文件

在 `Notation/` 下创建文件，套用以下模板（一维）：

```csharp
using System;

namespace GoogologyExpander
{
	/// <summary>
	/// XXX 记法的展开实现。
	/// [移植来源 / AI 生成标注写在这里]
	/// </summary>
	public static class XXX
	{
		/// <summary>
		/// 对 XXX 序列进行一次展开（一维统一接口）。
		/// [展开规则简述]
		/// </summary>
		public static int[] ExpandXXX(int[] sequence)
		{
			if (sequence == null)
				throw new ArgumentNullException(nameof(sequence));

			if (sequence.Length == 0)
				return Array.Empty<int>();

			// TODO: 展开逻辑
			throw new NotImplementedException();
		}
	}
}
```

二维模板（把入口替换为）：

```csharp
		/// <summary>
		/// 对 XXX 矩阵进行展开（二维统一接口，原地修改）。
		/// </summary>
		public static void ExpandXXX(List<List<int>> matrix, int n)
		{
			if (matrix == null)
				throw new ArgumentNullException(nameof(matrix));

			if (matrix.Count == 0) return;

			// TODO: 原地展开逻辑
		}
```

### 第 2 步：在 Form1.cs 注册

一维记法加入 `OneDimSystems` 数组并提供示例；二维记法加入 `TwoDimSystems` 与 `Examples`：

```csharp
// Form1.OneDimSystems 中追加一行
("XXX", XXX.ExpandXXX),

// Form1.Examples 中追加一行
["XXX"] = "1, 2, 3",
```

未注册的记法在界面上不可用，视为提交不完整。

### 第 3 步：自测与编译

- 提供的示例输入必须**真实展开成功**（不抛异常、结果符合记法定义）。
- `dotnet build` 必须 **0 错误**。

## 五、提交前强制自检清单

- [ ] 文件位于 `Notation/`，文件名 = 记法名小写
- [ ] 唯一 `public static class`，类名 = 记法名
- [ ] 入口签名完全符合一维（PrSS 样式）或二维（BMS 样式）规范
- [ ] `null` 输入抛 `ArgumentNullException(nameof(...))`
- [ ] 空输入返回空结果且不抛异常
- [ ] 非法输入的异常消息为中文且说明原因
- [ ] 一维不修改输入数组；二维原地修改且支持步数 `n`
- [ ] 已在 Form1.cs 注册并提供可真实展开的示例
- [ ] `dotnet build` 0 错误
- [ ] 许可证符合要求（见下节），移植注明来源，AI 生成已标注

## 六、许可证与署名（强制）

- 项目使用 **GPLv3**。与 GPLv3 不兼容许可证的代码**不得直接引用**。
- 不得将小幅修改的他人代码伪装为原创直接推送，一经发现即刻禁止贡献。
- AI 生成的代码必须标注（可在文件头部注明"部分代码由 AI 生成"）。

## 七、验收标准

- **非正式版贡献**：并入后不得导致严重漏洞或编译不通过。
- **正式版贡献**：并入后功能不得出现问题（既有记法的展开结果不得改变）。
- 维护者有权依据本文档要求修改或退回不符合结构的贡献。

## 八、常见错误

| 错误 | 正确做法 |
| --- | --- |
| 入口方法自创签名（返回 `string`、接收字符串等） | 严格套用一维/二维标准签名，字符串解析交给 Form1 |
| 一维入口修改了传入数组 | 返回新数组 |
| 二维入口返回新矩阵 | 原地修改传入矩阵 |
| 忘记 `null` 检查 | 所有入口第一行做 `null` 校验 |
| 新记法没在 Form1 注册 | 注册进 `OneDimSystems`/`TwoDimSystems` 与 `Examples` |
| 代码放进 `old/` 或 `Sequence/` | 这些目录不参与编译，放 `Notation/` |
| 未测试示例就提交 | 示例必须真实展开成功 |
