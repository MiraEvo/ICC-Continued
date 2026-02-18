using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Ink_Canvas.Helpers
{

    public static class EdgeGestureUtil
    {

        private static Guid DISABLE_TOUCH_SCREEN = new("32CE38B2-2C9A-41B1-9BC5-B3784394AA44");
        private static Guid IID_PROPERTY_STORE = new("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99");

        private static short VT_BOOL = 11;
        #region "Structures"

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct PropertyKey
        {
            public PropertyKey(Guid guid, uint pid)
            {
                fmtid = guid;
                this.pid = pid;
            }

            [MarshalAs(UnmanagedType.Struct)]
            public Guid fmtid;
            public uint pid;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct PropVariant
        {
            [FieldOffset(0)]
            public short vt;
            [FieldOffset(2)]
            private readonly short wReserved1;
            [FieldOffset(4)]
            private readonly short wReserved2;
            [FieldOffset(6)]
            private readonly short wReserved3;
            [FieldOffset(8)]
            private readonly sbyte cVal;
            [FieldOffset(8)]
            private readonly byte bVal;
            [FieldOffset(8)]
            private readonly short iVal;
            [FieldOffset(8)]
            public ushort uiVal;
            [FieldOffset(8)]
            private readonly int lVal;
            [FieldOffset(8)]
            private readonly uint ulVal;
            [FieldOffset(8)]
            private readonly int intVal;
            [FieldOffset(8)]
            private readonly uint uintVal;
            [FieldOffset(8)]
            private readonly long hVal;
            [FieldOffset(8)]
            private readonly long uhVal;
            [FieldOffset(8)]
            private readonly float fltVal;
            [FieldOffset(8)]
            private readonly double dblVal;
            [FieldOffset(8)]
            public bool boolVal;
            [FieldOffset(8)]
            private readonly int scode;
            [FieldOffset(8)]
            private readonly DateTime date;
            [FieldOffset(8)]
            private readonly System.Runtime.InteropServices.ComTypes.FILETIME filetime;

            [FieldOffset(8)]
            private readonly Blob blobVal;
            [FieldOffset(8)]
            private readonly IntPtr pwszVal;


            /// <summary>
            /// Helper method to gets blob data
            /// </summary>
            private byte[] GetBlob()
            {
                byte[] Result = new byte[blobVal.Length];
                Marshal.Copy(blobVal.Data, Result, 0, Result.Length);
                return Result;
            }

            /// <summary>
            /// Property value
            /// </summary>
            public object Value
            {
                get
                {
                    VarEnum ve = (VarEnum)vt;
                    return ve switch
                    {
                        VarEnum.VT_I1 => bVal,
                        VarEnum.VT_I2 => iVal,
                        VarEnum.VT_I4 => lVal,
                        VarEnum.VT_I8 => hVal,
                        VarEnum.VT_INT => iVal,
                        VarEnum.VT_UI4 => ulVal,
                        VarEnum.VT_LPWSTR => Marshal.PtrToStringUni(pwszVal) ?? string.Empty,
                        VarEnum.VT_BLOB => GetBlob(),
                        _ => throw new NotImplementedException("PropVariant " + ve.ToString())
                    };
                }
            }
        }

        internal struct Blob
        {
#pragma warning disable CS0649
            public int Length;
            public IntPtr Data;
#pragma warning restore CS0649
        }

        #endregion

        #region "Interfaces"

        [ComImport(), Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IPropertyStore
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetCount([Out(), In()] ref uint cProps);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetAt([In()] uint iProp, ref PropertyKey pkey);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void GetValue([In()] ref PropertyKey key, ref PropVariant pv);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void SetValue([In()] ref PropertyKey key, [In()] ref PropVariant pv);
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void Commit();
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void Release();
        }

        #endregion

        #region "Methods"

        [DllImport("shell32.dll", SetLastError = true)]
        private static extern int SHGetPropertyStoreForWindow(IntPtr handle, ref Guid riid, out IPropertyStore propertyStore);

        [RequiresUnmanagedCode("Uses shell32 COM interop to update edge gesture settings.")]
        public static void DisableEdgeGestures(IntPtr hwnd, bool enable)
        {
            IPropertyStore propertyStore = null;
            int hr = SHGetPropertyStoreForWindow(hwnd, ref IID_PROPERTY_STORE, out propertyStore);
            if (hr == 0)
            {
                PropertyKey propKey = new(DISABLE_TOUCH_SCREEN, 2);
                PropVariant var = new()
                {
                    vt = VT_BOOL,
                    boolVal = enable
                };
                propertyStore.SetValue(ref propKey, ref var);
                Marshal.FinalReleaseComObject(propertyStore);
            }
        }

        #endregion

    }
}
