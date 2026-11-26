# Sherlock Desktop

Sherlock Desktop 是一个面向 Windows 的非官方本地图形界面，封装了
[Sherlock Project](https://github.com/sherlock-project/sherlock) 的用户名公开检索功能。

本项目提供：

- 中文 Windows 桌面界面，无需 PowerShell
- 可选择全部站点、常用站点或自定义站点
- 可配置超时与代理
- 自动生成 TXT、CSV 和 Excel 报告
- 每次检索按用户名和时间独立归档，不覆盖历史结果

> 本项目不是 Sherlock Project 官方项目。Sherlock 的源码、名称和相关资源归原项目及其贡献者所有。

## 已构建程序

下载仓库中的：

```text
dist/SherlockDesktop.exe
```

首次使用前，需要准备 Sherlock 运行环境。双击：

```text
setup-windows.cmd
```

安装脚本要求电脑已安装 Python 3.10-3.13，并从 PyPI 安装 Sherlock 依赖。完成后双击
`dist/SherlockDesktop.exe` 即可使用。

默认检索结果保存在：

```text
outputs/Sherlock检索结果/用户名/日期-时间
```

## 使用方式

1. 输入目标用户名，只填写用户名本身，不要填写完整网址。
2. 选择检查范围。
3. 选择需要的报告格式。
4. 点击“开始检索”。
5. 完成后点击“结果目录”。

完整检查会访问数百个站点，通常需要几分钟。部分站点可能因网络、地区限制或反自动化策略返回超时、WAF 或未知状态，这些结果不能直接视为账号不存在。

## 从源码构建

桌面程序使用 Windows 自带的 .NET Framework WinForms 编译器构建：

```text
desktop/build.cmd
```

构建产物为：

```text
dist/SherlockDesktop.exe
```

## 默认不上传的内容

以下文件仅用于本地运行，不进入 Git 仓库：

- `sherlock/runtime/`：Python 与第三方依赖
- `sherlock/.venv/`：本地虚拟环境
- `outputs/`：检索结果和本地说明
- `work/`：开发与测试临时文件

## 合规使用

仅对你拥有授权或可以合法调查的公开账号使用本项目，并遵守目标网站服务条款和适用法律。

## License

本项目采用 MIT License。Sherlock Project 同样采用 MIT License，原始许可证位于
`sherlock/LICENSE`。
