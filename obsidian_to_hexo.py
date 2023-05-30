import shutil
import os

from note_path_normalizer import NotePathNormalizer
from post_content_normalizer import PostContentNormalizer


class ObsidianToHexo:
    def __init__(self, obsidian_vault_dir: str, hexo_posts_dir: str) -> None:
        self.obsidian_vault_dir = obsidian_vault_dir
        self.hexo_posts_dir = hexo_posts_dir
        self.obsidian_temp_path = self.hexo_posts_dir + "\\temp\\"  # Path to temporarily save obsidian notes

    def process(self) -> None:
        if os.path.isdir(self.obsidian_temp_path):
            shutil.rmtree(self.obsidian_temp_path)
        shutil.copytree(self.obsidian_vault_dir, self.obsidian_temp_path,
                        ignore=ObsidianToHexo.ignore_git_and_obsidian_folder)
        self.convert_obsidian_notes_to_hexo_page_bundles()
        shutil.rmtree(self.obsidian_temp_path)
        print("Handle Complete")

    def convert_obsidian_notes_to_hexo_page_bundles(self) -> None:
        for root, dirs, files in os.walk(self.obsidian_temp_path):
            for file in files:
                if file.endswith(".md"):
                    note_path = os.path.join(root, file)
                    if PostContentNormalizer.is_post_required_be_published(note_path):
                        post_path = self.create_hexo_page_bundle(root, file)
                        post_content_normalizer = PostContentNormalizer(note_path, post_path)
                        post_content_normalizer.normalize_post_content()

    def create_hexo_page_bundle(self, note_root: str, note: str) -> str:
        note_name = note.replace(".md", '')
        note_path = os.path.join(note_root, note)
        post_path = os.path.join(self.hexo_posts_dir, NotePathNormalizer.normalize_post_path(note_name) + ".md")

        if os.path.exists(post_path):
            os.remove(post_path)

        shutil.copy(note_path, post_path)

        # Copy assets if there are
        asset_path = (note_root + "\\assets\\" + note_name)
        if os.path.isdir(asset_path):
            dir_path = os.path.join(self.hexo_posts_dir, note_name)
            dir_path = NotePathNormalizer.normalize_post_path(dir_path)
            if os.path.exists(dir_path):
                shutil.rmtree(dir_path)
            shutil.copytree(asset_path, dir_path)
            for root, _, files in os.walk(dir_path):
                for file in files:
                    old_file_path = os.path.join(root, file)
                    new_file_path = os.path.join(root, file.lower())
                    shutil.move(old_file_path, new_file_path)

        return post_path

    @staticmethod
    def ignore_git_and_obsidian_folder(folder, names):
        ignored_folders = {".git", ".obsidian"}
        ignored_files = set()
        for name in names:
            if name in ignored_folders:
                ignored_files.add(name)
        return ignored_files
