using System;
using System.Collections.Generic;
using System.Linq;
using HtmlAgilityPack;
using System.IO;
using System.Net;
using TagLib;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Threading.Tasks;

namespace ID3TagEditor
{
    class Program
    {
        static void Main(string[] args)
        {
            ID3Tag tag = new ID3Tag();
            tag.ID3Control();
        }
    }

    public class ID3Tag
    {


        public static string URL0 = string.Empty;
        public static string URL1 = string.Empty;


        public void ChangeName()
        {
            string dir = @"C:\tmp\ID3Project\RenameSongs\";
            string newdir = @"C:\tmp\ID3Project\Songs\";
            string[] SongFiles = null;
            string extension = string.Empty;

            try
            {
                SongFiles = Directory.GetFiles(dir, "*.flac");
                if (SongFiles.Length == 0)
                {
                    SongFiles = Directory.GetFiles(dir, "*.mp3");
                    extension = "mp3";
                }
                foreach (string file in SongFiles)
                {
                    System.IO.File.Move(dir + Path.GetFileName(file), newdir + GetFilenamewithAlbum(file));
                }
                        
            }
            catch(Exception ex)
            {

            }
        }
       
        public void ID3Control()
        {

            string songsdir = @"C:\tmp\ID3Project\Songs\";
            string imagedir = @"C:\tmp\ID3Project\Images\";
            string imagedir2 = @"C:\tmp\ID3Project\Images2\";
            string filename = string.Empty;
            string ImageURL = string.Empty;
            string extension = "flac";

            string searchterm = "Official";
            string[] SongFiles = null;
            int count = 0;
            int error = 0;
            int index = 0;

            try
            {
                SongFiles = Directory.GetFiles(songsdir, "*.flac");
                if (SongFiles.Length == 0)
                {
                    SongFiles = Directory.GetFiles(songsdir, "*.mp3");
                    extension = "mp3";
                }
                foreach (string file in SongFiles)
                //Parallel.ForEach(SongFiles, file =>
                {

                    URL0 = string.Empty;
                    URL1 = string.Empty;

                    //string file = hahahaha;

                    filename = Path.GetFileNameWithoutExtension(file);
                    Console.WriteLine("File: " + filename);

                    ImageURL = GetImageFromYoutube(filename, searchterm, index);

                    string imagename = imagedir + filename + ".jpg";
                    string imagename2 = imagedir2 + filename + ".jpg";
                    if (ImageURL != "fail")
                    {
                        if (SaveImageinFolder(ImageURL, imagename))
                        {
                            Console.WriteLine("Image Saved");
                            if (Reszie(imagename, imagename2))
                            {
                                Console.WriteLine("Image Resized");
                                if (UpdateID3Tag(songsdir + filename + "." + extension, imagename2) == "success")
                                {
                                    Console.WriteLine("ID3 Tag Updated");
                                    count++;
                                }
                                else
                                {
                                    error++;
                                    MoveFile(file);
                                }
                            }
                            else
                            {
                                error++;
                                MoveFile(file);
                            }
                        }
                        else
                        {
                            error++;
                            MoveFile(file);
                        }
                    }
                    else
                    {
                        error++;
                        MoveFile(file);
                    }

                }
                //);
                Console.WriteLine("Total Files: " + SongFiles.Length);
                Console.WriteLine("Files updated: " + count);
                Console.WriteLine("Error occured: " + error);
                Console.Read();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }




        }

        public string GetImageFromYoutube(string filename, string query, int index)
        {

            string queryfilename = filename.Replace(' ', '+');
            queryfilename = queryfilename.Replace("&", "%26");
            queryfilename = queryfilename.Replace("(", "%28");
            queryfilename = queryfilename.Replace(")", "%29");

            string url = "https://www.youtube.com/results?search_query=" + queryfilename + "+" + query;
            string imageurl = string.Empty;
            try
            {
                using (WebClient client = new WebClient())
                {
                    string s = client.DownloadString(url);

                    s = WebUtility.HtmlDecode(s);
                    HtmlDocument result = new HtmlDocument();
                    result.LoadHtml(s);

                    List<HtmlNode> toftitle = result.DocumentNode.Descendants().Where
                        (x => (x.Name == "div" && x.Attributes["class"] != null && x.Attributes["class"].Value.Contains("yt-thumb video-thumb"))).ToList();


                    for (int i = 0; i < toftitle.Count; i++) 
                    {
                        if (CheckDownloadableImage(toftitle[i].LastChild.FirstChild.NextSibling.Attributes["src"].DeEntitizeValue.ToString()))
                        {
                            imageurl = toftitle[i].LastChild.FirstChild.NextSibling.Attributes["src"].DeEntitizeValue.ToString();
                            break;
                        }       
                    }

                    if (!imageurl.Contains("https://"))
                    {
                        imageurl = @"https:" + imageurl;
                    }
                    return imageurl;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "fail";
            }
        }

        public bool CheckDownloadableImage(string ImageURL)
        {
            bool result = false;

            try
            {
                WebClient webClient = new WebClient();
                webClient.DownloadFile(ImageURL, @"C:\tmp\checker.jpg");
                result = true;
                System.IO.File.Delete(@"C:\tmp\checker.jpg");
                return result;
            }

            catch (Exception ex)
            { 
                return false;
            }
        }

        public bool SaveImageinFolder(string imageURL, string imageName)
        {
            bool result = false;

            try
            {
                WebClient webClient = new WebClient();
                webClient.DownloadFile(imageURL, imageName);
                result = true;
                return result;
            }

            catch (Exception ex)
            {
                Console.WriteLine("Exeption downloading file : "+ex.Message );
            }
            
            return result;
        }

        public bool Reszie(string Imagelocation,string Newlocation)
        {
            try
            {
                Bitmap picture = new Bitmap(Imagelocation);
                picture = ResizeImage(picture, 600, 600);
                picture.Save(Newlocation);
                return true;
            }
            catch(Exception ex)
            {
                Console.WriteLine("Exception Occured: " + ex.Message);
                return false;
            }

        }

        public string UpdateID3Tag(string SongUrl, string PictureUrl)
        {
            try
            {

                using (var file = TagLib.File.Create(SongUrl))
                {
                    file.RemoveTags(TagTypes.AllTags);
                    file.Save();
                }

                using (var file = TagLib.File.Create(SongUrl))
                {

                    string[] differences = { "-" };
                    string[] imgurlarray = Path.GetFileNameWithoutExtension(SongUrl).Split(differences, StringSplitOptions.RemoveEmptyEntries);

                    IPicture newArt = new Picture(PictureUrl);
                    file.Tag.Pictures = new IPicture[1] { newArt };
                    string songname = Path.GetFileNameWithoutExtension(SongUrl);
                    file.Tag.Title = songname;
                    file.Tag.Album = songname;
                    file.Tag.AlbumArtists = new[] { songname };
                    //if (imgurlarray.Length > 1)
                    //{
                    //    file.Tag.Title = imgurlarray[1];
                    //}
                    //else
                    //{
                    //    file.Tag.Title = imgurlarray[0];
                    //}
                    //file.Tag.AlbumArtists = new[] { imgurlarray[0] };
                    //file.Tag.Album = imgurlarray[0];
                    file.Save();
                }
                return "success";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }

        }

        public void MoveFile(string file)
        {
            Console.WriteLine("Failed to update file ");
            System.IO.File.Delete(@"C:\tmp\ID3Project\Error\" + Path.GetFileName(file).ToString());
            System.IO.File.Move(Path.GetFullPath(file), @"C:\tmp\ID3Project\Error\" + Path.GetFileName(file).ToString());
        }

        public Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public string GetFilenamewithAlbum(string fileurl)
        {
            try
            {
                using (var file = TagLib.File.Create(fileurl))
                {
                    return Path.GetFileNameWithoutExtension(fileurl) + " - " + file.Tag.Title;
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return null;
            }
        }

        //NOT WORKING FUNCTIONS

        public string GetImageFromGenius(string filename, string query)
        {
            string queryfilename = filename.Replace(" ", "%20");
            queryfilename = queryfilename.Replace("&", "%26");

            string url = "https://genius.com/search?q=" + queryfilename + query;
            string imageurl = string.Empty;
            try
            {
                using (WebClient client = new WebClient())
                {
                    string s = client.DownloadString(url);

                    s = WebUtility.HtmlDecode(s);
                    HtmlDocument result = new HtmlDocument();
                    result.LoadHtml(s);
                    //List<HtmlNode> toftitle = result.DocumentNode.Descendants().Where
                    //    (x => (x.Name == "div" && x.Attributes["class"] != null && x.Attributes["class"].Value.Contains("yt-thumb video-thumb"))).ToList();


                    List<HtmlNode> toftitle = result.DocumentNode.Descendants().Where
                        (x => (x.Name == "div" && x.Attributes["class"] != null && x.Attributes["class"].Value.Contains("yt-thumb video-thumb"))).ToList();

                    imageurl = toftitle[0].LastChild.FirstChild.NextSibling.Attributes["src"].DeEntitizeValue.ToString();
                    return imageurl;
                }
            }
            catch (Exception)
            {
                return "fail";
            }
        }

        public string GetImageFromAmazon(string query)
        {
            string url = "https://www.amazon.com/s/ref=nb_sb_noss?url=search-alias%3Daps&field-keywords=" + query;
            string imageurl = string.Empty;
            try
            {
                using (WebClient client = new WebClient())
                {
                    string s = client.DownloadString(url);

                    s = WebUtility.HtmlDecode(s);
                    HtmlDocument result = new HtmlDocument();
                    result.LoadHtml(s);

                    List<HtmlNode> toftitle = result.DocumentNode.Descendants().Where
                        (x => (x.Name == "div" && x.Attributes["class"] != null && x.Attributes["class"].Value.Contains("a-section a-spacing-none a-inline-block s-position-relative"))).ToList();

                    imageurl = toftitle[1].FirstChild.FirstChild.Attributes["srcset"].DeEntitizeValue.ToString();

                    string[] differences = { "," };
                    string[] imgurlarray = imageurl.Split(differences, StringSplitOptions.RemoveEmptyEntries);
                    int max = imgurlarray.Length;

                    imageurl = imgurlarray[imgurlarray.Length - 1];
                    imageurl = imageurl.Remove(imageurl.Length - 3);


                    return imageurl;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "fail";
            }
        }


    }

}
