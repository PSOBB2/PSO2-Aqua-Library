﻿using AquaModelLibrary.Native.Fbx.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace AquaModelLibrary.Native
{
    public static class Native
    {
        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteFile(string filePath);

        public static IFbxExporter FbxExporter { get; set; }

        static Native()
        {
            string dllFilePath = Path.Combine(Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName),
                $"AquaModelLibrary.Native.X{(IntPtr.Size == 8 ? "64" : "86")}.dll");

            // Unblock DLL when extracted through Windows (thanks Sewer)
            DeleteFile(dllFilePath + ":Zone.Identifier");

            if (!File.Exists(dllFilePath))
                throw new FileNotFoundException("Native Aqua Model Library could not be found", dllFilePath);

            var assembly = Assembly.LoadFile(dllFilePath);

            assembly.GetType("AquaModelLibrary.NativeContext")
                .GetMethod("Initialize", BindingFlags.Public | BindingFlags.Static).Invoke(null, null);

            FbxExporter = (IFbxExporter)Activator.CreateInstance(
                assembly.GetType("AquaModelLibrary.Objects.Processing.Fbx.FbxExporterCore"));
        }
    }
}
