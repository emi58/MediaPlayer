﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Popups;

namespace MediaPlayer
{
    class YoutubeDecoder
    {
        private static Dictionary<string, string> chars = new Dictionary<string, string>(){
            {"24","$"},{"26","&"},{"2B","+"},{"2C",","},{"2F","/"},
            {"3A",":"},{"3B",";"},{"3D","="},{"3F","?"},{"40","@"},
            {"20"," "},{"3C","<"},{"3E",">"},{"23","#"},{"7B","{"},
            {"7D","}"},{"7C","|"},{"5C","\\"},{"5E","^"},{"7E","~"},
            {"5B","["},{"5D","]"},{"60","`"},{"25","%"}};

        public string VideoID
        {
            get;
            set;
        }    

        private string mDirectVideoURL;
        public string DirectVideoURL
        {
            get { return mDirectVideoURL; }
        }

        public YoutubeDecoder()
        {           
        }
        public YoutubeDecoder(string VideoID)
        {
            this.VideoID = VideoID;
        }

        public void swap<T>(ref T a, ref T b)
        {
            T aux = a;
            a = b;
            b = aux;
        }

        

        public void dechipherSignature(ref string signature)
        {
            //http://www.jwz.org/hacks/youtubedown
            char[] char_sig = signature.ToCharArray();
            swap(ref char_sig[0], ref char_sig[50]);
            swap(ref char_sig[0], ref char_sig[17]);
            Array.Reverse(char_sig);
            swap(ref char_sig[0], ref char_sig[7]);
            swap(ref char_sig[0], ref char_sig[65]);
            signature = new String(char_sig);
        }

        private void decodeURL(ref string content)
        {
            foreach (string key in chars.Keys)
                content = content.Replace("%" + key, chars[key]);

            content = content.Replace("%2C", ",");
        }

        private async Task<string> fetchURL()
        {
            WebRequest request = WebRequest.Create("http://www.youtube.com/watch?v=" + VideoID);
            WebResponse response;
            try
            {
                response = await request.GetResponseAsync();
            }
            catch
            {
                throw new Exception(ExceptionMessages.CONNECTION_FAILED);
            }
            string result = new StreamReader(response.GetResponseStream()).ReadToEnd();

            string startSearchString = "adaptive_fmts";
            int startIndex = result.IndexOf(startSearchString);
        
            if (startIndex == -1)
            {
                throw new Exception(ExceptionMessages.YOUTUBE_VIDEO_URL_NOT_FOUND);
            }

            startIndex += "adaptive_fmts".Length;
            result = result.Substring(startIndex, result.Length - startIndex);
            decodeURL(ref result);

            int audioIndex = result.IndexOf("type=audio");
            if (audioIndex == -1)
            {
                throw new Exception(ExceptionMessages.YOUTUBE_VIDEO_URL_NOT_FOUND);
            }
            audioIndex += "type=audio".Length;           
            
            result = result.Substring(audioIndex, result.Length - audioIndex);

            int urlIndex = result.IndexOf("url=");

            if (urlIndex == -1)
            {
                throw new Exception(ExceptionMessages.YOUTUBE_VIDEO_URL_NOT_FOUND);
            }
            urlIndex += "url=".Length;

            result = result.Substring(urlIndex, result.Length - urlIndex);

            int finalIndex = result.IndexOf("\\u0026");
            result = result.Substring(0, finalIndex);

            string[] remaining = { ",init=", ",type=", ",index=" , ",bitrate=" , ",itag="};

            foreach (string str in remaining)
            {
                int position = result.IndexOf(str);
                if (position != -1)
                {
                    result = result.Substring(0, position);
                }
            }

            return result;
        }

        public async Task getVideoCacheURL()
        {
            bool is_good = false;            
            try
            {
               mDirectVideoURL = await fetchURL();
               is_good = true;
            }
            catch (Exception er)
            {
                is_good = false;
            }
                
        }
    }
}
