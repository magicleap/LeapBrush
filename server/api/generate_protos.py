#!/usr/bin/env python3

import subprocess, os, shutil, argparse, sys


if __name__ == '__main__':
  parser = argparse.ArgumentParser()
  args = parser.parse_args()

  base_dir = os.path.dirname(sys.argv[0])

  protoc_bin_path = os.getenv('PROTOC_BIN_PATH')
  if protoc_bin_path is None:
    raise Exception('Set PROTOC_BIN_PATH to the path to protoc')

  subprocess.check_call(
    [protoc_bin_path, '--go_out=.', '--go_opt=paths=source_relative',
     '--go-grpc_out=.', '--go-grpc_opt=paths=source_relative',
     'leap_brush_api.proto'],
    cwd=base_dir)
