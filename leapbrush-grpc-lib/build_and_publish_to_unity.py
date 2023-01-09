#!/usr/bin/env python3

import os, subprocess, argparse, shutil, sys


if __name__ == '__main__':
  parser = argparse.ArgumentParser()
  args = parser.parse_args()

  android_root = os.getenv('ANDROID_ROOT')
  if android_root is None:
    raise Exception('Set ANDROID_ROOT variable to your android sdk root')

  protoc_bin_path = os.getenv('PROTOC_BIN_PATH')
  if protoc_bin_path is None:
    raise Exception('Set PROTOC_BIN_PATH to the path to protoc')

  grpc_cpp_plugin_bin_path = os.getenv('GRPC_CPP_PLUGIN_BIN_PATH')
  if grpc_cpp_plugin_bin_path is None:
    raise Exception('Set GRPC_CPP_PLUGIN_BIN_PATH to the path to grpc_cpp_plugin')

  base_dir = os.path.dirname(sys.argv[0])

  leapbrush_unity_lib_path = os.path.join(base_dir, '../LeapBrush/Assets/Plugins/Android/LeapBrushLib/')
  if not os.path.isdir(leapbrush_unity_lib_path):
    raise Exception('Expected directory %r to exist' % leapbrush_unity_lib_path)

  subprocess.check_call(
    ['cmake',
     '-Dleapbrush_PROTOBUF_PROTOC_EXECUTABLE:STRING=%s' % protoc_bin_path,
     '-Dleapbrush_GRPC_CPP_PLUGIN_EXECUTABLE:STRING=%s' % grpc_cpp_plugin_bin_path,
     '-DCMAKE_TOOLCHAIN_FILE=%s' % os.path.join(
       android_root, 'ndk/24.0.8215888/build/cmake/android.toolchain.cmake'),
     '-DANDROID_ABI=x86_64', '-DANDROID_PLATFORM=android-29'],
    cwd=base_dir)

  subprocess.check_call(
    ['make', '-j16', 'leapbrush_grpc', 'leapbrush_proto'],
    cwd=base_dir)

  subprocess.check_call(
    ['mv', os.path.join(base_dir, 'libleapbrush_grpc.so'), leapbrush_unity_lib_path])
  subprocess.check_call(
    ['mv', os.path.join(base_dir, 'libleapbrush_proto.so'), leapbrush_unity_lib_path])
