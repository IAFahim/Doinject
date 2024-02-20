﻿using System;
using System.Collections.Generic;
using System.Reflection;

namespace Doinject
{
    internal class TargetMethodsInfo
    {
        public List<MethodInfo> InjectMethods { get; } = new();
        public List<MethodInfo> PostInjectMethods { get; } = new();
        public List<MethodInfo> OnInjectedMethods { get; } = new();

        public TargetMethodsInfo(Type targetType)
        {
            foreach (var methodInfo in targetType.GetMethods(BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic))
            {
                if (methodInfo.GetCustomAttributes(typeof(InjectAttribute), true).Length > 0)
                {
                    if (!methodInfo.IsPublic)
                        throw new Exception($"Inject method must be public. {targetType.Name}.{methodInfo.Name}()");
                    InjectMethods.Add(methodInfo);
                }

                if (methodInfo.GetCustomAttributes(typeof(PostInjectAttribute), true).Length > 0)
                {
                    if (!methodInfo.IsPublic)
                        throw new Exception($"PostInject method must be public. {targetType.Name}.{methodInfo.Name}()");
                    if (methodInfo.GetParameters().Length > 0)
                        throw new Exception("PostInject method should not have any parameters");
                    PostInjectMethods.Add(methodInfo);
                }

                if (methodInfo.GetCustomAttributes(typeof(OnInjectedAttribute), true).Length > 0)
                {
                    if (!methodInfo.IsPublic)
                        throw new Exception($"OnInjected method must be public. {targetType.Name}.{methodInfo.Name}()");
                    if (methodInfo.GetParameters().Length > 0)
                        throw new Exception("OnInjected method should not have any parameters");
                    OnInjectedMethods.Add(methodInfo);
                }
            }
        }
    }
}