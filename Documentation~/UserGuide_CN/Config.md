# Config 命令

Config 命令用于管理 Obsidian2 的配置设置，包括全局配置和各个子命令的专用配置。

## 基本语法

```bash
obsidian2 config [选项] [子命令]
```

## 查看帮助信息

```bash
obsidian2 config --help
```

**输出示例：**
```
Description:
  Manage configuration settings

  Configuration file location:
  C:\Users\username\AppData\Local\Obsidian2\configuration.json

Usage:
  Obsidian2 config [command] [options]

Options:
  --list          List configuration settings
  --a             Show all command configurations (use with --list)
  -?, -h, --help  Show help and usage information

Commands:
  obsidian-vault-dir <directory>  Set Obsidian vault directory
  ignore                          Manage ignored paths
  export <filename>               Export current configuration to a JSON file []
  import <filename>               Import configuration from a JSON file
```

## 查看配置列表

通过 `obsidian2 config --list` 命令可以查看当前的配置列表，其中包括全局配置和各个子命令的专用配置。

### 查看全局配置

```bash
obsidian2 config --list
```

**输出示例：**
```
Global Configuration:
=====================
Obsidian Vault Path: C:\Users\username\Dropbox\PersonalNotes
Ignored Paths: .git, .obsidian, .trash

Note: Use 'obsidian2 config --list --a' to see all command configurations
```

#### 查看所有配置（包括命令配置）

使用 `--a` 选项可以查看所有配置，包括各个子命令的专用配置（每个子命令所支持的配置项将在子命令的文档中详细说明）。

```bash
obsidian2 config --list --a
```

如下输出带有 `hexo` 和 `compat` 的配置项。

**输出示例：**
```
Global Configuration:
=====================
Obsidian Vault Path: C:\Users\username\Dropbox\PersonalNotes
Ignored Paths: .git, .obsidian, .trash

Command Configurations:
=======================

--- compat ---
Compat Configuration:
====================
Assets Repository:
  Owner: newowner
  Name: testrepo
  Branch: main
  Image Path: images
  Access Token: Not set

--- hexo ---
Hexo Configuration:
==================
Posts Directory: D:\Test\Posts
```

## 全局配置

以下配置项适用于整个 Obsidian2 工具，即所有子命令都将应用这些配置。

### obsidian-vault-dir - 设置 Obsidian 库地址

设置 Obsidian 笔记库的根目录路径，参数 `<directory>` 为库的绝对路径，后续的所有命令都将基于该路径进行操作。

```bash
obsidian2 config obsidian-vault-dir <directory>
```

**示例：**
```bash
obsidian2 config obsidian-vault-dir "C:\Users\username\Documents\MyNotes"
```

### ignore - 管理忽略路径

`ignore` 命令用于管理在处理 Obsidian 笔记时需要忽略的文件和目录，后续所有命令的操作都将无视这些路径，路径支持通配符。可通过以下子命令进行操作：

#### 查看全部的忽略列表

```bash
obsidian2 config ignore list
```

**输出示例：**
```
Files in the following paths will not be processed:
.git
.obsidian
.trash
```

#### 添加忽略路径

```bash
obsidian2 config ignore add <path>
```

**示例：**
```bash
obsidian2 config ignore add "temp"
obsidian2 config ignore add ".backup"
```

#### 移除忽略路径

```bash
obsidian2 config ignore remove <path>
```

**示例：**
```bash
obsidian2 config ignore remove "temp"
```

### export - 导出配置

将当前配置导出到 JSON 文件中，便于备份或在其他环境中使用。

```bash
obsidian2 config export [filename] [选项]
```

**参数：**
- `[filename]` - 输出文件名（可选，默认为 obsidian2-config.json）

**选项：**
- `--output-dir <directory>` - 输出目录（可选，默认为当前目录）

**示例：**
```bash
# 导出到默认文件名
obsidian2 config export

# 导出到指定文件名
obsidian2 config export my-config.json

# 导出到指定目录
obsidian2 config export my-config.json --output-dir "D:\Backups"
```

**输出示例：**
```
Configuration exported successfully to: D:\Github\Personal\Obsidian2\Obsidian2\bin\Debug\net8.0\test-config.json
```

### import - 导入配置

从 JSON 文件导入配置设置。

```bash
obsidian2 config import <filename> [选项]
```

**参数：**
- `<filename>` - 要导入的 JSON 配置文件

**选项：**
- `--backup` - 导入前创建当前配置的备份（默认：False）
- `--force` - 强制导入，跳过确认提示（默认：False）

**示例：**
```bash
# 基本导入（会提示确认）
obsidian2 config import my-config.json

# 强制导入，跳过确认
obsidian2 config import my-config.json --force

# 导入时创建备份
obsidian2 config import my-config.json --backup
```

**输出示例：**
```
Current configuration backed up to: D:\MyWork\obsidian2-config-backup-20250831-140706.json
Configuration imported successfully from: my-config.json
```

> [!note]
>
> 可通过 import 和 export 命令进行配置的备份与恢复。
> 
> **重要变更：** 从当前版本开始，导入配置时默认**不会自动创建备份**。如需备份当前配置，请使用 `--backup` 选项。
> 
> **备份路径：** 备份文件使用绝对路径显示，保存在命令执行时的当前工作目录中。

## 相关命令

- `obsidian2 hexo config` - 管理 Hexo 相关配置
- `obsidian2 compat config` - 管理兼容性相关配置

更多信息请参考各子命令的专用文档。
