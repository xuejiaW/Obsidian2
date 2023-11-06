# Obsidian2Hexo

Obsidian2Hexo is a dot net tool that convert Obsidian vault to Hexo posts.

The posts in [My blog](https://tuncle.blog/) are all converted by this tool.

## Background

As someone who uses Obsidian to take notes and Hexo to write blog posts, you may have noticed that the style of the obsidian markdown has some differences with the Hexo markdown. For example, hexo butterfly theme requires the assets of one post should be placed in the folder with the same name as the post.And all posts should be in the root path of the post folder.

This is where this tool comes in. Created out of a personal need for a more streamlined way to transfer notes from Obsidian to Hexo, the script automates the process of converting an Obsidian note into a Hexo post.

For my personal situation, the attachment in my obsidian note is managed by
plugin [Custom Attachment Location](https://github.com/RainCat1998/obsidian-custom-attachment-location), and the callout is using [Obsidian Admonition](https://github.com/javalent/admonitions).

I prefer traditional Markdown hyperlink, thus I use `[]()` instead of `[[ ]]` in my notes.

And I am using [hexo butterfly theme](https://github.com/jerryc127/hexo-theme-butterfly).

Thus, This tool may not work for you if you have a different setup, but you can easily modify the code to fit your needs.

## Prerequisites

This tool requires .net 6.0 or above runtime. You can install via winget:

```powershell
winget install Microsoft.DotNet.SDK.7
```

or use chocolatey:

```powershell
choco install --y dotnet-sdk
```

## How To install

After clone the repository, you can use the following command to pack the tool:

```powershell
dotnet pack
```

Then install the tool globally:

```powershell
dotnet tool install --global --add-source .\nupkg obsidian2hexo-cli
```

## How to use

The tool requires two arguments: the path to the Obsidian vault and the path to the Hexo source folder. The command
should follow the following format:

```bash
obsidian2hexo-cli --obsidian-vault-dir <ObsidianVaultDir> --hexo-posts-dir <HexoPostDir>
```

An example call would be:

```bash
obsidian2hexo --obsidian-vault-dir C:\Users\wxjwa\Dropbox\PersonalNotes --hexo-posts-dir D:\Github\xuejiaW.github.io\source\_posts
```

## How it works

After parsing the Obsidian vault and hexo post folder path, the tool will do the following things:

### Create Hexo post file

-   Copy all contents except the `.git` and `.obsidian` folder from the Obsidian vault to a temporary folder. The
    temporary folder path would be `<HexoPostFolder>/temp`，e.g `D:\Github\xuejiaW.github.io\source\_posts\temp`.
-   For each markdown file in the temporary folder,which represent a note, the tool will do the following things:
    -   Parse the `published` metadata in the markdown file. If the `published` metadata is `false`, the tool will skip
        the file. Otherwise, the tool will continue.
    -   Copy the markdown file to the Hexo post folder with a normalized file name. The normalized file name would be
        converted from the original file name by the following rules:
        -   Replace chinese characters with their pinyin.
        -   Replace space with \_
        -   Convert all characters to lowercase.
    -   Create asset folder for the markdown file in the Hexo post folder if required
        -   The asset folder only be created if the `\assets\<noteName>` folder exists which means the note has assets.
        -   The created asset folder path would be named as the normalized file name.
        -   Copy all files in the `\assets\<noteName>` folder to the created asset folder, and make sure the file name is
            all lowercase.
    -   Normalized the content inside the copied markdown, which will be explained in
        the [next section](#normalize-the-content).

#### Example

For the note file `C:\Users\wxj\Dropbox\PersonalNotes\00_Books\贪婪的多巴胺.md` in the Obsidian vault, the tool will
generate the post file `D:\Github\xuejiaW.github.io\source\_posts\tan_lan_de_duo_ba_an.md` in the Hexo post folder.

And if the note has assets, i.e. the folder `C:\Users\wxj\Dropbox\PersonalNotes\00_Books\assets\贪婪的多巴胺` existed,
the tool will create the asset folder `D:\Github\xuejiaW.github.io\source\_posts\tan_lan_de_duo_ba_an` and copy all
assets to the folder.

### Normalize the content

The tool will normalize the content inside the copied markdown file by the following rules:

1. Normalize the hyperlink:
    1. If the hyperlink destination is a unpublished markdown file, the tool will convert the hyperlink to plain text
    2. If the hyperlink destination is a published markdown file, the tool will convert the hyperlink to the hexo post
       folder.
    3. If the hyperlink destination is a title of a published markdown file, the tool will further normalize the title
       which only convert space to underscore
    4. If the hyperlink destination is content in the asset folder, the tool will convert the hyperlink to the asset
       folder.
2. Convert Obsidian Admonition to Hexo Callback

#### Example

If one note in obsidian has the following content which contains a hyperlink to another note，hyperlink to asset and
Obsidian admonition:

````markdown
```ad-note
对于新的图形 API，如 DirectX12， Vulkan 等，命令的开销会相对较少，但仍然应当尽可能的减少命令的数量。
```
````

如在 Book 2 的 [Rasterizing](Book%202%20Pipeline.md#Rasterizing) 中所属，光栅化后的单位是 `pre-pixles` ，Warp
中的四个线程会被分给一个 `pre-pixels` 。对于一些没有真正覆盖三角形的 Pixels 而言，它们的颜色并无意义，因此虽然它们在
pre-pixels 中但并不会有线程去计算它们的颜色，这也就造成了 Warp 中线程的浪费。这种性能浪费会比较常见的出现在狭长的三角形中，如下示意图所示：

![Thin Triangles 造成的性能浪费](assets/Book%203%20Problems/pipeline_rasterizing03_.gif)

````

The upper content will be converted to the following content:

```markdown
{% note info %}
对于新的图形 API，如 DirectX12， Vulkan 等，命令的开销会相对较少，但仍然应当尽可能的减少命令的数量。
{% endnote %}

如在 Book 2 的 [Rasterizing](/book_2_pipeline/#Rasterizing) 中所属，光栅化后的单位是 `pre-pixles` ，Warp
中的四个线程会被分给一个 `pre-pixels` 。对于一些没有真正覆盖三角形的 Pixels 而言，它们的颜色并无意义，因此虽然它们在
pre-pixels 中但并不会有线程去计算它们的颜色，这也就造成了 Warp 中线程的浪费。这种性能浪费会比较常见的出现在狭长的三角形中，如下示意图所示：

![Thin Triangles 造成的性能浪费](/book_3_problems/pipeline_rasterizing03_.gif)
````
