using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using TinyBCSharp;


namespace DungeonDefendersOfflinePreprocessor
{
	public static class DxtConverter
	{
		static public byte[] ConvertDXTToBmp(byte[] dxtData, int width, int height, TinyBCSharp.BlockFormat format)
		{
			// 1. Initialize the decoder for the specific format
			var decoder = TinyBCSharp.BlockDecoder.Create(format);

			// 2. Prepare the output buffer (4 bytes per pixel for RGBA)
			byte[] rgbaData = new byte[width * height * 4];

			decoder.Decode(dxtData, width, height, rgbaData, width, height);

			return rgbaData;			
		}

		static public void SaveToPng( byte[] rgbaData, int srcWidth, int srcHeight, int dstWidth, int dstHeight, string path)
		{			
			using (Bitmap src = new Bitmap(srcWidth, srcHeight, PixelFormat.Format32bppArgb))
			{
				BitmapData srcData = src.LockBits(
					new Rectangle(0, 0, srcWidth, srcHeight),
					ImageLockMode.WriteOnly,
					PixelFormat.Format32bppArgb);

				Marshal.Copy(rgbaData, 0, srcData.Scan0, rgbaData.Length);
				src.UnlockBits(srcData);

				using (Bitmap dst = new Bitmap(dstWidth, dstHeight, PixelFormat.Format32bppArgb))
				using (Graphics g = Graphics.FromImage(dst))
				{
					g.CompositingMode = CompositingMode.SourceCopy;
					g.CompositingQuality = CompositingQuality.HighQuality;
					g.InterpolationMode = InterpolationMode.HighQualityBicubic;
					g.PixelOffsetMode = PixelOffsetMode.HighQuality;
					g.SmoothingMode = SmoothingMode.None;

					g.DrawImage(
						src,
						new Rectangle(0, 0, dstWidth, dstHeight),
						new Rectangle(0, 0, srcWidth, srcHeight),
						GraphicsUnit.Pixel);

					dst.Save(path, ImageFormat.Png);
				}
			}
		}
	}
}
