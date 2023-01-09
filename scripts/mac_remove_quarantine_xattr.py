#!/usr/bin/env python3

import xattr, os, sys, argparse

QUARANTINE_XATTR_KEY = 'com.apple.quarantine'


if __name__ == '__main__':
  parser = argparse.ArgumentParser()
  parser.add_argument('root_dir')
  parser.add_argument('--dry-run', action='store_true')
  args = parser.parse_args()

  for parent_dir, dirnames, filenames in os.walk(args.root_dir):
    for name in dirnames + filenames:
      path = os.path.join(parent_dir, name)
      if not os.path.exists(path):
        continue
      path_xattrs = xattr.xattr(path)
      if QUARANTINE_XATTR_KEY in path_xattrs:
        if args.dry_run:
          print(('[dry_run] %r found on %r' % (QUARANTINE_XATTR_KEY, path)))
        else:
          print(('Removing %r from %r' % (QUARANTINE_XATTR_KEY, path)))
          del path_xattrs[QUARANTINE_XATTR_KEY]
