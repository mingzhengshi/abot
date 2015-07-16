
using Abot.Crawler;
using Abot.Poco;
using Abot.Database;
using HtmlAgilityPack;
using log4net;

using System;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Data;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.ComponentModel;


namespace Abot.Demo
{
    class Program
    {  
        static void Main(string[] args)
        {
            log4net.Config.XmlConfigurator.Configure();
            PrintDisclaimer();
          
            ILog _logger = LogManager.GetLogger("AbotLogger");
            _logger.Info("//--------------------------------------------------------------------");
            _logger.Info("// crawler start");
            _logger.Info("//--------------------------------------------------------------------");
            //string bd = AppDomain.CurrentDomain.BaseDirectory;

            //Uri uriToCrawl = GetSiteToCrawl(args);
            //string rootUri = "http://www.monash.edu";
            string rootUri = DemoParameters.rootUri;
            Uri uriToCrawl = new Uri(rootUri);
            IWebCrawler crawler;

            //Uncomment only one of the following to see that instance in action
            //crawler = GetDefaultWebCrawler();
            //crawler = GetManuallyConfiguredWebCrawler();
            crawler = GetCustomBehaviorWebCrawler();

            //crawler.LoadCrawledUrls();

            //Subscribe to any of these asynchronous events, there are also sychronous versions of each.
            //This is where you process data about specific events of the crawl
            crawler.PageCrawlStartingAsync += crawler_ProcessPageCrawlStarting;
            crawler.PageCrawlCompletedAsync += crawler_ProcessPageCrawlCompleted;
            crawler.PageCrawlDisallowedAsync += crawler_PageCrawlDisallowed;
            crawler.PageLinksCrawlDisallowedAsync += crawler_PageLinksCrawlDisallowed;

            //Start the crawl
            //This is a synchronous call
            CrawlResult result = crawler.Crawl(uriToCrawl);

            //Now go view the log.txt file that is in the same directory as this executable. It has
            //all the statements that you were trying to read in the console window :).
            //Not enough data being logged? Change the app.config file's log4net log level from "INFO" TO "DEBUG"
            _logger.Info("//--------------------------------------------------------------------");
            _logger.Info("// crawler end");
            _logger.Info("//--------------------------------------------------------------------");

            PrintDisclaimer();
        }

        private static IWebCrawler GetDefaultWebCrawler()
        {
            return new PoliteWebCrawler();
        }

        private static IWebCrawler GetManuallyConfiguredWebCrawler()
        {
            //Create a config object manually
            CrawlConfiguration config = new CrawlConfiguration();
            config.CrawlTimeoutSeconds = 0;
            config.DownloadableContentTypes = "text/html, text/plain";
            config.IsExternalPageCrawlingEnabled = true;
            config.IsExternalPageLinksCrawlingEnabled = true;
            config.IsRespectRobotsDotTextEnabled = false;
            config.IsUriRecrawlingEnabled = false;
            config.MaxConcurrentThreads = 10;
            config.MaxPagesToCrawl = 100000;
            //config.MaxPagesToCrawlPerDomain = 10;
            //config.MaxPagesToCrawlPerDomain = 0;
            config.MinCrawlDelayPerDomainMilliSeconds = 2000;

            //Add you own values without modifying Abot's source code.
            //These are accessible in CrawlContext.CrawlConfuration.ConfigurationException object throughout the crawl
            //config.ConfigurationExtensions.Add("Somekey1", "SomeValue1");
            //config.ConfigurationExtensions.Add("Somekey2", "SomeValue2");

            //Initialize the crawler with custom configuration created above.
            //This override the app.config file values
            return new PoliteWebCrawler(config, null, null, null, null, null, null, null, null);
        }

        private static IWebCrawler GetCustomBehaviorWebCrawler()
        {
            IWebCrawler crawler = GetManuallyConfiguredWebCrawler();

            //Register a lambda expression that will make Abot not crawl any url that has the word "ghost" in it.
            //For example http://a.com/ghost, would not get crawled if the link were found during the crawl.
            //If you set the log4net log level to "DEBUG" you will see a log message when any page is not allowed to be crawled.
            //NOTE: This is lambda is run after the regular ICrawlDecsionMaker.ShouldCrawlPage method is run.
            crawler.ShouldCrawlPage((pageToCrawl, crawlContext) =>
            {
                if (pageToCrawl.Uri.AbsoluteUri.Contains(DemoParameters.shouldCrawlPageParameter1) ||
                    pageToCrawl.Uri.AbsoluteUri.Contains(DemoParameters.shouldCrawlPageParameter2))
                    return new CrawlDecision { Allow = true };

                return new CrawlDecision { Allow = false, Reason = "Do not crawl pages that are not listing or sold pages" };
            });

            //Register a lambda expression that will tell Abot to not crawl links on any page that is not internal to the root uri.
            //NOTE: This lambda is run after the regular ICrawlDecsionMaker.ShouldCrawlPageLinks method is run
            crawler.ShouldCrawlPageLinks((crawledPage, crawlContext) =>
            {
                if (crawledPage.Uri.AbsoluteUri.Contains(DemoParameters.shouldCrawlPageLinkParameter1))
                    return new CrawlDecision { Allow = true };

                return new CrawlDecision { Allow = false, Reason = "Do not crawl page link in the sold page" };
            });

            return crawler;
        }

        private static IWebCrawler GetCustomBehaviorUsingLambdaWebCrawler()
        {
            IWebCrawler crawler = GetDefaultWebCrawler();

            //Register a lambda expression that will make Abot not crawl any url that has the word "ghost" in it.
            //For example http://a.com/ghost, would not get crawled if the link were found during the crawl.
            //If you set the log4net log level to "DEBUG" you will see a log message when any page is not allowed to be crawled.
            //NOTE: This is lambda is run after the regular ICrawlDecsionMaker.ShouldCrawlPage method is run.
            crawler.ShouldCrawlPage((pageToCrawl, crawlContext) =>
            {
                if (pageToCrawl.Uri.AbsoluteUri.Contains("ghost"))
                    return new CrawlDecision { Allow = false, Reason = "Scared of ghosts" };

                return new CrawlDecision { Allow = true };
            });

            //Register a lambda expression that will tell Abot to not download the page content for any page after 5th.
            //Abot will still make the http request but will not read the raw content from the stream
            //NOTE: This lambda is run after the regular ICrawlDecsionMaker.ShouldDownloadPageContent method is run
            crawler.ShouldDownloadPageContent((crawledPage, crawlContext) =>
            {
                if (crawlContext.CrawledCount >= 5)
                    return new CrawlDecision { Allow = false, Reason = "We already downloaded the raw page content for 5 pages" };

                return new CrawlDecision { Allow = true };
            });

            //Register a lambda expression that will tell Abot to not crawl links on any page that is not internal to the root uri.
            //NOTE: This lambda is run after the regular ICrawlDecsionMaker.ShouldCrawlPageLinks method is run
            crawler.ShouldCrawlPageLinks((crawledPage, crawlContext) =>
            {
                if (!crawledPage.IsInternal)
                    return new CrawlDecision { Allow = false, Reason = "We dont crawl links of external pages" };

                return new CrawlDecision { Allow = true };
            });

            return crawler;
        }

        private static Uri GetSiteToCrawl(string[] args)
        {
            string userInput = "";
            if (args.Length < 1)
            {
                System.Console.WriteLine("Please enter ABSOLUTE url to crawl:");
                userInput = System.Console.ReadLine();
            }
            else
            {
                userInput = args[0];
            }

            if (string.IsNullOrWhiteSpace(userInput))
                throw new ApplicationException("Site url to crawl is as a required parameter");

            return new Uri(userInput);
        }

        private static void PrintDisclaimer()
        {
            PrintAttentionText("The demo is configured to only crawl a total of 10 pages and will wait 1 second in between http requests. This is to avoid getting you blocked by your isp or the sites you are trying to crawl. You can change these values in the app.config or Abot.Console.exe.config file.");
        }

        private static void PrintAttentionText(string text)
        {
            ConsoleColor originalColor = System.Console.ForegroundColor;
            System.Console.ForegroundColor = ConsoleColor.Yellow;
            System.Console.WriteLine(text);
            System.Console.ForegroundColor = originalColor;
        }

        static void crawler_ProcessPageCrawlStarting(object sender, PageCrawlStartingArgs e)
        {
            //Process data

        }

        static void crawler_ProcessPageCrawlCompleted(object sender, PageCrawlCompletedArgs e)
        {
            saveWebPage(e);

            if (e.CrawledPage.Uri.AbsoluteUri.Contains(DemoParameters.shouldSaveToPropertyTableParameter))
                saveProperty(e);
        }

        static void saveWebPage(PageCrawlCompletedArgs e)
        {
            // save to WebPage table

            //Process data
            var webpageContext = new WebPageDataContext(DemoParameters.connectionString);
            //IEnumerable<WebPage> wp = dbContext.WebPages.OrderBy(c => c.pageUrl);

            WebPage page = new WebPage
            {
                pageUrl = e.CrawledPage.Uri.ToString(),
                parentUrl = e.CrawledPage.ParentUri.ToString(),
                requestStartTime = e.CrawledPage.RequestStarted.ToString(),
                requestEndTime = e.CrawledPage.RequestCompleted.ToString(),
                downloadStartTime = e.CrawledPage.DownloadContentStarted.ToString(),
                downloadEndTime = e.CrawledPage.DownloadContentCompleted.ToString(),
                //pageHtml = e.CrawledPage.Content.Text
                pageHtml = ""
            };

            webpageContext.WebPages.InsertOnSubmit(page);

            try
            {
                webpageContext.SubmitChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                // Make some adjustments. 
                // ... 
                // Try again.
                webpageContext.SubmitChanges();
            }
        }

        static void saveProperty(PageCrawlCompletedArgs e)
        {
            // save to Property table
            var propertyContext = new PropertyDataContext(DemoParameters.connectionString);

            HtmlNode addressNode = e.CrawledPage.HtmlDocument.DocumentNode.SelectSingleNode("//span[@class='js-address']");
            string addr = "";
            if (addressNode != null) addr = addressNode.InnerText.Trim();

            HtmlNode priceNode = e.CrawledPage.HtmlDocument.DocumentNode.SelectSingleNode("//dd[@class='price']");
            string pric = "";
            if (priceNode != null) pric = priceNode.InnerText.Trim();

            HtmlNode propertytypeNode = e.CrawledPage.HtmlDocument.DocumentNode.SelectSingleNode("//dd[@class='propertytype']");
            string ptype = "";
            if (propertytypeNode != null) ptype = propertytypeNode.InnerText.Trim();

            HtmlNode saletypeNode = e.CrawledPage.HtmlDocument.DocumentNode.SelectSingleNode("//dd[@class='saleType']");
            string stype = "";
            if (saletypeNode != null) stype = saletypeNode.InnerText.Trim();

            HtmlNode saledateNode = e.CrawledPage.HtmlDocument.DocumentNode.SelectSingleNode("//dd[@class='saleDate']");
            string sdate = "";
            if (saledateNode != null) sdate = saledateNode.InnerText.Trim();

            HtmlNode landsizeNode = e.CrawledPage.HtmlDocument.DocumentNode.SelectSingleNode("//dd[@class='land']");
            string land = "";
            if (landsizeNode != null) land = landsizeNode.InnerText.Trim();

            HtmlNode featureNode = e.CrawledPage.HtmlDocument.DocumentNode.SelectSingleNode("//p[@class='features']");
            string feature = "";
            if (featureNode != null) feature = featureNode.InnerText.Trim();

            HtmlNode agentNode = e.CrawledPage.HtmlDocument.DocumentNode.SelectSingleNode("//ul[@class='cB-agentList']");
            string agentInfo = "";
            if (agentNode != null) agentInfo = agentNode.InnerText.Trim();

            HtmlNode schoolNode = e.CrawledPage.HtmlDocument.DocumentNode.SelectSingleNode("//div[@class='schoolData']");
            string school = "";
            if (schoolNode != null) school = schoolNode.InnerText.Trim();

            HtmlNode descriptionNode = e.CrawledPage.HtmlDocument.DocumentNode.SelectSingleNode("//div[@class='cT-productDescription']");
            string desc = "";
            if (descriptionNode != null) desc = descriptionNode.InnerText.Trim();

            Property p = new Property
            {
                pageUrl = e.CrawledPage.Uri.ToString(),
                address = addr,
                price = pric,
                propertyType = ptype,
                saleType = stype,
                saleDate = sdate,
                suburb = "",
                landSize = land,
                propertyFeature = feature,
                agents = agentInfo,
                schoolData = school,
                propertyDescription = desc
            };

            propertyContext.Properties.InsertOnSubmit(p);

            try
            {
                propertyContext.SubmitChanges();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                // Make some adjustments. 
                // ... 
                // Try again.
                propertyContext.SubmitChanges();
            }
        }


        static void crawler_PageLinksCrawlDisallowed(object sender, PageLinksCrawlDisallowedArgs e)
        {
            //Process data
        }

        static void crawler_PageCrawlDisallowed(object sender, PageCrawlDisallowedArgs e)
        {
            //Process data
        }
    }
}
