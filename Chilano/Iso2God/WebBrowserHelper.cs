using System;
using System.Threading;
using System.Windows.Forms;

namespace Chilano.Iso2God
{
    /// <summary>
    /// Helper class to load web pages and extract content using WebBrowser control
    /// </summary>
    internal class WebBrowserHelper
    {
        private WebBrowser browser;
        private string extractedTitle;
        private bool isCompleted;
        private bool hasError;
        
        /// <summary>
        /// Load xboxunity.net and extract game title by TitleID
        /// </summary>
        public string ExtractTitleFromXboxUnity(string titleId)
        {
            extractedTitle = null;
            isCompleted = false;
            hasError = false;
            
            Console.WriteLine("+ Starting WebBrowser to load xboxunity.net...");
            
            // Create a thread with STA for WebBrowser
            Thread browserThread = new Thread(() =>
            {
                try
                {
                    browser = new WebBrowser();
                    browser.ScriptErrorsSuppressed = true;
                    browser.DocumentCompleted += Browser_DocumentCompleted;
                    
                    string url = "https://xboxunity.net/#search=" + titleId;
                    Console.WriteLine("+ Loading URL: " + url);
                    
                    browser.Navigate(url);
                    
                    // Keep thread alive until completed or timeout
                    DateTime startTime = DateTime.Now;
                    while (!isCompleted && !hasError)
                    {
                        Application.DoEvents();
                        Thread.Sleep(100);
                        
                        // Timeout after 30 seconds
                        if ((DateTime.Now - startTime).TotalSeconds > 30)
                        {
                            Console.WriteLine("- WebBrowser timeout after 30 seconds");
                            hasError = true;
                            break;
                        }
                    }
                    
                    if (browser != null)
                    {
                        browser.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("- WebBrowser error: " + ex.Message);
                    hasError = true;
                }
            });
            
            browserThread.SetApartmentState(ApartmentState.STA);
            browserThread.Start();
            browserThread.Join(); // Wait for thread to complete
            
            return extractedTitle;
        }
        
        private void Browser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            try
            {
                WebBrowser wb = (WebBrowser)sender;
                
                // Check if this is the final document (not frames)
                if (wb.ReadyState != WebBrowserReadyState.Complete)
                    return;
                
                Console.WriteLine("+ Page loaded, waiting for JavaScript to execute...");
                
                // Wait a bit for JavaScript to load the content
                Thread.Sleep(3000);
                
                // Try to extract title from the loaded page
                extractedTitle = ExtractTitleFromDocument(wb.Document);
                
                if (!string.IsNullOrEmpty(extractedTitle))
                {
                    Console.WriteLine("+ Successfully extracted title: " + extractedTitle);
                }
                else
                {
                    Console.WriteLine("- Could not find title in loaded page");
                    
                    // Debug: show part of the page content
                    if (wb.Document != null && wb.Document.Body != null)
                    {
                        string bodyText = wb.Document.Body.InnerText;
                        if (bodyText.Length > 0)
                        {
                            Console.WriteLine("  Page content preview: " + bodyText.Substring(0, Math.Min(200, bodyText.Length)));
                        }
                    }
                }
                
                isCompleted = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("- Error extracting content: " + ex.Message);
                hasError = true;
                isCompleted = true;
            }
        }
        
        private string ExtractTitleFromDocument(HtmlDocument doc)
        {
            if (doc == null || doc.Body == null)
                return null;
            
            // Method 1: Look for elements with class "title" or id "title"
            HtmlElementCollection elements = doc.GetElementsByTagName("*");
            foreach (HtmlElement element in elements)
            {
                string className = element.GetAttribute("className");
                string id = element.GetAttribute("id");
                
                if ((className != null && className.ToLower().Contains("title")) ||
                    (id != null && id.ToLower().Contains("title")))
                {
                    string text = element.InnerText;
                    if (!string.IsNullOrEmpty(text) && text.Trim().Length > 0)
                    {
                        text = text.Trim();
                        
                        // Validate it's not just noise
                        if (text.Length > 2 && !text.Contains("XboxUnity") && !text.Contains("search"))
                        {
                            return text;
                        }
                    }
                }
            }
            
            // Method 2: Look for h1, h2, h3 tags
            string[] headerTags = new string[] { "h1", "h2", "h3" };
            foreach (string tag in headerTags)
            {
                HtmlElementCollection headers = doc.GetElementsByTagName(tag);
                foreach (HtmlElement header in headers)
                {
                    string text = header.InnerText;
                    if (!string.IsNullOrEmpty(text))
                    {
                        text = text.Trim();
                        if (text.Length > 2 && !text.Contains("XboxUnity"))
                        {
                            return text;
                        }
                    }
                }
            }
            
            // Method 3: Execute JavaScript to get the title
            try
            {
                HtmlElement head = doc.GetElementsByTagName("head")[0];
                HtmlElement scriptEl = doc.CreateElement("script");
                
                // Inject JavaScript to find title
                string script = @"
                    var title = '';
                    var elements = document.querySelectorAll('[class*=""title""], [id*=""title""]');
                    if (elements.length > 0) {
                        title = elements[0].innerText;
                    }
                    title;
                ";
                
                scriptEl.SetAttribute("text", script);
                head.AppendChild(scriptEl);
                
                object result = doc.InvokeScript("eval", new object[] { script });
                if (result != null && result.ToString().Length > 0)
                {
                    return result.ToString().Trim();
                }
            }
            catch
            {
                // JavaScript execution failed, ignore
            }
            
            return null;
        }
    }
}
