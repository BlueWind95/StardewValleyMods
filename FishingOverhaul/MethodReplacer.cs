using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TehPers.Stardew.FishingOverhaul {
    public class MethodReplacer {
        /// <summary>Replaces the body of target with source</summary>
        /// <param name="target">The method to be replaced</param>
        /// <param name="source">The method to replace it with</param>
        /// <returns>A key that can be used to access the original method</returns>
        public static unsafe int Replace(MethodBodyPtr target, MethodBodyPtr source) {
            return Replace(target, *source.ptr);
        }

        public static unsafe int Replace(MethodBodyPtr target, int source) {
            int original = *target.ptr;
            *target.ptr = source;
            return original;
        }

        // TODO: x64 uses long* instead of int*
        public class MethodBodyPtr {
            public unsafe int* ptr;
            public CompileMode mode;

            public unsafe MethodBodyPtr(MethodInfo method, CompileMode mode = CompileMode.RELEASE) {
                RuntimeHelpers.PrepareMethod(method.MethodHandle);
                ptr = (int*) method.MethodHandle.Value.ToPointer() + ((IntPtr.Size == 4) ? 2 : 1);
                this.mode = mode;
            }
        }



        public enum CompileMode {
            /// <summary>For most mods</summary>
            DEBUG,

            /// <summary>For SDV</summary>
            RELEASE
        }
    }
}
