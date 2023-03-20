#!/usr/bin/env -S python3 -u

import argparse
import os
import sys
import subprocess
import tempfile
import re


TARGET_ANDROID = 'Android'
TARGET_OSX = 'OSX'
TARGET_WINDOWS = 'Windows'
TARGET_LINUX = 'Linux'
TARGET_SERVER = 'Server'
TARGETS = [TARGET_ANDROID, TARGET_OSX, TARGET_WINDOWS, TARGET_LINUX, TARGET_SERVER]

TARGET_TO_UNITY_TARGET = {
  TARGET_ANDROID: 'Android',
  TARGET_WINDOWS: 'Win64',
  TARGET_OSX: 'OSXUniversal',
  TARGET_LINUX: 'Linux64'
}


if __name__ == '__main__':
  parser = argparse.ArgumentParser()
  parser.add_argument('--unity-editor-binary', required=True)
  parser.add_argument('--output-dir', required=True)
  parser.add_argument('--version-string-suffix')
  parser.add_argument('--target', dest='targets', default=[], choices=TARGETS, action='append')
  args = parser.parse_args()

  if not args.targets:
    args.targets = TARGETS

  if not os.path.isdir(args.output_dir):
    raise Exception('Expected directory %r to exist' % args.output_dir)

  root_dir = os.path.dirname(os.path.dirname(os.path.abspath(sys.argv[0])))
  unity_project_path = os.path.join(root_dir, 'LeapBrush')

  for target in args.targets:
    if target == TARGET_SERVER:
      print('Building server...')
      server_build_cmd = [
        os.path.join(root_dir, 'server', 'scripts', 'build.py'),
        '--output-dir', args.output_dir]
      if args.version_string_suffix:
        server_build_cmd.extend(['--version-string-suffix', args.version_string_suffix])
        subprocess.check_call(server_build_cmd)
    else:
      print('Building unity target %s...' % target)
      cmd = [args.unity_editor_binary, '-batchmode',
             '-buildTarget', TARGET_TO_UNITY_TARGET[target],
             '-projectPath', unity_project_path,
             '-executeMethod', 'MagicLeap.LeapBrushBuild.CommandLineBuild',
             '-outputDir', args.output_dir]
      if args.version_string_suffix:
        cmd.extend(['-versionStringSuffix', args.version_string_suffix])
      cmd.extend(['-quit', '-logfile', '/dev/stdout'])
      subprocess.check_call(cmd)
