#!/usr/bin/env -S python3 -u

import argparse
import glob
import os
import sys
import subprocess
import tempfile
import re


def Build(root_dir, go_os, arch, output_path):
  env = os.environ.copy()
  env['GOOS'] = go_os
  env['GOARCH'] = arch
  cmd = ['go', 'build', '-o', output_path]
  cmd.extend(glob.glob('cmd/leapbrush-server/*', root_dir=root_dir))
  subprocess.check_call(cmd, env=env, cwd=root_dir)


def BuildMac(root_dir, output_parent_dir, version_string):
  output_path = os.path.join(
    output_parent_dir, 'leapbrush-server-mac-%s.universal' % version_string)

  output_tmp_files = []
  for arch in ['arm64', 'amd64']:
    output_tmp = tempfile.NamedTemporaryFile()
    Build(root_dir, 'darwin', arch, output_tmp.name)
    output_tmp_files.append(output_tmp)

  subprocess.check_call(['lipo', '-create', '-output', output_path]
                        + [ t.name for t in output_tmp_files])


def BuildLinux(root_dir, output_parent_dir, version_string):
  output_path = os.path.join(
    output_parent_dir, 'leapbrush-server-linux-%s.x86_64' % version_string)
  Build(root_dir, 'linux', 'amd64', output_path)


def BuildWindows(root_dir, output_parent_dir, version_string):
  output_path = os.path.join(
    output_parent_dir, 'leapbrush-server-windows-%s.exe' % version_string)
  Build(root_dir, 'windows', 'amd64', output_path)


if __name__ == '__main__':
  parser = argparse.ArgumentParser()
  parser.add_argument('--output-dir', required=True)
  parser.add_argument('--version-string-suffix')
  args = parser.parse_args()

  if not os.path.isdir(args.output_dir):
    raise Exception('Expected directory %r to exist' % args.output_dir)

  root_dir = os.path.dirname(os.path.dirname(os.path.abspath(sys.argv[0])))

  with open(os.path.join(root_dir, 'cmd/leapbrush-server/version.go'), 'r') as in_f:
    m = re.search('^\s+serverVersion\s+=\s+"([^"]+)"$', in_f.read(), re.MULTILINE)
    assert m
    version = m.group(1)

  version_string = 'v%s' % version
  if args.version_string_suffix:
    version_string = '%s-%s' % (version_string, args.version_string_suffix)

  print('Building version %r...' % version_string)

  output_parent_dir = os.path.join(args.output_dir, version_string)
  if not os.path.exists(output_parent_dir):
    os.mkdir(output_parent_dir)

  BuildMac(root_dir, output_parent_dir, version_string)
  BuildWindows(root_dir, output_parent_dir, version_string)
  BuildLinux(root_dir, output_parent_dir, version_string)
