import os
import re

import frontmatter

from note_path_normalizer import NotePathNormalizer


def get_absolute_link_path(note_path: str, relative_link: str) -> str:
    decoded_relative_link = relative_link.replace("%20", " ")
    md_directory = os.path.dirname(note_path)
    abs_link_path = os.path.abspath(os.path.join(md_directory, decoded_relative_link))
    return abs_link_path


class PostContentNormalizer:

    def __init__(self, note_path: str, post_path: str) -> None:
        self.note_path = note_path
        self.post_path = post_path

    def normalize_post_content(self) -> None:
        with open(self.post_path, "r", encoding="utf-8") as f:
            content = f.read()
        modified_content = self.modify_md_links_in_text(content)
        modified_content = PostContentNormalizer.convert_ad_note_to_butterfly_callout(modified_content)
        with open(self.post_path, "w", encoding="utf-8") as f:
            f.writelines(modified_content)

    @staticmethod
    def convert_ad_note_to_butterfly_callout(text: str) -> str:
        ad_to_callout_map = {
            'ad-note': 'info',
            'ad-tip': 'info',
            'ad-warning': 'warning',
            'ad-quote': 'info',
            'ad-fail': 'danger'
        }
        pattern = r'```(ad-(?:note|tip|warning|quote|fail))\s*([\s\S]*?)```'

        def butterfly_callout(match):
            callout_type = ad_to_callout_map.get(match.group(1), 'info')
            content = match.group(2).strip()
            return f'{{% note {callout_type} %}}\n{content}\n{{% endnote %}}'

        converted_text = re.sub(pattern, butterfly_callout, text)
        return converted_text

    def modify_md_links_in_text(self, text: str) -> str:
        link_pattern = r'\[(.*?)\]\((.*?)\)'
        modified_text = re.sub(link_pattern, self.replace_link, text)
        return modified_text

    def replace_link(self, match):
        link_text = match.group(1)
        link_relative_path = match.group(2)

        fragment = ""

        if ".md" in link_relative_path:
            if "#" in link_relative_path:  # handle link for title
                link_relative_path, fragment = link_relative_path.split("#", 1)
                fragment = NotePathNormalizer.normalize_fragment(fragment)
                fragment = "/#" + fragment
            abs_link_path = get_absolute_link_path(self.note_path, link_relative_path)
            if not self.is_post_required_be_published(abs_link_path):
                return link_text

            link_relative_path = "/" + link_relative_path.split("/")[-1]
            link_relative_path = link_relative_path.replace(".md", "")

        if link_relative_path.startswith("#"):
            fragment = NotePathNormalizer.normalize_fragment(link_relative_path[1:])
            fragment = "/#" + fragment
            link_relative_path = "/" + os.path.basename(self.post_path).replace(".md", "")

        if "assets/" in link_relative_path:
            index_of_assets = link_relative_path.find('assets/')
            link_relative_path = '/' + link_relative_path[index_of_assets + len('assets/'):]

        link_relative_path = NotePathNormalizer.normalize_post_path(link_relative_path)

        new_link = f"[{link_text}]({link_relative_path}{fragment})"
        return new_link

    @staticmethod
    def is_post_required_be_published(post_path: str) -> bool:
        print(post_path)
        post = frontmatter.load(post_path)
        return post.metadata.get("published", False)
