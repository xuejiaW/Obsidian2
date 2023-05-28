import argparse
import os

from obsidian_to_hexo import ObsidianToHexo

parser = argparse.ArgumentParser()
parser.add_argument("--hexo-posts-dir", help="Directory of the target hexo posts directory", type=str)
parser.add_argument("--obsidian-vault-dir", help="Directory of the source Obsidian vault", type=str)


def main():
    args = parser.parse_args()
    if not args.hexo_posts_dir or not os.path.isdir(args.hexo_posts_dir):
        parser.error("The Hexo posts directory does not exist.")

    if not args.obsidian_vault_dir or not os.path.isdir(args.obsidian_vault_dir):
        parser.error("The Obsidian directory does not exist.")

    obsidian_to_hexo = ObsidianToHexo(obsidian_vault_dir=args.obsidian_vault_dir,
                                      hexo_posts_dir=args.hexo_posts_dir)
    obsidian_to_hexo.process()


if __name__ == '__main__':
    main()
