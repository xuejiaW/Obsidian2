from pypinyin import lazy_pinyin


class NotePathNormalizer:
    @staticmethod
    def normalize_post_path(text: str) -> str:
        ret = NotePathNormalizer.make_chinese_to_pinyin(text)
        ret = NotePathNormalizer.make_space_to_underscore(ret)
        ret = NotePathNormalizer.make_to_lowercase(ret)
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
