// %BANNER_BEGIN%
// ---------------------------------------------------------------------
// %COPYRIGHT_BEGIN%
// Copyright (c) (2018-2022) Magic Leap, Inc. All Rights Reserved.
// Use of this file is governed by the Software License Agreement, located here: https://www.magicleap.com/software-license-agreement-ml2
// Terms and conditions applicable to third-party materials accompanying this distribution may also be found in the top-level NOTICE file appearing herein.
// %COPYRIGHT_END%
// ---------------------------------------------------------------------
// %BANNER_END%

/// <summary>
/// Interface providing API to receive permission callbacks, through runtime permissions config.
/// </summary>
public interface IPermissionCallbackHandler
{
    public void OnPermissionGranted(string permission);
    public void OnPermissionDenied(string permission);
    public void OnPermissionDeniedAndDontAskAgain(string permission);
}