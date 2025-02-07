﻿using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace WGestures.Common.OsSpecific.Windows
{
    /// <summary>
    ///     对GDI hBitmap的封装。主要提供DrawWith方法以便画图。通过HBitmap属性方位原始hBitmap
    /// </summary>
    public class DiBitmap : IDisposable
    {
        private readonly IntPtr _memDc;
        private uint _errCode = 0;
        private Graphics _graphics;
        private IntPtr _oldObject;

        public DiBitmap(Size size, PixelFormat pixelFormat = PixelFormat.Format32bppArgb)
        {
            this.Size = size;
            this.PixelFormat = pixelFormat;

            var binfo = new Native.BITMAPINFO();
            binfo.bmiHeader.biSize = Marshal.SizeOf(typeof(Native.BITMAPINFOHEADER));
            binfo.bmiHeader.biWidth = this.Size.Width;
            binfo.bmiHeader.biHeight = this.Size.Height;
            binfo.bmiHeader.biBitCount = (short) Image.GetPixelFormatSize(this.PixelFormat);
            binfo.bmiHeader.biPlanes = 1;
            binfo.bmiHeader.biCompression = 0;
            //var screenDc = Native.GetDC(IntPtr.Zero);
            //var memDc = Native.CreateCompatibleDC(IntPtr.Zero);
            //if (memDc == IntPtr.Zero) throw new ApplicationException("初始化失败：创建MemDc失败(" + Native.GetLastError() + ")");

            _memDc = Native.CreateCompatibleDC(IntPtr.Zero);
            if (_memDc == IntPtr.Zero)
            {
                throw new ApplicationException(
                    "CreateCompatibleDC(IntPtr.Zero)失败(" + Native.GetLastError() + ")");
            }

            var ptrBits = IntPtr.Zero;
            this.HBitmap = Native.CreateDIBSection(
                _memDc,
                ref binfo,
                0,
                out ptrBits,
                IntPtr.Zero,
                0);

            //HBitmap = Native.CreateCompatibleBitmap(MemDc, size.Width, size.Height);
            this.PointerToBits = ptrBits;
            if (this.HBitmap == IntPtr.Zero)
            {
                throw new ApplicationException(
                    "初始化失败：CreateDIBSection(...)失败(" + Native.GetLastError() + ")");
            }

            _oldObject = Native.SelectObject(_memDc, this.HBitmap);
            _graphics = Graphics.FromHdc(_memDc);
            _graphics.CompositingQuality = CompositingQuality.HighSpeed;
            _graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Native.SelectObject(_memDc, _oldObject);
        }
        //public delegate void DrawingHandler(Graphics g);

        public IntPtr HBitmap { get; private set; }
        public IntPtr PointerToBits { get; }

        public Size Size { get; }
        public PixelFormat PixelFormat { get; }

        public void Dispose()
        {
            if (_graphics != null)
            {
                _graphics.Dispose();
                _graphics = null;
            }

            Native.SelectObject(_memDc, _oldObject);
            Native.DeleteDC(_memDc);

            if (this.HBitmap != IntPtr.Zero)
            {
                Native.DeleteObject(this.HBitmap);
                this.HBitmap = IntPtr.Zero;
            }
        }

        /*public void DrawWith(DrawingHandler drawingHandler)
        {

            try
            {                    
                
                _oldObject = Native.SelectObject(_memDc, HBitmap);

                if (_graphics == null)
                {
                    _graphics = Graphics.FromHdc(_memDc);

                    _graphics.CompositingQuality = CompositingQuality.HighSpeed;
                    _graphics.SmoothingMode = SmoothingMode.AntiAlias;
                }
                
                //using (_graphics)
                //{
                drawingHandler(_graphics);
                //}
            }
            finally
            {
                Native.SelectObject(_memDc, _oldObject);
                //Native.ReleaseDC(IntPtr.Zero, screenDc);

            }
        }*/

        public Graphics BeginDraw()
        {
            _oldObject = Native.SelectObject(_memDc, this.HBitmap);

            /*if (_graphics == null)
            {
                _graphics = Graphics.FromHdc(_memDc);

                _graphics.CompositingQuality = CompositingQuality.HighSpeed;
                _graphics.SmoothingMode = SmoothingMode.AntiAlias;
            }*/

            return _graphics;
        }

        public void EndDraw()
        {
            Native.SelectObject(_memDc, _oldObject);
        }

        public void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Dispose();
                GC.SuppressFinalize(this);
            }
            else
            {
                if (this.HBitmap != IntPtr.Zero)
                {
                    Native.DeleteObject(this.HBitmap);
                    this.HBitmap = IntPtr.Zero;
                }

                Native.SelectObject(_memDc, _oldObject);
                Native.DeleteDC(_memDc);
            }
        }

        ~DiBitmap()
        {
            this.Dispose(false);
        }
    }
}
