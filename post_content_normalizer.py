import re

from note_path_normalizer import NotePathNormalizer


class PostContentNormalizer:
    @staticmethod
    def normalize_post_content(post_path: str) -> None:
        print(post_path)
        with open(post_path, "r", encoding="utf-8") as f:
            content = f.read()
        modified_content = PostContentNormalizer.modify_md_links_in_text(content)
        modified_content = PostContentNormalizer.convert_ad_note_to_butterfly_callout(modified_content)
        with open(post_path, "w", encoding="utf-8") as f:
            f.writelines(modified_content)

    @staticmethod
    def convert_ad_note_to_butterfly_callout(text: str) -> str:
        pattern = r'```ad-note\s*([\s\S]*?)```'

        def butterfly_callout(match):
            content = match.group(1).strip()
            return f'{{% note info %}}\n{content}\n{{% endnote %}}'

        converted_text = re.sub(pattern, butterfly_callout, text)
        return converted_text

    @staticmethod
    def modify_md_links_in_text(text: str) -> str:
        link_pattern = r'\[(.*?)\]\((.*?)\)'

        modified_text = re.sub(link_pattern, PostContentNormalizer.replace_link, text)
        return modified_text

    @staticmethod
    def replace_link(match):
        link_text = match.group(1)
        link_path = match.group(2)

        if link_path.endswith(".md"):
            link_path = "/" + link_path.split("/")[-1]
            link_path = link_path.replace(".md", "")
        if "assets/" in link_path:
            index_of_assets = link_path.find('assets/')
            link_path = '/' + link_path[index_of_assets + len('assets/'):]

        link_path = NotePathNormalizer.normalize_post_path(link_path)
        new_link = f"[{link_text}]({link_path})"
        return new_link
