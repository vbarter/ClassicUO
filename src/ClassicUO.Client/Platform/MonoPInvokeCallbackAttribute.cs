// SPDX-License-Identifier: BSD-2-Clause

using System;

// Mono's ahead-of-time compiler (iOS) only generates native-to-managed wrappers for static
// methods marked with an attribute of this name. It matches by name only, so a local copy in
// any namespace works (and avoids clashing with ObjCRuntime in iOS hosts).
namespace ClassicUO.Platform
{
    [AttributeUsage(AttributeTargets.Method)]
    internal sealed class MonoPInvokeCallbackAttribute : Attribute
    {
        public MonoPInvokeCallbackAttribute(Type type)
        {
        }
    }
}
