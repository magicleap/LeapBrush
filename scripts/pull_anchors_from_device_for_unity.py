#!/usr/bin/env python3
"""Script to pull the anchor data for a localized device into a format suitable for copy+paste into Unity

Copy the json output into the LeapBrush scene at [Control] > LeapBrush : Anchors API Fake
@ Anchors (right click and paste onto the Anchors section)

"""

import subprocess, re, json, argparse


def StrToUnity(name, value):
  return  {
    "name": name,
    "type": 3,
    "val": value
  }


def FloatToUnity(name, value):
  return  {
    "name": name,
    "type": 2,
    "val": value
  }


def PositionToUnity(x, y, z):
  return {
    'name': 'position',
    'type': 9,
    'children': [
      FloatToUnity("x", x),
      FloatToUnity("y", -y),
      FloatToUnity("z", z)
    ]
  }


def RotationToUnity(x, y, z, w):
  return  {
    "name": "rotation",
    "type": 17,
    "val": "Quaternion(%f,%f,%f,%f)" % (x, -y, z, -w)
  }


def PoseToUnity(pos, rot):
  return {
    'name': 'Pose',
    'type': -1,
    'children': [
      pos,
      rot
    ]
  }


def AnchorToUnity(anchor_id, space_id, pose_u):
  return {
    "name": "data",
    "type": -1,
    "children": [
      StrToUnity("Id", anchor_id),
      StrToUnity("SpaceId", space_id),
      pose_u,
      {
        "name": "ExpirationTimeStamp",
        "type": 0,
        "val": 0
      },
      {
        "name": "IsPersisted",
        "type": 1,
        "val": True
      }
    ]
  }


if __name__ == '__main__':
  parser = argparse.ArgumentParser(description=__doc__,
                                   formatter_class=argparse.RawDescriptionHelpFormatter)
  parser.parse_args()

  output = subprocess.check_output(
    ['adb', 'shell', 'su', '0', 'pwscli', '-anchors'], text=True)

  anchor_id = None
  pos_u = None
  rot_u = None
  anchor_us = []

  for line in output.strip().split('\n'):
    m = re.match('^ID: [{]([a-f0-9-]+)[}]$', line)
    if m:
      anchor_id = m.group(1)
      print('ANCHOR ID', anchor_id)
      continue
    m = re.match('^Position: [(]([0-9.-]+), ([0-9.-]+), ([0-9.-]+)[)]$', line)
    if m:
      x, y, z = m.group(1, 2, 3)
      x, y, z = float(x), float(y), float(z)
      print('  POS', x, y, z)
      pos_u = PositionToUnity(x, y, z)
      continue
    m = re.match('^Rotation: [(]([0-9.-]+), ([0-9.-]+), ([0-9.-]+), ([0-9.-]+)[)]$', line)
    if m:
      x, y, z, w = m.group(1, 2, 3, 4)
      x, y, z, w = float(x), float(y), float(z), float(w)
      print('  ROT', x, y, z, w)
      rot_u = RotationToUnity(x, y, z, w)
      continue
    m = re.match('^Space: [{]([a-f0-9-]+)[}]$', line)
    if m:
      space_id = m.group(1)
      print('  SPACE ID', space_id)
      anchor_us.append(AnchorToUnity(
        anchor_id,
        space_id,
        PoseToUnity(pos_u, rot_u)))
      continue

  data_u = {
    "name": "_anchors",
    "type": -1,
    "arraySize": len(anchor_us),
    "arrayType": "FakeAnchor",
    "children": [
      {
        "name": "Array",
        "type": -1,
        "arraySize": len(anchor_us),
        "arrayType": "FakeAnchor",
        "children": [
          {
            "name": "size",
            "type": 12,
            "val": len(anchor_us)
          },
        ] + anchor_us
      }
    ]
  }

  print()
  print('GenericPropertyJSON:%s' % json.dumps(data_u))
