using System;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;

namespace TravelGuide.Runners;

//სიის გვერდის Selenium-ით გავლა: ღირსშესანიშნაობების ბმულების შეგროვება infinite-scroll-იანი გვერდიდან.
//გვერდების გაანალიზება ბრაუზერს აღარ საჭიროებს — ის PlaceAnalyser-ში, HTTP-ით სრულდება.
// ReSharper disable once ConvertToPrimaryConstructor
public sealed class GeorgianTravelGuideRunner
{
    private readonly IWebDriver _driver;
    private readonly string _startPoint;
    private readonly HarvestedUrlPersister _urlPersister;

    public GeorgianTravelGuideRunner(IWebDriver driver, string startPoint, HarvestedUrlPersister urlPersister)
    {
        _driver = driver;
        _startPoint = startPoint;
        _urlPersister = urlPersister;
    }

    public bool Run()
    {
        _driver.Navigate().GoToUrl(_startPoint);
        var pageTitle = _driver.Title;
        Console.WriteLine("Page Title: " + pageTitle);

        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(60);
        _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(600);
        _driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(600);

        if (!FindElementAndClick("რა ვნახოთ", "a", true))
        {
            return false;
        }

        if (!FindElementAndClick("ყველას ნახვა "))
        {
            return false;
        }

        FindElementAndWaitUntilDisappear("იტვირთება...", "span");

        if (!FindElementByClassNameAndClick("icon-arrow-down"))
        {
            return false;
        }

        if (!FindElementAndClick("მანძილი", "div"))
        {
            return false;
        }

        FindElementAndWaitUntilDisappear("იტვირთება...", "span");

        if (!FindElementByClassNameAndClick("remove"))
        {
            return false;
        }

        FindElementAndWaitUntilDisappear("იტვირთება...", "span");

        //სიის გვერდიდან ყველა ღირსშესანიშნაობის ბმულის შეგროვება და ბაზაში შენახვა
        List<string> urlList = ScrollAndCollectUrls();
        _urlPersister.PersistNewUrls(urlList);

        Console.WriteLine("Success");
        return true;
    }

    private List<string> ScrollAndCollectUrls()
    {
        var lastCount = 0;
        var sameCount = 0;
        const int maxSameCount = 100;

        List<string> urlList = FindAllLinksByClassName();

        //ციკლი ჩერდება, როცა ბმულების რაოდენობა ზედიზედ maxSameCount იტერაციის განმავლობაში აღარ გაიზრდება
        while (sameCount < maxSameCount)
        {
            if (lastCount == urlList.Count)
            {
                sameCount++;
                Console.WriteLine($"Same count: {sameCount}");
            }
            else
            {
                sameCount = 0;

                //ახლად ჩატვირთული ბმულები მაშინვე ინახება, რომ სქროლვის შეწყვეტისას შეგროვებული არ დაიკარგოს
                _urlPersister.PersistNewUrls(urlList);
            }

            lastCount = urlList.Count;
            Console.WriteLine($"lastCount: {lastCount}");

            for (var i = 0; i < 10; i++)
            {
                // Send Arrow Down to the body of the page
                _driver.FindElement(By.TagName("body")).SendKeys(Keys.ArrowDown);
            }

            //// Send Page Down to the body of the page
            //_driver.FindElement(By.TagName("body")).SendKeys(Keys.PageDown);

            //var js = (IJavaScriptExecutor)_driver;
            //js.ExecuteScript("window.scrollBy(0, window.innerHeight);");

            FindElementAndWaitUntilDisappear("სანახავი ადგილი", "span");

            urlList = FindAllLinksByClassName();
        }

        return urlList;
    }

    private List<string> FindAllLinksByClassName()
    {
        //IReadOnlyCollection<IWebElement> links = _driver.FindElements(By.XPath("//article[@class='g-card']//a[@class='picture']"));
        IReadOnlyCollection<IWebElement> links = _driver.FindElements(By.CssSelector("article.g-card a.picture"));

        var result = new List<string>();
        foreach (var link in links)
        {
            var href = link.GetAttribute("href");
            if (!string.IsNullOrEmpty(href))
            {
                result.Add(href);
            }
        }

        return result;
    }

    private bool FindElementByClassNameAndClick(string className)
    {
        try
        {
            WaitForPageLoad();

            var by = By.ClassName(className);
            var element = _driver.FindElement(by);

            WaitForElementToBeClickable(by);

            if (element is { Displayed: true, Enabled: true })
            {
                Console.WriteLine($"Clicking on element with class name: {className}");
                element.Click();
                return true;
            }

            Console.WriteLine($"Element with class name '{className}' is not interactable.");
            return false;
        }
        catch (NoSuchElementException)
        {
            Console.WriteLine($"Element with class name '{className}' not found.");
            return false;
        }
    }

    private void WaitForPageLoad()
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(driver =>
            Equals(((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState"), "complete"));
    }

    private void WaitForElementToBeClickable(By by)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(ExpectedConditions.ElementToBeClickable(by));
    }

    //private void WaitForElementToBeVisible(By by)
    //{
    //    var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
    //    wait.Until(ExpectedConditions.ElementIsVisible(by));
    //}

    //private void WaitForElementToBePresent(By by)
    //{
    //    var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
    //    wait.Until(ExpectedConditions.ElementExists(by));
    //}

    private void WaitForElementToNotDisplayed(By by)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(60));
        wait.Until(driver => !driver.FindElements(by).Any(e => e.Displayed));
    }

    //private void WaitForJQueryToFinish()
    //{
    //    var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
    //    wait.Until(driver =>
    //        (bool)((IJavaScriptExecutor)driver).ExecuteScript(
    //            "return window.jQuery != undefined && jQuery.active == 0"));
    //}

    private void FindElementAndWaitUntilDisappear(string captionToFind, string elementName = "a")
    {
        WaitForPageLoad();
        var by = By.XPath($"//{elementName}[text()='{captionToFind}']");
        WaitForElementToNotDisplayed(by);
    }

    private bool FindElementAndClick(string captionToFind, string elementName = "a", bool useNavigate = false)
    {
        WaitForPageLoad();

        var by = By.XPath($"//{elementName}[text()='{captionToFind}']");
        var element = _driver.FindElement(by);

        if (useNavigate)
        {
            var href = element.GetAttribute("href");
            if (string.IsNullOrEmpty(href))
            {
                Console.WriteLine($"The '{captionToFind}' link does not have a valid href attribute.");
                return false;
            }

            Console.WriteLine($"Navigating to: {href}");
            _driver.Navigate().GoToUrl(href);
            return true;
        }

        WaitForElementToBeClickable(by);

        Console.WriteLine($"Click on element: {element.Text}");
        if (element is { Displayed: true, Enabled: true })
        {
            element.Click();
        }
        else
        {
            Console.WriteLine("Element is not interactable.");
            return false;
        }

        return true;
    }
}
