/*
 * 文件用途：定义 RealRadio 模组的程序集元数据。
 * 说明：
 *   - 该文件由 MSBuild 在编译时读取，生成程序集名称、版本、版权等信息。
 *   - 程序集版本与 RealRadio.version 中的版本号保持一致，便于维护。
 *   - ComVisible 设置为 false，因为 KSP 模组不需要 COM 可见性。
 */

using System.Reflection;
using System.Runtime.InteropServices;

// 程序集标题：在文件属性中显示的名称
[assembly: AssemblyTitle("RealRadio")]
// 程序集描述：简要说明模组用途
[assembly: AssemblyDescription("KSP 1.12.5 无线电模组开发框架")]
// 程序集配置：Debug/Release 由编译时自动填充
[assembly: AssemblyConfiguration("")]
// 公司/作者名
[assembly: AssemblyCompany("")]
// 产品名称
[assembly: AssemblyProduct("RealRadio")]
// 版权信息
[assembly: AssemblyCopyright("Copyright © 2026")]
// 商标信息
[assembly: AssemblyTrademark("")]
// 文化设置：空字符串表示非特定文化
[assembly: AssemblyCulture("")]

// 不公开 COM 组件
[assembly: ComVisible(false)]

// 程序集 GUID：用于 COM 唯一标识，保持与项目 GUID 一致即可
[assembly: Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890")]

// 程序集版本号：主版本.次版本.修订号.构建号
[assembly: AssemblyVersion("0.1.0.0")]
// 文件版本号：通常与 AssemblyVersion 保持一致
[assembly: AssemblyFileVersion("0.1.0.0")]
