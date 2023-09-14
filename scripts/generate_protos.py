#!/usr/bin/env python3

import subprocess, os, shutil, argparse, sys


if __name__ == '__main__':
  parser = argparse.ArgumentParser()
  args = parser.parse_args()

  base_dir = os.path.dirname(sys.argv[0])

  protoc_bin_path = os.getenv('PROTOC_BIN_PATH')
  if protoc_bin_path is None:
    raise Exception('Set PROTOC_BIN_PATH to the path to protoc')

  grpc_csharp_plugin_bin_path = os.getenv('GRPC_CSHARP_PLUGIN_BIN_PATH')
  if grpc_csharp_plugin_bin_path is None:
    raise Exception('Set GRPC_CSHARP_PLUGIN_BIN_PATH to the path to grpc_csharp_plugin')

  subprocess.check_call(
    [protoc_bin_path, '-I', '../server/api/', '--csharp_out=.', '--grpc_out=.',
     '--plugin=protoc-gen-grpc=%s' % os.path.abspath(grpc_csharp_plugin_bin_path),
     '../server/api/leap_brush_api.proto'],
    cwd=base_dir)

  for filename in ['LeapBrushApi.cs', 'LeapBrushApiGrpc.cs']:
    from_path = os.path.join(base_dir, filename)
    assert os.path.exists(from_path)

    to_path = os.path.join(
      base_dir,
      '../LeapBrush/Assets/MagicLeap/LeapBrush/Scripts/Api/%s' % filename)

    assert os.path.exists(to_path)

    shutil.move(from_path, to_path)
