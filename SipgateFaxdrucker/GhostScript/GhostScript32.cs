﻿using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace SipgateFaxdrucker.GhostScript
{
    internal class GhostScript32
    {
        /*
        This code was adapted from Matthew Ephraim's Ghostscript.Net project
        external dll definitions moved into NativeMethods to
        satisfy FxCop requirements
        https://github.com/mephraim/ghostscriptsharp
        */

        /// <summary>
        /// Calls the Ghostscript API with a collection of arguments to be passed to it
        /// </summary>
        public static void CallApi(string[] args)
        {
            // Get a pointer to an instance of the Ghostscript API and run the API with the current arguments
            IntPtr gsInstancePtr;
            lock (_resourceLock)
            {
                NativeMethods32.CreateAPIInstance(out gsInstancePtr, IntPtr.Zero);
                try
                {
                    int result = NativeMethods32.InitAPI(gsInstancePtr, args.Length, args);

                    if (result < 0)
                    {
                        throw new ExternalException("Ghostscript conversion error", result);
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("ghostscript error" + e);
                }
                finally
                {
                    Cleanup(gsInstancePtr);
                }
            }
        }

        /// <summary>
        /// Frees up the memory used for the API arguments and clears the Ghostscript API instance
        /// </summary>
        private static void Cleanup(IntPtr gsInstancePtr)
        {
            NativeMethods32.ExitAPI(gsInstancePtr);
            NativeMethods32.DeleteAPIInstance(gsInstancePtr);
        }


        /// <summary>
        /// GS can only support a single instance, so we need to bottleneck any multi-threaded systems.
        /// </summary>
        private static object _resourceLock = new object();
    }
}
