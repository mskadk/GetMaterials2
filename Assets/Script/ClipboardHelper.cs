using System;
using System.Runtime.InteropServices;
using UnityEngine;

public static class ClipboardHelper
{
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    [DllImport("user32.dll")]
    private static extern bool OpenClipboard(IntPtr hWndNewOwner);
    [DllImport("user32.dll")]
    private static extern bool CloseClipboard();
    [DllImport("user32.dll")]
    private static extern bool EmptyClipboard();
    [DllImport("user32.dll")]
    private static extern IntPtr SetClipboardData(uint uFormat, IntPtr hMem);
    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalLock(IntPtr hMem);
    [DllImport("kernel32.dll")]
    private static extern bool GlobalUnlock(IntPtr hMem);
    [DllImport("kernel32.dll")]
    private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr dwBytes);
    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    private const uint CF_DIB = 8;
    private const uint GHND = 0x0042;

    public static void CopyTextureToClipboard(Texture2D texture)
    {
        // 将 Texture2D 编码为 PNG 或 JPG 并不是 Windows 剪贴板的标准位图格式 (DIB)
        // 这里我们需要构建一个简单的 DIB 结构。
        // 为了简化代码并保证兼容性，这里使用最简单的方法：
        // 1. 将 Texture2D 读作像素数组
        // 2. 构建 BITMAPINFOHEADER
        // 3. 写入内存
        // 这是一个简化版的实现，适用于大多数情况。

        int width = texture.width;
        int height = texture.height;
        byte[] texData = texture.GetRawTextureData(); // 只有当纹理格式为 RGBA32/RGB24 等时有效

        // 确保纹理可读且格式正确，建议在传入前转换为 RGBA32

        // DIB Header Size (40) + Pixel Data Size
        int headerSize = 40;
        int pixelDataSize = width * height * 4; // RGBA32
        int totalSize = headerSize + pixelDataSize;

        IntPtr hGlobal = GlobalAlloc(GHND, (UIntPtr)totalSize);
        IntPtr pGlobal = GlobalLock(hGlobal);

        try
        {
            // 1. 写入 BITMAPINFOHEADER
            int[] header = new int[10];
            header[0] = 40;         // biSize
            header[1] = width;      // biWidth
            header[2] = height;     // biHeight
            header[3] = 1 | (32 << 16); // biPlanes (1) | biBitCount (32)
            header[4] = 0;          // biCompression (BI_RGB)
            header[5] = pixelDataSize; // biSizeImage
            header[6] = 0;          // biXPelsPerMeter
            header[7] = 0;          // biYPelsPerMeter
            header[8] = 0;          // biClrUsed
            header[9] = 0;          // biClrImportant

            Marshal.Copy(header, 0, pGlobal, header.Length);

            // 2. 写入像素数据
            // Unity 的纹理是从下到上的，DIB 也是（如果高度为正），但颜色顺序可能不同。
            // Unity RGBA32 -> DIB BGRA32
            byte[] bgraData = new byte[pixelDataSize];

            // 获取像素颜色（比 GetRawTextureData 慢但更安全，能处理格式转换）
            Color32[] colors = texture.GetPixels32();

            for (int i = 0; i < colors.Length; i++)
            {
                // DIB 也是从下到上，所以不需要翻转 Y，只需要转换颜色通道
                bgraData[i * 4 + 0] = colors[i].b;
                bgraData[i * 4 + 1] = colors[i].g;
                bgraData[i * 4 + 2] = colors[i].r;
                bgraData[i * 4 + 3] = colors[i].a;
            }

            Marshal.Copy(bgraData, 0, new IntPtr(pGlobal.ToInt64() + 40), bgraData.Length);
        }
        finally
        {
            GlobalUnlock(hGlobal);
        }

        if (OpenClipboard(IntPtr.Zero))
        {
            EmptyClipboard();
            SetClipboardData(CF_DIB, hGlobal);
            CloseClipboard();
        }
    }
#else
    public static void CopyTextureToClipboard(Texture2D texture)
    {
        Debug.LogWarning("剪贴板复制功能仅支持 Windows 平台");
    }
#endif
}
