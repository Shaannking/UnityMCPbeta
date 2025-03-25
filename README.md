![image](https://github.com/user-attachments/assets/1610c553-051f-43d2-a308-d0261add843d)

# Unity MCP 包

Unity MCP 包是一个能够实现 Unity 与大型语言模型（如 Claude Desktop）通过模型上下文协议（Model Context Protocol，MCP）进行无缝通信的 Unity 包。该服务器充当桥梁，允许 Unity 向符合 MCP 的工具发送命令并接收响应，使开发者能够自动化工作流程、操作资产并以编程方式控制 Unity 编辑器。

欢迎使用这个开源项目的首次发布版本！无论您是想将大型语言模型整合到 Unity 工作流程中，还是想为这个令人兴奋的新工具做出贡献，我都感谢您抽出时间来了解它！

## 概述

Unity MCP 服务器提供了 Unity（通过 C#）和 Python 服务器之间的双向通信通道，实现：

- **资产管理**：以编程方式创建、导入和操作 Unity 资产。
- **场景控制**：管理场景、对象及其属性。
- **材质编辑**：修改材质及其属性。
- **脚本集成**：查看、创建和更新 Unity 脚本。
- **编辑器自动化**：控制 Unity 编辑器功能，如撤销、重做、播放和构建。

这个项目非常适合希望利用大型语言模型来增强 Unity 项目或自动化重复任务的开发者。

## 安装

要使用 Unity MCP 包，请确保您已安装以下内容：

- Unity 2020.3 LTS 或更新版本（⚠️ 目前仅适用于 URP 项目）
- Python 3.12 或更新版本
- uv 包管理器

### 步骤 1：安装 Python

从 python.org 下载并安装 Python 3.12 或更新版本。确保在安装过程中将 Python 添加到系统的 PATH 中。

### 步骤 2：安装 uv

uv 是一个简化依赖管理的 Python 包管理器。根据您的操作系统使用以下命令安装：

**Mac**：
```
brew install uv
```

**Windows**：
```
powershell -c "irm https://astral.sh/uv/install.ps1 | iex"
```
然后，将 uv 添加到您的 PATH：
```
set Path=%USERPROFILE%\.local\bin;%Path%
```

**Linux**：
```
curl -LsSf https://astral.sh/uv/install.sh | sh
```

有关其他安装方法，请参阅 uv 安装指南。

**重要提示**：请务必安装 uv 后再继续。

### 步骤 3：安装 Unity 包

1. 打开 Unity 并转到 Window > Package Manager。
2. 点击 + 按钮并选择 Add package from git URL。
3. 输入：https://github.com/VR-Jobs/UnityMCPbeta.git

安装后，Unity MCP 包将在您的 Unity 项目中可用。当与 Claude Desktop 或 Cursor 等 MCP 客户端一起使用时，服务器将自动启动。

## 功能

- **双向通信**：在 Unity 和大型语言模型之间无缝发送和接收数据。
- **资产管理**：以编程方式导入资产、实例化预制件和创建新预制件。
- **场景控制**：打开、保存和修改场景，以及创建和操作游戏对象。
- **材质编辑**：轻松应用和修改材质。
- **脚本集成**：在 Unity 中创建、查看和更新 C# 脚本。
- **编辑器自动化**：自动化 Unity 编辑器任务，如构建项目或进入播放模式。

## 贡献

我很希望您能帮助改进 Unity MCP 服务器！以下是贡献的方法：

### 创建分支
```
git checkout -b feature/your-feature-name
```
或
```
git checkout -b bugfix/your-bugfix-name
```

### 进行更改
实现您的功能或修复。

### 提交并推送
使用清晰、描述性的提交消息：
```
git commit -m "Add feature: your feature description"
git push origin feature/your-feature-name
```

### 提交拉取请求
向 master 分支提交一个包含您更改描述的拉取请求。

## 故障排除

遇到问题？尝试以下解决方法：

### Unity 桥接未运行
确保 Unity 编辑器已打开且 MCP 窗口处于活动状态。如有需要，重启 Unity。

### Python 服务器未连接
验证 Python 和 uv 已正确安装，并且 Unity MCP 包已正确设置。

### Claude Desktop 或 Cursor 的配置问题
确保您的 MCP 客户端已配置为与 Unity MCP 服务器通信。

如需更多帮助，请访问问题跟踪器或提交新问题。

常见错误：
1️⃣unity的urp渲染出问题：建议采用unity6000比较稳定。因为2021有些版本不支持urp的17.0

2️⃣unity6000创建项目，名称不能有空格，比如myproject（1）就不行。建议unityMCP这样的格式

3️⃣uv包安装失败，尝试采用

```
curl -LsSf https://astral.sh/uv/install.sh | sudo sh
```

然后添加到 PATH

```bash
source $HOME/.local/bin/env
```

然后检查安装验证 uv 是否可用：

```bash
uv --version
```
正确情况应该显示
uv 0.6.9 (3d9460278 2025-03-20)

4️⃣Claude连接失败？尝试采用衔接方案：

1. 创建一个包装脚本

首先，创建一个 shell 脚本来运行 uv 命令：

```
mkdir -p ~/unity_mcp_scripts
nano ~/unity_mcp_scripts/run_uv.sh
```

2. 在脚本中添加以下内容：此处应该是一个可以编辑的界面，有很多快捷键，类似text记事本。然后粘贴下面的内容。然后保存，确认，回到主界面


```
#!/bin/bash
export PATH="/Users/XXXXXX/.local/bin:$PATH"
cd "/Users/XXXXXX/unityMCP/Library/PackageCache/com.vrjobs.unitymcp/Python"
/Users/XXXXXX/.local/bin/uv run server.py
```

3. 然后粘贴下面内容，使得脚本可执行：

```
chmod +x ~/unity_mcp_scripts/run_uv.sh
```

4. 修改 Claude Desktop 配置文件，或者手动去unity的window-unityMCP，点击Manual手动设置claude文件，注意XXXXXX换成你的用户路径
   <img width="315" alt="截屏2025-03-24 22 23 04" src="https://github.com/user-attachments/assets/091c30b8-cc01-4819-9e1f-65971f8e49dc" />


或者直接输入
```bash
nano ~/Library/Application\ Support/Claude/claude_desktop_config.json
```

将内容替换为：

```
{
  "mcpServers": {
    "unityMCP": {
      "command": "/bin/bash",
      "args": [
        "/Users/XXXXXX/unity_mcp_scripts/run_uv.sh"
      ]
    }
  }
}
```

5. 重启Claude，正确可以用的情况应该是有一个🔌插头按钮，还有右侧锤子🔨显示工具情况：
<img width="726" alt="截屏2025-03-23 15 58 52" src="https://github.com/user-attachments/assets/c93e8d30-c440-4110-bc64-8576107ecf4a" />
<img width="724" alt="截屏2025-03-23 16 00 21" src="https://github.com/user-attachments/assets/4aa533a6-ec4e-42d2-8366-2496e7112b31" />



演示案例：

1️⃣草图生成消消乐小游戏：
<img width="926" alt="截屏2025-03-22 11 11 26" src="https://github.com/user-attachments/assets/60d41ab3-6d45-45ff-93ee-c81eae07d704" />

提示词：
i want to make a block click game, at the beginning, there are 4 multiple 4 totally 16 blocks, i can use left button click to click any blocks. if i click the green blocks like this picture, the continuous four green blocks will disappear together. and then the system will add an extra four green blocks to fill in the void area. again, if i click the orange blocks, all continuous orange blocks will disappear, and the system will add same number of blocks to fill in. please refer my draft picture, use cube, and use red, orange, yellow, green, blue to make a game.

最终效果
<img width="1420" alt="截屏2025-03-22 11 37 29" src="https://github.com/user-attachments/assets/4cb3be8a-5f35-4f2d-b127-b4cb66597d3b" />


2️⃣网络图片一张图复刻积木弹球游戏
![hq720](https://github.com/user-attachments/assets/0f430a33-f6a2-4d0d-bc53-229103a507fc)

提示词：
in my scene, please refer to this picture, help me use sphere and cube to make a game： Block ball game. I can use the mouse to move the left and right positions of the platform cube below. The gameplay is: at the beginning of the game, there is a small ball on the platform below. When I click the left mouse button, the ball is launched into the air. When the ball hits the colored blocks in the air, those blocks will disappear. Every time a block is hit, one point is scored. The ball returns. I need to shake the mouse to catch the ball with the rectangular cube below and let it catapult into the air again. If I don't catch the ball, the game fails, and a text mesh pro is displayed. When all the blocks in the air disappear, the game is won.

最终效果
<img width="650" alt="截屏2025-03-23 16 11 34" src="https://github.com/user-attachments/assets/ca3757ff-7e6a-4b2a-88af-3a5ee662f01b" />

3️⃣图片复刻3D场景
![9647d48b-062e-447d-a3c8-db86b3063e7a_Ekran_g_r_nt_s__2025-03-17_004005](https://github.com/user-attachments/assets/c5ccd37e-7974-4677-aeb3-88eb7f663a9d)

提示词：
please refer to this picture and use sphere, cube, and cylinder objects to build a similar scene. you should also make sure the color of each object is same

最终效果
<img width="626" alt="截屏2025-03-23 16 08 13" src="https://github.com/user-attachments/assets/83087fa5-a9f7-4575-b2b8-d32684082c75" />


🌟小练习：自己制作一个UI功能MCP：
提示：
1.新建UI功能实现的cs脚本UICommandHandler.cs。在Editor/Commands
2.新建python功能和UI对接的py脚本ui_tools.py
3.更新unity端CommandRegistry.cs和UnityMCPBridge.cs控制代码注册列表
4.更新python端i__init__.py注册列表。在Python/tools文件夹
5.更新server.py服务端协调代码的prompt


🙋目前不足的地方：
1️⃣UI界面的复刻需要用代码实现，不是很方便。效果也不太好

<img width="909" alt="image" src="https://github.com/user-attachments/assets/fd83e8b4-43d0-43f7-a5c4-4aa5b1edc9da" />

2️⃣制作积木弹球游戏，他是分别每一个积木单独创建、设置颜色、位置，很麻烦，

<img width="1137" alt="image" src="https://github.com/user-attachments/assets/dae9f5f4-5688-4550-b107-7ba307fcf048" />

3️⃣对于稍微复杂一点的游戏机制，比如动画Animator和Animation设置，物理材质设置还不太智能

4️⃣未来测试方向：文本生成游戏，图片生成游戏，商品生成游戏，草图生成游戏。场景所有物体信息理解和处理，所有代码文件夹综合分析。

## 联系方式

有问题或想聊聊这个项目？请联系！


## 致谢

非常感谢所有支持这个项目初始发布的人。特别感谢 Unity Technologies 提供的出色编辑器 API。

祝您编码愉快，享受将大型语言模型与 Unity 集成的过程！

## 附录：37个tools工具介绍：

# 37个Unity MCP工具的简介

1. **apply_prefab**: 将预制体实例的更改应用回原始预制体资产。

2. **attach_script**: 将脚本组件附加到GameObject上。

3. **build**: 为指定平台构建Unity项目。

4. **change_scene**: 切换到不同的场景，可选择是否保存当前场景。

5. **create_object**: 在Unity场景中创建游戏对象(如立方体、球体等)。

6. **create_prefab**: 从场景中的GameObject创建新的预制体资产。

7. **create_script**: 创建新的Unity脚本文件。

8. **delete_object**: 从场景中删除游戏对象。

9. **execute_command**: 在Unity编辑器中执行特定的编辑器命令或自定义脚本。

10. **execute_context_menu_item**: 在给定游戏对象的组件上执行特定的[ContextMenu]方法。

11. **find_objects_by_name**: 通过名称在场景中查找游戏对象。

12. **find_objects_by_tag**: 通过标签在场景中查找游戏对象。

13. **get_asset_list**: 获取项目中的资产列表。

14. **get_available_commands**: 获取可在Unity编辑器中执行的所有可用命令列表。

15. **get_component_properties**: 获取游戏对象上特定组件的属性。

16. **get_hierarchy**: 获取场景中游戏对象的当前层次结构。

17. **get_object_info**: 获取特定游戏对象的信息。

18. **get_object_properties**: 获取指定游戏对象的所有属性。

19. **get_scene_info**: 检索当前Unity场景的详细信息。

20. **get_selected_object**: 获取Unity编辑器中当前选择的游戏对象。

21. **import_asset**: 将资产(如3D模型、纹理)导入Unity项目。

22. **instantiate_prefab**: 在指定位置将预制体实例化到当前场景中。

23. **list_scripts**: 列出指定文件夹中的所有脚本文件。

24. **modify_object**: 修改游戏对象的属性和组件。

25. **new_scene**: 在Unity编辑器中创建新的空场景。

26. **open_scene**: 在Unity编辑器中打开指定的场景。

27. **pause**: 在播放模式下暂停游戏。

28. **play**: 在Unity编辑器中以播放模式启动游戏。

29. **read_console**: 从Unity控制台读取日志消息。

30. **redo**: 重做Unity编辑器中最后撤销的操作。

31. **save_scene**: 将当前场景保存到其文件中。

32. **select_object**: 在Unity编辑器中选择一个游戏对象。

33. **set_material**: 为游戏对象应用或创建材质。

34. **stop**: 停止游戏并退出播放模式。

35. **undo**: 撤销在Unity编辑器中执行的最后一个操作。

36. **update_script**: 更新现有Unity脚本的内容。

37. **view_script**: 查看Unity脚本文件的内容。
