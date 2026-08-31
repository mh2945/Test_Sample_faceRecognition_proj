using Alchera.FaceSDK;
using ICSharpCode.SharpZipLib.Zip;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FaceSDKSample
{
    public class VideoRec : IDisposable
    {
        public const string CODEC_MP4V = "mp4v";
        public const string CODEC_H264 = "h264"; // Failed to load OpenH264 library: openh264-1.8.0-win64.dll

        public const int FRAMES_PER_SEC = 30;
        public const int REDUNDANT_FRAME_CNT = 1;
                
        public const long REC_FILE_SIZ_1G_BYTES = 1024 * 1024 * 1024;  // 1GB
        public const long CHK_REC_FILE_SIZ_DUR_MS = 2000;

        //
        // err, status
        //

        public enum Error
        {
            NoError,            
            NotReadyWriter,

            InvalidCall = 1000,
            CvOperErr = 1100,
        };

        private Error last_err_ = Error.NotReadyWriter;
        private string last_err_desc_ = "";

        public bool SetLastErr(Error err, string err_desc = "")
        {
            last_err_ = err;
            last_err_desc_ = err_desc;

            return err == Error.NoError ? true : false;
        }

        public Error last_err { get { return last_err_; } }
        public string last_err_desc { get { return last_err_desc_; } }


        //
        // context
        //

        private string rec_file_dir_ = "";
        private string rec_file_name_ = "";
        
        private int rec_file_no_ = 0;

        private long rec_file_siz_bytes_ = 0;
        private readonly long rec_rolling_siz_bytes_ = REC_FILE_SIZ_1G_BYTES;

        private Size rec_vid_siz_;
        private FourCC rec_codec_;
        private bool rec_is_color_;
        private int rec_fps_;

        private long rec_last_add_frame_ms_ = 0;
        private long rec_last_read_file_siz_ = -1;

        private VideoWriter recorder_;

        //
        // util
        //

        public static string MakeFileName(string file_path, int file_no)
        {
            return $"{file_path}-{file_no:D3}.mp4";
        }

        //
        // VideoRec
        //
        
        private VideoRec()
        {
        }

        public VideoRec(string file_path, int w = 640, int h = 480, int fps = 30,
            long rec_rolling_siz_mb = 1024, string codec = VideoRec.CODEC_MP4V)
        {
            try
            {
                rec_file_name_ = Path.GetFileName(file_path);

                if(rec_file_name_ == "")
                    throw new ArgumentException("invalid call, file_name is empty, file_path="+file_path);
                
                rec_file_dir_ = Path.GetDirectoryName(file_path);

                if (rec_file_dir_ == "")
                    throw new ArgumentException("invalid call, file_dir is invalid, file_path=" + file_path);
            }
            catch(Exception e)
            {
                throw e;
            }

            rec_vid_siz_ = new Size(w, h);
            rec_fps_ = fps;
            rec_is_color_ = true;
            rec_codec_ = new FourCC(VideoWriter.FourCC(codec));
            rec_rolling_siz_bytes_ = rec_rolling_siz_mb * 1024 * 1024;
        }


        ~VideoRec()
        {
            Dispose();
        }

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this); // prevent calling twice finalizer
        }

        public void Close()
        {
            if (recorder_ == null)
                return;

            try
            {
                recorder_.Release();
                recorder_.Dispose();
            }
            catch
            {
                /* swallow */
            }
            finally
            {
                recorder_ = null;
            }
        }

        public bool IsOpened()
        {
            if (recorder_ == null || !recorder_.IsOpened())
                return false;

            return true;
        }

        //
        // entry
        //

        public long GetCurFileSizBytes()
        {
            return rec_file_siz_bytes_;
        }

        public long GetCurFileSizAsMB()
        {
            return GetCurFileSiz() / (1024 * 1024);
        }

        public string GetCurFilePath()
        {
            return rec_file_dir_ + "\\" + GetCurFileName();
        }

        public string GetCurFileName()
        {
            return VideoRec.MakeFileName(rec_file_name_, rec_file_no_);
        }
        
        public long GetLastReadFileSiz(bool use_cache=true)
        {
            if (rec_last_read_file_siz_ == -1 || !use_cache)
                return GetCurFileSiz();

            return rec_last_read_file_siz_;
        }

        public long GetCurFileSiz()
        {
            try
            {
                string file_path = GetCurFilePath();
                FileInfo fi = new FileInfo(file_path);
                rec_last_read_file_siz_ = fi.Exists ? fi.Length : 0;
            }
            catch
            {
                rec_last_read_file_siz_ = 0;
            }

            return rec_last_read_file_siz_;

        }

        public bool Roll()
        {
            rec_file_no_++;

            string new_rec_file_path = GetCurFilePath();

            VideoWriter new_recorder = null;

            try
            {
                new_recorder = VideoRec.CreateWriter(new_rec_file_path, rec_codec_, 
                    rec_fps_, rec_vid_siz_, rec_is_color_);
            }
            catch (Exception e)
            {
                SetLastErr(Error.CvOperErr, e.Message + ",codec=" + rec_codec_.ToString()
                    + ", fps=" + rec_fps_.ToString() + ", siz=" + rec_vid_siz_.ToString());

                rec_file_no_--;

                return false;
            }

            //
            // flush previous
            //

            Close();
                        
            recorder_ = new_recorder;
            rec_file_siz_bytes_ = 0;
            rec_last_add_frame_ms_ = 0;
            rec_last_read_file_siz_ = -1;

            SetLastErr(Error.NoError);

            return true;
        }


        private static VideoWriter CreateWriter(string file_path, FourCC codec, int fps, Size siz, bool is_color)
        {
            VideoWriter writer = null;

            try
            {
                writer = new VideoWriter(file_path, codec, fps, siz, is_color);

                if (!writer.IsOpened())
                {
                    throw new Exception("cv video writer is created. but not opened");
                }
            }
            catch (Exception e)
            {
                if (writer != null)
                {
                    writer.Release();
                    writer.Dispose();
                    writer = null;
                }

                throw new Exception(e.Message);
            }

            return writer;
        }
                
        public bool AddFrame(Mat frame) {

            if (frame == null)
                return false;

            if (recorder_ == null || !recorder_.IsOpened())
            {
                SetLastErr(Error.NotReadyWriter, "error on add frame, is_opened=" + recorder_?.IsOpened());
                return false;
            }
            
            long frame_bytes_ = 0;

            //
            // rolling
            //

            long cur_ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            if (cur_ms - rec_last_add_frame_ms_ >= CHK_REC_FILE_SIZ_DUR_MS)
            {
                if (GetCurFileSiz() > rec_rolling_siz_bytes_)
                {
                    if (!Roll())
                    {
                        SetLastErr(Error.CvOperErr, "error on rolling video file");
                        return false;
                    }

                    rec_file_siz_bytes_ = 0;
                }

                rec_last_add_frame_ms_ = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }

            //
            // add frame
            //

            try
            {                
                recorder_.Write(frame);
                frame_bytes_ = frame.ElemSize() * frame.Total();                
            }
            catch(Exception e)
            {
                SetLastErr(Error.CvOperErr, "error on writing frame, err=" + e.Message);
                return false;
            }
                        
            rec_file_siz_bytes_ += frame_bytes_;
            
            return true;
        }

        public bool AddBGRFrame(byte[] bgr_pixels, int w, int h, int redundant_frame_cnt = 1)
        {
            if (bgr_pixels == null)
            {
                SetLastErr(Error.InvalidCall, "bgr_pixels[] is null");
                return false;
            }

            if (w != rec_vid_siz_.Width || h != rec_vid_siz_.Height)
            {
                SetLastErr(Error.InvalidCall, $"w,h is not match, rec w={rec_vid_siz_.Width}, h={rec_vid_siz_.Height}");
                return false;
            }

            if (recorder_ == null)
            {
                SetLastErr(Error.NotReadyWriter, "error on add frame, recorder_ is null");
                return false;
            }

            if (!recorder_.IsOpened())
            {
                SetLastErr(Error.NotReadyWriter, "error on add frame, is_opened=" + recorder_.IsOpened());
                return false;
            }

            if (redundant_frame_cnt <= 0)
                redundant_frame_cnt = 1;

            // Mat: IDisposable
            using (Mat frame = new Mat(h, w, MatType.CV_8UC3))
            {
                int len = h * w * frame.Channels();

                if (bgr_pixels.Length != len)
                {
                    SetLastErr(Error.InvalidCall, $"bgr_pixels.Length is invalid");
                    return false;
                }

                Marshal.Copy(bgr_pixels, 0, frame.Data, bgr_pixels.Length);

                for (int i = 0; i < redundant_frame_cnt; i++)
                {
                    //  AVI file size cannot be larger than 2 GB
                    AddFrame(frame);
                }                
            }

            return true;
        }

    }

    //
    // save enc image mtl (for tracking)
    //

    //public static class EncImgSave
    //{
    //    public static bool SaveAsZip(string desc, string base_path, byte[] enc_img1, byte[] enc_img2, string prm1, string prm2)
    //    {
    //        var ms = new MemoryStream();

    //        try
    //        {
    //            //CSharpCode.SharpZipLib
    //            using (var zip = new ZipOutputStream(ms))
    //            {
    //                zip.IsStreamOwner = false;
    //                zip.SetLevel(ResolveZipCompressLvl(level));

    //                if (enc_passwd != "")
    //                    zip.Password = enc_passwd;

    //                for (int i = 0; i < srcs.Length; i++)
    //                {
    //                    string filename = string.Format(file_fmt, zip_entry_filename_start_idx + i);

    //                    var entry = new ZipEntry(filename)
    //                    {
    //                        DateTime = DateTime.UtcNow
    //                    };

    //                    if (enc_key_siz_bit == 128 || enc_key_siz_bit == 256)
    //                        entry.AESKeySize = enc_key_siz_bit;

    //                    zip.PutNextEntry(entry);

    //                    var data = srcs[i] ?? Array.Empty<byte>();
    //                    zip.Write(data, 0, data.Length);

    //                    zip.CloseEntry();
    //                }

    //                zip.Finish();
    //            }

    //        }
    //        catch (Exception e)
    //        {
    //            //Console.WriteLine(e.Message);
    //            return null;
    //        }

    //        return ms.ToArray();

    //        return true;
    //    }
    //}

}
