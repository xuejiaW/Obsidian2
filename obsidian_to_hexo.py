import shutil
import os
import frontmatter
import codecs
import re
from pypinyin import lazy_pinyin


def ignore_git_and_obsidian(folder, names):
    ignored_folders = {".git", ".obsidian"}
    ignored_files = set()
    for name in names:
        if name in ignored_folders:
            ignored_files.add(name)
    return ignored_files


def modify_md_links_in_text(text: str) -> str:
    link_pattern = r'\[(.*?)\]\((.*?)\)'

    modified_text = re.sub(link_pattern, replace_link, text)
    return modified_text


def replace_link(match):
    link_text = match.group(1)
    link_path = match.group(2)

    if link_path.endswith(".md"):
        link_path = "/" + link_path.split("/")[-1]
        link_path = link_path.replace(".md", "")
    if "assets/" in link_path:
        index_of_assets = link_path.find('assets/')
        link_path = '/' + link_path[index_of_assets + len('assets/'):]

    link_path = ObsidianToHexo.normalize_text(link_path)
    new_link = f"[{link_text}]({link_path})"
    return new_link


def replace_markdown_link(post_path: str) -> None:
    print(post_path)
    with open(post_path, "r", encoding="utf-8") as f:
        content = f.read()
    modified_content = modify_md_links_in_text(content)
    with open(post_path, "w", encoding="utf-8") as f:
        f.writelines(modified_content)


class ObsidianToHexo:
    def __init__(self, obsidian_vault_dir: str, hexo_posts_dir: str) -> None:
        self.obsidian_vault_dir = obsidian_vault_dir
        self.hexo_posts_dir = hexo_posts_dir
        self.obsidian_temp_path = self.hexo_posts_dir + "\\temp\\"  # Path to temporarily save obsidian notes

    def process(self) -> None:
        if os.path.isdir(self.obsidian_temp_path):
            shutil.rmtree(self.obsidian_temp_path)
        shutil.copytree(self.obsidian_vault_dir, self.obsidian_temp_path, ignore=ignore_git_and_obsidian)
        self.convert_obsidian_notes_to_hexo_page_bundles()
        shutil.rmtree(self.obsidian_temp_path)
        print("Handle Complete")

    def convert_obsidian_notes_to_hexo_page_bundles(self) -> None:
        for root, dirs, files in os.walk(self.obsidian_temp_path):
            for file in files:
                if file.endswith(".md"):
                    if self.is_post_required_be_published(os.path.join(root, file)):
                        post_path = self.create_hexo_page_bundle(root, file)
                        replace_markdown_link(post_path)

    def create_hexo_page_bundle(self, note_root: str, note: str) -> str:
        note_name = note.replace(".md", '')
        note_path = os.path.join(note_root, note)
        post_path = os.path.join(self.hexo_posts_dir, self.normalize_text(note_name) + ".md")

        if os.path.exists(post_path):
            os.remove(post_path)

        shutil.copy(note_path, post_path)

        # Copy assets if there are
        asset_path = (note_root + "\\assets\\" + note_name)
        if os.path.isdir(asset_path):
            dir_path = os.path.join(self.hexo_posts_dir, note_name)
            dir_path = self.normalize_text(dir_path)
            if os.path.exists(dir_path):
                shutil.rmtree(dir_path)
            shutil.copytree(asset_path, dir_path)

        return post_path

    @staticmethod
    def is_post_required_be_published(post_path: str) -> bool:
        post = frontmatter.load(post_path)
        return post.metadata.get("published", False)

    @staticmethod
    def add_math_metadata(post_path: str) -> None:
        post = frontmatter.load(post_path)
        post['math'] = True
        file = codecs.open(post_path, "w", "utf-8")
        file.write(frontmatter.dumps(post, sort_keys=False))
        file.close()

    @staticmethod
    def normalize_text(text: str) -> str:
        ret = ObsidianToHexo.make_chinese_to_pinyin(text)
        ret = ObsidianToHexo.make_space_to_underscore(ret)
        ret = ObsidianToHexo.make_to_lowercase(ret)
        return ret

    @staticmethod
    def make_chinese_to_pinyin(text: str) -> str:
        pinyin_result = []
        last_was_pinyin = False

        for c in text:
            if '\u4e00' <= c <= '\u9fa5':
                pinyin_for_char = lazy_pinyin(c)
                pinyin_result.extend(pinyin_for_char)
                pinyin_result.append('_')
                last_was_pinyin = True
            else:
                if last_was_pinyin:
                    pinyin_result.pop()
                    last_was_pinyin = False
                pinyin_result.append(c)

        if last_was_pinyin:
            pinyin_result.pop()

        return ''.join(pinyin_result)

    @staticmethod
    def make_space_to_underscore(text: str) -> str:
        ret = text.replace(" ", "_")
        ret = ret.replace("%20", "_")
        return ret

    @staticmethod
    def make_to_lowercase(text: str) -> str:
        return text.lower()
