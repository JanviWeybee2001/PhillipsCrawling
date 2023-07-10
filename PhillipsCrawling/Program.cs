using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.DevTools.V112.DOM;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;

namespace PhillipsCrawling
{
    public class Auction : DateTimes
    {
        public int Id { get; set; }
        public string AuctionTitle { get; set; }
        public string Catalogues { get; set; }
        public string ImageUrl { get; set; }
    }

    public class DateTimes
    {
        public string AuctionCity { get; set; } = string.Empty;
        public int StartDate { get; set; } = 0;
        public string StartMonth { get; set; } = string.Empty;
        public int StartYear { get; set; } = 0;
        public int EndDate { get; set; } = 0;
        public string EndMonth { get; set; } = string.Empty;
        public int EndYear { get; set; } = 0;
    }

    public class Watch
    {
        public string Id { get; set; }
        public int AuctionId { get; set; }
        public string ImageLinks { get; set; } = string.Empty;
        public string WatchUrl { get; set; } = string.Empty;       
        public string WatchTitle { get; set; } = string.Empty;
        public string WatchPrice { get; set; } = string.Empty;
        public string WatchManufacturer { get; set; } = string.Empty;
        public string Reference { get; set; } = string.Empty;
        public string Dimensions { get; set; } = string.Empty;
        public string CatalogueNotes { get; set; } = string.Empty;
    }

    public class Program
    {
        public static string Prefix = "https://phillips.com";

        public static string connectionString = "data source=JANVI-DESAI\\SQLEXPRESS; database=Phillips; Integrated Security=SSPI";

        public void InsertUpdateAuction(Auction model)
        {
            // Create and configure the SqlCommand object for the stored procedure
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand("InsertOrUpdateAuction", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add parameters and their values
                    command.Parameters.AddWithValue("@Id", model.Id); 
                    command.Parameters.AddWithValue("@Title", model.AuctionTitle); 
                    command.Parameters.AddWithValue("@Catalogues",model.Catalogues); 
                    command.Parameters.AddWithValue("@ImageUrl", model.ImageUrl); 
                    command.Parameters.AddWithValue("@AuctionCity", model.AuctionCity); 
                    command.Parameters.AddWithValue("@StartDate", model.StartDate); 
                    command.Parameters.AddWithValue("@StartMonth",model.StartMonth);
                    command.Parameters.AddWithValue("@StartYear", model.StartYear); 
                    command.Parameters.AddWithValue("@EndDate", model.EndDate); 
                    command.Parameters.AddWithValue("@EndMonth", model.EndMonth); 
                    command.Parameters.AddWithValue("@EndYear", model.EndYear); 

                    // Open the database connection
                    connection.Open();

                    // Execute the stored procedure
                    int rowsAffected = command.ExecuteNonQuery();

                    // Close the database connection
                    connection.Close();
                }
            }
        }
        public void InsertUpdateWatch(Watch watch)
        {
            // Create and configure the SqlCommand object for the stored procedure
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                using (SqlCommand command = new SqlCommand("InsertOrUpdateWatch", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add parameters for the stored procedure
                    command.Parameters.AddWithValue("@Id", watch.Id.Trim());
                    command.Parameters.AddWithValue("@AuctionId", watch.AuctionId);
                    command.Parameters.AddWithValue("@ImageLinks", watch.ImageLinks);
                    command.Parameters.AddWithValue("@WatchUrl", watch.WatchUrl);
                    command.Parameters.AddWithValue("@WatchTitle", watch.WatchTitle);
                    command.Parameters.AddWithValue("@WatchPrice", watch.WatchPrice);
                    command.Parameters.AddWithValue("@WatchManufacturer", watch.WatchManufacturer);
                    command.Parameters.AddWithValue("@Reference", watch.Reference);
                    command.Parameters.AddWithValue("@Dimensions", watch.Dimensions);
                    command.Parameters.AddWithValue("@CatalogueNotes", watch.CatalogueNotes);

                    connection.Open();
                    // Execute the command
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }
        }

        public HtmlDocument StringToHtml(string html)
        {
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            return htmlDoc;
        }

        static void ScrollToBottom(IWebDriver driver)
        {
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            long scrollHeight = (long)js.ExecuteScript("return document.documentElement.scrollHeight");

            while (true)
            {
                js.ExecuteScript("window.scrollTo(0, document.documentElement.scrollHeight);");
                Thread.Sleep(3000); // Adjust the delay as needed

                long newScrollHeight = (long)js.ExecuteScript("return document.documentElement.scrollHeight");
                if (newScrollHeight == scrollHeight)
                {
                    break;
                }
                scrollHeight = newScrollHeight;
            }
        }

        public DateTimes GetDateTime(string html)
        {
            DateTimes model = new DateTimes();
            string regexString = @"^\b((?:[a-zA-Z]+\s*)+?)?(\d{1,2})(?:(\s+[A-Za-z]+)?\s*)?(?:(\s+\d{4})\s*)?(?:\s*-)?(?:\s*&)?(?:\s*)?(\s*\d{1,2})?(?:(\s+[A-Za-z]+))?(?:(\s+\d{4})\s*)?\s+?(?:.*)$";
            MatchCollection matches = Regex.Matches(html.Trim(), regexString);
            foreach (Match match in matches)
            {
                model = new DateTimes()
                {
                    AuctionCity = match.Groups[1].Value.Replace("Auction", "").Trim(),
                    StartDate = string.IsNullOrWhiteSpace(match.Groups[2].Value) ? 0 : Convert.ToInt16(match.Groups[2].Value.Trim()),
                    StartMonth = match.Groups[3].Value.Trim(),
                    StartYear = string.IsNullOrWhiteSpace(match.Groups[4].Value) ? 0 : Convert.ToInt16(match.Groups[4].Value.Trim()),
                    EndDate = string.IsNullOrWhiteSpace(match.Groups[5].Value) ? 0 : Convert.ToInt16(match.Groups[5].Value.Trim()),
                    EndMonth = match.Groups[6].Value.Trim(),
                    EndYear = string.IsNullOrWhiteSpace(match.Groups[7].Value) ? 0 : Convert.ToInt16(match.Groups[7].Value.Trim()),
                };
            }
            return model;
        }

        private void AuctionDetail(string html, string xpath)
        {
            Auction auctionModel = new Auction();
            var htmlDoc = StringToHtml(html);
            var matchingDivs = htmlDoc.DocumentNode.SelectNodes(xpath).ToList();
            var contentXPath = @"//div[contains(@class,'content-body')]";
            var imageXPath = @"//div[contains(@class,'image')]/a";
            var auction = 1;
            Console.WriteLine(matchingDivs.Count());
            foreach (var div in matchingDivs)
            {
                var innerDiv = StringToHtml(div.InnerHtml);
                //Console.WriteLine(div.InnerText);
                Console.WriteLine("\nAuction="+auction);
                var contentDiv = innerDiv.DocumentNode.SelectSingleNode(contentXPath);
                var titleDiv = contentDiv.ChildNodes.FirstOrDefault(x => x.Name == "h2");
                var title = titleDiv!=null ? titleDiv.InnerText.Trim() : " ";
                var catalogueLink = titleDiv != null ? Prefix + titleDiv.ChildNodes.First(x => x.Name == "a").Attributes.First(y => y.Name == "href").Value : "";
                var date = contentDiv.ChildNodes.First(x => x.Name == "p");
                var imageDiv = innerDiv.DocumentNode.SelectSingleNode(imageXPath);
                var imageUrl = imageDiv!=null? imageDiv.ChildNodes.First(x => x.Name == "div").ChildNodes.First
                    (y => y.Name == "img").Attributes.First(y => y.Name == "src").Value : "";
                //Console.WriteLine(imageUrl);
                //Console.WriteLine(title);
                //Console.WriteLine(auctionLink);
                //Console.WriteLine(date.InnerText);
                var dates = GetDateTime(date.InnerText);
                auctionModel = new Auction()
                {
                    Id= auction,
                    AuctionTitle = title,
                    Catalogues = catalogueLink,
                    AuctionCity = dates.AuctionCity,
                    ImageUrl = imageUrl,
                    StartDate = dates.StartDate,
                    StartMonth = dates.StartMonth,
                    StartYear = dates.StartYear,
                    EndDate = dates.EndDate,
                    EndMonth = dates.EndMonth,
                    EndYear = dates.EndYear
                };
                InsertUpdateAuction(auctionModel);
                WatchDetail(auctionModel.Catalogues,auction);
                auction++;
            }
        }

        private void WatchDetail(string url,int AuctionId)
        {
            if(!string.IsNullOrWhiteSpace(url))
            {
                using (IWebDriver driver = new ChromeDriver())
                {
                    driver.Url = url;
                    // Scroll to the bottom of the page to trigger lazy loading
                    ScrollToBottom(driver);

                    // Wait for the page to load the additional content
                    Thread.Sleep(2000); // Adjust the delay as needed
                    string watchXPath = @"//li[contains(@class, 'lot single-cell')]";
                    var pagesource = driver.PageSource;
                    AuctionDetailPage(pagesource, watchXPath, url.Split("/").Last(), AuctionId);
                    driver.Quit();
                }
            }
        }

        public void WatchDetailPageSource(Watch model)
        {
            using (IWebDriver driver = new ChromeDriver())
            {
                driver.Url = model.WatchUrl;              

                // Wait for the page to load the additional content
                Thread.Sleep(2000); // Adjust the delay as needed
                var pagesource = driver.PageSource;
                string watchDetailXPath = @"//div[contains(@class,'row lot-page__row')]";
                WatchDetailPage(pagesource, watchDetailXPath,model);
                driver.Quit();
            }
        }

        private void WatchDetailPage(string html, string xPath,Watch model)
        {
            var htmlDoc = StringToHtml(html);
            var watchImageXPath = @"//div[contains(@class,'thumbnail-slide')]";
            var matchingDivs = htmlDoc.DocumentNode.SelectNodes(watchImageXPath).ToList();
            //var WatchDetail = htmlDoc.DocumentNode.SelectSingleNode(xPath);
            var priceXPath = @"//p[contains(@class,'lot-page__lot__sold')]";
            var priceDiv = htmlDoc.DocumentNode.SelectSingleNode(priceXPath);
            model.WatchPrice = priceDiv!=null ? priceDiv.InnerText.Trim().Replace("<!-- -->",""): "";
            var catalogueXath = @"//div[contains(@class,'lot-essay')]/p";
            var catalogueDiv = htmlDoc.DocumentNode.SelectSingleNode(catalogueXath);
            model.CatalogueNotes = catalogueDiv != null ? catalogueDiv.InnerText.Trim() : string.Empty;
            var dimentionXath = @"//strong[contains(text(),'Dimensions')]";
            var dimentionDiv = htmlDoc.DocumentNode.SelectSingleNode(dimentionXath);
            model.Dimensions = dimentionDiv != null ? dimentionDiv.ParentNode.ChildNodes.First(x=>x.Name=="text").InnerText : string.Empty;
            string watchImages = string.Empty;
            foreach (var image in matchingDivs)
            {
                var innerDiv = StringToHtml(image.InnerHtml);
                var imageDiv = innerDiv.DocumentNode.ChildNodes.FirstOrDefault(x => x.Name == "div" && x.Attributes.Any(y => y.Name == "class" && y.Value.Contains("phillips-image")));
                watchImages += imageDiv == null ? "" : "," + imageDiv.ChildNodes.FirstOrDefault(x => x.Name == "img")!.Attributes.First(x=>x.Name=="src").Value; 
            }
            model.ImageLinks = watchImages;
            InsertUpdateWatch(model);
        }

        private void AuctionDetailPage(string html, string xpath,string AuctionName,int AuctionId)
        {
            Watch model = new Watch();
            var htmlDoc = StringToHtml(html);
            var matchingDivs = htmlDoc.DocumentNode.SelectNodes(xpath).ToList();
            var referenceXPath = @"//span[contains(text(), 'Ref')]";
            var lotXPath = @"//strong[contains(@class, 'phillips-lot__description__lot-number-wrapper__lot-number')]";
            var lotDescriptionXPath = @"//a[contains(@class, 'phillips-lot__description phillips-lot__description--is-watch phillips-lot__description--has-hammer')]";
            foreach (var div in matchingDivs)
            {
                var innerDiv = StringToHtml(div.InnerHtml);
                var referenceSpan = innerDiv.DocumentNode.SelectSingleNode(referenceXPath);
                var lotDiscriptionDiv = innerDiv.DocumentNode.SelectSingleNode(lotDescriptionXPath);
                if(referenceSpan!=null)  model.Reference=  referenceSpan.InnerText;
                if(lotDiscriptionDiv!=null)
                {
                    model.WatchManufacturer = lotDiscriptionDiv.ChildNodes.First(x=>x.Attributes.Any(y=>y.Name=="class" && y.Value.Contains("phillips-lot__description__artist"))).InnerText;
                    model.WatchUrl = lotDiscriptionDiv.Attributes.First(x => x.Name == "href").Value;
                    var lotNumber = innerDiv.DocumentNode.SelectSingleNode(lotXPath); 
                    if (lotNumber != null) model.Id = AuctionName + "-" + lotNumber.InnerText;
                    model.AuctionId = AuctionId;
                    model.WatchTitle = model.WatchManufacturer;
                    WatchDetailPageSource(model);
                }
            }
            if (matchingDivs.Count() == 11)
            {
                var x = 0;
            }
            Console.WriteLine(matchingDivs.Count());
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");
            Program program = new Program();
            var url = "https://www.phillips.com/auctions/past/filter/Departments=Watches/sort/newest";
            using (IWebDriver driver = new ChromeDriver())
            {
                driver.Url = url;
                // Scroll to the bottom of the page to trigger lazy loading
                ScrollToBottom(driver);

                // Wait for the page to load the additional content
                Thread.Sleep(2000); // Adjust the delay as needed
                string watchXPath = @"//li[contains(@class, 'has-image auction')]";
                var pagesource = driver.PageSource;
                program.AuctionDetail(pagesource, watchXPath);
                driver.Quit();
            }
            Console.WriteLine("Bye.!");
            Console.ReadLine();
        }
    }
}